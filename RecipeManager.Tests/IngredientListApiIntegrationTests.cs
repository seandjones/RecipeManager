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

    private WebApplicationFactory<ApiServiceProgram> Factory => _factory ?? throw new InvalidOperationException("Test factory is not initialized.");
    private HttpClient Client => _client ?? throw new InvalidOperationException("Test client is not initialized.");

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

        var createResponse = await Client.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "Weekly Grocery",
            Description = "Initial description"
        });

        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(created);

        var getAllResponse = await Client.GetAsync("/api/ingredient-lists");
        Assert.AreEqual(HttpStatusCode.OK, getAllResponse.StatusCode);
        var allLists = await getAllResponse.Content.ReadFromJsonAsync<List<IngredientListSummaryResponse>>();
        Assert.IsNotNull(allLists);
        Assert.IsTrue(allLists.Any(l => l.Id == created!.Id));

        var getByIdResponse = await Client.GetAsync($"/api/ingredient-lists/{created!.Id}");
        Assert.AreEqual(HttpStatusCode.OK, getByIdResponse.StatusCode);
        var detail = await getByIdResponse.Content.ReadFromJsonAsync<IngredientListDetailResponse>();
        Assert.IsNotNull(detail);
        Assert.AreEqual(created.Id, detail.Id);

        var updateResponse = await Client.PutAsJsonAsync($"/api/ingredient-lists/{created.Id}", new IngredientListRequest
        {
            Name = "Weekly Grocery Updated",
            Description = "Updated description"
        });

        Assert.AreEqual(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(updated);
        Assert.AreEqual("Weekly Grocery Updated", updated.Name);

        var deleteResponse = await Client.DeleteAsync($"/api/ingredient-lists/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [TestMethod]
    public async Task IngredientAndRecipeEndpoints_Work_ForAuthorizedUser()
    {
        var userId = Guid.NewGuid();
        AddUserHeader(userId);

        var createListResponse = await Client.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "Meal Prep",
            Description = "List for testing ingredient endpoints"
        });

        var list = await createListResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(list);

        var createRecipeResponse = await Client.PostAsJsonAsync("/api/recipes", new RecipeRequest
        {
            Name = "Test Recipe",
            Description = "Recipe for linking",
            Ingredients = "Eggs, Milk",
            Instructions = "Mix and cook"
        });

        Assert.AreEqual(HttpStatusCode.Created, createRecipeResponse.StatusCode);
        var recipe = await createRecipeResponse.Content.ReadFromJsonAsync<Recipe>();
        Assert.IsNotNull(recipe);

        var addIngredientResponse = await Client.PostAsJsonAsync($"/api/ingredient-lists/{list!.Id}/ingredients", new IngredientRequest
        {
            Name = "Tomato",
            Quantity = "2",
            Unit = "pcs",
            IsChecked = false
        });

        Assert.AreEqual(HttpStatusCode.Created, addIngredientResponse.StatusCode);
        var ingredient = await addIngredientResponse.Content.ReadFromJsonAsync<IngredientItemResponse>();
        Assert.IsNotNull(ingredient);

        var updateIngredientResponse = await Client.PutAsJsonAsync($"/api/ingredient-lists/{list.Id}/ingredients/{ingredient!.Id}", new IngredientRequest
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

        var addRecipeLinkResponse = await Client.PostAsJsonAsync($"/api/ingredient-lists/{list.Id}/recipes", new RecipeLinkRequest
        {
            RecipeId = recipe!.Id
        });

        Assert.AreEqual(HttpStatusCode.OK, addRecipeLinkResponse.StatusCode);

        var getListResponse = await Client.GetAsync($"/api/ingredient-lists/{list.Id}");
        var detail = await getListResponse.Content.ReadFromJsonAsync<IngredientListDetailResponse>();
        Assert.IsNotNull(detail);
        Assert.IsTrue(detail.Recipes.Any(r => r.Id == recipe.Id));

        var removeRecipeLinkResponse = await Client.DeleteAsync($"/api/ingredient-lists/{list.Id}/recipes/{recipe.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, removeRecipeLinkResponse.StatusCode);

        var deleteIngredientResponse = await Client.DeleteAsync($"/api/ingredient-lists/{list.Id}/ingredients/{ingredient.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteIngredientResponse.StatusCode);
    }

    [TestMethod]
    public async Task OwnerOnlyEndpoints_Reject_NonOwner()
    {
        var ownerId = Guid.NewGuid();
        AddUserHeader(ownerId);

        var createResponse = await Client.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "Owner List",
            Description = "Owner-only checks"
        });

        var created = await createResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(created);

        Client.DefaultRequestHeaders.Remove("X-User-Id");
        AddUserHeader(Guid.NewGuid());

        var updateResponse = await Client.PutAsJsonAsync($"/api/ingredient-lists/{created!.Id}", new IngredientListRequest
        {
            Name = "Hacker Edit",
            Description = "Should fail"
        });

        Assert.AreEqual(HttpStatusCode.Forbidden, updateResponse.StatusCode);

        var deleteResponse = await Client.DeleteAsync($"/api/ingredient-lists/{created.Id}");
        Assert.AreEqual(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [TestMethod]
    public async Task SharedUser_CanAccess_List()
    {
        var ownerId = Guid.NewGuid();
        AddUserHeader(ownerId);

        var createResponse = await Client.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "Shared List",
            Description = "Share access check"
        });

        var list = await createResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(list);

        var sharedUserId = Guid.NewGuid();
        using var scope = Factory.Services.CreateScope();
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

        Client.DefaultRequestHeaders.Remove("X-User-Id");
        AddUserHeader(sharedUserId);

        var getResponse = await Client.GetAsync($"/api/ingredient-lists/{list.Id}");
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);

        var addIngredientResponse = await Client.PostAsJsonAsync($"/api/ingredient-lists/{list.Id}/ingredients", new IngredientRequest
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
        var createResponse = await Client.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
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
        var response = await Client.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
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

        var createListResponse = await Client.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "Validation List",
            Description = "For invalid ingredient checks"
        });

        var list = await createListResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(list);

        var tooLongUnit = new string('u', 51);
        var response = await Client.PostAsJsonAsync($"/api/ingredient-lists/{list!.Id}/ingredients", new IngredientRequest
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

    [TestMethod]
    public async Task ShareViaEmail_CreatesShare_And_SendsInvitation()
    {
        var ownerId = Guid.NewGuid();
        AddUserHeader(ownerId);

        var createListResponse = await Client.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "Shareable List",
            Description = "Email invite flow"
        });

        var list = await createListResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(list);

        var inviteEmail = "collab@example.com";
        var shareResponse = await Client.PostAsJsonAsync($"/api/ingredient-lists/{list!.Id}/share/email", new ShareIngredientListByEmailRequest
        {
            Email = inviteEmail,
            AccessLevel = "Editor"
        });

        Assert.AreEqual(HttpStatusCode.OK, shareResponse.StatusCode);

        var testEmailService = Factory.Services.GetRequiredService<IEmailService>() as TestEmailService;
        Assert.IsNotNull(testEmailService);
        Assert.AreEqual(inviteEmail, testEmailService!.LastSentEmail);
        Assert.AreEqual("Shareable List", testEmailService.LastShareListName);
        Assert.AreEqual(AccessLevel.Editor, testEmailService.LastShareAccessLevel);
        Assert.IsFalse(string.IsNullOrWhiteSpace(testEmailService.LastShareUrl));

        var sharingListResponse = await Client.GetAsync($"/api/ingredient-lists/{list.Id}/sharing");
        Assert.AreEqual(HttpStatusCode.OK, sharingListResponse.StatusCode);

        var shares = await sharingListResponse.Content.ReadFromJsonAsync<List<IngredientListShareResponse>>();
        Assert.IsNotNull(shares);
        Assert.IsTrue(shares.Any(s => s.ShareType == "Email" && s.SharedWithEmail == inviteEmail && s.AccessLevel == "Editor"));
    }

    [TestMethod]
    public async Task ShareLink_AllowsTokenAccess_And_RejectsExpiredLink()
    {
        AddUserHeader(Guid.NewGuid());

        var createListResponse = await Client.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "Link Shared List",
            Description = "Share token flow"
        });

        var list = await createListResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(list);

        var generateLinkResponse = await Client.PostAsJsonAsync($"/api/ingredient-lists/{list!.Id}/share/link", new CreateIngredientListShareLinkRequest
        {
            AccessLevel = "Viewer",
            ExpiresInDays = 2
        });

        Assert.AreEqual(HttpStatusCode.OK, generateLinkResponse.StatusCode);
        var linkPayload = await generateLinkResponse.Content.ReadFromJsonAsync<IngredientListShareLinkResponse>();
        Assert.IsNotNull(linkPayload);
        StringAssert.Contains(linkPayload!.Url, "/ingredient-lists/shared/");

        var sharedAccessResponse = await Client.GetAsync($"/api/ingredient-lists/shared/{linkPayload!.Token}");
        Assert.AreEqual(HttpStatusCode.OK, sharedAccessResponse.StatusCode);
        var sharedPayload = await sharedAccessResponse.Content.ReadFromJsonAsync<SharedIngredientListAccessResponse>();
        Assert.IsNotNull(sharedPayload);
        Assert.AreEqual("Viewer", sharedPayload.AccessLevel);
        Assert.IsFalse(sharedPayload.CanEdit);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IngredientListDbContext>();
            var token = await db.ListShareTokens.SingleAsync(t => t.Token == linkPayload.Token);
            token.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var expiredResponse = await Client.GetAsync($"/api/ingredient-lists/shared/{linkPayload.Token}");
        Assert.AreEqual(HttpStatusCode.BadRequest, expiredResponse.StatusCode);
    }

    [TestMethod]
    public async Task ShareTokenEditor_CanModifyIngredients_ViewerCannot()
    {
        AddUserHeader(Guid.NewGuid());

        var createListResponse = await Client.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "Token Edit List",
            Description = "Editor and viewer behavior"
        });

        var list = await createListResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(list);

        var editorLinkResponse = await Client.PostAsJsonAsync($"/api/ingredient-lists/{list!.Id}/share/link", new CreateIngredientListShareLinkRequest
        {
            AccessLevel = "Editor",
            ExpiresInDays = 2
        });
        Assert.AreEqual(HttpStatusCode.OK, editorLinkResponse.StatusCode);

        var editorLink = await editorLinkResponse.Content.ReadFromJsonAsync<IngredientListShareLinkResponse>();
        Assert.IsNotNull(editorLink);

        var addByEditorResponse = await Client.PostAsJsonAsync($"/api/ingredient-lists/shared/{editorLink!.Token}/ingredients", new IngredientRequest
        {
            Name = "Flour",
            Quantity = "2",
            Unit = "cups",
            IsChecked = false
        });
        Assert.AreEqual(HttpStatusCode.Created, addByEditorResponse.StatusCode);

        var addedIngredient = await addByEditorResponse.Content.ReadFromJsonAsync<IngredientItemResponse>();
        Assert.IsNotNull(addedIngredient);

        var updateByEditorResponse = await Client.PutAsJsonAsync($"/api/ingredient-lists/shared/{editorLink.Token}/ingredients/{addedIngredient!.Id}", new IngredientRequest
        {
            Name = "Flour",
            Quantity = "3",
            Unit = "cups",
            IsChecked = true
        });
        Assert.AreEqual(HttpStatusCode.OK, updateByEditorResponse.StatusCode);

        var viewerLinkResponse = await Client.PostAsJsonAsync($"/api/ingredient-lists/{list.Id}/share/link", new CreateIngredientListShareLinkRequest
        {
            AccessLevel = "Viewer",
            ExpiresInDays = 2
        });
        Assert.AreEqual(HttpStatusCode.OK, viewerLinkResponse.StatusCode);

        var viewerLink = await viewerLinkResponse.Content.ReadFromJsonAsync<IngredientListShareLinkResponse>();
        Assert.IsNotNull(viewerLink);

        var addByViewerResponse = await Client.PostAsJsonAsync($"/api/ingredient-lists/shared/{viewerLink!.Token}/ingredients", new IngredientRequest
        {
            Name = "Salt",
            IsChecked = false
        });
        Assert.AreEqual(HttpStatusCode.Forbidden, addByViewerResponse.StatusCode);
    }

    [TestMethod]
    public async Task RevokeShare_RemovesSharedUserAccess()
    {
        var ownerId = Guid.NewGuid();
        AddUserHeader(ownerId);

        var createListResponse = await Client.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "Revocable List",
            Description = "Revoke sharing flow"
        });

        var list = await createListResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(list);

        var sharedEmail = "remove-me@example.com";
        var shareResponse = await Client.PostAsJsonAsync($"/api/ingredient-lists/{list!.Id}/share/email", new ShareIngredientListByEmailRequest
        {
            Email = sharedEmail,
            AccessLevel = "Viewer"
        });

        Assert.AreEqual(HttpStatusCode.OK, shareResponse.StatusCode);

        var sharesResponse = await Client.GetAsync($"/api/ingredient-lists/{list.Id}/sharing");
        Assert.AreEqual(HttpStatusCode.OK, sharesResponse.StatusCode);
        var shares = await sharesResponse.Content.ReadFromJsonAsync<List<IngredientListShareResponse>>();
        Assert.IsNotNull(shares);

        var emailShare = shares!.FirstOrDefault(s => s.ShareType == "Email" && s.SharedWithEmail == sharedEmail);
        Assert.IsNotNull(emailShare);

        var revokeResponse = await Client.DeleteAsync($"/api/ingredient-lists/{list.Id}/sharing/{emailShare!.ShareId}");
        Assert.AreEqual(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        Guid sharedUserId;
        using (var scope = Factory.Services.CreateScope())
        {
            var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var sharedUser = await authDb.Users.SingleAsync(u => u.Email == sharedEmail);
            sharedUserId = sharedUser.Id;
        }

        Client.DefaultRequestHeaders.Remove("X-User-Id");
        AddUserHeader(sharedUserId);

        var getAfterRevoke = await Client.GetAsync($"/api/ingredient-lists/{list.Id}");
        Assert.AreEqual(HttpStatusCode.Forbidden, getAfterRevoke.StatusCode);
    }

    [TestMethod]
    public async Task ViewerSharedUser_CanReadList_ButCannotModifyIngredients()
    {
        var ownerId = Guid.NewGuid();
        AddUserHeader(ownerId);

        var createResponse = await Client.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "View Only List",
            Description = "Viewer access test"
        });

        var list = await createResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(list);

        var viewerUserId = Guid.NewGuid();
        using var setupScope = Factory.Services.CreateScope();
        var db = setupScope.ServiceProvider.GetRequiredService<IngredientListDbContext>();
        db.ListSharings.Add(new ListSharing
        {
            Id = Guid.NewGuid(),
            IngredientListId = list!.Id,
            SharedWithUserId = viewerUserId,
            ShareType = "Email",
            AccessLevel = AccessLevel.Viewer,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        Client.DefaultRequestHeaders.Remove("X-User-Id");
        AddUserHeader(viewerUserId);

        var getResponse = await Client.GetAsync($"/api/ingredient-lists/{list.Id}");
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);

        var addIngredientResponse = await Client.PostAsJsonAsync($"/api/ingredient-lists/{list.Id}/ingredients", new IngredientRequest
        {
            Name = "Viewer Item",
            Quantity = "1",
            Unit = "pcs",
            IsChecked = false
        });

        Assert.AreEqual(HttpStatusCode.Forbidden, addIngredientResponse.StatusCode);
    }

    [TestMethod]
    public async Task NonOwner_CannotShareList_ViaEmail()
    {
        var ownerId = Guid.NewGuid();
        AddUserHeader(ownerId);

        var createResponse = await Client.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "Owner-Protected List",
            Description = "Share authorization check"
        });

        var list = await createResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(list);

        Client.DefaultRequestHeaders.Remove("X-User-Id");
        AddUserHeader(Guid.NewGuid());

        var shareResponse = await Client.PostAsJsonAsync($"/api/ingredient-lists/{list!.Id}/share/email", new ShareIngredientListByEmailRequest
        {
            Email = "attacker@example.com",
            AccessLevel = "Editor"
        });

        Assert.AreEqual(HttpStatusCode.Forbidden, shareResponse.StatusCode);
    }

    [TestMethod]
    public async Task GetSharingInfo_RequiresOwner_RejectNonOwner()
    {
        var ownerId = Guid.NewGuid();
        AddUserHeader(ownerId);

        var createResponse = await Client.PostAsJsonAsync("/api/ingredient-lists", new IngredientListRequest
        {
            Name = "Sharing Info List",
            Description = "Owner-only sharing info"
        });

        var list = await createResponse.Content.ReadFromJsonAsync<IngredientListSummaryResponse>();
        Assert.IsNotNull(list);

        var sharingInfoOwnerResponse = await Client.GetAsync($"/api/ingredient-lists/{list!.Id}/sharing");
        Assert.AreEqual(HttpStatusCode.OK, sharingInfoOwnerResponse.StatusCode);

        Client.DefaultRequestHeaders.Remove("X-User-Id");
        AddUserHeader(Guid.NewGuid());

        var sharingInfoNonOwnerResponse = await Client.GetAsync($"/api/ingredient-lists/{list.Id}/sharing");
        Assert.AreEqual(HttpStatusCode.Forbidden, sharingInfoNonOwnerResponse.StatusCode);
    }

    private void AddUserHeader(Guid userId)
    {
        Client.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());
    }
}
