// main.bicep - Main deployment template for Azure Workflow System
// Orchestrates all Azure resources with security best practices and environment parity

// Target scope
targetScope = 'resourceGroup'

// Parameters
@description('Environment name (dev, prod)')
@allowed(['dev', 'prod'])
param environmentName string

@description('Location for all resources')
param location string = resourceGroup().location

@description('Unique suffix for resource names to ensure global uniqueness')
param resourceSuffix string = uniqueString(resourceGroup().id)

@description('PostgreSQL administrator login')
param postgresAdminLogin string

@description('PostgreSQL administrator password')
@secure()
param postgresAdminPassword string

@description('Container image for API service')
param apiContainerImage string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

@description('Container image for Bot service')
param botContainerImage string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

@description('Tags to apply to all resources')
param tags object = {
  Environment: environmentName
  Project: 'AzureWorkflowSystem'
  ManagedBy: 'Bicep'
  'azd-env-name': environmentName
}

// Variables
var resourceNamePrefix = 'awfs-${environmentName}-${resourceSuffix}'
var containerAppEnvironmentName = '${resourceNamePrefix}-containerenv'

// User-assigned managed identity for secure service-to-service authentication
resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${resourceNamePrefix}-identity'
  location: location
  tags: tags
}

// Key Vault for secrets management
module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVault-deployment'
  params: {
    keyVaultName: '${resourceNamePrefix}-kv'
    location: location
    tenantId: tenant().tenantId
    userAssignedIdentityPrincipalId: userAssignedIdentity.properties.principalId
    tags: tags
  }
}

// Storage Account for blob storage and file uploads
module blobStorage 'modules/blobStorage.bicep' = {
  name: 'blobStorage-deployment'
  params: {
    storageAccountName: replace('${resourceNamePrefix}storage', '-', '')
    location: location
    userAssignedIdentityPrincipalId: userAssignedIdentity.properties.principalId
    tags: tags
  }
}

// Application Insights for monitoring and logging
module appInsights 'modules/appInsights.bicep' = {
  name: 'appInsights-deployment'
  params: {
    appInsightsName: '${resourceNamePrefix}-appinsights'
    location: location
    tags: tags
  }
}

// PostgreSQL Flexible Server for database
module postgresql 'modules/postgresql.bicep' = {
  name: 'postgresql-deployment'
  params: {
    serverName: '${resourceNamePrefix}-postgres'
    location: location
    administratorLogin: postgresAdminLogin
    administratorLoginPassword: postgresAdminPassword
    userAssignedIdentityId: userAssignedIdentity.id
    dataEncryptionKeyUri: keyVault.outputs.dataEncryptionKeyUri
    storageSizeGB: environmentName == 'prod' ? 128 : 32
    skuName: environmentName == 'prod' ? 'Standard_D2s_v3' : 'Standard_B1ms'
    skuTier: environmentName == 'prod' ? 'GeneralPurpose' : 'Burstable'
    backupRetentionDays: environmentName == 'prod' ? 35 : 7
    geoRedundantBackup: environmentName == 'prod' ? 'Enabled' : 'Disabled'
    highAvailabilityMode: environmentName == 'prod' ? 'ZoneRedundant' : 'Disabled'
    publicNetworkAccess: 'Disabled'
  }
}

// API Management for API gateway
module apiManagement 'modules/apim.bicep' = {
  name: 'apiManagement-deployment'
  params: {
    apimName: '${resourceNamePrefix}-apim'
    location: location
    publisherEmail: 'admin@example.com'
    publisherName: 'Azure Workflow System'
    skuName: environmentName == 'prod' ? 'Basic' : 'Developer'
    skuCapacity: environmentName == 'prod' ? 2 : 1
    userAssignedIdentityId: userAssignedIdentity.id
    tags: tags
  }
}

// Get reference to Key Vault for secret creation
resource keyVaultRef 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: '${resourceNamePrefix}-kv'
  dependsOn: [
    keyVault
  ]
}

// Store secrets in Key Vault
resource postgresConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVaultRef
  name: 'postgres-connection-string'
  properties: {
    value: '${postgresql.outputs.connectionString}User Id=${postgresAdminLogin};Password=${postgresAdminPassword};Ssl Mode=Require;Trust Server Certificate=true;'
  }
}

resource appInsightsConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVaultRef
  name: 'appinsights-connection-string'
  properties: {
    value: appInsights.outputs.connectionString
  }
}

resource storageConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVaultRef
  name: 'storage-connection-string'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${blobStorage.outputs.storageAccountName};EndpointSuffix=${environment().suffixes.storage}'
  }
}

// API Container App
module apiContainerApp 'modules/containerApp.bicep' = {
  name: 'apiContainerApp-deployment'
  params: {
    containerAppName: '${resourceNamePrefix}-api'
    location: location
    containerAppEnvironmentName: containerAppEnvironmentName
    containerImage: apiContainerImage
    targetPort: 8080
    cpuCores: environmentName == 'prod' ? '1.0' : '0.5'
    memorySize: environmentName == 'prod' ? '2Gi' : '1Gi'
    minReplicas: environmentName == 'prod' ? 2 : 1
    maxReplicas: environmentName == 'prod' ? 10 : 3
    userAssignedIdentityId: userAssignedIdentity.id
    environmentVariables: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: environmentName == 'prod' ? 'Production' : 'Development'
      }
      {
        name: 'ConnectionStrings__DefaultConnection'
        secretRef: 'postgres-connection-string'
      }
      {
        name: 'ApplicationInsights__ConnectionString'
        secretRef: 'appinsights-connection-string'
      }
      {
        name: 'AzureStorage__ConnectionString'
        secretRef: 'storage-connection-string'
      }
      {
        name: 'KeyVault__VaultUri'
        value: keyVault.outputs.keyVaultUri
      }
    ]
    secrets: [
      {
        name: 'postgres-connection-string'
        keyVaultUrl: '${keyVault.outputs.keyVaultUri}secrets/postgres-connection-string'
        identity: userAssignedIdentity.id
      }
      {
        name: 'appinsights-connection-string'
        keyVaultUrl: '${keyVault.outputs.keyVaultUri}secrets/appinsights-connection-string'
        identity: userAssignedIdentity.id
      }
      {
        name: 'storage-connection-string'
        keyVaultUrl: '${keyVault.outputs.keyVaultUri}secrets/storage-connection-string'
        identity: userAssignedIdentity.id
      }
    ]
  }
  dependsOn: [
    postgresConnectionSecret
    appInsightsConnectionSecret
    storageConnectionSecret
  ]
}

// Bot Container App
module botContainerApp 'modules/containerApp.bicep' = {
  name: 'botContainerApp-deployment'
  params: {
    containerAppName: '${resourceNamePrefix}-bot'
    location: location
    containerAppEnvironmentName: containerAppEnvironmentName
    containerImage: botContainerImage
    targetPort: 3978
    cpuCores: environmentName == 'prod' ? '0.5' : '0.25'
    memorySize: environmentName == 'prod' ? '1Gi' : '0.5Gi'
    minReplicas: environmentName == 'prod' ? 1 : 1
    maxReplicas: environmentName == 'prod' ? 5 : 2
    userAssignedIdentityId: userAssignedIdentity.id
    environmentVariables: [
      {
        name: 'NODE_ENV'
        value: environmentName == 'prod' ? 'production' : 'development'
      }
      {
        name: 'API_BASE_URL'
        value: 'https://${apiContainerApp.outputs.containerAppFqdn}'
      }
      {
        name: 'ApplicationInsights__ConnectionString'
        secretRef: 'appinsights-connection-string'
      }
      {
        name: 'KeyVault__VaultUri'
        value: keyVault.outputs.keyVaultUri
      }
    ]
    secrets: [
      {
        name: 'appinsights-connection-string'
        keyVaultUrl: '${keyVault.outputs.keyVaultUri}secrets/appinsights-connection-string'
        identity: userAssignedIdentity.id
      }
    ]
  }
  dependsOn: [
    appInsightsConnectionSecret
  ]
}

// Outputs
output resourceGroupName string = resourceGroup().name
output environmentName string = environmentName
output location string = location

// Identity
output userAssignedIdentityId string = userAssignedIdentity.id
output userAssignedIdentityClientId string = userAssignedIdentity.properties.clientId

// Key Vault
output keyVaultName string = keyVault.outputs.keyVaultName
output keyVaultUri string = keyVault.outputs.keyVaultUri

// Storage
output storageAccountName string = blobStorage.outputs.storageAccountName
output storageAccountId string = blobStorage.outputs.storageAccountId

// Database
output postgresServerName string = postgresql.outputs.serverName
output postgresServerFqdn string = postgresql.outputs.serverFqdn
output postgresDatabaseName string = postgresql.outputs.databaseName

// Container Apps
output apiContainerAppName string = apiContainerApp.outputs.containerAppName
output apiContainerAppFqdn string = apiContainerApp.outputs.containerAppFqdn
output botContainerAppName string = botContainerApp.outputs.containerAppName
output botContainerAppFqdn string = botContainerApp.outputs.containerAppFqdn
output containerAppEnvironmentId string = apiContainerApp.outputs.containerAppEnvironmentId

// Application Insights
output appInsightsName string = appInsights.outputs.appInsightsName
output appInsightsInstrumentationKey string = appInsights.outputs.instrumentationKey
output appInsightsConnectionString string = appInsights.outputs.connectionString

// API Management
output apiManagementName string = apiManagement.outputs.apimName
output apiManagementGatewayUrl string = apiManagement.outputs.gatewayUrl
