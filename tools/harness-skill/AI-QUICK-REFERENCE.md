# AI Agent Quick Reference

Fast lookup guide for AI coding assistants working on RecipeManager.

## When Developer Says... Do This:

### "Add a new page"
1. Create `RecipeManager.Web/Components/Pages/YourPage.razor`
2. Add `@page "/your-route"`
3. Add `@attribute [StreamRendering(true)]` for streaming
4. Add `@attribute [OutputCache(Duration = N)]` if caching needed
5. Update `NavMenu.razor` with new NavLink

### "Add API endpoint"
1. Open `RecipeManager.ApiService/Program.cs`
2. Add `app.MapGet/Post/Put/Delete(...)`
3. Add `.WithName("...")` and `.WithOpenApi()`
4. Create matching client method in Web project

### "Add database"
1. Add Aspire database package to ApiService
2. Add database resource in `AppHost.cs` (e.g., `builder.AddPostgres()`)
3. Add `.WithReference(db)` and `.WaitFor(db)` to dependent services
4. Configure DbContext in ApiService Program.cs

### "Add authentication"
1. Add auth NuGet packages to Web project
2. Configure in `Web/Program.cs` before `builder.Build()`
3. Add `app.UseAuthentication()` and `app.UseAuthorization()` middleware
4. Use `@attribute [Authorize]` on protected pages
5. Add `<AuthorizeView>` in components

### "Make this component interactive"
- Add `@rendermode InteractiveServer` to the component
- Ensure component is in `Pages` or explicitly rendered

### "Add form validation"
- Use `<EditForm Model="@model" OnValidSubmit="...">`
- Add `<DataAnnotationsValidator />` and `<ValidationSummary />`
- Use `<InputText>`, `<InputTextArea>`, etc. with `@bind-Value`
- Add `<ValidationMessage For="() => model.Property" />`

### "Connect to Redis"
- Already configured! Redis is in `AppHost.cs` as "cache"
- Web already uses it via `builder.AddRedisOutputCache("cache")`
- API can reference it with `.WithReference(cache).WaitFor(cache)`

## File Locations Cheat Sheet

```
RecipeManager/
├── .github/
│   └── copilot-instructions.md    ← Aspire architecture guide
├── tools/harness-skill/
│   ├── README.md                   ← This AI reference (detailed)
│   ├── CODE-EXAMPLES.md            ← Copy-paste examples
│   └── AI-QUICK-REFERENCE.md       ← This file (quick lookup)
├── RecipeManager.AppHost/
│   └── AppHost.cs                  ← Service orchestration
├── RecipeManager.Web/
│   ├── Program.cs                  ← Web app configuration
│   ├── Components/
│   │   ├── Pages/                  ← Routable pages (@page)
│   │   ├── Layout/                 ← Layouts and NavMenu
│   │   └── _Imports.razor          ← Global usings
│   └── [Clients/]                  ← API clients (create if needed)
├── RecipeManager.ApiService/
│   └── Program.cs                  ← API endpoints & config
├── RecipeManager.ServiceDefaults/
│   └── Extensions.cs               ← Shared Aspire configuration
└── RecipeManager.Tests/
    └── WebTests.cs                 ← Integration tests
```

## Common Patterns

### Primary Constructor (C# 12)
```csharp
public class MyApiClient(HttpClient httpClient, ILogger<MyApiClient> logger)
{
    // httpClient and logger are automatically available
}
```

### Collection Expressions (C# 12)
```csharp
forecasts ??= [];  // Instead of: new List<Forecast>()
```

### Service Discovery URLs
```csharp
client.BaseAddress = new("https+http://servicename");
// "https+http://" = prefer HTTPS, fallback to HTTP
// "servicename" must match the name in AppHost.cs
```

### Aspire Service Pattern
Every service project should have:
```csharp
// In Program.cs BEFORE builder.Build()
builder.AddServiceDefaults();

// After var app = builder.Build() and before app.Run()
app.MapDefaultEndpoints();
```

## Environment Info

- **.NET Version**: 10
- **Aspire Version**: 13.1.0
- **Blazor Mode**: Interactive Server (not WebAssembly)
- **Test Framework**: MSTest with MSTestRunner
- **Nullable**: Enabled everywhere
- **Existing Services**: webfrontend, apiservice, cache (Redis)

## Commands

```bash
# Run the app (starts all services)
dotnet run --project RecipeManager.AppHost

# Run tests
dotnet test

# Add package to a project
dotnet add RecipeManager.ProjectName package PackageName

# Build specific project
dotnet build RecipeManager.ProjectName
```

## Quick Checks

**Is this an Aspire service project?** → Should call `builder.AddServiceDefaults()`  
**Does it talk to other services?** → Use HttpClient with service discovery URL  
**Is it a Blazor page?** → Needs `@page` directive  
**Is it a Blazor component?** → Can be used in pages without `@page`  
**Does it need interactivity?** → Add `@rendermode InteractiveServer`  
**Does it fetch data?** → Consider `@attribute [StreamRendering(true)]`  
**Should it cache?** → Add `@attribute [OutputCache(Duration = N)]`

## Troubleshooting Quick Fixes

| Symptom | Likely Fix |
|---------|-----------|
| "Service not found" | Check service name in AppHost matches HttpClient BaseAddress |
| Blazor not updating | Remove/reduce OutputCache duration, or call `StateHasChanged()` |
| Health check fails | Add `.WaitFor()` in AppHost for dependencies |
| Test timeout | Increase timeout, ensure `WaitForResourceHealthyAsync()` called |
| Can't connect to API | Verify `https+http://apiservice` URL, check Aspire Dashboard |
| Redis error | Ensure `.WaitFor(cache)` in AppHost before dependent services |
| Interactive features not working | Add `@rendermode InteractiveServer` to component |

## Decision Matrix

| Developer Request | Primary File(s) to Modify |
|------------------|---------------------------|
| New page/route | `Web/Components/Pages/*.razor`, `NavMenu.razor` |
| New API endpoint | `ApiService/Program.cs` |
| New service | Create project, modify `AppHost.cs` |
| Add database | `AppHost.cs`, `ApiService.csproj`, `ApiService/Program.cs` |
| Add auth | `Web.csproj`, `Web/Program.cs`, protected pages |
| Add dependency | `AppHost.cs` (`.WithReference()`, `.WaitFor()`) |
| Configure logging | `ServiceDefaults/Extensions.cs` or service's Program.cs |
| Add test | `RecipeManager.Tests/*.cs` |
| Add package | Run `dotnet add` command |
