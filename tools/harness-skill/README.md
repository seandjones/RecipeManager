# RecipeManager Harness Skill

**Structured Workflow System for AI Agents**

This harness provides a disciplined approach to planning, implementing, and verifying features in the RecipeManager .NET Aspire project. This project is primarily focused on storing recipes found online. The main functionality is to store the recipes and create grocery lists based on these recipes.

## Purpose

This harness provides:
- 📋 **Structured Planning** - Turn vague requests into testable tasks
- 🔄 **Execute/Evaluate Loop** - Implement with independent verification
- ✅ **Quality Gates** - Acceptance criteria for every task
- 🧪 **TDD Workflow** - Test-first development for backend code
- 📊 **Progress Tracking** - Session notes and plan status

## Workflow Overview

```
User Feedback → Triage → Clarify → Plan → Execute → Evaluate → Complete
                   ↓        ↓         ↓       ↓         ↓          ↓
                Category  Questions  JSON   Implement  Verify    Commit
```

### Quick Start

1. **User shares feedback**: "I want to add recipe CRUD operations"
2. **AI triages**: Categorizes as `feature`, estimates complexity
3. **AI clarifies**: Asks about data model, UI preferences
4. **AI creates plan**: `.harness/plans/add-recipe-crud.json` with tasks
5. **AI executes each task**: Following session protocol (TDD for backend)
6. **Evaluator verifies**: Independent subagent checks acceptance criteria
7. **AI commits**: Updates plan status, appends to progress.md

## Directory Structure

```
tools/harness-skill/           # This directory - workflow guides
├── INDEX.md                   # Skill definition and overview
├── README.md                  # This file - detailed documentation
├── references/                # Reference guides
│   ├── plan-format.md         # Plan JSON schema specification
│   ├── session-protocol.md    # Step-by-step execution guide
│   ├── evaluator-guide.md     # How to evaluate task completion
│   └── tdd-guide.md           # TDD workflow for backend code
├── templates/                 # Templates for new files
│   └── plan-template.json     # Starting point for new plans
└── CODE-EXAMPLES.md           # RecipeManager code patterns (reference)

.harness/                      # Actual work artifacts
├── plans/                     # Per-ticket JSON plans
│   └── {slug}.json
├── progress.md                # Cross-plan session notes
├── eval_feedback/             # Evaluator verdict JSONs
│   └── {slug}-task{id}.json
└── runner.py                  # Automation script (status tracking)
```

## Detailed Workflow

### 1. Intake & Triage

When user shares feedback, bugs, or feature requests:

**Auto-detect triggers:**
- "I want to add..."
- "Can you implement..."
- "There's a bug where..."
- "We need to..."
- User says `/harness`

**Triage process:**
1. Read all items before acting
2. Categorize each: `bug` | `feature` | `improvement` | `chore`
3. Identify dependencies between items
4. Propose priority order:
   - Bugs first
   - Blocking dependencies
   - Small wins (quick value)
   - Large efforts last

**Output format:**
```
## Triage Results

1. [BUG] Fix API health check timeout (Priority: HIGH)
   - Blocks deployment
   - Estimated: 1-2 hours

2. [FEATURE] Add recipe CRUD operations (Priority: MEDIUM)
   - Depends on database setup
   - Estimated: 4-6 hours

3. [IMPROVEMENT] Add output caching to recipe list (Priority: LOW)
   - Depends on #2
   - Estimated: 30 minutes
```

### 2. Clarify Requirements

Use questions to turn vague feedback into testable requirements.

**Guidelines:**
- Batch related questions (max 4 per call)
- Provide concrete options - don't make user think from scratch
- Use previews for UI questions (ASCII mockups preferred)
- One round usually enough, two max
- Skip if requirement already specific

**Example clarification:**
```
## Questions about Recipe CRUD Feature

1. **Data Model** - Which fields should Recipe have?
   A) Basic: Name, Description, Ingredients (text), Instructions (text)
   B) Detailed: + PrepTime, CookTime, Servings, Categories, Tags
   C) Full: All above + Images, Nutrition, Ratings

2. **Storage** - Where should recipes be stored?
   A) PostgreSQL (relational, good for querying)
   B) Cosmos DB (NoSQL, good for scale)
   C) In-memory for now (prototyping)

3. **UI Layout** - How should recipe list look?
   A) Table (compact, many rows visible)
   B) Grid cards (visual, images if available)
   C) List with thumbnails (mobile-friendly)

4. **Authentication** - Who can create/edit recipes?
   A) Anyone (no auth for MVP)
   B) Registered users only
   C) Admin-only recipe management
```

### 3. Create Plan

Generate `.harness/plans/{slug}.json` following the [plan format](references/plan-format.md).

**Plan structure:**
- Auto-generated slug from title (lowercase, hyphenated, max 40 chars)
- Testable acceptance criteria on every task
- Each task completable in one session (< 2 hours)
- Tasks ordered by dependency

**Example plan summary:**
```
## Plan: add-recipe-crud-operations

### Tasks:
1. Create Recipe model and database schema
   - ✓ Recipe entity with required fields
   - ✓ EF Core DbContext configuration
   - ✓ Migration created and applied

2. Implement Recipe API endpoints
   - ✓ GET /api/recipes (list all)
   - ✓ GET /api/recipes/{id} (get by ID)
   - ✓ POST /api/recipes (create)
   - ✓ PUT /api/recipes/{id} (update)
   - ✓ DELETE /api/recipes/{id} (delete)
   - ✓ Integration tests for all operations

3. Create RecipeApiClient in Web project
   - ✓ Service discovery configuration
   - ✓ CRUD methods with error handling
   - ✓ Unit tests

4. Build Recipes list page
   - ✓ /recipes route with grid layout
   - ✓ StreamRendering + OutputCache
   - ✓ Create/Edit/Delete actions

... (continued)
```

### 4. Execute Tasks

For each pending task, follow the [session protocol](references/session-protocol.md).

**Summary:**
1. **Orient** - Read plan, progress notes, recent commits
2. **Verify baseline** - Run verification before changes
3. **Implement** 
   - Backend: TDD (failing test → minimal code → passing test)
   - Frontend: Direct implementation, manual testing
4. **Run verification** - Ensure all tests pass
5. **Evaluate** - Spawn evaluator subagent (MANDATORY)
6. **Update state** - Mark complete only after PASS verdict

**TDD Example** (backend):
```bash
# 1. Write failing test
dotnet test --filter GetRecipes_ReturnsOk
# FAILS - endpoint doesn't exist

# 2. Implement endpoint
# (add code to Program.cs)

# 3. Run test again
dotnet test --filter GetRecipes_ReturnsOk
# PASSES

# 4. Refactor if needed (keep tests green)
```

See [TDD Guide](references/tdd-guide.md) for complete workflow.

### 5. Evaluate (Mandatory Gate)

**CRITICAL: Never skip evaluation. Never self-evaluate.**

After implementing each task:

1. **Spawn evaluator subagent** with read-only access
2. **Evaluator follows** [evaluator guide](references/evaluator-guide.md)
3. **Evaluator produces verdict**: PASS | FAIL | NEEDS WORK
4. **If FAIL**: Fix issues and spawn NEW evaluator
5. **Retry limit**: 2 cycles max, then ask user

**Evaluator prompt template:**
```
You are a skeptical code evaluator. Find problems, not praise.

Follow evaluation steps in tools/harness-skill/references/evaluator-guide.md

Evaluate task {id}: "{title}" from .harness/plans/{slug}.json

Do NOT fix code. Only read, run verification, and produce VERDICT.
```

**Hard gate rules:**
- ❌ Do NOT mark task complete without PASS verdict
- ❌ Do NOT self-evaluate your own work
- ❌ Do NOT skip evaluation "just this once"
- ✅ DO spawn new evaluator for each retry
- ✅ DO fix all issues before re-evaluating

### 6. Update Progress

After evaluator returns PASS:

1. **Update plan JSON**
   ```json
   {
     "id": 2,
     "status": "complete",
     "notes": "All CRUD endpoints implemented. Tests passing (15/15)."
   }
   ```

2. **Commit changes**
   ```bash
   git add .
   git commit -m "feat: implement recipe CRUD API endpoints

   - Added GET /api/recipes (list)
   - Added GET /api/recipes/{id} (get by ID)
   - Added POST /api/recipes (create)
   - Added PUT /api/recipes/{id} (update)
   - Added DELETE /api/recipes/{id} (delete)
   - Integration tests verify all operations

   Resolves task #2 from plan: add-recipe-crud-operations"
   ```

3. **Append to progress.md**
   ```markdown
   ## 2026-04-12 - Recipe API CRUD (Task #2)

   Implemented all recipe CRUD endpoints in ApiService/Program.cs

   **Files Changed:**
   - RecipeManager.ApiService/Program.cs
   - RecipeManager.Tests/RecipeApiTests.cs

   **Test Results:**
   - 15/15 tests passing
   - New tests: GetRecipes, GetRecipeById, CreateRecipe, etc.

   **Gotchas:**
   - Remember to call SaveChangesAsync() after modifications
   - Validation errors return 400 with ProblemDetails

   **Next:** Task #3 - Create RecipeApiClient in Web project
   ```

## Automation

The `.harness/runner.py` script provides status tracking and dry-run capabilities:

```bash
# Show plan status
python3 .harness/runner.py --plan .harness/plans/add-recipe-crud.json --status

# Dry run (show what would happen)
python3 .harness/runner.py --plan .harness/plans/add-recipe-crud.json --dry-run

# Run specific task (shows info, actual execution TBD)
python3 .harness/runner.py --plan .harness/plans/add-recipe-crud.json --task 2
```

**Note:** Full automated execution is planned but not yet implemented. Current workflow is interactive via AI chat.

## Reference Materials

The harness includes reference guides for common patterns:

- **[CODE-EXAMPLES.md](CODE-EXAMPLES.md)** - Existing RecipeManager code patterns
  - Blazor components (Weather.razor, Counter.razor)
  - API endpoints (WeatherForecast)
  - Service configuration (AppHost.cs, Program.cs)
  - Integration tests (WebTests.cs)

- **[AI-QUICK-REFERENCE.md](AI-QUICK-REFERENCE.md)** - Quick lookup for AI agents
  - Common patterns cheat sheet
  - File locations
  - Decision matrix for modifications

These provide context when implementing new features similar to existing code.

## Best Practices

✅ **Do:**
- Follow the session protocol for every task
- Write tests before code (backend)
- Spawn evaluator subagent for verification
- Keep tasks small (< 2 hours)
- Update progress.md after each task
- Commit atomically with clear messages

❌ **Don't:**
- Skip baseline verification
- Self-evaluate your work
- Mark tasks complete without PASS verdict
- Work on multiple tasks simultaneously
- Ignore test failures
- Make vague acceptance criteria

## Troubleshooting

**Plan creation fails:**
- Check JSON syntax in plan file
- Verify all required fields present
- Ensure slug is unique

**Baseline verification fails:**
- Fix existing issues before starting
- Check for uncommitted changes
- Verify dependencies are running

**Evaluator keeps failing:**
- Read the feedback carefully
- Check if acceptance criteria are too vague
- After 2 retries, stop and ask user

**Not sure how to test something:**
- Check [TDD Guide](references/tdd-guide.md)
- Look at existing tests in CODE-EXAMPLES.md
- Ask user for clarification on criterion

## Getting Help

If you're stuck:
1. Read the relevant reference guide
2. Check CODE-EXAMPLES.md for similar patterns
3. Review recent progress.md entries
4. Ask the user for clarification

## File Ownership

| File/Directory | Who Updates | When |
|----------------|-------------|------|
| `tools/harness-skill/` | Maintainers | When workflow changes |
| `.harness/plans/*.json` | AI agent | During planning & execution |
| `.harness/progress.md` | AI agent | After each completed task |
| `.harness/eval_feedback/` | Evaluator | After each evaluation |
| `.harness/runner.py` | Maintainers | When adding automation |

### 1. Adding a New Blazor Component with API Integration

When a developer asks to add a new page that fetches data from the API:

**Step 1: Create the API Client (in RecipeManager.Web)**
```csharp
// RecipeManager.Web/Clients/RecipeApiClient.cs
namespace RecipeManager.Web.Clients;

public class RecipeApiClient(HttpClient httpClient)
{
    public async Task<Recipe[]> GetRecipesAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<Recipe[]>("/api/recipes", cancellationToken) 
            ?? Array.Empty<Recipe>();
    }

    public async Task<Recipe?> GetRecipeAsync(int id, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<Recipe>($"/api/recipes/{id}", cancellationToken);
    }

    public async Task<Recipe> CreateRecipeAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/recipes", recipe, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Recipe>(cancellationToken))!;
    }
}

public record Recipe(int Id, string Name, string Description, string[] Ingredients, string[] Instructions);
```

**Step 2: Register Client in Program.cs**
```csharp
// RecipeManager.Web/Program.cs
builder.Services.AddHttpClient<RecipeApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
```

**Step 3: Create Blazor Component**
```razor
@* RecipeManager.Web/Components/Pages/Recipes.razor *@
@page "/recipes"
@attribute [StreamRendering(true)]
@attribute [OutputCache(Duration = 10)]

@inject RecipeApiClient RecipeApi

<PageTitle>Recipes</PageTitle>

<h1>Recipes</h1>

@if (recipes == null)
{
    <p><em>Loading...</em></p>
}
else if (recipes.Length == 0)
{
    <p>No recipes found.</p>
}
else
{
    <div class="recipe-grid">
        @foreach (var recipe in recipes)
        {
            <div class="recipe-card">
                <h3>@recipe.Name</h3>
                <p>@recipe.Description</p>
                <a href="/recipes/@recipe.Id">View Details</a>
            </div>
        }
    </div>
}

@code {
    private Recipe[]? recipes;

    protected override async Task OnInitializedAsync()
    {
        recipes = await RecipeApi.GetRecipesAsync();
    }
}
```

**Step 4: Add API Endpoint**
```csharp
// RecipeManager.ApiService/Program.cs
app.MapGet("/api/recipes", () =>
{
    // TODO: Replace with database query
    return new[]
    {
        new Recipe(1, "Pasta Carbonara", "Classic Italian pasta dish", 
            new[] { "Pasta", "Eggs", "Bacon", "Parmesan" },
            new[] { "Boil pasta", "Cook bacon", "Mix eggs and cheese", "Combine all" }),
        new Recipe(2, "Chicken Tikka Masala", "Spiced curry dish",
            new[] { "Chicken", "Yogurt", "Spices", "Cream" },
            new[] { "Marinate chicken", "Cook curry", "Add cream" })
    };
})
.WithName("GetRecipes")
.WithOpenApi();

record Recipe(int Id, string Name, string Description, string[] Ingredients, string[] Instructions);
```

### 2. Adding a New Infrastructure Service (Database, Message Queue, etc.)

When adding PostgreSQL to the project:

**Step 1: Add NuGet Package to ApiService**
```xml
<!-- RecipeManager.ApiService/RecipeManager.ApiService.csproj -->
<PackageReference Include="Aspire.Npgsql.EntityFrameworkCore.PostgreSQL" Version="13.1.0" />
```

**Step 2: Update AppHost to include PostgreSQL**
```csharp
// RecipeManager.AppHost/AppHost.cs
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()  // Persist data
    .AddDatabase("recipedb");

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.RecipeManager_ApiService>("apiservice")
    .WithReference(postgres)  // Add database reference
    .WaitFor(postgres)        // Wait for DB to be ready
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.RecipeManager_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);
```

**Step 3: Configure EF Core in ApiService**
```csharp
// RecipeManager.ApiService/Program.cs
builder.AddNpgsqlDbContext<RecipeDbContext>("recipedb");

// After app is built, apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecipeDbContext>();
    db.Database.EnsureCreated();  // Or use: await db.Database.MigrateAsync();
}
```

### 3. Adding Authentication

When implementing authentication:

**Step 1: Add Auth Packages**
```xml
<!-- RecipeManager.Web/RecipeManager.Web.csproj -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="10.0.0" />
```

**Step 2: Configure Authentication in Web**
```csharp
// RecipeManager.Web/Program.cs
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

// In middleware pipeline
app.UseAuthentication();
app.UseAuthorization();
```

**Step 3: Protect Blazor Components**
```razor
@* RecipeManager.Web/Components/Pages/MyRecipes.razor *@
@page "/my-recipes"
@attribute [Authorize]

<AuthorizeView>
    <Authorized>
        <h1>Welcome, @context.User.Identity?.Name!</h1>
        @* Component content *@
    </Authorized>
    <NotAuthorized>
        <p>You need to log in to view your recipes.</p>
    </NotAuthorized>
</AuthorizeView>
```

## Common Tasks

### Task: Add Form with Validation

```razor
@* RecipeManager.Web/Components/Pages/CreateRecipe.razor *@
@page "/recipes/create"
@inject RecipeApiClient RecipeApi
@inject NavigationManager Navigation

<h1>Create New Recipe</h1>

<EditForm Model="@model" OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <div class="mb-3">
        <label>Name:</label>
        <InputText @bind-Value="model.Name" class="form-control" />
        <ValidationMessage For="() => model.Name" />
    </div>

    <div class="mb-3">
        <label>Description:</label>
        <InputTextArea @bind-Value="model.Description" class="form-control" />
        <ValidationMessage For="() => model.Description" />
    </div>

    <button type="submit" class="btn btn-primary">Create</button>
</EditForm>

@code {
    private RecipeFormModel model = new();

    private async Task HandleSubmit()
    {
        var recipe = new Recipe(0, model.Name, model.Description, 
            Array.Empty<string>(), Array.Empty<string>());
        await RecipeApi.CreateRecipeAsync(recipe);
        Navigation.NavigateTo("/recipes");
    }

    private class RecipeFormModel
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = "";

        [Required, StringLength(500)]
        public string Description { get; set; } = "";
    }
}
```

### Task: Add Logging to Track Operations

```csharp
// RecipeManager.ApiService/Program.cs
app.MapPost("/api/recipes", async (Recipe recipe, RecipeDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("Creating recipe: {RecipeName}", recipe.Name);

    try
    {
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        logger.LogInformation("Recipe created successfully with ID: {RecipeId}", recipe.Id);
        return Results.Created($"/api/recipes/{recipe.Id}", recipe);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to create recipe: {RecipeName}", recipe.Name);
        return Results.Problem("Failed to create recipe");
    }
})
.WithName("CreateRecipe")
.WithOpenApi();
```

### Task: Add Health Check for Database

```csharp
// RecipeManager.ApiService/Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("recipedb")!);
```

## Troubleshooting Guide

### Issue: "Service not found" errors

**Symptom:** Web app can't connect to API service  
**Solution:** Check service name matches in AppHost and HttpClient registration

```csharp
// AppHost.cs - service name is "apiservice"
var apiService = builder.AddProject<Projects.RecipeManager_ApiService>("apiservice");

// Web/Program.cs - must match exactly
builder.Services.AddHttpClient<RecipeApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");  // Must match "apiservice"
});
```

### Issue: Components not updating after data changes

**Symptom:** Blazor component shows stale data  
**Solution:** Disable OutputCache for dynamic content or use shorter duration

```razor
@* Remove or adjust OutputCache for frequently changing data *@
@attribute [OutputCache(Duration = 5)]  @* 5 seconds instead of default *@
```

### Issue: Health checks failing

**Symptom:** Services show unhealthy in Aspire Dashboard  
**Solution:** Ensure all dependencies are ready before service starts

```csharp
// AppHost.cs - use WaitFor for all dependencies
builder.AddProject<Projects.RecipeManager_Web>("webfrontend")
    .WithReference(cache)
    .WaitFor(cache)        // Wait for cache to be healthy
    .WithReference(apiService)
    .WaitFor(apiService);  // Wait for API to be healthy
```

### Issue: Integration tests timeout

**Symptom:** Tests fail with timeout exceptions  
**Solution:** Increase timeout and ensure WaitForResourceHealthyAsync

```csharp
// RecipeManager.Tests/WebTests.cs
private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);  // Increase if needed

await app.ResourceNotifications.WaitForResourceHealthyAsync("webfrontend", cancellationToken)
    .WaitAsync(DefaultTimeout, cancellationToken);
```

## Testing Patterns

### Integration Test Template

```csharp
// RecipeManager.Tests/ApiTests.cs
[TestClass]
public class ApiTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    [TestMethod]
    public async Task GetRecipes_ReturnsOkWithRecipes()
    {
        // Arrange
        var cts = new CancellationTokenSource(DefaultTimeout);
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.RecipeManager_AppHost>(cts.Token);

        await using var app = await appHost.BuildAsync(cts.Token);
        await app.StartAsync(cts.Token);

        // Act
        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cts.Token);
        var response = await httpClient.GetAsync("/api/recipes", cts.Token);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var recipes = await response.Content.ReadFromJsonAsync<Recipe[]>(cts.Token);
        Assert.IsNotNull(recipes);
        Assert.IsTrue(recipes.Length > 0);
    }
}
```

## Reference: Project File Locations

- **Blazor Components:** `RecipeManager.Web/Components/Pages/`
- **Blazor Layout:** `RecipeManager.Web/Components/Layout/`
- **API Clients:** `RecipeManager.Web/Clients/` (create this folder)
- **API Endpoints:** `RecipeManager.ApiService/Program.cs`
- **Service Orchestration:** `RecipeManager.AppHost/AppHost.cs`
- **Shared Config:** `RecipeManager.ServiceDefaults/Extensions.cs`
- **Integration Tests:** `RecipeManager.Tests/`

## Quick Commands for AI to Suggest

```bash
# Start the application (from solution root)
dotnet run --project RecipeManager.AppHost

# Run tests
dotnet test

# Build specific project
dotnet build RecipeManager.ApiService

# Add package to ApiService
dotnet add RecipeManager.ApiService package Npgsql.EntityFrameworkCore.PostgreSQL

# Create database migration (if using EF Core)
dotnet ef migrations add InitialCreate --project RecipeManager.ApiService

# View Aspire Dashboard logs
# Navigate to http://localhost:18888 after starting AppHost
```

## Decision Tree for Common Requests

**"Add a new page"** → Create Blazor component in `Components/Pages/`, add NavLink in `NavMenu.razor`

**"Add API endpoint"** → Add MapGet/MapPost in `ApiService/Program.cs`, create matching client method

**"Connect to database"** → Add Aspire package, update AppHost with database resource, configure in ApiService

**"Add authentication"** → Add auth packages, configure in Web/Program.cs, use `[Authorize]` attribute

**"Add caching"** → Use `[OutputCache]` attribute on Blazor components (Redis already configured)

**"Add background job"** → Create hosted service in ApiService, register in Program.cs

**"Add new service"** → Create new project, reference ServiceDefaults, register in AppHost with `.AddProject()`
