namespace RecipeManager.ApiService.Services;

/// <summary>
/// Email templates for authentication emails
/// </summary>
public static class EmailTemplates
{
    /// <summary>
    /// Gets the plain text version of the login code email
    /// </summary>
    public static string GetLoginCodePlainText(string code, int expiresInMinutes)
    {
        return $@"Your RecipeManager Login Code

Your login code is: {code}

This code will expire in {expiresInMinutes} minutes.

If you did not request this code, please ignore this email.

- RecipeManager Team";
    }

    /// <summary>
    /// Gets the HTML version of the login code email
    /// </summary>
    public static string GetLoginCodeHtml(string code, int expiresInMinutes)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Your Login Code</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            background-color: #f9f9f9;
            border-radius: 8px;
            padding: 30px;
            margin: 20px 0;
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .header h1 {{
            color: #0066cc;
            margin: 0;
            font-size: 24px;
        }}
        .code-container {{
            background-color: #fff;
            border: 2px solid #0066cc;
            border-radius: 8px;
            padding: 20px;
            text-align: center;
            margin: 20px 0;
        }}
        .code {{
            font-size: 32px;
            font-weight: bold;
            letter-spacing: 8px;
            color: #0066cc;
            font-family: 'Courier New', monospace;
        }}
        .expiry {{
            color: #666;
            font-size: 14px;
            margin-top: 10px;
        }}
        .footer {{
            text-align: center;
            color: #666;
            font-size: 12px;
            margin-top: 30px;
        }}
        .warning {{
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 12px;
            margin: 20px 0;
            font-size: 14px;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔐 RecipeManager Login</h1>
        </div>
        
        <p>Hello,</p>
        
        <p>You requested a login code for RecipeManager. Use the code below to complete your sign-in:</p>
        
        <div class=""code-container"">
            <div class=""code"" role=""text"" aria-label=""Login code: {code}"">{code}</div>
            <div class=""expiry"">Expires in {expiresInMinutes} minutes</div>
        </div>
        
        <div class=""warning"">
            <strong>⚠️ Security Notice:</strong> If you did not request this code, please ignore this email. Do not share this code with anyone.
        </div>
        
        <div class=""footer"">
            <p>This is an automated message from RecipeManager.</p>
            <p>&copy; 2026 RecipeManager. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Gets the email subject for login code emails
    /// </summary>
    public static string GetLoginCodeSubject()
    {
        return "Your RecipeManager Login Code";
    }
}
