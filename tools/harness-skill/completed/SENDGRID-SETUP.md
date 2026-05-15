# SendGrid Configuration with User Secrets

## Overview

The RecipeManager API uses SendGrid for sending authentication emails in production. In development, emails are logged to the console instead of being sent.

## Development Environment

In development, the `DevelopmentEmailService` is used, which logs emails to the console. No SendGrid API key is required.

## Production Environment

For production or when testing real email sending, you need to configure a SendGrid API key.

### Getting a SendGrid API Key

1. Sign up for a free SendGrid account at https://sendgrid.com/
2. Navigate to Settings → API Keys
3. Click "Create API Key"
4. Give it a name (e.g., "RecipeManager Development")
5. Select "Full Access" or "Restricted Access" with Mail Send permissions
6. Copy the API key (you won't be able to see it again)

### Configuring the API Key with User Secrets

**Never commit API keys to source control!** Use .NET User Secrets for local development.

#### Initialize User Secrets

```powershell
cd RecipeManager.ApiService
dotnet user-secrets init
```

#### Set the SendGrid API Key

```powershell
dotnet user-secrets set "SendGrid:ApiKey" "YOUR_SENDGRID_API_KEY_HERE"
```

#### Optional: Customize From Email

```powershell
dotnet user-secrets set "SendGrid:FromEmail" "youremail@example.com"
dotnet user-secrets set "SendGrid:FromName" "Your Name"
```

#### List Current Secrets

```powershell
dotnet user-secrets list
```

#### Remove a Secret

```powershell
dotnet user-secrets remove "SendGrid:ApiKey"
```

### Production Configuration

For production deployment, configure the SendGrid API key using:

- **Azure App Service**: Application Settings
- **Environment Variables**: `SendGrid__ApiKey`
- **Azure Key Vault**: Store securely and reference in configuration
- **Configuration Server**: Centralized configuration management

### Testing Email Sending

Once configured, you can switch to production mode to test real email sending:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet run --project RecipeManager.ApiService
```

Or update Program.cs temporarily to use SendGridEmailService in development.

## Configuration Structure

```json
{
  "SendGrid": {
    "ApiKey": "SG.your-api-key-here",
    "FromEmail": "noreply@recipemanager.com",
    "FromName": "RecipeManager"
  }
}
```

## Troubleshooting

### Email Not Sending

1. **Check API Key**: Ensure the SendGrid API key is correctly configured
2. **Check Permissions**: Verify the API key has Mail Send permissions
3. **Check Logs**: Look for error messages in the application logs
4. **Check SendGrid Dashboard**: View activity and any errors in SendGrid

### Development Email Not Logging

1. **Check Environment**: Ensure `ASPNETCORE_ENVIRONMENT=Development`
2. **Check Log Level**: Ensure logging is set to Information or higher
3. **Check Console Output**: Development emails are logged to the console

## Security Best Practices

1. **Never commit API keys** to source control
2. **Use different API keys** for development, staging, and production
3. **Rotate API keys** regularly
4. **Use restricted access** API keys with only required permissions
5. **Monitor SendGrid usage** for suspicious activity
6. **Use User Secrets** for local development
7. **Use secure configuration** providers for production (Azure Key Vault, etc.)
