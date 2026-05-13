using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecipeManager.ApiService.Data;
using RecipeManager.ApiService.Models;
using RecipeManager.ApiService.Services;
using ApiServiceProgram = RecipeManager.ApiService.Program;

namespace RecipeManager.Tests;

[TestClass]
public class IngredientListApiIntegrationTests
{
    private WebApplicationFactory<ApiServiceProgram>? _factory;
    private HttpClient? _client;

    [TestInitialize]
    public void Initialize()
    {
        var inMemoryDbName = Guid.NewGuid().ToString();
        var sharedDataDbName = inMemoryDbName + "_Data";

        _factory = new WebApplicationFactory<ApiServiceProgram>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var toRemove = services
                        .Where(s => s.ServiceType.FullName?.Contains("EntityFrameworkCore") == true
                            || s.ServiceType == typeof(AuthDbContext)
                            || s.ServiceType == typeof(RecipeDbContext)
                            || s.ServiceType == typeof(IngredientListDbContext)
                            || s.ServiceType == typeof(DbContextOptions<AuthDbContext>)
                            || s.ServiceType == typeof(DbContextOptions<RecipeDbContext>)
                            || s.ServiceType == typeof(DbContextOptions<IngredientListDbContext>))
                        .ToList();

                    foreach (var service in toRemove)
                    {
                        services.Remove(service);
                    }

                    services.AddDbContext<AuthDbContext>(options =>
                        options.UseInMemoryDatabase(inMemoryDbName + "_Auth"),
                        contextLifetime: ServiceLifetime.Scoped,
                        optionsLifetime: ServiceLifetime.Scoped);

                    services.AddDbContext<RecipeDbContext>(options =>
                        options.UseInMemoryDatabase(sharedDataDbName),
                        contextLifetime: ServiceLifetime.Scoped,
                        optionsLifetime: ServiceLifetime.Scoped);

                    services.AddDbContext<IngredientListDbContext>(options =>
                        options.UseInMemoryDatabase(sharedDataDbName),
                        contextLifetime: ServiceLifetime.Scoped,
                        optionsLifetime: ServiceLifetime.Scoped);

                    var emailServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
                    if (emailServiceDescriptor != null)
                    {
                        services.Remove(emailServiceDescriptor);
                    }
                    services.AddSingleton<IEmailService, TestEmailService>();
                });
            });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [TestMethod]
    public async Task CreateGetUpdateDelete_IngredientList_Flow_Works()
    {
        var userId = Guid.NewGuid();
        AddUserHeader(userId);

        var createResponse = await _client!.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "Weekly Grocery",
            Description = "Initial description"
        });

        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(created);

        var getAllResponse = await _client.GetAsync("/api/ingredient-lists");
        Assert.AreEqual(HttpStatusCode.OK, getAllResponse.StatusCode);
        var allLists = await getAllResponse.Content.ReadFromJsonAsync<List<IngredientListSummaryResponse>>();
        Assert.IsNotNull(allLists);
        Assert.IsTrue(allLists.Any(l => l.Id == created!.Id));

        var getByIdResponse = await _client.GetAsync($"/api/ingredient-lists/{created!.Id}");
        Assert.AreEqual(HttpStatusCode.OK, getByIdResponse.StatusCode);
        var detail = await getByIdResponse.Content.ReadFromJsonAsync<IngredientListDetailResponse>();
        Assert.IsNotNull(detail);
        Assert.AreEqual(created.Id, detail.Id);

        var updateResponse = await _client.PutAsJsonAsync($"/api/ingredient-lists/{created.Id}", new IngredientListRequest
        {
            Name = "Weekly Grocery Updated",
            Description = "Updated description"
        });

        Assert.AreEqual(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(updated);
        Assert.AreEqual("Weekly Grocery Updated", updated.Name);

        var deleteResponse = await _client.DeleteAsync($"/api/ingredient-lists/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [TestMethod]
    public async Task IngredientAndRecipeEndpoints_Work_ForAuthorizedUser()
    {
        var userId = Guid.NewGuid();
        AddUserHeader(userId);

        var createListResponse = await _client!.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "Meal Prep",
            Description = "List for testing ingredient endpoints"
        });

        var list = await createListResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(list);

        var createRecipeResponse = await _client.PostAsJsonAsync("/api/recipes", new RecipeRequest
        {
            Name = "Test Recipe",
            Description = "Recipe for linking",
            Ingredients = "Eggs, Milk",
            Instructions = "Mix and cook"
        });

        Assert.AreEqual(HttpStatusCode.Created, createRecipeResponse.StatusCode);
        var recipe = await createRecipeResponse.Content.ReadFromJsonAsync<Recipe>();
        Assert.IsNotNull(recipe);

        var addIngredientResponse = await _client.PostAsJsonAsync($"/api/ingredient-lists/{list!.Id}/ingredients", new IngredientRequest
        {
            Name = "Tomato",
            Quantity = "2",
            Unit = "pcs",
            IsChecked = false
        });

        Assert.AreEqual(HttpStatusCode.Created, addIngredientResponse.StatusCode);
        var ingredient = await addIngredientResponse.Content.ReadFromJsonAsync<IngredientItemResponse>();
        Assert.IsNotNull(ingredient);

        var updateIngredientResponse = await _client.PutAsJsonAsync($"/api/ingredient-lists/{list.Id}/ingredients/{ingredient!.Id}", new IngredientRequest
        {
            Name = "Tomato",
            Quantity = "3",
            Unit = "pcs",
            IsChecked = true
        });

        Assert.AreEqual(HttpStatusCode.OK, updateIngredientResponse.StatusCode);
        var updatedIngredient = await updateIngredientResponse.Content.ReadFromJsonAsync<IngredientItemResponse>();
        Assert.IsNotNull(updatedIngredient);
        Assert.IsTrue(updatedIngredient.IsChecked);
        Assert.AreEqual("3", updatedIngredient.Quantity);

        var addRecipeLinkResponse = await _client.PostAsJsonAsync($"/api/ingredient-lists/{list.Id}/recipes", new RecipeLinkRequest
        {
            RecipeId = recipe!.Id
        });

        Assert.AreEqual(HttpStatusCode.OK, addRecipeLinkResponse.StatusCode);

        var getListResponse = await _client.GetAsync($"/api/ingredient-lists/{list.Id}");
        var detail = await getListResponse.Content.ReadFromJsonAsync<IngredientListDetailResponse>();
        Assert.IsNotNull(detail);
        Assert.IsTrue(detail.Recipes.Any(r => r.Id == recipe.Id));

        var removeRecipeLinkResponse = await _client.DeleteAsync($"/api/ingredient-lists/{list.Id}/recipes/{recipe.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, removeRecipeLinkResponse.StatusCode);

        var deleteIngredientResponse = await _client.DeleteAsync($"/api/ingredient-lists/{list.Id}/ingredients/{ingredient.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteIngredientResponse.StatusCode);
    }

    [TestMethod]
    public async Task OwnerOnlyEndpoints_Reject_NonOwner()
    {
        var ownerId = Guid.NewGuid();
        AddUserHeader(ownerId);

        var createResponse = await _client!.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "Owner List",
            Description = "Owner-only checks"
        });

        var created = await createResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(created);

        _client.DefaultRequestHeaders.Remove("X-User-Id");
        AddUserHeader(Guid.NewGuid());

        var updateResponse = await _client.PutAsJsonAsync($"/api/ingredient-lists/{created!.Id}", new IngredientListRequest
        {
            Name = "Hacker Edit",
            Description = "Should fail"
        });

        Assert.AreEqual(HttpStatusCode.Forbidden, updateResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/api/ingredient-lists/{created.Id}");
        Assert.AreEqual(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [TestMethod]
    public async Task SharedUser_CanAccess_List()
    {
        var ownerId = Guid.NewGuid();
        AddUserHeader(ownerId);

        var createResponse = await _client!.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "Shared List",
            Description = "Share access check"
        });

        var list = await createResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(list);

        var sharedUserId = Guid.NewGuid();
        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IngredientListDbContext>();
        db.ListSharings.Add(new ListSharing
        {
            Id = Guid.NewGuid(),
            IngredientListId = list!.Id,
            SharedWithUserId = sharedUserId,
            ShareType = "Email",
            AccessLevel = AccessLevel.Editor,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        _client.DefaultRequestHeaders.Remove("X-User-Id");
        AddUserHeader(sharedUserId);

        var getResponse = await _client.GetAsync($"/api/ingredient-lists/{list.Id}");
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);

        var addIngredientResponse = await _client.PostAsJsonAsync($"/api/ingredient-lists/{list.Id}/ingredients", new IngredientRequest
        {
            Name = "Shared Item",
            Quantity = "1",
            Unit = "pack",
            IsChecked = false
        });

        Assert.AreEqual(HttpStatusCode.Created, addIngredientResponse.StatusCode);
    }

    [TestMethod]
    public async Task Endpoints_RequireUserId()
    {
        var createResponse = await _client!.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "No User",
            Description = "Should fail"
        });

        Assert.AreEqual(HttpStatusCode.Unauthorized, createResponse.StatusCode);
    }

    [TestMethod]
    public async Task CreateList_WithInvalidPayload_ReturnsBadRequestWithMessage()
    {
        AddUserHeader(Guid.NewGuid());

        var tooLongName = new string('a', 256);
        var response = await _client!.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = tooLongName,
            Description = "invalid"
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.IsNotNull(payload);
        Assert.AreEqual("Name cannot exceed 255 characters.", payload["error"]);
    }

    [TestMethod]
    public async Task AddIngredient_WithInvalidPayload_ReturnsBadRequestWithMessage()
    {
        AddUserHeader(Guid.NewGuid());

        var createListResponse = await _client!.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "Validation List",
            Description = "For invalid ingredient checks"
        });

        var list = await createListResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(list);

        var tooLongUnit = new string('u', 51);
        var response = await _client.PostAsJsonAsync($"/api/ingredient-lists/{list!.Id}/ingredients", new IngredientRequest
        {
            Name = "Valid Name",
            Quantity = "1",
            Unit = tooLongUnit,
            IsChecked = false
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.IsNotNull(payload);
        Assert.AreEqual("Unit cannot exceed 50 characters.", payload["error"]);
    }

    private void AddUserHeader(Guid userId)
    {
        _client!.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());
    }
}
