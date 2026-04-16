using RecipeManager.Web.Models;
using System.Net;
using System.Net.Http.Json;

namespace RecipeManager.Web.Services;

/// <summary>
/// Client for calling authentication API endpoints
/// </summary>
public class AuthApiClient(HttpClient httpClient)
{
    /// <summary>
    /// Requests a login code to be sent to the specified email address
    /// </summary>
    /// <param name="email">Email address to send code to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating success/failure and retry information</returns>
    public async Task<RequestLoginCodeResponse> RequestLoginCodeAsync(
        string email, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new RequestLoginCodeRequest { Email = email };
            var response = await httpClient.PostAsJsonAsync(
                "/api/auth/request-code", 
                request, 
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RequestLoginCodeResponse>(cancellationToken);
                return result ?? new RequestLoginCodeResponse 
                { 
                    Success = false, 
                    Message = "Invalid response from server" 
                };
            }

            // Handle rate limiting (429 Too Many Requests)
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds;
                var errorResult = await response.Content.ReadFromJsonAsync<RequestLoginCodeResponse>(cancellationToken);
                
                return new RequestLoginCodeResponse
                {
                    Success = false,
                    Message = errorResult?.Message ?? "Too many requests. Please try again later.",
                    RetryAfterSeconds = retryAfter.HasValue ? (int)retryAfter.Value : errorResult?.RetryAfterSeconds
                };
            }

            // Handle other error responses
            var errorResponse = await response.Content.ReadFromJsonAsync<RequestLoginCodeResponse>(cancellationToken);
            return errorResponse ?? new RequestLoginCodeResponse 
            { 
                Success = false, 
                Message = $"Request failed with status code {response.StatusCode}" 
            };
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions to allow proper cancellation handling
            throw;
        }
        catch (HttpRequestException ex)
        {
            return new RequestLoginCodeResponse 
            { 
                Success = false, 
                Message = $"Network error: {ex.Message}" 
            };
        }
        catch (Exception ex)
        {
            return new RequestLoginCodeResponse 
            { 
                Success = false, 
                Message = $"An error occurred: {ex.Message}" 
            };
        }
    }

    /// <summary>
    /// Verifies a login code for the specified email address
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="code">6-digit login code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating success/failure and user information</returns>
    public async Task<VerifyLoginCodeResponse> VerifyCodeAsync(
        string email, 
        string code, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new VerifyLoginCodeRequest { Email = email, Code = code };
            var response = await httpClient.PostAsJsonAsync(
                "/api/auth/verify-code", 
                request, 
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<VerifyLoginCodeResponse>(cancellationToken);
                return result ?? new VerifyLoginCodeResponse 
                { 
                    Success = false, 
                    Message = "Invalid response from server" 
                };
            }

            // Handle error responses
            var errorResponse = await response.Content.ReadFromJsonAsync<VerifyLoginCodeResponse>(cancellationToken);
            return errorResponse ?? new VerifyLoginCodeResponse 
            { 
                Success = false, 
                Message = $"Request failed with status code {response.StatusCode}" 
            };
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions to allow proper cancellation handling
            throw;
        }
        catch (HttpRequestException ex)
        {
            return new VerifyLoginCodeResponse 
            { 
                Success = false, 
                Message = $"Network error: {ex.Message}" 
            };
        }
        catch (Exception ex)
        {
            return new VerifyLoginCodeResponse 
            { 
                Success = false, 
                Message = $"An error occurred: {ex.Message}" 
            };
        }
    }

    /// <summary>
    /// Logs out the current user (placeholder for future implementation)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if logout was successful</returns>
    public async Task<bool> LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsync(
                "/api/auth/logout", 
                null, 
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            // If logout fails, we'll still consider it successful
            // since the frontend can clear its own state
            return true;
        }
    }
}
