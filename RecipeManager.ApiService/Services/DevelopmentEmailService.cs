using Microsoft.Extensions.Logging;

namespace RecipeManager.ApiService.Services;

/// <summary>
/// Development email service that logs emails to the console instead of sending them
/// </summary>
public class DevelopmentEmailService(ILogger<DevelopmentEmailService> logger) : IEmailService
{
    public Task<bool> SendLoginCodeAsync(string email, string code, int expiresInMinutes, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("========================================");
        logger.LogInformation("📧 DEVELOPMENT EMAIL SERVICE");
        logger.LogInformation("========================================");
        logger.LogInformation("To: {Email}", email);
        logger.LogInformation("Subject: {Subject}", EmailTemplates.GetLoginCodeSubject());
        logger.LogInformation("----------------------------------------");
        logger.LogInformation("Login Code: {Code}", code);
        logger.LogInformation("Expires In: {ExpiresInMinutes} minutes", expiresInMinutes);
        logger.LogInformation("----------------------------------------");
        logger.LogInformation("Plain Text Body:");
        logger.LogInformation("{PlainText}", EmailTemplates.GetLoginCodePlainText(code, expiresInMinutes));
        logger.LogInformation("========================================");

        return Task.FromResult(true);
    }
}
