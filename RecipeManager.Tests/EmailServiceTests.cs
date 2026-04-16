using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RecipeManager.ApiService.Services;

namespace RecipeManager.Tests;

[TestClass]
public class EmailServiceTests
{
    [TestMethod]
    public async Task DevelopmentEmailService_SendsEmail_ReturnsTrue()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DevelopmentEmailService>>();
        var service = new DevelopmentEmailService(loggerMock.Object);

        // Act
        var result = await service.SendLoginCodeAsync("test@example.com", "123456", 15);

        // Assert
        Assert.IsTrue(result, "Development email service should always return true");
    }

    [TestMethod]
    public async Task DevelopmentEmailService_LogsEmailDetails()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DevelopmentEmailService>>();
        var service = new DevelopmentEmailService(loggerMock.Object);

        // Act
        await service.SendLoginCodeAsync("user@example.com", "987654", 10);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("user@example.com")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "Email address should be logged");

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("987654")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "Login code should be logged");
    }

    [TestMethod]
    public async Task DevelopmentEmailService_SupportsCancellation()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DevelopmentEmailService>>();
        var service = new DevelopmentEmailService(loggerMock.Object);
        using var cts = new CancellationTokenSource();

        // Act
        var result = await service.SendLoginCodeAsync("test@example.com", "123456", 15, cts.Token);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task SendGridEmailService_WithEmptyApiKey_ReturnsFalse()
    {
        // Arrange
        var options = Options.Create(new SendGridOptions { ApiKey = "" });
        var loggerMock = new Mock<ILogger<SendGridEmailService>>();
        var service = new SendGridEmailService(options, loggerMock.Object);

        // Act
        var result = await service.SendLoginCodeAsync("test@example.com", "123456", 15);

        // Assert
        Assert.IsFalse(result, "SendGrid service should return false when API key is missing");
    }

    [TestMethod]
    public async Task SendGridEmailService_WithEmptyApiKey_LogsError()
    {
        // Arrange
        var options = Options.Create(new SendGridOptions { ApiKey = "" });
        var loggerMock = new Mock<ILogger<SendGridEmailService>>();
        var service = new SendGridEmailService(options, loggerMock.Object);

        // Act
        await service.SendLoginCodeAsync("test@example.com", "123456", 15);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("API key")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Error should be logged when API key is missing");
    }

    [TestMethod]
    public void EmailTemplates_GeneratesPlainTextWithCode()
    {
        // Act
        var plainText = EmailTemplates.GetLoginCodePlainText("123456", 15);

        // Assert
        Assert.IsTrue(plainText.Contains("123456"), "Plain text should contain the code");
        Assert.IsTrue(plainText.Contains("15 minutes"), "Plain text should contain expiry time");
    }

    [TestMethod]
    public void EmailTemplates_GeneratesHtmlWithCode()
    {
        // Act
        var html = EmailTemplates.GetLoginCodeHtml("654321", 20);

        // Assert
        Assert.IsTrue(html.Contains("654321"), "HTML should contain the code");
        Assert.IsTrue(html.Contains("20 minutes"), "HTML should contain expiry time");
        Assert.IsTrue(html.Contains("<!DOCTYPE html>"), "HTML should be valid HTML");
        Assert.IsTrue(html.Contains("RecipeManager"), "HTML should contain app name");
    }

    [TestMethod]
    public void EmailTemplates_HtmlIsAccessible()
    {
        // Act
        var html = EmailTemplates.GetLoginCodeHtml("111222", 15);

        // Assert
        Assert.IsTrue(html.Contains("aria-label"), "HTML should include ARIA labels for accessibility");
        Assert.IsTrue(html.Contains("lang=\"en\""), "HTML should specify language");
        Assert.IsTrue(html.Contains("role="), "HTML should include role attributes");
    }

    [TestMethod]
    public void EmailTemplates_SubjectIsDescriptive()
    {
        // Act
        var subject = EmailTemplates.GetLoginCodeSubject();

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(subject), "Subject should not be empty");
        Assert.IsTrue(subject.Contains("Login"), "Subject should mention login");
        Assert.IsTrue(subject.Contains("Code"), "Subject should mention code");
    }
}
