using System.Security.Claims;
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

    private static HubCallerContext CreateHubContext(string connectionId, Guid? userId)
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
        return mock.Object;
    }
}
