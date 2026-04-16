# Code Examples Reference

Complete, working code examples from the RecipeManager project for AI agents to reference and adapt.

## Table of Contents
1. [Blazor Components](#blazor-components)
2. [API Endpoints](#api-endpoints)
3. [Service Configuration](#service-configuration)
4. [Testing](#testing)

## Blazor Components

### Simple Page Component (existing example)
```razor
@* RecipeManager.Web/Components/Pages/Counter.razor *@
@page "/counter"
@rendermode InteractiveServer

<PageTitle>Counter</PageTitle>

<h1>Counter</h1>

<p role="status">Current count: @currentCount</p>

<button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

@code {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
```

### Component with API Data (existing example)
```razor
@* RecipeManager.Web/Components/Pages/Weather.razor *@
@page "/weather"
@attribute [StreamRendering(true)]
@attribute [OutputCache(Duration = 5)]

@inject WeatherApiClient WeatherApi

<PageTitle>Weather</PageTitle>

<h1>Weather</h1>

<p>This component demonstrates showing data loaded from a backend API service.</p>

@if (forecasts == null)
{
    <p><em>Loading...</em></p>
}
else
{
    <table class="table">
        <thead>
            <tr>
                <th>Date</th>
                <th aria-label="Temperature in Celsius">Temp. (C)</th>
                <th aria-label="Temperature in Fahrenheit">Temp. (F)</th>
                <th>Summary</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var forecast in forecasts)
            {
                <tr>
                    <td>@forecast.Date.ToShortDateString()</td>
                    <td>@forecast.TemperatureC</td>
                    <td>@forecast.TemperatureF</td>
                    <td>@forecast.Summary</td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private WeatherForecast[]? forecasts;

    protected override async Task OnInitializedAsync()
    {
        forecasts = await WeatherApi.GetWeatherAsync();
    }
}
```

### API Client (existing example)
```csharp
// RecipeManager.Web/WeatherApiClient.cs
namespace RecipeManager.Web;

public class WeatherApiClient(HttpClient httpClient)
{
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<WeatherForecast>? forecasts = null;

        await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/weatherforecast", cancellationToken))
        {
            if (forecasts?.Count >= maxItems)
            {
                break;
            }
            if (forecast is not null)
            {
                forecasts ??= [];
                forecasts.Add(forecast);
            }
        }

        return forecasts?.ToArray() ?? [];
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

## API Endpoints

### Simple GET Endpoint (existing example)
```csharp
// RecipeManager.ApiService/Program.cs
app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");
```

### POST Endpoint with Validation
```csharp
app.MapPost("/api/recipes", async (Recipe recipe, IValidator<Recipe> validator) =>
{
    var validationResult = await validator.ValidateAsync(recipe);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    // Save to database
    // ...
    
    return Results.Created($"/api/recipes/{recipe.Id}", recipe);
})
.WithName("CreateRecipe")
.WithOpenApi();
```

### GET with Parameters
```csharp
app.MapGet("/api/recipes/{id:int}", async (int id, RecipeDbContext db) =>
{
    var recipe = await db.Recipes.FindAsync(id);
    return recipe is not null ? Results.Ok(recipe) : Results.NotFound();
})
.WithName("GetRecipeById")
.WithOpenApi();
```

## Service Configuration

### AppHost Configuration (existing)
```csharp
// RecipeManager.AppHost/AppHost.cs
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.RecipeManager_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.RecipeManager_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
```

### Web Service Configuration (existing)
```csharp
// RecipeManager.Web/Program.cs
using RecipeManager.Web;
using RecipeManager.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseOutputCache();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
```

### API Service Configuration (existing)
```csharp
// RecipeManager.ApiService/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Map endpoints here...

app.MapDefaultEndpoints();

app.Run();
```

## Authentication

### Protected Page
```razor
@* RecipeManager.Web/Components/Pages/Counter.razor *@
@page "/counter"
@attribute [Authorize]
@rendermode InteractiveServer

<PageTitle>Counter</PageTitle>

<h1>Counter</h1>

<p role="status">Current count: @currentCount</p>

<button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

@code {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
```

### Public Login Page
```razor
@* RecipeManager.Web/Components/Pages/Login.razor *@
@page "/login"
@attribute [AllowAnonymous]
@inject NavigationManager Navigation
@inject AuthApiClient AuthApi

<PageTitle>Login</PageTitle>

<div class="login-container">
    <div class="login-card">
        <h1>Welcome</h1>
        <p>Enter your email to receive a verification code</p>

        <EditForm Model="@loginModel" OnValidSubmit="HandleSubmit">
            <DataAnnotationsValidator />
            <ValidationSummary />

            <div class="form-group">
                <label for="email">Email Address</label>
                <InputText id="email" @bind-Value="loginModel.Email" 
                          class="form-control" type="email" autocomplete="email" />
                <ValidationMessage For="() => loginModel.Email" />
            </div>

            @if (!string.IsNullOrEmpty(errorMessage))
            {
                <div class="alert alert-danger">@errorMessage</div>
            }

            <button type="submit" class="btn btn-primary" disabled="@isSubmitting">
                @(isSubmitting ? "Sending..." : "Send Code")
            </button>
        </EditForm>
    </div>
</div>

@code {
    private LoginRequest loginModel = new();
    private string? errorMessage;
    private bool isSubmitting;

    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    private async Task HandleSubmit()
    {
        isSubmitting = true;
        errorMessage = null;

        try
        {
            var result = await AuthApi.RequestLoginCodeAsync(loginModel.Email);
            if (result.IsSuccess)
            {
                Navigation.NavigateTo($"/verify-code?email={Uri.EscapeDataString(loginModel.Email)}&returnUrl={Uri.EscapeDataString(ReturnUrl ?? "/")}");
            }
            else
            {
                errorMessage = result.Message ?? "Failed to send code";
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private class LoginRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = "";
    }
}
```

### Authentication API Client
```csharp
// RecipeManager.Web/Services/AuthApiClient.cs
namespace RecipeManager.Web.Services;

public class AuthApiClient(HttpClient httpClient, ILogger<AuthApiClient> logger)
{
    public async Task<AuthResponse> RequestLoginCodeAsync(string email)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/auth/request-code", new { email });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                return result ?? new AuthResponse { IsSuccess = false, Message = "Invalid response" };
            }

            var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return new AuthResponse { IsSuccess = false, Message = error?["message"] ?? "Request failed" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error requesting login code for {Email}", email);
            return new AuthResponse { IsSuccess = false, Message = "An error occurred" };
        }
    }

    public async Task<AuthResponse> VerifyCodeAsync(string email, string code)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/auth/verify-code", new { email, code });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                return result ?? new AuthResponse { IsSuccess = false, Message = "Invalid response" };
            }

            var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return new AuthResponse { IsSuccess = false, Message = error?["message"] ?? "Verification failed" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying code for {Email}", email);
            return new AuthResponse { IsSuccess = false, Message = "An error occurred" };
        }
    }

    public async Task<AuthResponse> LogoutAsync()
    {
        try
        {
            var response = await httpClient.PostAsync("/api/auth/logout", null);
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            return result ?? new AuthResponse { IsSuccess = false, Message = "Logout failed" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error logging out");
            return new AuthResponse { IsSuccess = false, Message = "An error occurred" };
        }
    }
}
```

### Navigation with User Display
```razor
@* RecipeManager.Web/Components/Layout/NavMenu.razor *@
<div class="nav-scrollable" onclick="document.querySelector('.navbar-toggler').click()">
    <nav class="flex-column">
        <AuthorizeView>
            <Authorized>
                <div class="nav-item px-3">
                    <span class="user-greeting">Hello, @context.User.Identity?.Name</span>
                </div>
                <div class="nav-item px-3">
                    <NavLink class="nav-link" href="/" Match="NavLinkMatch.All">
                        <span class="bi bi-house-door-fill-nav-menu" aria-hidden="true"></span> Home
                    </NavLink>
                </div>
                <div class="nav-item px-3">
                    <NavLink class="nav-link" href="counter">
                        <span class="bi bi-plus-square-fill-nav-menu" aria-hidden="true"></span> Counter
                    </NavLink>
                </div>
                <div class="nav-item px-3">
                    <button class="btn btn-link nav-link" @onclick="HandleLogout">
                        <span class="bi bi-box-arrow-right" aria-hidden="true"></span> Logout
                    </button>
                </div>
            </Authorized>
            <NotAuthorized>
                <div class="nav-item px-3">
                    <NavLink class="nav-link" href="/login">
                        <span class="bi bi-box-arrow-in-right" aria-hidden="true"></span> Login
                    </NavLink>
                </div>
            </NotAuthorized>
        </AuthorizeView>
    </nav>
</div>

@code {
    [Inject] private AuthenticationService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private async Task HandleLogout()
    {
        await AuthService.SignOutAsync();
        Navigation.NavigateTo("/", forceLoad: true);
    }
}
```

### Authentication API Endpoints
```csharp
// RecipeManager.ApiService/Program.cs

// Request verification code
app.MapPost("/api/auth/request-code", async (RequestCodeRequest request, IAuthService authService) =>
{
    if (string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest(new { message = "Email is required" });
    }

    var result = await authService.RequestLoginCodeAsync(request.Email);

    return result.IsSuccess 
        ? Results.Ok(new { message = result.Message }) 
        : Results.BadRequest(new { message = result.Message });
})
.WithName("RequestCode")
.WithOpenApi();

// Verify code
app.MapPost("/api/auth/verify-code", async (VerifyCodeRequest request, IAuthService authService, HttpContext httpContext) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
    {
        return Results.BadRequest(new { message = "Email and code are required" });
    }

    var result = await authService.VerifyCodeAsync(request.Email, request.Code);

    if (!result.IsSuccess)
    {
        return Results.BadRequest(new { message = result.Message });
    }

    // Create authentication cookie
    httpContext.Response.Cookies.Append("auth_token", result.Data!, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = DateTimeOffset.UtcNow.AddDays(30)
    });

    return Results.Ok(new { message = "Authenticated successfully" });
})
.WithName("VerifyCode")
.WithOpenApi();

// Logout
app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    httpContext.Response.Cookies.Delete("auth_token");
    return Results.Ok(new { message = "Logged out successfully" });
})
.WithName("Logout")
.WithOpenApi();
```

## Testing

### Integration Test (existing)
```csharp
// RecipeManager.Tests/WebTests.cs
using Microsoft.Extensions.Logging;

namespace RecipeManager.Tests;

[TestClass]
public class WebTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    [TestMethod]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource(DefaultTimeout).Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.RecipeManager_AppHost>(cancellationToken);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        var httpClient = app.CreateHttpClient("webfrontend");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("webfrontend", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        var response = await httpClient.GetAsync("/", cancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
```

## Component Imports (existing)
```razor
@* RecipeManager.Web/Components/_Imports.razor *@
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.AspNetCore.OutputCaching
@using Microsoft.JSInterop
@using RecipeManager.Web
@using RecipeManager.Web.Components
```

## Service Defaults (existing patterns)
```csharp
// RecipeManager.ServiceDefaults/Extensions.cs - Key excerpts

// Adding service defaults in any service
builder.AddServiceDefaults();

// This provides:
// - OpenTelemetry (metrics, traces, logs)
// - Health checks (/health, /alive)
// - Service discovery
// - HTTP client resilience

// Mapping default endpoints
app.MapDefaultEndpoints();

// This maps:
// - /health - All health checks
// - /alive - Only "live" tagged checks
// (Only in Development for security)
```

## Usage Instructions for AI

When a developer asks to:
- **Add a new page**: Use the Blazor component examples, follow the pattern in Weather.razor
- **Add API endpoint**: Follow the MapGet/MapPost patterns from ApiService
- **Connect services**: Reference the AppHost.cs pattern with .WithReference() and .WaitFor()
- **Add tests**: Use the integration test pattern from WebTests.cs
- **Add HTTP client**: Follow the WeatherApiClient pattern with primary constructor
