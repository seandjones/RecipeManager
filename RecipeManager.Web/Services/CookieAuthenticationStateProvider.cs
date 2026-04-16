using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using System.Security.Claims;

namespace RecipeManager.Web.Services;

/// <summary>
/// Custom authentication state provider that integrates with cookie authentication
/// </summary>
public class CookieAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    public CookieAuthenticationStateProvider(
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
    }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        // Return whether the user is still authenticated
        // In a real application, you might want to check if the user still exists in the database
        return Task.FromResult(authenticationState.User.Identity?.IsAuthenticated ?? false);
    }
}
