using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using RecipeManager.Web.Models;
using RecipeManager.Web.Services;
using System.Net;
using System.Text.Json;

namespace RecipeManager.Tests;

[TestClass]
public class AuthApiClientTests
{
    private Mock<HttpMessageHandler> CreateMockHttpMessageHandler(
        HttpStatusCode statusCode,
        object? responseContent = null)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = responseContent != null
                ? new StringContent(JsonSerializer.Serialize(responseContent))
                : new StringContent("")
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return mockHandler;
    }

    [TestMethod]
    public async Task RequestLoginCodeAsync_WithSuccessResponse_ReturnsSuccessResult()
    {
        // Arrange
        var expectedResponse = new RequestLoginCodeResponse
        {
            Success = true,
            Message = "Code sent successfully"
        };

        var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, expectedResponse);
        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://test.api")
        };
        var client = new AuthApiClient(httpClient);

        // Act
        var result = await client.RequestLoginCodeAsync("test@example.com");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        Assert.AreEqual("Code sent successfully", result.Message);
    }

    [TestMethod]
    public async Task RequestLoginCodeAsync_WithRateLimitError_ReturnsRetryAfterSeconds()
    {
        // Arrange
        var expectedResponse = new RequestLoginCodeResponse
        {
            Success = false,
            Message = "Too many requests",
            RetryAfterSeconds = 300
        };

        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.TooManyRequests,
            Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
        };
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(300));

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://test.api")
        };
        var client = new AuthApiClient(httpClient);

        // Act
        var result = await client.RequestLoginCodeAsync("test@example.com");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.RetryAfterSeconds);
        Assert.AreEqual(300, result.RetryAfterSeconds);
    }

    [TestMethod]
    public async Task RequestLoginCodeAsync_WithNetworkError_ReturnsFailureResult()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://test.api")
        };
        var client = new AuthApiClient(httpClient);

        // Act
        var result = await client.RequestLoginCodeAsync("test@example.com");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message?.Contains("Network error") ?? false);
    }

    [TestMethod]
    public async Task VerifyCodeAsync_WithSuccessResponse_ReturnsUserInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedResponse = new VerifyLoginCodeResponse
        {
            Success = true,
            Message = "Code verified",
            UserId = userId,
            Email = "test@example.com"
        };

        var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, expectedResponse);
        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://test.api")
        };
        var client = new AuthApiClient(httpClient);

        // Act
        var result = await client.VerifyCodeAsync("test@example.com", "123456");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        Assert.AreEqual(userId, result.UserId);
        Assert.AreEqual("test@example.com", result.Email);
    }

    [TestMethod]
    public async Task VerifyCodeAsync_WithInvalidCode_ReturnsFailureResult()
    {
        // Arrange
        var expectedResponse = new VerifyLoginCodeResponse
        {
            Success = false,
            Message = "Invalid code"
        };

        var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.BadRequest, expectedResponse);
        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://test.api")
        };
        var client = new AuthApiClient(httpClient);

        // Act
        var result = await client.VerifyCodeAsync("test@example.com", "000000");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid code", result.Message);
    }

    [TestMethod]
    public async Task VerifyCodeAsync_WithNetworkError_ReturnsFailureResult()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://test.api")
        };
        var client = new AuthApiClient(httpClient);

        // Act
        var result = await client.VerifyCodeAsync("test@example.com", "123456");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message?.Contains("Network error") ?? false);
    }

    [TestMethod]
    public async Task LogoutAsync_WithSuccessResponse_ReturnsTrue()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://test.api")
        };
        var client = new AuthApiClient(httpClient);

        // Act
        var result = await client.LogoutAsync();

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task LogoutAsync_WithNetworkError_ReturnsTrue()
    {
        // Arrange - logout should succeed even if network fails
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://test.api")
        };
        var client = new AuthApiClient(httpClient);

        // Act
        var result = await client.LogoutAsync();

        // Assert
        Assert.IsTrue(result); // Should still return true for graceful degradation
    }

    [TestMethod]
    public async Task RequestLoginCodeAsync_SupportsCancellation()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://test.api")
        };
        var client = new AuthApiClient(httpClient);

        // Act & Assert - HttpClient throws TaskCanceledException for cancelled requests
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(
            async () => await client.RequestLoginCodeAsync("test@example.com", cts.Token));
    }

    [TestMethod]
    public async Task VerifyCodeAsync_SupportsCancellation()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://test.api")
        };
        var client = new AuthApiClient(httpClient);

        // Act & Assert - HttpClient throws TaskCanceledException for cancelled requests
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(
            async () => await client.VerifyCodeAsync("test@example.com", "123456", cts.Token));
    }
}
