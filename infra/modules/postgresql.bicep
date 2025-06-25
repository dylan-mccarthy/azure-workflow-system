// Bicep module: postgresql.bicep
// Creates PostgreSQL Flexible Server with security best practices

@description('Name of the PostgreSQL flexible server')
param serverName string

@description('Location for the PostgreSQL server')
param location string = resourceGroup().location

@description('SKU name for the PostgreSQL server')
param skuName string = 'Standard_B1ms'

@description('SKU tier for the PostgreSQL server')
param skuTier string = 'Burstable'

@description('Storage size for the server (GB)')
param storageSizeGB int = 32

@description('Storage tier for the server')
param storageTier string = 'P4'

@description('PostgreSQL version')
param postgresqlVersion string = '16'

@description('Administrator login name')
param administratorLogin string

@description('Administrator login password')
@secure()
param administratorLoginPassword string

@description('Backup retention period in days')
param backupRetentionDays int = 7

@description('Enable geo-redundant backup')
param geoRedundantBackup string = 'Disabled'

@description('Enable high availability')
param highAvailabilityMode string = 'Disabled'

@description('Virtual network subnet ID for private access')
param delegatedSubnetResourceId string = ''

@description('Private DNS zone resource ID')
param privateDnsZoneResourceId string = ''

@description('Enable public network access')
param publicNetworkAccess string = 'Disabled'

@description('User-assigned managed identity resource ID for data encryption')
param userAssignedIdentityId string = ''

@description('Key Vault URI for data encryption')
param dataEncryptionKeyUri string = ''

// PostgreSQL Flexible Server with security hardening
resource postgreSQLServer 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: serverName
  location: location
  identity: !empty(userAssignedIdentityId) ? {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  } : null
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    version: postgresqlVersion
    
    // Storage configuration
    storage: {
      storageSizeGB: storageSizeGB
      tier: storageTier
      autoGrow: 'Enabled'
      type: 'Premium_LRS'
    }
    
    // Backup configuration
    backup: {
      backupRetentionDays: backupRetentionDays
      geoRedundantBackup: geoRedundantBackup
    }
    
    // High availability configuration
    highAvailability: {
      mode: highAvailabilityMode
    }
    
    // Network configuration for private access
    network: {
      delegatedSubnetResourceId: !empty(delegatedSubnetResourceId) ? delegatedSubnetResourceId : null
      privateDnsZoneArmResourceId: !empty(privateDnsZoneResourceId) ? privateDnsZoneResourceId : null
      publicNetworkAccess: publicNetworkAccess
    }
    
    // Authentication configuration
    authConfig: {
      activeDirectoryAuth: 'Enabled'
      passwordAuth: 'Enabled'
    }
    
    // Data encryption configuration
    dataEncryption: !empty(dataEncryptionKeyUri) ? {
      type: 'AzureKeyVault'
      primaryKeyURI: dataEncryptionKeyUri
      primaryUserAssignedIdentityId: userAssignedIdentityId
    } : {
      type: 'SystemManaged'
    }
    
    // Maintenance window (Sunday 2 AM - 3 AM)
    maintenanceWindow: {
      customWindow: 'Enabled'
      dayOfWeek: 0
      startHour: 2
      startMinute: 0
    }
  }
}

// PostgreSQL Configuration for security hardening
resource postgreSQLConfig 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2024-08-01' = [
  for config in [
    { name: 'log_statement', value: 'all' }
    { name: 'log_min_duration_statement', value: '1000' }
    { name: 'log_connections', value: 'on' }
    { name: 'log_disconnections', value: 'on' }
    { name: 'log_checkpoints', value: 'on' }
    { name: 'log_lock_waits', value: 'on' }
    { name: 'log_line_prefix', value: '%t [%p]: [%l-1] user=%u,db=%d,app=%a,client=%h ' }
    { name: 'shared_preload_libraries', value: 'pg_stat_statements' }
    { name: 'azure.extensions', value: 'pg_stat_statements,pg_qs' }
  ]: {
    parent: postgreSQLServer
    name: config.name
    properties: {
      value: config.value
      source: 'user-override'
    }
  }
]

// Create database
resource postgreSQLDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgreSQLServer
  name: 'workflowdb'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// Firewall rules for Azure services (only if public access is enabled)
resource firewallRuleAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = if (publicNetworkAccess == 'Enabled') {
  parent: postgreSQLServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Diagnostic settings for monitoring and auditing
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${serverName}-diagnostics'
  scope: postgreSQLServer
  properties: {
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
    ]
    logAnalyticsDestinationType: 'Dedicated'
  }
}

// Outputs
output serverName string = postgreSQLServer.name
output serverId string = postgreSQLServer.id
output serverFqdn string = postgreSQLServer.properties.fullyQualifiedDomainName
output databaseName string = postgreSQLDatabase.name
output connectionString string = 'Server=${postgreSQLServer.properties.fullyQualifiedDomainName};Database=workflowdb;Port=5432;Ssl Mode=Require;'
