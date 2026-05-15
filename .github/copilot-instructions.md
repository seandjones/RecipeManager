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

## Ingredient Lists & Real-Time Synchronization

### Data Model

Ingredient Lists enable users to create shopping/cooking lists with associated recipes and shared collaboration:

**Core Entities:**
- `IngredientList`: Name, Description, OwnerId, CreatedAt, UpdatedAt
- `Ingredient`: IngredientListId, Name, Quantity, Unit, IsChecked, CreatedAt
- `RecipeIngredientList`: Junction table (N:N) linking recipes to ingredient lists
- `ListSharing`: IngredientListId, SharedWithUserId, ShareType (Email|Link), AccessLevel (Viewer|Editor), CreatedAt
- `ListShareToken`: IngredientListId, Token (Guid), ExpiresAt, AccessLevel

**Access Levels:**
- **Owner**: Full control (create, modify, delete, share, revoke)
- **Editor**: Can modify ingredients/recipes and checkbox state in real-time
- **Viewer**: Read-only access, cannot modify items

### SignalR Integration Pattern

Real-time updates across connected clients use `IngredientListHub` with group-based broadcasting:

```csharp
// In Program.cs
app.MapHub<IngredientListHub>("/hubs/ingredient-list");

// Hub interface for client callbacks
public interface IIngredientListClient
{
    Task OnIngredientAdded(Guid listId, Ingredient ingredient);
    Task OnIngredientRemoved(Guid listId, Guid ingredientId);
    Task OnIngredientUpdated(Guid listId, Ingredient ingredient);
    Task OnIngredientCheckStateUpdated(Guid listId, Guid ingredientId, bool isChecked);
    Task OnRecipeAdded(Guid listId, int recipeId);
    Task OnRecipeRemoved(Guid listId, int recipeId);
}

// Hub methods for clients to send updates
public class IngredientListHub(IngredientListDbContext dbContext) : Hub<IIngredientListClient>
{
    public async Task JoinListGroup(Guid listId)
    {
        await EnsureUserHasAccess(listId);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"ingredient-list-{listId}");
    }

    public async Task UpdateIngredientCheckState(Guid listId, Guid ingredientId, bool isChecked)
    {
        // Verify access, update DB, then broadcast to group
        await EnsureUserHasAccess(listId);
        var ingredient = await dbContext.Ingredients
            .FirstOrDefaultAsync(i => i.Id == ingredientId && i.IngredientListId == listId);
        
        ingredient.IsChecked = isChecked;
        await dbContext.SaveChangesAsync();
        
        await Clients.Group($"ingredient-list-{listId}")
            .OnIngredientCheckStateUpdated(listId, ingredientId, isChecked);
    }
}
```

**Authorization in Hub:**
- All hub methods call `EnsureUserHasAccess(listId)` which verifies:
  - User owns the list, OR
  - User has an active `ListSharing` entry with the list
- Throws `HubException` if unauthorized

### Real-Time Synchronization Pattern

In the Blazor component, inject `IngredientListSignalRClient` to manage the SignalR connection lifecycle:

```csharp
@inject IngredientListSignalRClient SignalRClient

protected override async Task OnInitializedAsync()
{
    // Load list data
    list = await IngredientListApi.GetListAsync(Id);
    
    // Subscribe to real-time events
    SignalRClient.OnIngredientAdded += HandleIngredientAdded;
    SignalRClient.OnIngredientRemoved += HandleIngredientRemoved;
    SignalRClient.OnIngredientCheckStateUpdated += HandleCheckStateUpdated;
    
    // Join the list's SignalR group
    await SignalRClient.InitializeAsync(Id);
}

private async Task HandleCheckStateUpdated(Guid listId, Guid ingredientId, bool isChecked)
{
    if (listId != Id) return;
    
    var ingredient = list.Ingredients.FirstOrDefault(i => i.Id == ingredientId);
    if (ingredient != null)
    {
        ingredient.IsChecked = isChecked;
        StateHasChanged();
    }
}

async ValueTask IAsyncDisposable.DisposeAsync()
{
    SignalRClient.OnIngredientAdded -= HandleIngredientAdded;
    SignalRClient.OnIngredientRemoved -= HandleIngredientRemoved;
    SignalRClient.OnIngredientCheckStateUpdated -= HandleCheckStateUpdated;
    
    await SignalRClient.DisconnectAsync(Id);
}
```

**Client Connection Management:**
- `InitializeAsync(listId)`: Establish connection and join group with exponential backoff on failure
- `DisconnectAsync(listId)`: Leave group and close connection
- Automatic reconnection on disconnect with backoff strategy

### Sharing & Authorization Pattern

**Email Invitation Flow:**
```csharp
// POST /api/ingredient-lists/{id}/share/email
var shareResponse = await _client.PostAsJsonAsync($"/api/ingredient-lists/{listId}/share/email", new
{
    Email = "friend@example.com",
    AccessLevel = "Editor"
});
// Creates ListSharing entry and sends email invitation with share URL
```

**Shareable Link Flow:**
```csharp
// POST /api/ingredient-lists/{id}/share/link
var linkResponse = await _client.PostAsJsonAsync($"/api/ingredient-lists/{listId}/share/link", new
{
    AccessLevel = "Viewer",
    ExpiresInDays = 7
});
// Returns: { Token: Guid, Url: string, AccessLevel: string, ExpiresAt: DateTime }

// GET /api/ingredient-lists/shared/{token}
// No authentication required; token grants temporary access (read-only if Viewer)
```

**Authorization Checks:**
- **Owner-only endpoints**: Update list, delete list, share settings, revoke shares
  - `HasIngredientListAccessAsync(listId)` — owner OR shared user
  - Rejects with 403 if unauthorized
- **Write-restricted endpoints**: Add/modify/delete ingredients
  - `HasIngredientListWriteAccessAsync(listId)` — owner OR Editor-level shared user
  - Rejects with 403 if Viewer or no access
- **Share management**: Owner-only via `GET /api/ingredient-lists/{id}/sharing` and `DELETE /api/ingredient-lists/{id}/sharing/{shareId}`

### API Client Pattern for Real-Time Features

Typed `IngredientListApiClient` with service discovery for async/await-based API operations:

```csharp
public class IngredientListApiClient(HttpClient httpClient)
{
    public async Task<IngredientList[]> GetListsAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<IngredientList[]>("/api/ingredient-lists", cancellationToken) ?? [];

    public async Task<IngredientList?> GetListAsync(Guid listId, CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<IngredientList>($"/api/ingredient-lists/{listId}", cancellationToken);

    public async Task<IngredientItem?> AddIngredientAsync(Guid listId, IngredientRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/ingredient-lists/{listId}/ingredients", request, cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<IngredientItem>(cancellationToken) : null;
    }

    public async Task<bool> ShareListViaEmailAsync(Guid listId, string email, string accessLevel, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/ingredient-lists/{listId}/share/email", 
            new { Email = email, AccessLevel = accessLevel }, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IngredientListShareLink?> GenerateShareLinkAsync(Guid listId, string accessLevel, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/ingredient-lists/{listId}/share/link",
            new { AccessLevel = accessLevel, ExpiresInDays = 7 }, cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<IngredientListShareLink>(cancellationToken) : null;
    }
}

// Register in Program.cs
builder.Services.AddHttpClient<IngredientListApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
```

**Real-Time Client Registration:**
```csharp
builder.Services.AddScoped<IngredientListSignalRClient>();
```

### Testing Ingredient Lists

**Integration tests** use `WebApplicationFactory` with in-memory databases:
```csharp
var createResponse = await _client.PostAsJsonAsync("/api/ingredient-lists", new
{
    Name = "Shopping List",
    Description = "Weekly grocery run"
});
var list = await createResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();

// Add ingredient
var ingredientResponse = await _client.PostAsJsonAsync($"/api/ingredient-lists/{list.Id}/ingredients", new
{
    Name = "Milk",
    Quantity = "1",
    Unit = "L"
});
```

**Hub tests** use Moq to verify group broadcasts:
```csharp
var clientsMock = new Mock<IHubCallerClients<IIngredientListClient>>();
var groupClientMock = new Mock<IIngredientListClient>();
clientsMock.Setup(c => c.Group($"ingredient-list-{listId}")).Returns(groupClientMock.Object);

var hub = new IngredientListHub(dbContext)
{
    Context = CreateHubContext("conn-1", ownerId),
    Clients = clientsMock.Object
};

await hub.UpdateIngredientCheckState(listId, ingredientId, isChecked: true);

groupClientMock.Verify(c => c.OnIngredientCheckStateUpdated(listId, ingredientId, true), Times.Once);
```

## Project-Specific Details

- **Target Framework**: .NET 10
- **Aspire Version**: 13.1.0
- **Blazor Render Mode**: Interactive Server (not WebAssembly or Auto)
- **Authentication**: Passwordless email verification (6-digit codes, 15-minute expiration, 30-day cookie)
- **Database**: PostgreSQL 18 with EF Core (AuthDbContext: Users, LoginCodes; IngredientListDbContext: Lists, Ingredients, Recipes, Sharing)
- **Caching**: Redis via Aspire.StackExchange.Redis.OutputCaching
- **Real-Time**: SignalR for ingredient list synchronization at `/hubs/ingredient-list`
- **API Style**: Minimal APIs with OpenAPI/Swagger in development
- **Test Framework**: MSTest with MSTestRunner (new executable test projects); bUnit for Blazor component testing
- **Nullable**: Enabled across all projects
