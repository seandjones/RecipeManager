using System.Net;
using System.Net.Http.Json;
using RecipeManager.ApiService.Models;

namespace RecipeManager.Tests;

[TestClass]
public class AuthApiIntegrationTests
{
    [TestMethod]
    [TestCategory("Integration")]
    public async Task RequestCode_WithValidEmail_ReturnsSuccess()
    {
        // This test requires the full application to be running
        // For now, it's a placeholder for when we add proper API integration testing
        // We'll implement this after Task #5 when the web project has authentication middleware
        
        Assert.Inconclusive("Full integration tests require the application to be running. Implement after Task #5.");
    }

    [TestMethod]
    public void RequestLoginCodeRequest_CanBeCreated()
    {
        // Arrange & Act
        var request = new RequestLoginCodeRequest { Email = "test@example.com" };

        // Assert
        Assert.AreEqual("test@example.com", request.Email);
    }

    [TestMethod]
    public void VerifyLoginCodeRequest_CanBeCreated()
    {
        // Arrange & Act
        var request = new VerifyLoginCodeRequest
        {
            Email = "test@example.com",
            Code = "123456"
        };

        // Assert
        Assert.AreEqual("test@example.com", request.Email);
        Assert.AreEqual("123456", request.Code);
    }

    [TestMethod]
    public void RequestLoginCodeResponse_WithSuccess_HasCorrectProperties()
    {
        // Arrange & Act
        var response = new RequestLoginCodeResponse
        {
            Success = true,
            Message = "Code sent"
        };

        // Assert
        Assert.IsTrue(response.Success);
        Assert.AreEqual("Code sent", response.Message);
        Assert.IsNull(response.RetryAfterSeconds);
    }

    [TestMethod]
    public void RequestLoginCodeResponse_WithRateLimit_HasRetryAfter()
    {
        // Arrange & Act
        var response = new RequestLoginCodeResponse
        {
            Success = false,
            Message = "Too many requests",
            RetryAfterSeconds = 3600
        };

        // Assert
        Assert.IsFalse(response.Success);
        Assert.IsNotNull(response.RetryAfterSeconds);
        Assert.AreEqual(3600, response.RetryAfterSeconds);
    }

    [TestMethod]
    public void VerifyLoginCodeResponse_WithSuccess_HasUserInfo()
    {
        // Arrange & Act
        var userId = Guid.NewGuid();
        var response = new VerifyLoginCodeResponse
        {
            Success = true,
            Message = "Login successful",
            UserId = userId,
            Email = "test@example.com"
        };

        // Assert
        Assert.IsTrue(response.Success);
        Assert.AreEqual(userId, response.UserId);
        Assert.AreEqual("test@example.com", response.Email);
    }

    [TestMethod]
    public void VerifyLoginCodeResponse_WithFailure_HasNoUserInfo()
    {
        // Arrange & Act
        var response = new VerifyLoginCodeResponse
        {
            Success = false,
            Message = "Invalid code"
        };

        // Assert
        Assert.IsFalse(response.Success);
        Assert.IsNull(response.UserId);
        Assert.IsNull(response.Email);
    }
}
