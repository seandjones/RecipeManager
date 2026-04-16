namespace RecipeManager.ApiService.Models;

/// <summary>
/// Request model for requesting a login code
/// </summary>
public record RequestLoginCodeRequest
{
    public string Email { get; init; } = string.Empty;
}

/// <summary>
/// Response model for requesting a login code
/// </summary>
public record RequestLoginCodeResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public int? RetryAfterSeconds { get; init; }
}

/// <summary>
/// Request model for verifying a login code
/// </summary>
public record VerifyLoginCodeRequest
{
    public string Email { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}

/// <summary>
/// Response model for verifying a login code
/// </summary>
public record VerifyLoginCodeResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public Guid? UserId { get; init; }
    public string? Email { get; init; }
}
