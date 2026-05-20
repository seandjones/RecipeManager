using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using RecipeManager.ApiService.Data;
using RecipeManager.ApiService.Services;

namespace RecipeManager.Tests;

[TestClass]
public class IngredientListHubTests
{
    private static IngredientListDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IngredientListDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new IngredientListDbContext(options);
    }

    [TestMethod]
    public void IngredientListHub_HasRequiredMethods()
    {
        var methods = typeof(IngredientListHub).GetMethods().Select(m => m.Name).ToHashSet();

        Assert.IsTrue(methods.Contains("JoinListGroup"));
        Assert.IsTrue(methods.Contains("LeaveListGroup"));
        Assert.IsTrue(methods.Contains("UpdateIngredientCheckState"));
        Assert.IsTrue(methods.Contains("AddIngredient"));
        Assert.IsTrue(methods.Contains("RemoveIngredient"));
        Assert.IsTrue(methods.Contains("UpdateIngredientDetails"));
        Assert.IsTrue(methods.Contains("AddRecipeToList"));
        Assert.IsTrue(methods.Contains("RemoveRecipeFromList"));
    }

    [TestMethod]
    public async Task JoinListGroup_WithoutUser_ThrowsHubException()
    {
        await using var db = CreateContext();
        var listId = Guid.NewGuid();

        db.IngredientLists.Add(new IngredientList
        {
            Id = listId,
            Name = "Test",
            OwnerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var hub = new IngredientListHub(db)
        {
            Context = CreateHubContext("conn-1", null),
            Groups = new Mock<IGroupManager>().Object,
            Clients = new Mock<IHubCallerClients<IIngredientListClient>>().Object
        };

        await Assert.ThrowsExactlyAsync<HubException>(() => hub.JoinListGroup(listId));
    }

    [TestMethod]
    public async Task JoinListGroup_AsOwner_AddsConnectionToGroup()
    {
        await using var db = CreateContext();
        var ownerId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        db.IngredientLists.Add(new IngredientList
        {
            Id = listId,
            Name = "Owner List",
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var groups = new Mock<IGroupManager>();

        var hub = new IngredientListHub(db)
        {
            Context = CreateHubContext("conn-owner", ownerId),
            Groups = groups.Object,
            Clients = new Mock<IHubCallerClients<IIngredientListClient>>().Object
        };

        await hub.JoinListGroup(listId);

        groups.Verify(g => g.AddToGroupAsync("conn-owner", $"ingredient-list-{listId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task JoinListGroup_AsSharedUser_AddsConnectionToGroup()
    {
        await using var db = CreateContext();
        var ownerId = Guid.NewGuid();
        var sharedUserId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        db.IngredientLists.Add(new IngredientList
        {
            Id = listId,
            Name = "Shared List",
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.ListSharings.Add(new ListSharing
        {
            IngredientListId = listId,
            SharedWithUserId = sharedUserId,
            ShareType = "Email",
            AccessLevel = AccessLevel.Editor,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var groups = new Mock<IGroupManager>();

        var hub = new IngredientListHub(db)
        {
            Context = CreateHubContext("conn-shared", sharedUserId),
            Groups = groups.Object,
            Clients = new Mock<IHubCallerClients<IIngredientListClient>>().Object
        };

        await hub.JoinListGroup(listId);

        groups.Verify(g => g.AddToGroupAsync("conn-shared", $"ingredient-list-{listId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task JoinListGroup_WithXUserIdHeader_AddsConnectionToGroup()
    {
        await using var db = CreateContext();
        var ownerId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        db.IngredientLists.Add(new IngredientList
        {
            Id = listId,
            Name = "Header Auth List",
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var groups = new Mock<IGroupManager>();

        var hub = new IngredientListHub(db)
        {
            Context = CreateHubContext("conn-header", null, ownerId),
            Groups = groups.Object,
            Clients = new Mock<IHubCallerClients<IIngredientListClient>>().Object
        };

        await hub.JoinListGroup(listId);

        groups.Verify(g => g.AddToGroupAsync("conn-header", $"ingredient-list-{listId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateIngredientCheckState_SavesToDatabaseAndBroadcastsEvent()
    {
        await using var db = CreateContext();
        var ownerId = Guid.NewGuid();
        var listId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();

        db.IngredientLists.Add(new IngredientList
        {
            Id = listId,
            Name = "Test List",
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.Ingredients.Add(new Ingredient
        {
            Id = ingredientId,
            IngredientListId = listId,
            Name = "Milk",
            IsChecked = false,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var clientsMock = new Mock<IHubCallerClients<IIngredientListClient>>();
        var groupClientMock = new Mock<IIngredientListClient>();
        clientsMock.Setup(c => c.Group($"ingredient-list-{listId}")).Returns(groupClientMock.Object);

        var hub = new IngredientListHub(db)
        {
            Context = CreateHubContext("conn-1", ownerId),
            Groups = new Mock<IGroupManager>().Object,
            Clients = clientsMock.Object
        };

        await hub.UpdateIngredientCheckState(listId, ingredientId, isChecked: true);

        var ingredient = await db.Ingredients.FindAsync(ingredientId);
        Assert.IsTrue(ingredient!.IsChecked);

        groupClientMock.Verify(c => c.OnIngredientCheckStateUpdated(listId, ingredientId, true), Times.Once);
    }

    [TestMethod]
    public async Task AddIngredient_SavesToDatabaseAndBroadcastsEvent()
    {
        await using var db = CreateContext();
        var ownerId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        db.IngredientLists.Add(new IngredientList
        {
            Id = listId,
            Name = "Broadcast List",
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var clientsMock = new Mock<IHubCallerClients<IIngredientListClient>>();
        var groupClientMock = new Mock<IIngredientListClient>();
        clientsMock.Setup(c => c.Group($"ingredient-list-{listId}")).Returns(groupClientMock.Object);

        var hub = new IngredientListHub(db)
        {
            Context = CreateHubContext("conn-1", ownerId),
            Groups = new Mock<IGroupManager>().Object,
            Clients = clientsMock.Object
        };

        var newIngredient = new Ingredient
        {
            Name = "Butter",
            Quantity = "200",
            Unit = "g",
            IsChecked = false
        };

        await hub.AddIngredient(listId, newIngredient);

        Assert.AreEqual(1, await db.Ingredients.CountAsync(i => i.IngredientListId == listId));
        groupClientMock.Verify(c => c.OnIngredientAdded(listId, It.Is<Ingredient>(i => i.Name == "Butter")), Times.Once);
    }

    [TestMethod]
    public async Task RemoveIngredient_DeletesFromDatabaseAndBroadcastsEvent()
    {
        await using var db = CreateContext();
        var ownerId = Guid.NewGuid();
        var listId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();

        db.IngredientLists.Add(new IngredientList
        {
            Id = listId,
            Name = "Remove Test",
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.Ingredients.Add(new Ingredient
        {
            Id = ingredientId,
            IngredientListId = listId,
            Name = "Sugar",
            IsChecked = false,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var clientsMock = new Mock<IHubCallerClients<IIngredientListClient>>();
        var groupClientMock = new Mock<IIngredientListClient>();
        clientsMock.Setup(c => c.Group($"ingredient-list-{listId}")).Returns(groupClientMock.Object);

        var hub = new IngredientListHub(db)
        {
            Context = CreateHubContext("conn-1", ownerId),
            Groups = new Mock<IGroupManager>().Object,
            Clients = clientsMock.Object
        };

        await hub.RemoveIngredient(listId, ingredientId);

        Assert.AreEqual(0, await db.Ingredients.CountAsync(i => i.Id == ingredientId));
        groupClientMock.Verify(c => c.OnIngredientRemoved(listId, ingredientId), Times.Once);
    }

    [TestMethod]
    public async Task UpdateIngredientCheckState_WithUnauthorizedUser_ThrowsHubException()
    {
        await using var db = CreateContext();
        var ownerId = Guid.NewGuid();
        var unauthorizedUserId = Guid.NewGuid();
        var listId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();

        db.IngredientLists.Add(new IngredientList
        {
            Id = listId,
            Name = "Private List",
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.Ingredients.Add(new Ingredient
        {
            Id = ingredientId,
            IngredientListId = listId,
            Name = "Salt",
            IsChecked = false,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var hub = new IngredientListHub(db)
        {
            Context = CreateHubContext("conn-bad", unauthorizedUserId),
            Groups = new Mock<IGroupManager>().Object,
            Clients = new Mock<IHubCallerClients<IIngredientListClient>>().Object
        };

        await Assert.ThrowsExactlyAsync<HubException>(() =>
            hub.UpdateIngredientCheckState(listId, ingredientId, isChecked: true));
    }

    private static HubCallerContext CreateHubContext(string connectionId, Guid? userId, Guid? headerUserId = null)
    {
        var mock = new Mock<HubCallerContext>();
        mock.SetupGet(c => c.ConnectionId).Returns(connectionId);

        ClaimsPrincipal principal;
        if (userId.HasValue)
        {
            principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
            ], "test"));
        }
        else
        {
            principal = new ClaimsPrincipal(new ClaimsIdentity());
        }

        mock.SetupGet(c => c.User).Returns(principal);

        var features = new FeatureCollection();
        if (headerUserId.HasValue)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-User-Id"] = headerUserId.Value.ToString();
            features.Set<Microsoft.AspNetCore.Http.Connections.Features.IHttpContextFeature>(new TestHttpContextFeature { HttpContext = httpContext });
        }

        mock.SetupGet(c => c.Features).Returns(features);
        return mock.Object;
    }

    private sealed class TestHttpContextFeature : Microsoft.AspNetCore.Http.Connections.Features.IHttpContextFeature
    {
        public HttpContext? HttpContext { get; set; } = new DefaultHttpContext();
    }
}
