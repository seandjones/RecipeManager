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
}
