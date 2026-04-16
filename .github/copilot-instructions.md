# RecipeManager - .NET Aspire Project Instructions

## Architecture Overview

This is a **.NET 10 Aspire distributed application** with a microservices architecture:

- **RecipeManager.AppHost**: Aspire orchestrator that defines service topology, dependencies, and infrastructure
- **RecipeManager.Web**: Blazor Server frontend with Interactive Server Components
- **RecipeManager.ApiService**: Minimal API backend service
- **RecipeManager.ServiceDefaults**: Shared library with Aspire service defaults (telemetry, health checks, resilience)
- **RecipeManager.Tests**: Integration tests using Aspire.Hosting.Testing

### Service Communication
- Web frontend calls API service using service discovery via `https+http://apiservice` scheme
- Redis is used for output caching in the web frontend
- All inter-service communication uses HttpClient with automatic service discovery and resilience patterns

## Key Conventions

### Service Dependencies Pattern
In `AppHost.cs`, services are orchestrated with explicit dependencies and health checks:
```csharp
var cache = builder.AddRedis("cache");
var apiService = builder.AddProject<Projects.RecipeManager_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.RecipeManager_Web>("webfrontend")
    .WithReference(cache)
    .WaitFor(cache)  // Ensures cache starts before web
    .WithReference(apiService)
    .WaitFor(apiService);
```

### Service Defaults Pattern
All service projects (Web, ApiService) call `builder.AddServiceDefaults()` in Program.cs to get:
- OpenTelemetry instrumentation (metrics, tracing, logging)
- Health checks (`/health`, `/alive`)
- Service discovery with resilience handlers
- HTTP client defaults with retry policies

### Blazor Component Conventions
- Use `@attribute [StreamRendering(true)]` for streaming responses
- Use `@attribute [OutputCache(Duration = 5)]` with Redis caching
- HttpClient dependencies injected as typed clients (e.g., `WeatherApiClient`)
- Interactive components use `InteractiveServer` render mode

### API Client Pattern
Typed HttpClient wrappers with service discovery:
```csharp
builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
```
Clients use `GetFromJsonAsAsyncEnumerable` for streaming responses.

## Development Workflows

### Running the Application
1. **Set RecipeManager.AppHost as startup project** (F5 in Visual Studio)
2. Aspire Dashboard opens automatically showing all services, logs, traces, and metrics
3. Services start in dependency order (Redis → ApiService → Web)

### Testing
- Integration tests in `RecipeManager.Tests` use `DistributedApplicationTestingBuilder`
- Tests spin up the full Aspire app with orchestration
- Use `app.ResourceNotifications.WaitForResourceHealthyAsync()` before HTTP calls
- Example: `WebTests.GetWebResourceRootReturnsOkStatusCode()`

### Adding New Services
1. Create new project and add reference to `RecipeManager.ServiceDefaults`
2. Call `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()` in Program.cs
3. Register in `AppHost.cs` with `.AddProject<Projects.YourService>()`
4. Define dependencies with `.WithReference()` and `.WaitFor()`

### Health Checks
- Health endpoints only exposed in Development (security concern)
- `/health`: All checks must pass (readiness)
- `/alive`: Only "live" tagged checks (liveness)

## Authentication Patterns

### Protecting Pages

Use `@attribute [Authorize]` on pages that require authentication:

```razor
@page "/counter"
@attribute [Authorize]
@rendermode InteractiveServer

<PageTitle>Counter</PageTitle>
<h1>Counter</h1>
```

### Public Pages

Use `@attribute [AllowAnonymous]` for pages accessible without authentication:

```razor
@page "/login"
@attribute [AllowAnonymous]
@inject NavigationManager Navigation
@inject AuthApiClient AuthApi

<h1>Login</h1>
```

### Authentication API Client

Use `AuthApiClient` to call authentication endpoints:

```csharp
builder.Services.AddHttpClient<AuthApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
```

Client methods:
- `RequestLoginCodeAsync(email)` - Request verification code
- `VerifyCodeAsync(email, code)` - Verify code and authenticate
- `LogoutAsync()` - Sign out user

### Authentication State

Use `CookieAuthenticationStateProvider` and `AuthenticationService`:

```csharp
builder.Services.AddScoped<CookieAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => 
    sp.GetRequiredService<CookieAuthenticationStateProvider>());
builder.Services.AddScoped<AuthenticationService>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();
```

### Conditional UI with AuthorizeView

Show different UI based on authentication status:

```razor
<AuthorizeView>
    <Authorized>
        <span class="user-email">@context.User.Identity?.Name</span>
        <button @onclick="HandleLogout">Logout</button>
    </Authorized>
    <NotAuthorized>
        <NavLink href="/login">Login</NavLink>
    </NotAuthorized>
</AuthorizeView>
```

### Sign In/Out Pattern

Use `AuthenticationService` for authentication operations:

```csharp
@inject AuthenticationService AuthService
@inject NavigationManager Navigation

private async Task HandleSignIn(string email)
{
    await AuthService.SignInAsync(email);
    Navigation.NavigateTo("/", forceLoad: true);
}

private async Task HandleLogout()
{
    await AuthService.SignOutAsync();
    Navigation.NavigateTo("/", forceLoad: true);
}
```

**Important**: Use `forceLoad: true` after authentication state changes to refresh the authentication context.

### Navigation with Return URLs

Preserve return URLs when redirecting to login:

```csharp
var returnUrl = Navigation.ToBaseRelativePath(Navigation.Uri);
Navigation.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
```

### Database Context Pattern

Authentication uses `AuthDbContext` with PostgreSQL:

```csharp
builder.AddNpgsqlDbContext<AuthDbContext>("recipedb");
```

Entities: `User`, `LoginCode`

### Rate Limiting Pattern

Authentication endpoints implement rate limiting (3 requests/hour):

```csharp
var recentCodes = await dbContext.LoginCodes
    .Where(lc => lc.Email == email && lc.CreatedAt > DateTime.UtcNow.AddHours(-1))
    .CountAsync();

if (recentCodes >= 3)
{
    return Results.BadRequest(new { message = "Too many requests. Please try again later." });
}
```

## Project-Specific Details

- **Target Framework**: .NET 10
- **Aspire Version**: 13.1.0
- **Blazor Render Mode**: Interactive Server (not WebAssembly or Auto)
- **Authentication**: Passwordless email verification (6-digit codes, 15-minute expiration, 30-day cookie)
- **Database**: PostgreSQL 18 with EF Core (AuthDbContext: Users, LoginCodes)
- **Caching**: Redis via Aspire.StackExchange.Redis.OutputCaching
- **API Style**: Minimal APIs with OpenAPI/Swagger in development
- **Test Framework**: MSTest with MSTestRunner (new executable test projects)
- **Nullable**: Enabled across all projects
