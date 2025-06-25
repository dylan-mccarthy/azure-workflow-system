// Bicep module: apim.bicep
@description('Name of the API Management service')
param apimName string

@description('Location for the API Management service')
param location string = resourceGroup().location

@description('Publisher email address')
param publisherEmail string

@description('Publisher organization name')
param publisherName string

@description('SKU name for API Management')
param skuName string = 'Developer'

@description('SKU capacity for API Management')
param skuCapacity int = 1

@description('User-assigned managed identity ID')
param userAssignedIdentityId string

@description('Tags to apply to resources')
param tags object = {}

// API Management service with security configuration
resource apimService 'Microsoft.ApiManagement/service@2021-08-01' = {
  name: apimName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
  sku: {
    name: skuName
    capacity: skuCapacity
  }
  properties: {
    publisherName: publisherName
    publisherEmail: publisherEmail
    customProperties: {
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Protocols.Tls10': 'False'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Protocols.Tls11': 'False'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Protocols.Ssl30': 'False'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Backend.Protocols.Tls10': 'False'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Backend.Protocols.Tls11': 'False'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Backend.Protocols.Ssl30': 'False'
    }
  }
}

output apimName string = apimService.name
output apimServiceId string = apimService.id
output gatewayUrl string = apimService.properties.gatewayUrl
