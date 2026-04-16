using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RecipeManager.ApiService.Data;
using RecipeManager.ApiService.Services;

namespace RecipeManager.Tests;

[TestClass]
public class AuthServiceTests
{
    private AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AuthDbContext(options);
    }

    [TestMethod]
    public async Task RequestLoginCode_WithValidEmail_ReturnsSuccess()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock.Setup(x => x.SendLoginCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var loggerMock = new Mock<ILogger<AuthService>>();
        var authService = new AuthService(dbContext, emailServiceMock.Object, loggerMock.Object);

        // Act
        var result = await authService.RequestLoginCodeAsync("test@example.com");

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Message);
        Assert.IsNull(result.RetryAfterSeconds);

        // Verify user was created
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.IsNotNull(user);

        // Verify login code was created
        var loginCode = await dbContext.LoginCodes.FirstOrDefaultAsync(lc => lc.UserId == user.Id);
        Assert.IsNotNull(loginCode);
        Assert.AreEqual(6, loginCode.Code.Length);
        Assert.IsFalse(loginCode.IsUsed);
        Assert.IsTrue(loginCode.ExpiresAt > DateTime.UtcNow);
    }

    [TestMethod]
    public async Task RequestLoginCode_NormalizesEmail()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock.Setup(x => x.SendLoginCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var loggerMock = new Mock<ILogger<AuthService>>();
        var authService = new AuthService(dbContext, emailServiceMock.Object, loggerMock.Object);

        // Act
        await authService.RequestLoginCodeAsync("  TEST@EXAMPLE.COM  ");

        // Assert
        var user = await dbContext.Users.FirstOrDefaultAsync();
        Assert.IsNotNull(user);
        Assert.AreEqual("test@example.com", user.Email);
    }

    [TestMethod]
    public async Task RequestLoginCode_ExceedingRateLimit_ReturnsTooManyRequests()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock.Setup(x => x.SendLoginCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var loggerMock = new Mock<ILogger<AuthService>>();
        var authService = new AuthService(dbContext, emailServiceMock.Object, loggerMock.Object);

        // Act - Request 3 codes (rate limit)
        await authService.RequestLoginCodeAsync("test@example.com");
        await authService.RequestLoginCodeAsync("test@example.com");
        await authService.RequestLoginCodeAsync("test@example.com");

        // Fourth request should be rate limited
        var result = await authService.RequestLoginCodeAsync("test@example.com");

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.RetryAfterSeconds);
        Assert.IsTrue(result.RetryAfterSeconds > 0);
        Assert.IsTrue(result.Message!.Contains("Too many"));
    }

    [TestMethod]
    public async Task RequestLoginCode_EmailSendFails_ReturnsError()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock.Setup(x => x.SendLoginCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var loggerMock = new Mock<ILogger<AuthService>>();
        var authService = new AuthService(dbContext, emailServiceMock.Object, loggerMock.Object);

        // Act
        var result = await authService.RequestLoginCodeAsync("test@example.com");

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message!.Contains("Failed to send email"));
    }

    [TestMethod]
    public async Task VerifyLoginCode_WithValidCode_ReturnsSuccess()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock.Setup(x => x.SendLoginCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var loggerMock = new Mock<ILogger<AuthService>>();
        var authService = new AuthService(dbContext, emailServiceMock.Object, loggerMock.Object);

        // Request a code first
        await authService.RequestLoginCodeAsync("test@example.com");

        // Get the code from database
        var user = await dbContext.Users.FirstAsync(u => u.Email == "test@example.com");
        var loginCode = await dbContext.LoginCodes.FirstAsync(lc => lc.UserId == user.Id);

        // Act
        var result = await authService.VerifyLoginCodeAsync("test@example.com", loginCode.Code);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(user.Id, result.UserId);
        Assert.AreEqual(user.Email, result.Email);

        // Verify code is marked as used
        await dbContext.Entry(loginCode).ReloadAsync();
        Assert.IsTrue(loginCode.IsUsed);

        // Verify LastLoginAt is updated
        await dbContext.Entry(user).ReloadAsync();
        Assert.IsNotNull(user.LastLoginAt);
    }

    [TestMethod]
    public async Task VerifyLoginCode_WithInvalidCode_ReturnsUnauthorized()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var emailServiceMock = new Mock<IEmailService>();
        var loggerMock = new Mock<ILogger<AuthService>>();
        var authService = new AuthService(dbContext, emailServiceMock.Object, loggerMock.Object);

        // Act
        var result = await authService.VerifyLoginCodeAsync("test@example.com", "123456");

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNull(result.UserId);
    }

    [TestMethod]
    public async Task VerifyLoginCode_WithExpiredCode_ReturnsExpiredMessage()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Users.Add(user);

        var expiredCode = new LoginCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Code = "123456",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Expired
            IsUsed = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20)
        };
        dbContext.LoginCodes.Add(expiredCode);
        await dbContext.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        var loggerMock = new Mock<ILogger<AuthService>>();
        var authService = new AuthService(dbContext, emailServiceMock.Object, loggerMock.Object);

        // Act
        var result = await authService.VerifyLoginCodeAsync("test@example.com", "123456");

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message!.Contains("expired"));
    }

    [TestMethod]
    public async Task VerifyLoginCode_WithUsedCode_ReturnsUsedMessage()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Users.Add(user);

        var usedCode = new LoginCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Code = "123456",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = true, // Already used
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        dbContext.LoginCodes.Add(usedCode);
        await dbContext.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        var loggerMock = new Mock<ILogger<AuthService>>();
        var authService = new AuthService(dbContext, emailServiceMock.Object, loggerMock.Object);

        // Act
        var result = await authService.VerifyLoginCodeAsync("test@example.com", "123456");

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message!.Contains("already been used"));
    }

    [TestMethod]
    public async Task VerifyLoginCode_NormalizesInputs()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock.Setup(x => x.SendLoginCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var loggerMock = new Mock<ILogger<AuthService>>();
        var authService = new AuthService(dbContext, emailServiceMock.Object, loggerMock.Object);

        // Request a code
        await authService.RequestLoginCodeAsync("test@example.com");

        // Get the code
        var user = await dbContext.Users.FirstAsync();
        var loginCode = await dbContext.LoginCodes.FirstAsync();

        // Act - Use uppercase email and code with whitespace
        var result = await authService.VerifyLoginCodeAsync("  TEST@EXAMPLE.COM  ", $"  {loginCode.Code}  ");

        // Assert
        Assert.IsTrue(result.Success);
    }
}
