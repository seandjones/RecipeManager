using RecipeManager.ApiService.Data;

namespace RecipeManager.ApiService.Services;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a login code to the specified email address
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="code">6-digit login code</param>
    /// <param name="expiresInMinutes">Number of minutes until code expires</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email was sent successfully, false otherwise</returns>
    Task<bool> SendLoginCodeAsync(string email, string code, int expiresInMinutes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an ingredient list share invitation email.
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="listName">Name of the shared ingredient list</param>
    /// <param name="shareUrl">Share URL containing the token</param>
    /// <param name="accessLevel">Access level granted by the share</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email was sent successfully, false otherwise</returns>
    Task<bool> SendIngredientListShareInvitationAsync(string email, string listName, string shareUrl, AccessLevel accessLevel, CancellationToken cancellationToken = default);
}
