using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecipeManager.ApiService.Data;
using RecipeManager.ApiService.Models;

namespace RecipeManager.ApiService.Services;

/// <summary>
/// Authentication service implementation with rate limiting
/// </summary>
public class AuthService(
    AuthDbContext dbContext,
    IEmailService emailService,
    ILogger<AuthService> logger) : IAuthService
{
    private const int CodeExpirationMinutes = 15;
    private const int RateLimitHours = 1;
    private const int MaxRequestsPerHour = 3;

    public async Task<RequestLoginCodeResponse> RequestLoginCodeAsync(string email, CancellationToken cancellationToken = default)
    {
        // Normalize email
        email = email.Trim().ToLowerInvariant();

        // Check rate limiting
        var oneHourAgo = DateTime.UtcNow.AddHours(-RateLimitHours);
        var recentCodesCount = await dbContext.LoginCodes
            .Include(lc => lc.User)
            .Where(lc => lc.User.Email == email && lc.CreatedAt >= oneHourAgo)
            .CountAsync(cancellationToken);

        if (recentCodesCount >= MaxRequestsPerHour)
        {
            // Find the oldest code in the last hour to calculate retry time
            var oldestCode = await dbContext.LoginCodes
                .Include(lc => lc.User)
                .Where(lc => lc.User.Email == email && lc.CreatedAt >= oneHourAgo)
                .OrderBy(lc => lc.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var retryAfter = oldestCode != null
                ? (int)Math.Ceiling((oldestCode.CreatedAt.AddHours(RateLimitHours) - DateTime.UtcNow).TotalSeconds)
                : 3600;

            logger.LogWarning("Rate limit exceeded for email {Email}. Recent requests: {Count}", email, recentCodesCount);

            return new RequestLoginCodeResponse
            {
                Success = false,
                Message = $"Too many login attempts. Please try again in {Math.Ceiling(retryAfter / 60.0)} minutes.",
                RetryAfterSeconds = retryAfter
            };
        }

        // Find or create user
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.Users.Add(user);
        }

        // Generate 6-digit code
        var code = GenerateCode();

        // Create login code
        var loginCode = new LoginCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(CodeExpirationMinutes),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.LoginCodes.Add(loginCode);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Send email
        var emailSent = await emailService.SendLoginCodeAsync(email, code, CodeExpirationMinutes, cancellationToken);

        if (!emailSent)
        {
            logger.LogError("Failed to send login code email to {Email}", email);
            return new RequestLoginCodeResponse
            {
                Success = false,
                Message = "Failed to send email. Please try again later."
            };
        }

        logger.LogInformation("Login code sent to {Email}", email);

        return new RequestLoginCodeResponse
        {
            Success = true,
            Message = "Login code sent to your email address."
        };
    }

    public async Task<VerifyLoginCodeResponse> VerifyLoginCodeAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        // Normalize inputs
        email = email.Trim().ToLowerInvariant();
        code = code.Trim();

        // Find user
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null)
        {
            logger.LogWarning("Login attempt for non-existent user: {Email}", email);
            return new VerifyLoginCodeResponse
            {
                Success = false,
                Message = "Invalid email or code."
            };
        }

        // Find the most recent unused, non-expired code
        var loginCode = await dbContext.LoginCodes
            .Where(lc => lc.UserId == user.Id &&
                         lc.Code == code &&
                         !lc.IsUsed &&
                         lc.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(lc => lc.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (loginCode == null)
        {
            // Check if code exists but is expired or used
            var existingCode = await dbContext.LoginCodes
                .Where(lc => lc.UserId == user.Id && lc.Code == code)
                .OrderByDescending(lc => lc.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingCode != null)
            {
                if (existingCode.IsUsed)
                {
                    logger.LogWarning("Attempt to use already-used code for {Email}", email);
                    return new VerifyLoginCodeResponse
                    {
                        Success = false,
                        Message = "This code has already been used."
                    };
                }

                if (existingCode.ExpiresAt <= DateTime.UtcNow)
                {
                    logger.LogWarning("Attempt to use expired code for {Email}", email);
                    return new VerifyLoginCodeResponse
                    {
                        Success = false,
                        Message = "This code has expired. Please request a new one."
                    };
                }
            }

            logger.LogWarning("Invalid code attempt for {Email}", email);
            return new VerifyLoginCodeResponse
            {
                Success = false,
                Message = "Invalid email or code."
            };
        }

        // Mark code as used
        loginCode.IsUsed = true;
        user.LastLoginAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successful login for {Email}", email);

        return new VerifyLoginCodeResponse
        {
            Success = true,
            Message = "Login successful.",
            UserId = user.Id,
            Email = user.Email
        };
    }

    private static string GenerateCode()
    {
        // Generate a random 6-digit code
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}
