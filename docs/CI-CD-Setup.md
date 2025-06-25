# CI/CD Pipeline Setup Guide

This document provides instructions for setting up the CI/CD pipelines for the Azure Workflow System.

## Overview

The project uses GitHub Actions for continuous integration and deployment with the following workflows:

1. **CI (Continuous Integration)** - `ci.yml`
   - Runs on all pull requests and pushes to main
   - Performs linting, testing, security scanning, and Bicep validation
   
2. **CD Development** - `cd-dev.yml`
   - Automatically deploys to development environment on main branch pushes
   - Can be manually triggered with options to deploy infrastructure/applications separately
   
3. **CD Production** - `cd-prod.yml`
   - Manual deployment to production with confirmation required
   - Triggered by releases or manual workflow dispatch

## Required Secrets and Variables

### GitHub Secrets (Repository Settings > Secrets and Variables > Actions)

#### For Development Environment:
- `AZURE_CREDENTIALS` - Azure service principal credentials for development deployments
  ```json
  {
    "clientId": "<dev-service-principal-client-id>",
    "clientSecret": "<dev-service-principal-secret>",
    "subscriptionId": "<azure-subscription-id>",
    "tenantId": "<azure-tenant-id>"
  }
  ```

#### For Production Environment:
- `AZURE_CREDENTIALS_PROD` - Azure service principal credentials for production deployments
  ```json
  {
    "clientId": "<prod-service-principal-client-id>",
    "clientSecret": "<prod-service-principal-secret>",
    "subscriptionId": "<azure-subscription-id>",
    "tenantId": "<azure-tenant-id>"
  }
  ```

#### Optional Secrets:
- `CODECOV_TOKEN` - Token for uploading test coverage reports to Codecov

### GitHub Variables (Repository Settings > Secrets and Variables > Actions)

#### For Development Environment:
- `AZURE_SUBSCRIPTION_ID` - Azure subscription ID for development resources
- `AZURE_RESOURCE_GROUP_DEV` - Resource group name for development environment
- `AZURE_LOCATION` - Azure region (e.g., "eastus", "westus2")

#### For Production Environment:
- `AZURE_SUBSCRIPTION_ID_PROD` - Azure subscription ID for production resources (can be same as dev)
- `AZURE_RESOURCE_GROUP_PROD` - Resource group name for production environment

## Azure Service Principal Setup

### Create Service Principal for Development

```bash
# Create service principal for development
az ad sp create-for-rbac --name "azure-workflow-system-dev" \
  --role "Contributor" \
  --scopes "/subscriptions/<subscription-id>/resourceGroups/<dev-resource-group>" \
  --sdk-auth

# Grant additional permissions if needed
az role assignment create \
  --assignee "<service-principal-client-id>" \
  --role "User Access Administrator" \
  --scope "/subscriptions/<subscription-id>/resourceGroups/<dev-resource-group>"
```

### Create Service Principal for Production

```bash
# Create service principal for production
az ad sp create-for-rbac --name "azure-workflow-system-prod" \
  --role "Contributor" \
  --scopes "/subscriptions/<subscription-id>/resourceGroups/<prod-resource-group>" \
  --sdk-auth

# Grant additional permissions if needed
az role assignment create \
  --assignee "<service-principal-client-id>" \
  --role "User Access Administrator" \
  --scope "/subscriptions/<subscription-id>/resourceGroups/<prod-resource-group>"
```

## Branch Protection Rules

Set up branch protection for the `main` branch:

1. Go to repository Settings > Branches
2. Add rule for `main` branch:
   - ✅ Require a pull request before merging
   - ✅ Require approvals (minimum 1)
   - ✅ Dismiss stale PR approvals when new commits are pushed
   - ✅ Require review from code owners
   - ✅ Require status checks to pass before merging
     - Add required status checks: `CI Status Check`
   - ✅ Require branches to be up to date before merging
   - ✅ Require conversation resolution before merging
   - ✅ Restrict pushes that create files or modify files outside of the repository's root directory

## Environment Setup

### Create GitHub Environments

1. Go to repository Settings > Environments
2. Create `development` environment:
   - Set deployment branch rule to `main`
   - Add required reviewers if desired
   - Add environment secrets specific to development
3. Create `production` environment:
   - Set deployment branch rule to `main`
   - Add required reviewers (recommended for production)
   - Add environment secrets specific to production

## Workflow Features

### CI Pipeline Features:
- **Code Quality**: ESLint and Prettier checks
- **Testing**: Jest unit tests with coverage reporting
- **Security**: Trivy vulnerability scanning
- **Infrastructure**: Bicep validation and linting
- **Build Artifacts**: Creates build artifacts for deployment

### CD Development Pipeline Features:
- **Automatic Deployment**: Triggers on main branch pushes
- **Manual Deployment**: Can be triggered manually with options
- **Infrastructure as Code**: Deploys Bicep templates
- **Application Deployment**: Deploys applications to Container Apps
- **Smoke Tests**: Basic connectivity and health checks
- **Deployment Summary**: Detailed deployment status and URLs

### CD Production Pipeline Features:
- **Manual Only**: Requires explicit confirmation or release trigger
- **Security Validation**: Pre-deployment security checkpoint
- **Confirmation Required**: Must type "DEPLOY_TO_PRODUCTION" to confirm
- **Production Safeguards**: Additional verification steps
- **Comprehensive Monitoring**: Enhanced post-deployment checks
- **Notifications**: Detailed deployment summaries

## Usage Examples

### Running CI Manually
CI runs automatically on PRs and main branch pushes, but you can also:
1. Go to Actions tab
2. Select "CI - Continuous Integration"
3. Click "Run workflow"

### Manual Development Deployment
1. Go to Actions tab
2. Select "CD - Deploy to Development"
3. Click "Run workflow"
4. Choose options:
   - Deploy infrastructure: true/false
   - Deploy applications: true/false

### Manual Production Deployment
1. Go to Actions tab
2. Select "CD - Deploy to Production"
3. Click "Run workflow"
4. Fill in:
   - Deploy infrastructure: true/false
   - Deploy applications: true/false
   - Confirmation: `DEPLOY_TO_PRODUCTION`

## Monitoring and Troubleshooting

### View Deployment Logs
- Go to Actions tab in GitHub
- Click on the specific workflow run
- Expand the job and step to view detailed logs

### Common Issues
1. **Azure Authentication Failed**: Check service principal credentials and permissions
2. **Resource Group Not Found**: Ensure resource groups exist or workflows create them
3. **Bicep Validation Errors**: Check template syntax and parameter files
4. **Missing Secrets/Variables**: Verify all required secrets and variables are set

### Health Checks
- Monitor deployment summaries in workflow runs
- Check Azure portal for resource status
- Review application logs in Container Apps
- Monitor Application Insights for telemetry

## Security Considerations

1. **Service Principals**: Use least-privilege access
2. **Secrets Management**: Rotate secrets regularly
3. **Environment Isolation**: Separate dev/prod credentials
4. **Audit Logs**: Monitor deployment activities
5. **Branch Protection**: Enforce code review requirements
6. **Vulnerability Scanning**: Address security findings promptly

## Maintenance

### Regular Tasks:
- Review and rotate Azure service principal secrets
- Update workflow dependencies (actions versions)
- Monitor deployment success rates
- Review security scan results
- Update Bicep templates as needed

### Upgrades:
- Test workflow changes in development first
- Update Azure CLI and Bicep versions periodically
- Review and update security scanning tools
- Keep Node.js and npm dependencies updated
