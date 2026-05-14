using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using RecipeManager.Web.Models;
using RecipeManager.Web.Components.Pages;
using RecipeManager.Web.Services;
using System.Net;
using System.Security.Claims;
using System.Reflection;
using System.Text.Json;

namespace RecipeManager.Tests;

[TestClass]
public class IngredientListApiClientTests
{
    [TestMethod]
    public async Task GetListsAsync_WithOwnedAndSharedLists_ReturnsBoth()
    {
        var payload = new object[]
        {
            new
            {
                Id = Guid.NewGuid(),
                Name = "Owned List",
                Description = "Owned",
                OwnerId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new
            {
                Id = Guid.NewGuid(),
                Name = "Shared List",
                Description = "Shared",
                OwnerId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var client = CreateClient(HttpStatusCode.OK, payload);

        var result = await client.GetListsAsync();

        Assert.AreEqual(2, result.Length);
        Assert.IsTrue(result.Any(l => l.Name == "Owned List"));
        Assert.IsTrue(result.Any(l => l.Name == "Shared List"));
    }

    [TestMethod]
    public async Task GenerateShareLinkAsync_WithSuccessResponse_ReturnsLinkPayload()
    {
        var expectedToken = Guid.NewGuid();
        var payload = new
        {
            Token = expectedToken,
            Url = $"https://example.test/api/ingredient-lists/shared/{expectedToken}",
            AccessLevel = "Viewer",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        var client = CreateClient(HttpStatusCode.OK, payload);

        var result = await client.GenerateShareLinkAsync(Guid.NewGuid(), "Viewer");

        Assert.IsNotNull(result);
        Assert.AreEqual(expectedToken, result.Token);
        Assert.AreEqual("Viewer", result.AccessLevel);
    }

    [TestMethod]
    public async Task GetSharedAccessAsync_WithNotFound_ReturnsNull()
    {
        var client = CreateClient(HttpStatusCode.NotFound);

        var result = await client.GetSharedAccessAsync(Guid.NewGuid());

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetListAsync_WithSuccessResponse_MapsIngredientsAndRecipes()
    {
        var listId = Guid.NewGuid();
        var payload = new
        {
            Id = listId,
            Name = "Detail List",
            Description = "Has details",
            OwnerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Ingredients = new[] { new { Id = Guid.NewGuid(), Name = "Tomato", Quantity = "2", Unit = "pcs", IsChecked = false, CreatedAt = DateTime.UtcNow } },
            Recipes = new[] { new { Id = 42, Name = "Soup", Description = "Hot" } }
        };

        var client = CreateClient(
            HttpStatusCode.OK,
            payload,
            request =>
            {
                Assert.AreEqual(HttpMethod.Get, request.Method);
                Assert.IsTrue(request.RequestUri!.AbsolutePath.EndsWith($"/api/ingredient-lists/{listId}"));
            });

        var result = await client.GetListAsync(listId);

        Assert.IsNotNull(result);
        Assert.AreEqual("Detail List", result!.Name);
        Assert.AreEqual(1, result.Ingredients.Count);
        Assert.AreEqual(1, result.Recipes.Count);
    }

    [TestMethod]
    public async Task CreateListAsync_WithSuccessResponse_ReturnsCreatedList()
    {
        var payload = new
        {
            Id = Guid.NewGuid(),
            Name = "Created",
            Description = "Created description",
            OwnerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var client = CreateClient(
            HttpStatusCode.Created,
            payload,
            request =>
            {
                Assert.AreEqual(HttpMethod.Post, request.Method);
                Assert.IsTrue(request.RequestUri!.AbsolutePath.EndsWith("/api/ingredient-lists"));
            });

        var result = await client.CreateListAsync(new IngredientListRequest { Name = "Created" });

        Assert.IsNotNull(result);
        Assert.AreEqual("Created", result!.Name);
    }

    [TestMethod]
    public async Task UpdateListAsync_WithSuccessResponse_ReturnsUpdatedList()
    {
        var listId = Guid.NewGuid();
        var payload = new
        {
            Id = listId,
            Name = "Updated",
            Description = "Updated description",
            OwnerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var client = CreateClient(
            HttpStatusCode.OK,
            payload,
            request =>
            {
                Assert.AreEqual(HttpMethod.Put, request.Method);
                Assert.IsTrue(request.RequestUri!.AbsolutePath.EndsWith($"/api/ingredient-lists/{listId}"));
            });

        var result = await client.UpdateListAsync(listId, new IngredientListRequest { Name = "Updated" });

        Assert.IsNotNull(result);
        Assert.AreEqual("Updated", result!.Name);
    }

    [TestMethod]
    public async Task DeleteListAsync_WithSuccessResponse_ReturnsTrue()
    {
        var listId = Guid.NewGuid();
        var client = CreateClient(
            HttpStatusCode.NoContent,
            null,
            request =>
            {
                Assert.AreEqual(HttpMethod.Delete, request.Method);
                Assert.IsTrue(request.RequestUri!.AbsolutePath.EndsWith($"/api/ingredient-lists/{listId}"));
            });

        var result = await client.DeleteListAsync(listId);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task AddIngredientAsync_WithSuccessResponse_ReturnsIngredient()
    {
        var listId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var payload = new
        {
            Id = ingredientId,
            Name = "Salt",
            Quantity = "1",
            Unit = "tsp",
            IsChecked = false,
            CreatedAt = DateTime.UtcNow
        };

        var client = CreateClient(
            HttpStatusCode.Created,
            payload,
            request =>
            {
                Assert.AreEqual(HttpMethod.Post, request.Method);
                Assert.IsTrue(request.RequestUri!.AbsolutePath.EndsWith($"/api/ingredient-lists/{listId}/ingredients"));
            });

        var result = await client.AddIngredientAsync(listId, new IngredientRequest { Name = "Salt" });

        Assert.IsNotNull(result);
        Assert.AreEqual(ingredientId, result!.Id);
    }

    [TestMethod]
    public async Task UpdateIngredientAsync_WithSuccessResponse_ReturnsIngredient()
    {
        var listId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var payload = new
        {
            Id = ingredientId,
            Name = "Pepper",
            Quantity = "2",
            Unit = "tsp",
            IsChecked = true,
            CreatedAt = DateTime.UtcNow
        };

        var client = CreateClient(
            HttpStatusCode.OK,
            payload,
            request =>
            {
                Assert.AreEqual(HttpMethod.Put, request.Method);
                Assert.IsTrue(request.RequestUri!.AbsolutePath.EndsWith($"/api/ingredient-lists/{listId}/ingredients/{ingredientId}"));
            });

        var result = await client.UpdateIngredientAsync(listId, ingredientId, new IngredientRequest { Name = "Pepper" });

        Assert.IsNotNull(result);
        Assert.IsTrue(result!.IsChecked);
    }

    [TestMethod]
    public async Task DeleteIngredientAsync_WithSuccessResponse_ReturnsTrue()
    {
        var listId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var client = CreateClient(
            HttpStatusCode.NoContent,
            null,
            request =>
            {
                Assert.AreEqual(HttpMethod.Delete, request.Method);
                Assert.IsTrue(request.RequestUri!.AbsolutePath.EndsWith($"/api/ingredient-lists/{listId}/ingredients/{ingredientId}"));
            });

        var result = await client.DeleteIngredientAsync(listId, ingredientId);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task AddRecipeToListAsync_WithSuccessResponse_ReturnsTrue()
    {
        var listId = Guid.NewGuid();
        var client = CreateClient(
            HttpStatusCode.OK,
            new { listId, recipeId = 7 },
            request =>
            {
                Assert.AreEqual(HttpMethod.Post, request.Method);
                Assert.IsTrue(request.RequestUri!.AbsolutePath.EndsWith($"/api/ingredient-lists/{listId}/recipes"));
            });

        var result = await client.AddRecipeToListAsync(listId, 7);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task RemoveRecipeFromListAsync_WithSuccessResponse_ReturnsTrue()
    {
        var listId = Guid.NewGuid();
        var recipeId = 9;
        var client = CreateClient(
            HttpStatusCode.NoContent,
            null,
            request =>
            {
                Assert.AreEqual(HttpMethod.Delete, request.Method);
                Assert.IsTrue(request.RequestUri!.AbsolutePath.EndsWith($"/api/ingredient-lists/{listId}/recipes/{recipeId}"));
            });

        var result = await client.RemoveRecipeFromListAsync(listId, recipeId);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ShareListViaEmailAsync_WithSuccessResponse_ReturnsTrue()
    {
        var listId = Guid.NewGuid();
        var client = CreateClient(
            HttpStatusCode.OK,
            new { message = "sent" },
            request =>
            {
                Assert.AreEqual(HttpMethod.Post, request.Method);
                Assert.IsTrue(request.RequestUri!.AbsolutePath.EndsWith($"/api/ingredient-lists/{listId}/share/email"));
            });

        var result = await client.ShareListViaEmailAsync(listId, "test@example.com", "Viewer");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task GetSharedAccessAsync_WithSuccessResponse_ReturnsSharedList()
    {
        var token = Guid.NewGuid();
        var payload = new
        {
            Id = Guid.NewGuid(),
            Name = "Shared Access",
            Description = "From token",
            OwnerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Ingredients = new[] { new { Id = Guid.NewGuid(), Name = "Milk", Quantity = "1", Unit = "L", IsChecked = false, CreatedAt = DateTime.UtcNow } },
            Recipes = new[] { new { Id = 3, Name = "Pancakes", Description = "Breakfast" } },
            AccessLevel = "Viewer",
            CanEdit = false,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        var client = CreateClient(
            HttpStatusCode.OK,
            payload,
            request =>
            {
                Assert.AreEqual(HttpMethod.Get, request.Method);
                Assert.IsTrue(request.RequestUri!.AbsolutePath.EndsWith($"/api/ingredient-lists/shared/{token}"));
            });

        var result = await client.GetSharedAccessAsync(token);

        Assert.IsNotNull(result);
        Assert.AreEqual("Shared Access", result!.Name);
        Assert.AreEqual(1, result.Ingredients.Count);
        Assert.AreEqual(1, result.Recipes.Count);
    }

    [TestMethod]
    public void IngredientListDetailPage_RendersLoadedData()
    {
        using var ctx = new Bunit.TestContext();
        var listId = Guid.NewGuid();
        ConfigureIngredientListDetailServices(ctx, listId);

        var cut = ctx.RenderComponent<IngredientListDetail>(parameters => parameters.Add(p => p.Id, listId));

        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(cut.Markup.Contains("Weekly List"));
            Assert.IsTrue(cut.Markup.Contains("Fresh produce and pantry"));
            Assert.IsTrue(cut.Markup.Contains("Tomatoes"));
            Assert.IsTrue(cut.Markup.Contains("Tomato Soup"));
        });
    }

    [TestMethod]
    public async Task IngredientListDetailPage_UpdatesViaSignalREvent()
    {
        using var ctx = new Bunit.TestContext();
        var listId = Guid.NewGuid();
        ConfigureIngredientListDetailServices(ctx, listId);
        var signalR = (FakeIngredientListSignalRClient)ctx.Services.GetRequiredService<IngredientListSignalRClient>();

        var cut = ctx.RenderComponent<IngredientListDetail>(parameters => parameters.Add(p => p.Id, listId));
        cut.WaitForAssertion(() => Assert.IsTrue(cut.Markup.Contains("Tomatoes")));

        await signalR.EmitIngredientAddedAsync(listId, new IngredientItem
        {
            Id = Guid.NewGuid(),
            Name = "Realtime Basil",
            Quantity = "1",
            Unit = "bunch",
            IsChecked = false,
            CreatedAt = DateTime.UtcNow
        });

        cut.WaitForAssertion(() => Assert.IsTrue(cut.Markup.Contains("Realtime Basil")));
    }

    [TestMethod]
    public void IngredientListDetailPage_CheckStateSyncsAcrossOpenViews()
    {
        using var ctx = new Bunit.TestContext();
        var listId = Guid.NewGuid();
        ConfigureIngredientListDetailServices(ctx, listId);

        var cutA = ctx.RenderComponent<IngredientListDetail>(parameters => parameters.Add(p => p.Id, listId));
        var cutB = ctx.RenderComponent<IngredientListDetail>(parameters => parameters.Add(p => p.Id, listId));

        cutA.WaitForAssertion(() => Assert.AreEqual(0, cutA.FindAll("input[type=checkbox][checked]").Count));
        cutB.WaitForAssertion(() => Assert.AreEqual(0, cutB.FindAll("input[type=checkbox][checked]").Count));

        cutA.Find("input[type=checkbox]").Change(true);

        cutA.WaitForAssertion(() => Assert.AreEqual(1, cutA.FindAll("input[type=checkbox][checked]").Count));
        cutB.WaitForAssertion(() => Assert.AreEqual(1, cutB.FindAll("input[type=checkbox][checked]").Count));
    }

    private static void ConfigureIngredientListDetailServices(Bunit.TestContext ctx, Guid listId)
    {
        var userId = Guid.NewGuid();
        var seededIngredientId = Guid.NewGuid();
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Email, "test@example.com")
                ], "test"))
            }
        };

        var ingredientListApiClient = new IngredientListApiClient(new HttpClient(new RouteHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri?.AbsolutePath == $"/api/ingredient-lists/{listId}")
            {
                var payload = new
                {
                    Id = listId,
                    Name = "Weekly List",
                    Description = "Fresh produce and pantry",
                    OwnerId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Ingredients = new[]
                    {
                        new { Id = seededIngredientId, Name = "Tomatoes", Quantity = "3", Unit = "pcs", IsChecked = false, CreatedAt = DateTime.UtcNow }
                    },
                    Recipes = new[]
                    {
                        new { Id = 7, Name = "Tomato Soup", Description = "Simple soup" }
                    }
                };

                return JsonResponse(HttpStatusCode.OK, payload);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }))
        {
            BaseAddress = new Uri("https://test.api")
        });

        var recipeApiClient = new RecipeApiClient(new HttpClient(new RouteHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri?.AbsolutePath == "/api/recipes/")
            {
                var payload = new[]
                {
                    new
                    {
                        Id = 7,
                        Name = "Tomato Soup",
                        Description = "Simple soup",
                        Ingredients = "Tomatoes\nSalt",
                        Instructions = "Blend\nCook",
                        PrepTimeMinutes = 10,
                        CookTimeMinutes = 20,
                        Servings = 4,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                return JsonResponse(HttpStatusCode.OK, payload);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }))
        {
            BaseAddress = new Uri("https://test.api")
        });

        ctx.Services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
        ctx.Services.AddScoped<AuthenticationService>();
        ctx.Services.AddSingleton<IngredientListSignalRClient, FakeIngredientListSignalRClient>();
        ctx.Services.AddSingleton(ingredientListApiClient);
        ctx.Services.AddSingleton(recipeApiClient);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, object payload)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload))
        };
    }

    private static IngredientListApiClient CreateClient(HttpStatusCode statusCode, object? responseContent = null, Action<HttpRequestMessage>? assertRequest = null)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                assertRequest?.Invoke(request);

                return new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = responseContent is null
                        ? new StringContent(string.Empty)
                        : new StringContent(JsonSerializer.Serialize(responseContent))
                };
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://test.api")
        };

        return new IngredientListApiClient(httpClient);
    }

    private sealed class RouteHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }

    private sealed class FakeIngredientListSignalRClient(NavigationManager navigationManager)
        : IngredientListSignalRClient(navigationManager)
    {
        public override Task InitializeAsync(Guid listId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public override Task DisconnectAsync(Guid listId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public override async Task UpdateIngredientCheckStateAsync(Guid listId, Guid ingredientId, bool isChecked, CancellationToken cancellationToken = default)
            => await EmitIngredientCheckStateUpdatedAsync(listId, ingredientId, isChecked);

        public async Task EmitIngredientAddedAsync(Guid listId, IngredientItem ingredient)
        {
            var field = typeof(IngredientListSignalRClient)
                .GetField("OnIngredientAdded", BindingFlags.Instance | BindingFlags.NonPublic);
            var callback = field?.GetValue(this) as Func<Guid, IngredientItem, Task>;
            if (callback is not null)
            {
                await callback(listId, ingredient);
            }
        }

        private async Task EmitIngredientCheckStateUpdatedAsync(Guid listId, Guid ingredientId, bool isChecked)
        {
            var field = typeof(IngredientListSignalRClient)
                .GetField("OnIngredientCheckStateUpdated", BindingFlags.Instance | BindingFlags.NonPublic);
            var callback = field?.GetValue(this) as Func<Guid, Guid, bool, Task>;
            if (callback is not null)
            {
                await callback(listId, ingredientId, isChecked);
            }
        }
    }
}
