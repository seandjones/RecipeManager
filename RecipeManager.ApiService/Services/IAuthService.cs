using RecipeManager.ApiService.Data;
using RecipeManager.ApiService.Models;

namespace RecipeManager.ApiService.Services;

/// <summary>
/// Service for handling authentication operations
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Requests a login code for the specified email address
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure with retry information</returns>
    Task<RequestLoginCodeResponse> RequestLoginCodeAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a login code for the specified email address
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="code">6-digit login code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure with user information</returns>
    Task<VerifyLoginCodeResponse> VerifyLoginCodeAsync(string email, string code, CancellationToken cancellationToken = default);
}
