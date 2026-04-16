using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace RecipeManager.ApiService.Services;

/// <summary>
/// SendGrid email service configuration
/// </summary>
public class SendGridOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@recipemanager.com";
    public string FromName { get; set; } = "RecipeManager";
}

/// <summary>
/// Email service that sends emails using SendGrid
/// </summary>
public class SendGridEmailService(
    IOptions<SendGridOptions> options,
    ILogger<SendGridEmailService> logger) : IEmailService
{
    private readonly SendGridOptions _options = options.Value;

    public async Task<bool> SendLoginCodeAsync(string email, string code, int expiresInMinutes, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                logger.LogError("SendGrid API key is not configured");
                return false;
            }

            var client = new SendGridClient(_options.ApiKey);

            var from = new EmailAddress(_options.FromEmail, _options.FromName);
            var to = new EmailAddress(email);
            var subject = EmailTemplates.GetLoginCodeSubject();
            var plainTextContent = EmailTemplates.GetLoginCodePlainText(code, expiresInMinutes);
            var htmlContent = EmailTemplates.GetLoginCodeHtml(code, expiresInMinutes);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            var response = await client.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Login code email sent successfully to {Email}", email);
                return true;
            }
            else
            {
                var body = await response.Body.ReadAsStringAsync(cancellationToken);
                logger.LogError("Failed to send email to {Email}. Status: {StatusCode}, Body: {Body}",
                    email, response.StatusCode, body);
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while sending email to {Email}", email);
            return false;
        }
    }
}
