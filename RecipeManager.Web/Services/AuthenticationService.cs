using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace RecipeManager.Web.Services;

/// <summary>
/// Service for handling authentication operations (login/logout)
/// </summary>
public class AuthenticationService(IHttpContextAccessor httpContextAccessor)
{
    /// <summary>
    /// Signs in a user with the specified user ID and email
    /// </summary>
    public async Task SignInAsync(Guid userId, string email)
    {
        var httpContext = httpContextAccessor.HttpContext 
            ?? throw new InvalidOperationException("HttpContext is not available");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, email)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            claimsPrincipal,
            authProperties);
    }

    /// <summary>
    /// Signs out the current user
    /// </summary>
    public async Task SignOutAsync()
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Gets the current user ID from claims
    /// </summary>
    public Guid? GetCurrentUserId()
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userIdClaim = httpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets the current user email from claims
    /// </summary>
    public string? GetCurrentUserEmail()
    {
        var httpContext = httpContextAccessor.HttpContext;
        return httpContext?.User.FindFirst(ClaimTypes.Email)?.Value;
    }
}
