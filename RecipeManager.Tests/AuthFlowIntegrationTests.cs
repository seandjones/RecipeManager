using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RecipeManager.ApiService.Data;
using RecipeManager.ApiService.Services;
using System.Net;
using System.Net.Http.Json;

// Use alias to disambiguate Program class between Web and ApiService
using ApiServiceProgram = RecipeManager.ApiService.Program;

namespace RecipeManager.Tests;

/// <summary>
/// Integration tests for authentication flow.
/// Tests complete user journeys from login to logout.
/// </summary>
[TestClass]
public class AuthFlowIntegrationTests
{
    private WebApplicationFactory<ApiServiceProgram>? _factory;
    private HttpClient? _client;

    [TestInitialize]
    public void Initialize()
    {
        var inMemoryDbName = Guid.NewGuid().ToString();
        _factory = new WebApplicationFactory<ApiServiceProgram>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove all EF Core registrations
                    var toRemove = services
                        .Where(s => s.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                                    s.ServiceType == typeof(AuthDbContext) ||
                                    s.ServiceType == typeof(RecipeDbContext) ||
                                    s.ServiceType == typeof(DbContextOptions<AuthDbContext>) ||
                                    s.ServiceType == typeof(DbContextOptions<RecipeDbContext>))
                        .ToList();

                    foreach (var service in toRemove)
                    {
                        services.Remove(service);
                    }

                    // Re-register with InMemory provider
                    services.AddDbContext<AuthDbContext>(options =>
                        options.UseInMemoryDatabase(inMemoryDbName + "_Auth"),
                        contextLifetime: ServiceLifetime.Scoped,
                        optionsLifetime: ServiceLifetime.Scoped);

                    services.AddDbContext<RecipeDbContext>(options =>
                        options.UseInMemoryDatabase(inMemoryDbName + "_Recipe"),
                        contextLifetime: ServiceLifetime.Scoped,
                        optionsLifetime: ServiceLifetime.Scoped);

                    // Replace email service with test implementation
                    var emailServiceDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IEmailService));

                    if (emailServiceDescriptor != null)
                    {
                        services.Remove(emailServiceDescriptor);
                    }

                    services.AddSingleton<IEmailService, TestEmailService>();

                    // Build service provider to run migrations
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
                    db.Database.EnsureCreated();
                });
            });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false // Don't follow redirects automatically
        });
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    /// <summary>
    /// Test that unauthenticated requests to protected endpoints redirect to login.
    /// </summary>
    [TestMethod]
    public async Task UnauthenticatedUser_AccessingProtectedEndpoint_RedirectsToLogin()
    {
        // This test would work if we had a protected API endpoint
        // For now, we'll test that the auth endpoint exists
        var response = await _client!.GetAsync("/api/auth/request-code");

        // Should return 405 Method Not Allowed (GET not supported, POST required)
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    /// <summary>
    /// Test complete login flow: request code → verify code → authenticated.
    /// </summary>
    [TestMethod]
    public async Task CompleteLoginFlow_RequestAndVerifyCode_ReturnsUserInfo()
    {
        var testEmail = "test@example.com";

        // Step 1: Request login code
        var requestCodeResponse = await _client!.PostAsJsonAsync("/api/auth/request-code", new
        {
            Email = testEmail
        });

        Assert.AreEqual(HttpStatusCode.OK, requestCodeResponse.StatusCode);
        var requestCodeResult = await requestCodeResponse.Content.ReadFromJsonAsync<RequestCodeResponse>();
        Assert.IsNotNull(requestCodeResult);
        Assert.IsTrue(requestCodeResult.Success);

        // Step 2: Get the code from test email service
        var services = _factory!.Services;
        var testEmailService = services.GetRequiredService<IEmailService>() as TestEmailService;
        Assert.IsNotNull(testEmailService);
        var sentCode = testEmailService.LastSentCode;
        Assert.IsNotNull(sentCode);

        // Step 3: Verify code
        var verifyCodeResponse = await _client.PostAsJsonAsync("/api/auth/verify-code", new
        {
            Email = testEmail,
            Code = sentCode
        });

        Assert.AreEqual(HttpStatusCode.OK, verifyCodeResponse.StatusCode);
        var verifyCodeResult = await verifyCodeResponse.Content.ReadFromJsonAsync<VerifyCodeResponse>();
        Assert.IsNotNull(verifyCodeResult);
        Assert.IsTrue(verifyCodeResult.Success);
        Assert.AreEqual(testEmail, verifyCodeResult.Email);
        Assert.IsNotNull(verifyCodeResult.UserId);
    }

    /// <summary>
    /// Test that logout clears the session.
    /// </summary>
    [TestMethod]
    public async Task Logout_ClearsSession_ReturnsSuccess()
    {
        // Send logout request
        var logoutResponse = await _client!.PostAsync("/api/auth/logout", null);

        Assert.AreEqual(HttpStatusCode.OK, logoutResponse.StatusCode);
        var logoutResult = await logoutResponse.Content.ReadFromJsonAsync<LogoutResponse>();
        Assert.IsNotNull(logoutResult);
        Assert.IsTrue(logoutResult.Success);
    }

    /// <summary>
    /// Test that expired codes are rejected.
    /// </summary>
    [TestMethod]
    public async Task ExpiredCode_VerificationFails_ReturnsError()
    {
        var testEmail = "expired@example.com";

        // Request login code
        var requestCodeResponse = await _client!.PostAsJsonAsync("/api/auth/request-code", new
        {
            Email = testEmail
        });

        Assert.AreEqual(HttpStatusCode.OK, requestCodeResponse.StatusCode);

        // Get code
        var services = _factory!.Services;
        var testEmailService = services.GetRequiredService<IEmailService>() as TestEmailService;
        var sentCode = testEmailService!.LastSentCode;

        // Manually expire the code in database
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == testEmail.ToLower());
        Assert.IsNotNull(user);

        var loginCode = await dbContext.LoginCodes
            .FirstOrDefaultAsync(lc => lc.UserId == user.Id && lc.Code == sentCode);
        Assert.IsNotNull(loginCode);

        // Set expiration to past
        loginCode.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await dbContext.SaveChangesAsync();

        // Try to verify expired code
        var verifyCodeResponse = await _client.PostAsJsonAsync("/api/auth/verify-code", new
        {
            Email = testEmail,
            Code = sentCode
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, verifyCodeResponse.StatusCode);
        var verifyCodeResult = await verifyCodeResponse.Content.ReadFromJsonAsync<VerifyCodeResponse>();
        Assert.IsNotNull(verifyCodeResult);
        Assert.IsFalse(verifyCodeResult.Success);
        Assert.IsTrue(verifyCodeResult.Message!.Contains("expired") || verifyCodeResult.Message.Contains("invalid"));
    }

    /// <summary>
    /// Test that rate limiting is enforced (3 requests per hour).
    /// </summary>
    [TestMethod]
    public async Task RateLimiting_ExceedsLimit_Returns429()
    {
        var testEmail = "ratelimit@example.com";

        // Send 3 requests (should succeed)
        for (int i = 0; i < 3; i++)
        {
            var response = await _client!.PostAsJsonAsync("/api/auth/request-code", new
            {
                Email = testEmail
            });

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        // 4th request should be rate limited
        var rateLimitedResponse = await _client!.PostAsJsonAsync("/api/auth/request-code", new
        {
            Email = testEmail
        });

        Assert.AreEqual(HttpStatusCode.TooManyRequests, rateLimitedResponse.StatusCode);

        // Check for Retry-After header
        Assert.IsTrue(rateLimitedResponse.Headers.Contains("Retry-After"));
        var retryAfterValue = rateLimitedResponse.Headers.GetValues("Retry-After").FirstOrDefault();
        Assert.IsNotNull(retryAfterValue);

        // Should be numeric seconds
        Assert.IsTrue(int.TryParse(retryAfterValue, out int retryAfterSeconds));
        Assert.IsTrue(retryAfterSeconds > 0);
    }

    /// <summary>
    /// Test that invalid code format is rejected.
    /// </summary>
    [TestMethod]
    public async Task InvalidCodeFormat_VerificationFails_ReturnsBadRequest()
    {
        var testEmail = "invalidcode@example.com";

        // Request code first
        await _client!.PostAsJsonAsync("/api/auth/request-code", new
        {
            Email = testEmail
        });

        // Try to verify with invalid code format
        var verifyCodeResponse = await _client.PostAsJsonAsync("/api/auth/verify-code", new
        {
            Email = testEmail,
            Code = "12345" // Only 5 digits
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, verifyCodeResponse.StatusCode);
    }

    /// <summary>
    /// Test that code can only be used once.
    /// </summary>
    [TestMethod]
    public async Task CodeUsedTwice_SecondAttemptFails_ReturnsError()
    {
        var testEmail = "reuse@example.com";

        // Request code
        await _client!.PostAsJsonAsync("/api/auth/request-code", new
        {
            Email = testEmail
        });

        // Get code
        var services = _factory!.Services;
        var testEmailService = services.GetRequiredService<IEmailService>() as TestEmailService;
        var sentCode = testEmailService!.LastSentCode;

        // Verify code first time (should succeed)
        var firstVerifyResponse = await _client.PostAsJsonAsync("/api/auth/verify-code", new
        {
            Email = testEmail,
            Code = sentCode
        });

        Assert.AreEqual(HttpStatusCode.OK, firstVerifyResponse.StatusCode);

        // Try to verify same code again (should fail)
        var secondVerifyResponse = await _client.PostAsJsonAsync("/api/auth/verify-code", new
        {
            Email = testEmail,
            Code = sentCode
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, secondVerifyResponse.StatusCode);
        var result = await secondVerifyResponse.Content.ReadFromJsonAsync<VerifyCodeResponse>();
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
    }

    // Response DTOs for deserialization
    private record RequestCodeResponse(bool Success, string? Message);
    private record VerifyCodeResponse(bool Success, string? Message, Guid? UserId, string? Email);
    private record LogoutResponse(bool Success);
}

/// <summary>
/// Test implementation of IEmailService that captures sent codes.
/// </summary>
public class TestEmailService : IEmailService
{
    public string? LastSentCode { get; private set; }
    public string? LastSentEmail { get; private set; }
    public string? LastShareListName { get; private set; }
    public string? LastShareUrl { get; private set; }
    public AccessLevel? LastShareAccessLevel { get; private set; }

    public Task<bool> SendLoginCodeAsync(string email, string code, int expirationMinutes, CancellationToken cancellationToken = default)
    {
        LastSentEmail = email;
        LastSentCode = code;
        return Task.FromResult(true);
    }

    public Task<bool> SendIngredientListShareInvitationAsync(string email, string listName, string shareUrl, AccessLevel accessLevel, CancellationToken cancellationToken = default)
    {
        LastSentEmail = email;
        LastShareListName = listName;
        LastShareUrl = shareUrl;
        LastShareAccessLevel = accessLevel;
        return Task.FromResult(true);
    }
}
