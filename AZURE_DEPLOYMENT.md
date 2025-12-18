# Azure Application Insights Configuration

## Connection String Configuration

This application is configured to use Application Insights with a placeholder connection string that can be easily overridden during Azure deployment.

### Local Development

For local development, replace the `PLACEHOLDER_CONNECTION_STRING` value in `appsettings.json` or `appsettings.Development.json` with your actual Application Insights connection string.

### Azure App Service Deployment

When deploying to Azure App Service, configure the Application Insights connection string using **Application Settings** in the Azure Portal:

#### Option 1: Connection Strings Section (Recommended)
1. Go to your App Service → **Configuration** → **Connection strings**
2. Add a new connection string:
   - **Name**: `ApplicationInsights`
   - **Value**: Your Application Insights connection string
   - **Type**: `Custom`

#### Option 2: Application Settings
1. Go to your App Service → **Configuration** → **Application settings**
2. Add a new application setting:
   - **Name**: `ApplicationInsights__ConnectionString`
   - **Value**: Your Application Insights connection string

#### Option 3: Environment Variable
Set the environment variable:
```bash
APPLICATIONINSIGHTS_CONNECTION_STRING=<your_connection_string>
```

### Configuration Priority

The application will resolve the connection string in this order:
1. Environment variable: `APPLICATIONINSIGHTS_CONNECTION_STRING`
2. Connection string: `ConnectionStrings:ApplicationInsights`
3. Application setting: `ApplicationInsights:ConnectionString`

### Getting Your Connection String

1. Navigate to your Application Insights resource in the Azure Portal
2. Go to **Overview** page
3. Copy the **Connection String** (not the Instrumentation Key)
4. The connection string format looks like:
   ```
   InstrumentationKey=12345678-1234-1234-1234-123456789012;IngestionEndpoint=https://region.in.applicationinsights.azure.com/;LiveEndpoint=https://region.livediagnostics.monitor.azure.com/
   ```

### Azure DevOps / GitHub Actions

For CI/CD pipelines, use Azure App Service Deploy task variables:
- Variable name: `ConnectionStrings.ApplicationInsights` or `ApplicationInsights.ConnectionString`
- Value: Your Application Insights connection string
- Scope: Release/Pipeline variables (marked as secret)

### Benefits of This Approach

✅ **Security**: No connection strings in source control  
✅ **Flexibility**: Easy environment-specific configuration  
✅ **Azure Integration**: Seamless with Azure App Service  
✅ **CI/CD Ready**: Works with deployment pipelines  
✅ **Multiple Override Options**: Supports various configuration patterns