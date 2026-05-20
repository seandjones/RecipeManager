using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace RecipeManager.Web.Services;

public sealed class CurrentUserHeaderHandler(
    AuthenticationStateProvider authenticationStateProvider,
    IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains("X-User-Id"))
        {
            // Prefer HttpContext claims when available and fall back to Blazor auth state.
            var userId = ResolveCurrentUserId(httpContextAccessor.HttpContext?.User);

            if (!userId.HasValue)
            {
                var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
                userId = ResolveCurrentUserId(authenticationState.User);
            }

            if (userId.HasValue)
            {
                request.Headers.TryAddWithoutValidation("X-User-Id", userId.Value.ToString());
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private static Guid? ResolveCurrentUserId(ClaimsPrincipal? user)
    {
        var claimValue = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst("userId")?.Value
            ?? user?.FindFirst("sub")?.Value;

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}