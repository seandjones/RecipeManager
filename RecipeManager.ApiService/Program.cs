using Microsoft.EntityFrameworkCore;
using RecipeManager.ApiService.Data;
using RecipeManager.ApiService.Models;
using RecipeManager.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
// Temporarily disabled for local development without Docker
// builder.AddServiceDefaults();

// Add database context with local PostgreSQL
// builder.AddNpgsqlDbContext<AuthDbContext>("recipedb");
var connectionString = builder.Configuration.GetConnectionString("recipedb") 
    ?? "Host=localhost;Port=5432;Database=recipedb;Username=recipeuser;Password=recipe_dev_password";
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure email service
builder.Services.Configure<SendGridOptions>(builder.Configuration.GetSection("SendGrid"));

// Register email service based on environment
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEmailService, DevelopmentEmailService>();
}
else
{
    builder.Services.AddSingleton<IEmailService, SendGridEmailService>();
}

// Register authentication service
builder.Services.AddScoped<IAuthService, AuthService>();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "API service is running.");

// Authentication endpoints
var authGroup = app.MapGroup("/api/auth")
    .WithTags("Authentication")
    .WithOpenApi();

authGroup.MapPost("/request-code", async (
    RequestLoginCodeRequest request,
    IAuthService authService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    // Validate email
    if (string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest(new { error = "Email is required." });
    }

    var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
    if (!emailValidator.IsValid(request.Email))
    {
        return Results.BadRequest(new { error = "Invalid email format." });
    }

    var result = await authService.RequestLoginCodeAsync(request.Email, cancellationToken);

    if (!result.Success)
    {
        if (result.RetryAfterSeconds.HasValue)
        {
            // Rate limit exceeded
            httpContext.Response.Headers["Retry-After"] = result.RetryAfterSeconds.Value.ToString();
            return Results.Json(
                new { error = result.Message, retryAfter = result.RetryAfterSeconds },
                statusCode: 429);
        }

        return Results.BadRequest(new { error = result.Message });
    }

    return Results.Ok(new { message = result.Message });
})
.WithName("RequestLoginCode")
.WithSummary("Request a login code")
.WithDescription("Sends a 6-digit login code to the specified email address. Rate limited to 3 requests per hour per email.")
.Produces(200)
.Produces(400)
.Produces(429);

authGroup.MapPost("/verify-code", async (
    VerifyLoginCodeRequest request,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    // Validate inputs
    if (string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest(new { error = "Email is required." });
    }

    if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length != 6 || !request.Code.All(char.IsDigit))
    {
        return Results.BadRequest(new { error = "Invalid code format. Code must be 6 digits." });
    }

    var result = await authService.VerifyLoginCodeAsync(request.Email, request.Code, cancellationToken);

    if (!result.Success)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        message = result.Message,
        userId = result.UserId,
        email = result.Email
    });
})
.WithName("VerifyLoginCode")
.WithSummary("Verify a login code")
.WithDescription("Verifies a 6-digit login code for the specified email address. Returns user information on success.")
.Produces(200)
.Produces(400)
.Produces(401);

authGroup.MapPost("/logout", () =>
{
    // In this passwordless system, logout is handled client-side
    // The client should clear their session/cookie
    // This endpoint exists for consistency and future expansion
    return Results.Ok(new { message = "Logout successful." });
})
.WithName("Logout")
.WithSummary("Logout")
.WithDescription("Invalidates the current session. In passwordless authentication, this is primarily a client-side operation.")
.Produces(200);

// Temporarily disabled for local development without Docker
// app.MapDefaultEndpoints();

app.Run();

// Make Program class accessible for testing
namespace RecipeManager.ApiService
{
    public partial class Program { }
}
