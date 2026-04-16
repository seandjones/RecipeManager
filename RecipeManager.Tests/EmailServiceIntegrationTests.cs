using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecipeManager.ApiService.Services;

namespace RecipeManager.Tests;

[TestClass]
public class EmailServiceIntegrationTests
{
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("RequiresApiKey")]
    public async Task SendGridEmailService_WithValidApiKey_SendsEmail()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<EmailServiceIntegrationTests>()
            .Build();

        var apiKey = configuration["SendGrid:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Assert.Inconclusive("SendGrid API key not configured. Configure using: dotnet user-secrets set \"SendGrid:ApiKey\" \"YOUR_KEY\"");
            return;
        }

        var options = Options.Create(new SendGridOptions
        {
            ApiKey = apiKey,
            FromEmail = configuration["SendGrid:FromEmail"] ?? "test@recipemanager.dev",
            FromName = configuration["SendGrid:FromName"] ?? "RecipeManager Test"
        });

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<SendGridEmailService>();
        var service = new SendGridEmailService(options, logger);

        // Act
        var testEmail = configuration["SendGrid:TestEmail"] ?? "your-email@example.com";
        var result = await service.SendLoginCodeAsync(testEmail, "123456", 15);

        // Assert
        Assert.IsTrue(result, "Email should be sent successfully with valid API key");
        Console.WriteLine($"✅ Test email sent successfully to {testEmail}");
        Console.WriteLine("⚠️  Check your inbox to verify the email was received.");
    }

    [TestMethod]
    public async Task SendGridEmailService_WithInvalidApiKey_ReturnsFalse()
    {
        // Arrange
        var options = Options.Create(new SendGridOptions
        {
            ApiKey = "SG.invalid-api-key-for-testing",
            FromEmail = "test@example.com",
            FromName = "Test"
        });

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<SendGridEmailService>();
        var service = new SendGridEmailService(options, logger);

        // Act
        var result = await service.SendLoginCodeAsync("test@example.com", "123456", 15);

        // Assert
        Assert.IsFalse(result, "Email should fail with invalid API key");
    }
}
