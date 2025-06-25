# Infrastructure as Code (IaC) - Bicep Templates

This directory contains the Bicep templates for deploying the Azure Platform Support Workflow Management System infrastructure.

## Architecture Overview

The infrastructure consists of the following Azure resources:

- **Azure Container Apps**: Hosts the API and Bot services with auto-scaling capabilities
- **PostgreSQL Flexible Server**: Database with encryption at rest and in transit
- **Azure Key Vault**: Secure storage for secrets and encryption keys
- **Azure Storage Account**: Blob storage for file attachments
- **Azure API Management**: API gateway with security policies
- **Application Insights**: Application monitoring and logging
- **Log Analytics Workspace**: Centralized logging

## Security Features

- **Managed Identity**: All services use Azure Managed Identity for authentication
- **Network Security**: Private endpoints and network access controls
- **Encryption**: Data encryption at rest and in transit
- **Key Management**: Azure Key Vault for secret and key management
- **RBAC**: Role-based access control for all resources
- **TLS 1.2+**: Enforced across all communication channels

## Environment Configuration

### Development Environment
- **Container Apps**: Basic tier with 1-3 replicas
- **PostgreSQL**: Basic tier (Standard_B1ms) with 32GB storage
- **API Management**: Developer tier
- **High Availability**: Disabled for cost optimization

### Production Environment
- **Container Apps**: Standard tier with 2-10 replicas
- **PostgreSQL**: General Purpose tier (Standard_D2s_v3) with 128GB storage, geo-redundant backup
- **API Management**: Basic tier with 2 instances
- **High Availability**: Zone-redundant configuration

## Deployment Instructions

### Prerequisites

1. Azure CLI installed and configured
2. Azure subscription with appropriate permissions
3. Resource group created

### Deploy Development Environment

```powershell
# Login to Azure
az login

# Set subscription (if needed)
az account set --subscription "your-subscription-id"

# Create resource group
az group create --name "rg-awfs-dev" --location "East US 2"

# Deploy infrastructure
az deployment group create \
  --resource-group "rg-awfs-dev" \
  --template-file "main.bicep" \
  --parameters "@parameters/dev.parameters.json"
```

### Deploy Production Environment

```powershell
# Create resource group
az group create --name "rg-awfs-prod" --location "East US 2"

# Update production parameters with secure values
# IMPORTANT: Replace postgres password in prod.parameters.json

# Deploy infrastructure
az deployment group create \
  --resource-group "rg-awfs-prod" \
  --template-file "main.bicep" \
  --parameters "@parameters/prod.parameters.json"
```

### Validate Deployment

```powershell
# Check deployment status
az deployment group show \
  --resource-group "rg-awfs-dev" \
  --name "main"

# List deployed resources
az resource list --resource-group "rg-awfs-dev" --output table
```

## Modules

### Core Modules

- **`modules/containerApp.bicep`**: Azure Container Apps with environment and scaling configuration
- **`modules/postgresql.bicep`**: PostgreSQL Flexible Server with security hardening
- **`modules/keyVault.bicep`**: Key Vault with access policies and encryption keys
- **`modules/blobStorage.bicep`**: Storage Account with security policies
- **`modules/apim.bicep`**: API Management with security configurations
- **`modules/appInsights.bicep`**: Application Insights with Log Analytics workspace

### Module Features

Each module includes:
- Comprehensive security configurations
- Diagnostic settings for monitoring
- Role-based access control
- Network security controls
- Tags for resource management

## Environment Variables

The deployment creates the following secrets in Key Vault:

- `postgres-connection-string`: Database connection string
- `appinsights-connection-string`: Application Insights connection
- `storage-connection-string`: Storage account connection

## Monitoring and Logging

- **Application Insights**: Application performance monitoring
- **Log Analytics**: Centralized logging for all resources
- **Diagnostic Settings**: Enabled for all resources with 30-day retention
- **Metrics**: Custom metrics for business KPIs

## Cost Optimization

### Development Environment
- Smaller SKUs for all resources
- Single availability zone
- Shorter backup retention (7 days)
- Basic monitoring tier

### Production Environment
- Optimized SKUs for performance and availability
- Zone-redundant storage and high availability
- Extended backup retention (35 days)
- Enhanced monitoring and alerting

## Security Considerations

1. **Secrets Management**: All secrets stored in Key Vault, never in source code
2. **Network Security**: Private endpoints and network ACLs configured
3. **Identity**: Managed Identity used for all service-to-service authentication
4. **Encryption**: TDE for PostgreSQL, encryption at rest for Storage
5. **Access Control**: Least privilege RBAC assignments
6. **Auditing**: Comprehensive logging and monitoring enabled

## Troubleshooting

### Common Issues

1. **Resource naming conflicts**: Ensure resource names are globally unique
2. **Permission errors**: Verify Azure RBAC permissions for deployment
3. **Quota limits**: Check Azure subscription quotas for resources
4. **Network connectivity**: Verify private endpoint configurations

### Deployment Validation

```powershell
# Test connectivity to deployed resources
az postgres flexible-server connect \
  --name "your-postgres-server" \
  --database "workflowdb" \
  --username "awfsadmin"

# Check Container App status
az containerapp show \
  --name "your-api-app" \
  --resource-group "rg-awfs-dev"
```

## Next Steps

After successful deployment:

1. Configure custom domains for Container Apps
2. Set up CI/CD pipelines for application deployment
3. Configure monitoring alerts and dashboards
4. Implement backup and disaster recovery procedures
5. Review and adjust scaling policies based on usage patterns

## Support

For deployment issues or questions:
- Review Azure Activity Log for detailed error messages
- Check resource-specific diagnostic logs
- Validate Bicep templates using `az deployment group validate`
- Consult Azure documentation for resource-specific configurations
