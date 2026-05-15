using RecipeManager.ApiService.Data;

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

    public static string GetIngredientListShareSubject(string listName)
    {
        return $"Ingredient List Shared: {listName}";
    }

    public static string GetIngredientListSharePlainText(string listName, string shareUrl, AccessLevel accessLevel)
    {
        return $@"An ingredient list was shared with you in RecipeManager.

List: {listName}
Access Level: {accessLevel}

Open the shared list:
{shareUrl}

This link may expire based on the sharer's settings.

- RecipeManager Team";
    }

    public static string GetIngredientListShareHtml(string listName, string shareUrl, AccessLevel accessLevel)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Ingredient List Shared</title>
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
        .button {{
            display: inline-block;
            background-color: #0a7d32;
            color: #fff !important;
            text-decoration: none;
            padding: 12px 18px;
            border-radius: 6px;
            font-weight: 600;
            margin-top: 12px;
        }}
        .meta {{
            color: #555;
            font-size: 14px;
            margin-top: 10px;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>Shared Ingredient List</h1>
        <p>You were invited to collaborate on an ingredient list in RecipeManager.</p>
        <p><strong>List:</strong> {listName}<br />
           <strong>Access:</strong> {accessLevel}</p>
        <p><a class=""button"" href=""{shareUrl}"">Open Shared List</a></p>
        <p class=""meta"">If the button does not work, copy this URL into your browser:<br />{shareUrl}</p>
    </div>
</body>
</html>";
    }
}
