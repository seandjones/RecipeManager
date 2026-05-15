using Microsoft.Extensions.Logging;
using RecipeManager.ApiService.Data;
using System;

namespace RecipeManager.ApiService.Services;

/// <summary>
/// Development email service that logs emails to the console instead of sending them
/// </summary>
public class DevelopmentEmailService(ILogger<DevelopmentEmailService> logger) : IEmailService
{
    public Task<bool> SendLoginCodeAsync(string email, string code, int expiresInMinutes, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("📧 DEVELOPMENT EMAIL SERVICE");
        Console.WriteLine("========================================");
        Console.WriteLine($"To: {email}");
        Console.WriteLine($"Subject: {EmailTemplates.GetLoginCodeSubject()}");
        Console.WriteLine("----------------------------------------");
        Console.WriteLine($"Login Code: {code}");
        Console.WriteLine($"Expires In: {expiresInMinutes} minutes");
        Console.WriteLine("----------------------------------------");
        Console.WriteLine("Plain Text Body:");
        Console.WriteLine(EmailTemplates.GetLoginCodePlainText(code, expiresInMinutes));
        Console.WriteLine("========================================");

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

    public Task<bool> SendIngredientListShareInvitationAsync(string email, string listName, string shareUrl, AccessLevel accessLevel, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("========================================");
        logger.LogInformation("📧 DEVELOPMENT EMAIL SERVICE");
        logger.LogInformation("========================================");
        logger.LogInformation("To: {Email}", email);
        logger.LogInformation("Subject: {Subject}", EmailTemplates.GetIngredientListShareSubject(listName));
        logger.LogInformation("----------------------------------------");
        logger.LogInformation("List: {ListName}", listName);
        logger.LogInformation("AccessLevel: {AccessLevel}", accessLevel);
        logger.LogInformation("Share URL: {ShareUrl}", shareUrl);
        logger.LogInformation("----------------------------------------");
        logger.LogInformation("Plain Text Body:");
        logger.LogInformation("{PlainText}", EmailTemplates.GetIngredientListSharePlainText(listName, shareUrl, accessLevel));
        logger.LogInformation("========================================");

        return Task.FromResult(true);
    }
}
