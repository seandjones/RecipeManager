using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecipeManager.Web.Services;
using System.Net;
using System.Security.Claims;

namespace RecipeManager.Tests;

[TestClass]
public class CurrentUserHeaderHandlerTests
{
    [TestMethod]
    public async Task SendAsync_WithAuthenticatedUser_AddsXUserIdHeader()
    {
        var userId = Guid.NewGuid();
        var authenticationStateProvider = new TestAuthenticationStateProvider(
            new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                authenticationType: "Cookies")));
        var httpContextAccessor = new HttpContextAccessor();

        HttpRequestMessage? capturedRequest = null;
        var handler = new CurrentUserHeaderHandler(authenticationStateProvider, httpContextAccessor)
        {
            InnerHandler = new CaptureHandler(request =>
            {
                capturedRequest = request;
                return new HttpResponseMessage(HttpStatusCode.OK);
            })
        };

        using var client = new HttpClient(handler);

        await client.PostAsync("https://example.test/api/ingredient-lists", new StringContent("{}"));

        Assert.IsNotNull(capturedRequest);
        Assert.IsTrue(capturedRequest!.Headers.TryGetValues("X-User-Id", out var values));
        Assert.AreEqual(userId.ToString(), values.First());
    }

    [TestMethod]
    public async Task SendAsync_WithHttpContextUser_AddsXUserIdHeaderFromHttpContext()
    {
        var userId = Guid.NewGuid();
        var authenticationStateProvider = new TestAuthenticationStateProvider(new ClaimsPrincipal(new ClaimsIdentity()));

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                    authenticationType: "Cookies"))
            }
        };

        HttpRequestMessage? capturedRequest = null;
        var handler = new CurrentUserHeaderHandler(authenticationStateProvider, httpContextAccessor)
        {
            InnerHandler = new CaptureHandler(request =>
            {
                capturedRequest = request;
                return new HttpResponseMessage(HttpStatusCode.OK);
            })
        };

        using var client = new HttpClient(handler);

        await client.PostAsync("https://example.test/api/ingredient-lists", new StringContent("{}"));

        Assert.IsNotNull(capturedRequest);
        Assert.IsTrue(capturedRequest!.Headers.TryGetValues("X-User-Id", out var values));
        Assert.AreEqual(userId.ToString(), values.First());
    }

    private sealed class TestAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(user));
    }

    private sealed class CaptureHandler(Func<HttpRequestMessage, HttpResponseMessage> handleRequest) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handleRequest(request));
    }
}