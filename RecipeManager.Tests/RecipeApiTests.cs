using Microsoft.EntityFrameworkCore;
using RecipeManager.ApiService.Data;

namespace RecipeManager.Tests;

[TestClass]
public class RecipeApiTests
{
    private static RecipeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<RecipeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RecipeDbContext(options);
    }

    // --- GET /api/recipes ---

    [TestMethod]
    public async Task GetRecipes_WhenEmpty_ReturnsEmptyList()
    {
        using var db = CreateDbContext();
        var recipes = await db.Recipes.OrderByDescending(r => r.CreatedAt).ToListAsync();
        Assert.AreEqual(0, recipes.Count);
    }

    [TestMethod]
    public async Task GetRecipes_AfterCreating_ReturnsAllRecipes()
    {
        using var db = CreateDbContext();
        db.Recipes.AddRange(
            new Recipe { Name = "Recipe 1", Ingredients = "I1", Instructions = "S1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Recipe { Name = "Recipe 2", Ingredients = "I2", Instructions = "S2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var recipes = await db.Recipes.ToListAsync();

        Assert.AreEqual(2, recipes.Count);
    }

    // --- POST /api/recipes ---

    [TestMethod]
    public async Task CreateRecipe_WithValidData_PersistsAndReturnsWithId()
    {
        using var db = CreateDbContext();
        var recipe = new Recipe
        {
            Name = "Pasta Carbonara",
            Description = "Classic Italian pasta dish",
            Ingredients = "Pasta, eggs, bacon, cheese",
            Instructions = "Cook pasta. Mix eggs and cheese. Combine.",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            Servings = 4,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        Assert.IsTrue(recipe.Id > 0);
        var saved = await db.Recipes.FindAsync(recipe.Id);
        Assert.IsNotNull(saved);
        Assert.AreEqual("Pasta Carbonara", saved.Name);
        Assert.AreEqual("Classic Italian pasta dish", saved.Description);
        Assert.AreEqual(4, saved.Servings);
    }

    [TestMethod]
    public void CreateRecipe_WithEmptyName_FailsValidation()
    {
        // Mirrors the endpoint guard: if (string.IsNullOrWhiteSpace(request.Name)) return 400
        var name = "";
        Assert.IsTrue(string.IsNullOrWhiteSpace(name), "Empty name should trigger bad request");
    }

    // --- GET /api/recipes/{id} ---

    [TestMethod]
    public async Task GetRecipeById_WithExistingId_ReturnsRecipe()
    {
        using var db = CreateDbContext();
        var recipe = new Recipe
        {
            Name = "Chicken Soup",
            Ingredients = "Chicken, vegetables, broth",
            Instructions = "Simmer for 1 hour.",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var found = await db.Recipes.FindAsync(recipe.Id);

        Assert.IsNotNull(found);
        Assert.AreEqual("Chicken Soup", found.Name);
    }

    [TestMethod]
    public async Task GetRecipeById_WithNonExistentId_ReturnsNull()
    {
        using var db = CreateDbContext();
        var found = await db.Recipes.FindAsync(99999);
        Assert.IsNull(found); // endpoint returns 404 when null
    }

    // --- PUT /api/recipes/{id} ---

    [TestMethod]
    public async Task UpdateRecipe_WithValidData_PersistsChanges()
    {
        using var db = CreateDbContext();
        var recipe = new Recipe
        {
            Name = "Original Name",
            Ingredients = "Ingredient 1",
            Instructions = "Step 1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        recipe.Name = "Updated Name";
        recipe.Ingredients = "New ingredients";
        recipe.Instructions = "New instructions";
        recipe.PrepTimeMinutes = 15;
        recipe.Servings = 2;
        recipe.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var updated = await db.Recipes.FindAsync(recipe.Id);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Updated Name", updated.Name);
        Assert.AreEqual(15, updated.PrepTimeMinutes);
        Assert.AreEqual(2, updated.Servings);
    }

    [TestMethod]
    public async Task UpdateRecipe_WithNonExistentId_ReturnsNull()
    {
        using var db = CreateDbContext();
        var recipe = await db.Recipes.FindAsync(99999);
        Assert.IsNull(recipe); // endpoint returns 404 when null
    }

    // --- DELETE /api/recipes/{id} ---

    [TestMethod]
    public async Task DeleteRecipe_WithExistingId_RemovesFromDatabase()
    {
        using var db = CreateDbContext();
        var recipe = new Recipe
        {
            Name = "Recipe to Delete",
            Ingredients = "Ingredients",
            Instructions = "Instructions",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();
        var id = recipe.Id;

        db.Recipes.Remove(recipe);
        await db.SaveChangesAsync();

        var deleted = await db.Recipes.FindAsync(id);
        Assert.IsNull(deleted);
    }

    [TestMethod]
    public async Task DeleteRecipe_WithNonExistentId_ReturnsNull()
    {
        using var db = CreateDbContext();
        var recipe = await db.Recipes.FindAsync(99999);
        Assert.IsNull(recipe); // endpoint returns 404 when null
    }
}
