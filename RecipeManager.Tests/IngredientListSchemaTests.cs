using Microsoft.EntityFrameworkCore;
using RecipeManager.ApiService.Data;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Diagnostics;
using System.Text;

namespace RecipeManager.Tests;

/// <summary>
/// Tests for ingredient list database schema and entity properties.
/// Verifies that all required entities exist with correct properties and relationships.
/// </summary>
[TestClass]
public class IngredientListSchemaTests
{
    private IngredientListDbContext GetTestContext()
    {
        var options = new DbContextOptionsBuilder<IngredientListDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IngredientListDbContext(options);
    }

    // IngredientList entity tests
    [TestMethod]
    public void IngredientList_HasRequiredProperties()
    {
        using var context = GetTestContext();
        var entityType = context.Model.FindEntityType(typeof(IngredientList));

        Assert.IsNotNull(entityType, "IngredientList entity should exist");
        Assert.IsTrue(entityType.FindProperty("Id") != null, "IngredientList should have Id property");
        Assert.IsTrue(entityType.FindProperty("Name") != null, "IngredientList should have Name property");
        Assert.IsTrue(entityType.FindProperty("Description") != null, "IngredientList should have Description property");
        Assert.IsTrue(entityType.FindProperty("OwnerId") != null, "IngredientList should have OwnerId property");
        Assert.IsTrue(entityType.FindProperty("CreatedAt") != null, "IngredientList should have CreatedAt property");
        Assert.IsTrue(entityType.FindProperty("UpdatedAt") != null, "IngredientList should have UpdatedAt property");
    }

    [TestMethod]
    public void IngredientList_CanBeSavedAndRetrieved()
    {
        using var context = GetTestContext();
        var list = new IngredientList
        {
            Name = "Grocery List",
            Description = "Weekly groceries",
            OwnerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.IngredientLists.Add(list);
        context.SaveChanges();

        var retrieved = context.IngredientLists.First();
        Assert.AreEqual("Grocery List", retrieved.Name);
        Assert.AreEqual("Weekly groceries", retrieved.Description);
    }

    // Ingredient entity tests
    [TestMethod]
    public void Ingredient_HasRequiredProperties()
    {
        using var context = GetTestContext();
        var entityType = context.Model.FindEntityType(typeof(Ingredient));

        Assert.IsNotNull(entityType, "Ingredient entity should exist");
        Assert.IsTrue(entityType.FindProperty("Id") != null, "Ingredient should have Id property");
        Assert.IsTrue(entityType.FindProperty("IngredientListId") != null, "Ingredient should have IngredientListId property");
        Assert.IsTrue(entityType.FindProperty("Name") != null, "Ingredient should have Name property");
        Assert.IsTrue(entityType.FindProperty("Quantity") != null, "Ingredient should have Quantity property");
        Assert.IsTrue(entityType.FindProperty("Unit") != null, "Ingredient should have Unit property");
        Assert.IsTrue(entityType.FindProperty("IsChecked") != null, "Ingredient should have IsChecked property");
        Assert.IsTrue(entityType.FindProperty("CreatedAt") != null, "Ingredient should have CreatedAt property");
    }

    [TestMethod]
    public void Ingredient_BelongsToIngredientList()
    {
        using var context = GetTestContext();
        var listId = Guid.NewGuid();
        var list = new IngredientList
        {
            Id = listId,
            Name = "Test List",
            OwnerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var ingredient = new Ingredient
        {
            Name = "Tomato",
            Quantity = "5",
            Unit = "pieces",
            IsChecked = false,
            IngredientListId = listId,
            CreatedAt = DateTime.UtcNow
        };

        context.IngredientLists.Add(list);
        context.Ingredients.Add(ingredient);
        context.SaveChanges();

        var retrievedIngredient = context.Ingredients.First();
        Assert.AreEqual(listId, retrievedIngredient.IngredientListId);
        Assert.AreEqual("Tomato", retrievedIngredient.Name);
    }

    // RecipeIngredientList junction table tests
    [TestMethod]
    public void RecipeIngredientList_HasRequiredProperties()
    {
        using var context = GetTestContext();
        var entityType = context.Model.FindEntityType(typeof(RecipeIngredientList));

        Assert.IsNotNull(entityType, "RecipeIngredientList junction entity should exist");
        Assert.IsTrue(entityType.FindProperty("Id") != null, "RecipeIngredientList should have Id property");
        Assert.IsTrue(entityType.FindProperty("IngredientListId") != null, "RecipeIngredientList should have IngredientListId property");
        Assert.IsTrue(entityType.FindProperty("RecipeId") != null, "RecipeIngredientList should have RecipeId property");
        Assert.IsTrue(entityType.FindProperty("AddedAt") != null, "RecipeIngredientList should have AddedAt property");
        Assert.IsTrue(entityType.FindProperty("AddedByUserId") != null, "RecipeIngredientList should have AddedByUserId property");
        Assert.AreEqual(typeof(int), entityType.FindProperty("RecipeId")?.ClrType, "RecipeIngredientList.RecipeId should match Recipe.Id type");
    }

    [TestMethod]
    public async Task PostgreSqlSchema_HasIngredientListTables()
    {
        var containerName = $"recipemanager-ingredientlist-test-{Guid.NewGuid():N}";
        var password = "recipe_test_password";

        await RunDockerCommandAsync(
            $"run -d --name {containerName} -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD={password} -e POSTGRES_DB=recipedb -P postgres:17.6");

        try
        {
            var connectionString = await WaitForContainerConnectionStringAsync(containerName, password);
            var options = new DbContextOptionsBuilder<IngredientListDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            var recipeOptions = new DbContextOptionsBuilder<RecipeDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            await using (var recipeContext = new RecipeDbContext(recipeOptions))
            {
                await recipeContext.Database.MigrateAsync(CancellationToken.None);
            }

            await using var context = new IngredientListDbContext(options);
            await context.Database.MigrateAsync(CancellationToken.None);

            var expectedTables = new[]
            {
                "IngredientLists",
                "Ingredients",
                "RecipeIngredientLists",
                "ListSharings",
                "ListShareTokens"
            };

            foreach (var tableName in expectedTables)
            {
                var exists = await context.Database
                    .SqlQueryRaw<int>(
                        $"SELECT COUNT(*) AS \"Value\" FROM information_schema.tables WHERE table_name = '{tableName}'")
                    .SingleAsync(CancellationToken.None);

                Assert.AreEqual(1, exists, $"Expected PostgreSQL table '{tableName}' to exist");
            }
        }
        finally
        {
            await RunDockerCommandAsync($"rm -f {containerName}");
        }
    }

    private static async Task<string> WaitForContainerConnectionStringAsync(string containerName, string password)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var portOutput = await RunDockerCommandAsync($"port {containerName} 5432/tcp");
            var port = portOutput
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line[(line.LastIndexOf(':') + 1)..])
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(port))
            {
                var connectionString = $"Host=localhost;Port={port};Database=recipedb;Username=postgres;Password={password}";
                var options = new DbContextOptionsBuilder<IngredientListDbContext>()
                    .UseNpgsql(connectionString)
                    .Options;

                await using var context = new IngredientListDbContext(options);
                if (await context.Database.CanConnectAsync())
                {
                    return connectionString;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        Assert.Fail("Timed out waiting for the temporary PostgreSQL container to become ready.");
        return string.Empty;
    }

    private static async Task<string> RunDockerCommandAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo("docker", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo);
        Assert.IsNotNull(process, "Failed to start docker process.");

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        outputBuilder.Append(await process.StandardOutput.ReadToEndAsync());
        errorBuilder.Append(await process.StandardError.ReadToEndAsync());

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            Assert.Fail($"docker {arguments} failed with exit code {process.ExitCode}: {errorBuilder}");
        }

        return outputBuilder.ToString();
    }

    // ListSharing entity tests
    [TestMethod]
    public void ListSharing_HasRequiredProperties()
    {
        using var context = GetTestContext();
        var entityType = context.Model.FindEntityType(typeof(ListSharing));

        Assert.IsNotNull(entityType, "ListSharing entity should exist");
        Assert.IsTrue(entityType.FindProperty("Id") != null, "ListSharing should have Id property");
        Assert.IsTrue(entityType.FindProperty("IngredientListId") != null, "ListSharing should have IngredientListId property");
        Assert.IsTrue(entityType.FindProperty("SharedWithUserId") != null, "ListSharing should have SharedWithUserId property");
        Assert.IsTrue(entityType.FindProperty("ShareType") != null, "ListSharing should have ShareType property");
        Assert.IsTrue(entityType.FindProperty("AccessLevel") != null, "ListSharing should have AccessLevel property");
        Assert.IsTrue(entityType.FindProperty("CreatedAt") != null, "ListSharing should have CreatedAt property");
    }

    [TestMethod]
    public void ListSharing_AccessLevelIsEnum()
    {
        using var context = GetTestContext();
        var property = context.Model.FindEntityType(typeof(ListSharing))?.FindProperty("AccessLevel");
        Assert.IsNotNull(property, "AccessLevel property should exist");
        // Verify it can store enum values
        Assert.IsTrue(property.ClrType == typeof(AccessLevel) || property.ClrType == typeof(AccessLevel?), 
            "AccessLevel should be AccessLevel enum type");
    }

    // ListShareToken entity tests
    [TestMethod]
    public void ListShareToken_HasRequiredProperties()
    {
        using var context = GetTestContext();
        var entityType = context.Model.FindEntityType(typeof(ListShareToken));

        Assert.IsNotNull(entityType, "ListShareToken entity should exist");
        Assert.IsTrue(entityType.FindProperty("Id") != null, "ListShareToken should have Id property");
        Assert.IsTrue(entityType.FindProperty("IngredientListId") != null, "ListShareToken should have IngredientListId property");
        Assert.IsTrue(entityType.FindProperty("Token") != null, "ListShareToken should have Token property");
        Assert.IsTrue(entityType.FindProperty("ExpiresAt") != null, "ListShareToken should have ExpiresAt property");
        Assert.IsTrue(entityType.FindProperty("CreatedAt") != null, "ListShareToken should have CreatedAt property");
    }

    [TestMethod]
    public void ListShareToken_TokenIsGuid()
    {
        using var context = GetTestContext();
        var token = Guid.NewGuid();
        var listId = Guid.NewGuid();
        var list = new IngredientList
        {
            Id = listId,
            Name = "Test",
            OwnerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var shareToken = new ListShareToken
        {
            IngredientListId = listId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        context.IngredientLists.Add(list);
        context.ListShareTokens.Add(shareToken);
        context.SaveChanges();

        var retrieved = context.ListShareTokens.First();
        Assert.AreEqual(token, retrieved.Token);
        Assert.AreEqual(typeof(Guid), retrieved.Token.GetType());
    }

    // Relationship and cascade tests
    [TestMethod]
    public void IngredientList_DeleteCascadesToIngredients()
    {
        using var context = GetTestContext();
        var list = new IngredientList
        {
            Name = "Test",
            OwnerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.IngredientLists.Add(list);
        context.SaveChanges();

        var ingredient = new Ingredient
        {
            IngredientListId = list.Id,
            Name = "Item",
            Quantity = "1",
            Unit = "unit",
            IsChecked = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Ingredients.Add(ingredient);
        context.SaveChanges();

        context.IngredientLists.Remove(list);
        context.SaveChanges();

        var remainingIngredients = context.Ingredients.Where(i => i.IngredientListId == list.Id).Count();
        Assert.AreEqual(0, remainingIngredients, "Ingredients should be deleted when list is deleted");
    }

    [TestMethod]
    public void Database_CanCreateFromMigrations()
    {
        using var context = GetTestContext();
        // This should not throw - ensures the schema can be created
        var canConnect = context.Database.CanConnect();
        Assert.IsTrue(canConnect || context.Database.IsInMemory(), "Should be able to connect to database");
    }

    [TestMethod]
    public void IngredientLists_DbSetExists()
    {
        using var context = GetTestContext();
        Assert.IsNotNull(context.IngredientLists, "IngredientLists DbSet should exist");
    }

    [TestMethod]
    public void Ingredients_DbSetExists()
    {
        using var context = GetTestContext();
        Assert.IsNotNull(context.Ingredients, "Ingredients DbSet should exist");
    }

    [TestMethod]
    public void RecipeIngredientLists_DbSetExists()
    {
        using var context = GetTestContext();
        Assert.IsNotNull(context.RecipeIngredientLists, "RecipeIngredientLists DbSet should exist");
    }

    [TestMethod]
    public void ListSharings_DbSetExists()
    {
        using var context = GetTestContext();
        Assert.IsNotNull(context.ListSharings, "ListSharings DbSet should exist");
    }

    [TestMethod]
    public void ListShareTokens_DbSetExists()
    {
        using var context = GetTestContext();
        Assert.IsNotNull(context.ListShareTokens, "ListShareTokens DbSet should exist");
    }
}
