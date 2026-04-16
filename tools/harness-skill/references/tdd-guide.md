# TDD Guide for RecipeManager

Test-Driven Development workflow for backend code in .NET Aspire projects.

## Why TDD for Backend?

**Benefits:**
- ✅ Catches bugs before they reach production
- ✅ Forces you to think about API design first
- ✅ Provides living documentation
- ✅ Makes refactoring safer
- ✅ Reduces debugging time

**When to use:**
- API endpoints
- Business logic
- Data access code
- Services and clients

**When to skip:**
- Blazor components (test manually)
- Simple DTOs/models
- Configuration code
- Very obvious code

## TDD Cycle (Red-Green-Refactor)

### 1. RED - Write Failing Test

```csharp
[TestMethod]
public async Task GetRecipeById_WithValidId_ReturnsRecipe()
{
    // Arrange
    var testRecipe = new Recipe 
    { 
        Id = 1, 
        Name = "Test Recipe",
        Description = "Test Description"
    };
    
    var context = CreateTestDbContext();
    context.Recipes.Add(testRecipe);
    await context.SaveChangesAsync();
    
    // Act
    var response = await _client.GetAsync("/api/recipes/1");
    
    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    var recipe = await response.Content.ReadFromJsonAsync<Recipe>();
    Assert.IsNotNull(recipe);
    Assert.AreEqual("Test Recipe", recipe.Name);
}
```

**Run the test - it should FAIL:**
```bash
dotnet test --filter GetRecipeById_WithValidId_ReturnsRecipe
# Expected: endpoint not found or 404
```

### 2. GREEN - Write Minimum Code to Pass

```csharp
app.MapGet("/api/recipes/{id:int}", async (int id, RecipeDbContext db) =>
{
    var recipe = await db.Recipes.FindAsync(id);
    return recipe is not null ? Results.Ok(recipe) : Results.NotFound();
})
.WithName("GetRecipeById")
.WithOpenApi();
```

**Run the test again - it should PASS:**
```bash
dotnet test --filter GetRecipeById_WithValidId_ReturnsRecipe
# Expected: green check mark
```

### 3. REFACTOR - Clean Up (Optional)

If the code works but is messy:

```csharp
// Extract to service if logic gets complex
public class RecipeService
{
    private readonly RecipeDbContext _db;
    
    public async Task<Recipe?> GetRecipeByIdAsync(int id)
    {
        return await _db.Recipes.FindAsync(id);
    }
}

// Then use in endpoint
app.MapGet("/api/recipes/{id:int}", async (int id, RecipeService service) =>
{
    var recipe = await service.GetRecipeByIdAsync(id);
    return recipe is not null ? Results.Ok(recipe) : Results.NotFound();
});
```

**Run tests again - still green:**
```bash
dotnet test
# All tests should still pass
```

## Test Patterns for RecipeManager

### Pattern 1: API Integration Tests

Test the full HTTP request/response cycle:

```csharp
[TestClass]
public class RecipeApiTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private HttpClient _client = null!;
    private DistributedApplication _app = null!;
    
    [TestInitialize]
    public async Task Setup()
    {
        var cts = new CancellationTokenSource(Timeout);
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.RecipeManager_AppHost>(cts.Token);
        
        _app = await appHost.BuildAsync(cts.Token);
        await _app.StartAsync(cts.Token);
        
        _client = _app.CreateHttpClient("apiservice");
        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("apiservice", cts.Token);
    }
    
    [TestCleanup]
    public async Task Cleanup()
    {
        await _app.DisposeAsync();
    }
    
    [TestMethod]
    public async Task GetRecipes_ReturnsOkWithRecipes()
    {
        var response = await _client.GetAsync("/api/recipes");
        
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var recipes = await response.Content.ReadFromJsonAsync<Recipe[]>();
        Assert.IsNotNull(recipes);
    }
}
```

### Pattern 2: Service Unit Tests

Test business logic in isolation:

```csharp
[TestClass]
public class RecipeServiceTests
{
    private RecipeDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<RecipeDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        return new RecipeDbContext(options);
    }
    
    [TestMethod]
    public async Task CreateRecipe_WithValidData_SavesToDatabase()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new RecipeService(context);
        var recipe = new Recipe { Name = "Test", Description = "Test" };
        
        // Act
        var result = await service.CreateRecipeAsync(recipe);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Id > 0);
        Assert.AreEqual(1, await context.Recipes.CountAsync());
    }
}
```

### Pattern 3: Validation Tests

Test error cases and validation:

```csharp
[TestMethod]
public async Task CreateRecipe_WithEmptyName_ReturnsBadRequest()
{
    var recipe = new Recipe { Name = "", Description = "Test" };
    var response = await _client.PostAsJsonAsync("/api/recipes", recipe);
    
    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
}

[TestMethod]
public async Task GetRecipeById_WithInvalidId_ReturnsNotFound()
{
    var response = await _client.GetAsync("/api/recipes/99999");
    
    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
}
```

## Test Organization

```
RecipeManager.Tests/
├── ApiTests/
│   ├── RecipeApiTests.cs      # Integration tests for recipe endpoints
│   └── HealthCheckTests.cs    # Health check tests
├── ServiceTests/
│   ├── RecipeServiceTests.cs  # Unit tests for recipe service
│   └── ValidationTests.cs     # Validation logic tests
└── WebTests/
    └── WebTests.cs            # Existing web tests
```

## Naming Conventions

Test names should follow: `MethodName_Scenario_ExpectedBehavior`

Examples:
- `GetRecipeById_WithValidId_ReturnsRecipe`
- `GetRecipeById_WithInvalidId_ReturnsNotFound`
- `CreateRecipe_WithEmptyName_ReturnsBadRequest`
- `UpdateRecipe_WithValidData_UpdatesDatabase`
- `DeleteRecipe_WithExistingId_RemovesFromDatabase`

## AAA Pattern (Arrange-Act-Assert)

Always structure tests with clear sections:

```csharp
[TestMethod]
public async Task Example()
{
    // Arrange - set up test data and dependencies
    var recipe = new Recipe { Name = "Test" };
    var context = CreateTestContext();
    
    // Act - execute the code being tested
    var result = await service.CreateRecipeAsync(recipe);
    
    // Assert - verify the outcome
    Assert.IsNotNull(result);
    Assert.AreEqual("Test", result.Name);
}
```

## Common Test Helpers

```csharp
// Create test database context
private RecipeDbContext CreateTestDbContext()
{
    var options = new DbContextOptionsBuilder<RecipeDbContext>()
        .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
        .Options;
    return new RecipeDbContext(options);
}

// Seed test data
private async Task<Recipe> SeedRecipe(RecipeDbContext context, string name)
{
    var recipe = new Recipe 
    { 
        Name = name,
        Description = "Test Description",
        CreatedAt = DateTime.UtcNow
    };
    context.Recipes.Add(recipe);
    await context.SaveChangesAsync();
    return recipe;
}

// Create authenticated client (if using auth)
private HttpClient CreateAuthenticatedClient(string userId)
{
    var client = _app.CreateHttpClient("apiservice");
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", CreateTestToken(userId));
    return client;
}
```

## Quick TDD Workflow

1. **Read acceptance criterion**: "GET /api/recipes/{id} returns single recipe or 404"

2. **Write failing test**:
   ```bash
   # Create RecipeApiTests.cs
   # Add GetRecipeById_WithValidId_ReturnsRecipe test
   dotnet test --filter GetRecipeById
   # FAILS - endpoint doesn't exist
   ```

3. **Implement minimal code**:
   ```csharp
   app.MapGet("/api/recipes/{id:int}", async (int id, RecipeDbContext db) =>
   {
       var recipe = await db.Recipes.FindAsync(id);
       return recipe is not null ? Results.Ok(recipe) : Results.NotFound();
   });
   ```

4. **Run test again**:
   ```bash
   dotnet test --filter GetRecipeById
   # PASSES
   ```

5. **Add test for 404 case**:
   ```csharp
   [TestMethod]
   public async Task GetRecipeById_WithInvalidId_ReturnsNotFound()
   {
       var response = await _client.GetAsync("/api/recipes/99999");
       Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
   }
   ```

6. **Verify it passes** (code already handles this):
   ```bash
   dotnet test --filter GetRecipeById
   # Both tests PASS
   ```

7. **Move to next criterion**

## When Tests Feel Hard

If writing a test feels difficult:

1. **Simplify the code** - maybe it's doing too much
2. **Extract a service** - test the service independently
3. **Use test doubles** - mock external dependencies
4. **Ask for help** - the difficulty is a signal

## Red Flags

🚩 Test has no assertions  
🚩 Test name doesn't describe behavior  
🚩 Test tests implementation details, not behavior  
🚩 Test is flaky (passes/fails randomly)  
🚩 Test takes > 1 second to run  
🚩 Test depends on other tests running first  

## Resources

- MSTest Documentation: https://learn.microsoft.com/dotnet/core/testing/unit-testing-with-mstest
- Aspire Testing: https://learn.microsoft.com/dotnet/aspire/testing/
- EF Core In-Memory Testing: https://learn.microsoft.com/ef/core/testing/
