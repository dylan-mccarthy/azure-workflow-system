// Bicep module: keyVault.bicep
@description('Name of the Key Vault')
param keyVaultName string

@description('Location for the Key Vault')
param location string = resourceGroup().location

@description('Tenant ID for the Key Vault')
param tenantId string = subscription().tenantId

@description('Principal ID of the user-assigned managed identity')
param userAssignedIdentityPrincipalId string

@description('Tags to apply to resources')
param tags object = {}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enablePurgeProtection: true
    enabledForDeployment: true
    enabledForDiskEncryption: true
    enabledForTemplateDeployment: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enableRbacAuthorization: false
    accessPolicies: [
      {
        tenantId: tenantId
        objectId: userAssignedIdentityPrincipalId
        permissions: {
          keys: ['get', 'list', 'create', 'decrypt', 'encrypt', 'wrapKey', 'unwrapKey']
          secrets: ['get', 'list', 'set']
          certificates: ['get', 'list']
        }
      }
    ]
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
    }
  }
}

// Data encryption key for PostgreSQL
resource dataEncryptionKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = {
  parent: keyVault
  name: 'postgres-data-encryption-key'
  properties: {
    kty: 'RSA'
    keySize: 2048
    keyOps: ['encrypt', 'decrypt', 'wrapKey', 'unwrapKey']
  }
}

output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output keyVaultId string = keyVault.id
output dataEncryptionKeyUri string = dataEncryptionKey.properties.keyUriWithVersion
