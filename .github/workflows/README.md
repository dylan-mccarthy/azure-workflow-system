# GitHub Actions Workflows

This directory contains the GitHub Actions workflows for the Azure Workflow System project.

## Workflows

### 1. CI - Continuous Integration (`ci.yml`)
**Trigger:** Pull requests and pushes to main branch

**Purpose:** Validates code quality, runs tests, and performs security scanning

**Jobs:**
- **Lint and Test**: ESLint, Prettier, Jest unit tests with coverage
- **Validate Infrastructure**: Bicep template validation and linting
- **Security Scan**: Trivy vulnerability scanning with SARIF upload
- **Build API**: Creates build artifacts (placeholder for future API code)
- **Status Check**: Aggregates all CI results

**Requirements:** None (runs without Azure credentials)

### 2. CD - Deploy to Development (`cd-dev.yml`)
**Trigger:** Pushes to main branch (automatic) or manual workflow dispatch

**Purpose:** Deploys infrastructure and applications to development environment

**Jobs:**
- **Deploy Infrastructure**: Deploys Bicep templates to Azure development environment
- **Deploy Applications**: Builds and deploys applications to Container Apps
- **Deployment Summary**: Generates deployment status report

**Requirements:**
- Azure credentials configured in `AZURE_CREDENTIALS` secret
- Development environment variables (see CI-CD-Setup.md)

### 3. CD - Deploy to Production (`cd-prod.yml`)
**Trigger:** Manual workflow dispatch with confirmation or release events

**Purpose:** Deploys infrastructure and applications to production environment

**Jobs:**
- **Validation**: Pre-deployment security checkpoint and confirmation validation
- **Deploy Infrastructure**: Deploys Bicep templates to Azure production environment
- **Deploy Applications**: Builds and deploys applications to production Container Apps
- **Deployment Notification**: Comprehensive production deployment summary

**Requirements:**
- Production Azure credentials in `AZURE_CREDENTIALS_PROD` secret
- Production environment variables (see CI-CD-Setup.md)
- Manual confirmation by typing "DEPLOY_TO_PRODUCTION"

### 4. Test CI/CD Setup (`test-setup.yml`)
**Trigger:** Manual workflow dispatch only

**Purpose:** Validates CI/CD pipeline configuration and setup

**Jobs:**
- **Test Basic**: Validates Node.js setup, dependencies, linting, and testing
- **Test Bicep**: Validates Bicep template compilation and linting
- **Test Security**: Runs security scanning tests
- **Test Summary**: Aggregates test results

**Requirements:** None (designed for setup validation)

## Workflow Features

### Security Features
- Vulnerability scanning with Trivy
- SARIF upload to GitHub Security tab
- Production deployment confirmation requirements
- Service principal authentication with least privilege
- Environment isolation between dev/prod

### Quality Assurance
- Code linting with ESLint
- Code formatting with Prettier
- Unit testing with Jest and coverage reporting
- Infrastructure as Code validation
- Build artifact creation and management

### Deployment Features
- Infrastructure as Code with Bicep
- Automated development deployments
- Manual production deployments with safeguards
- Smoke testing and health checks
- Comprehensive deployment reporting
- Rollback capabilities (manual)

## Setup Instructions

See `docs/CI-CD-Setup.md` for detailed setup instructions including:
- Required secrets and variables
- Azure service principal configuration
- Branch protection rules
- Environment setup

## Usage Examples

### Manual Development Deployment
1. Go to Actions tab in GitHub
2. Select "CD - Deploy to Development"
3. Click "Run workflow"
4. Choose deployment options

### Production Deployment
1. Go to Actions tab in GitHub
2. Select "CD - Deploy to Production"  
3. Click "Run workflow"
4. Set confirmation field to: `DEPLOY_TO_PRODUCTION`
5. Choose deployment options

### Testing Setup
1. Go to Actions tab in GitHub
2. Select "Test CI/CD Setup"
3. Click "Run workflow"
4. Choose test type: basic, bicep-validation, or security-scan

## Monitoring

### Deployment Status
- Check workflow run summaries for deployment details
- Monitor Azure portal for resource status
- Review deployment logs in Actions tab

### Security Monitoring
- Check Security tab for vulnerability reports
- Review Trivy scan results in workflow logs
- Monitor dependency updates and security patches

### Performance Monitoring
- Monitor workflow execution times
- Track deployment success rates
- Review resource utilization in Azure

## Troubleshooting

### Common Issues
1. **Authentication failures**: Check service principal credentials and permissions
2. **Missing resources**: Ensure resource groups exist or are created by workflows
3. **Bicep validation errors**: Verify template syntax and parameter files
4. **Build failures**: Check Node.js version and dependency compatibility

### Debug Tips
- Use workflow dispatch to test individual components
- Check detailed logs in each workflow step
- Validate Bicep templates locally before pushing
- Test authentication with Azure CLI locally

## Maintenance

### Regular Tasks
- Review and update workflow dependencies
- Rotate Azure service principal credentials
- Update security scanning tools and configurations
- Monitor workflow performance and optimize as needed

### Best Practices
- Test workflow changes in feature branches first
- Keep sensitive data in secrets, not variables
- Use environment-specific configurations
- Document any custom workflow modifications
