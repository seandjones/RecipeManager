using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RecipeManager.ApiService.Data;

namespace RecipeManager.ApiService.Services;

public class IngredientListHub(IngredientListDbContext dbContext) : Hub<IIngredientListClient>
{
    private static string GroupName(Guid listId) => $"ingredient-list-{listId}";

    public async Task JoinListGroup(Guid listId)
    {
        await EnsureUserHasAccess(listId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(listId));
    }

    public async Task LeaveListGroup(Guid listId)
    {
        await EnsureUserHasAccess(listId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(listId));
    }

    public async Task UpdateIngredientCheckState(Guid listId, Guid ingredientId, bool isChecked)
    {
        await EnsureUserHasAccess(listId);

        var ingredient = await dbContext.Ingredients
            .FirstOrDefaultAsync(i => i.Id == ingredientId && i.IngredientListId == listId);

        if (ingredient is null)
        {
            throw new HubException("Ingredient not found.");
        }

        ingredient.IsChecked = isChecked;
        await dbContext.SaveChangesAsync();

        await Clients.Group(GroupName(listId))
            .OnIngredientCheckStateUpdated(listId, ingredientId, isChecked);
    }

    public async Task AddIngredient(Guid listId, Ingredient ingredient)
    {
        await EnsureUserHasAccess(listId);

        ingredient.Id = Guid.NewGuid();
        ingredient.IngredientListId = listId;
        ingredient.CreatedAt = DateTime.UtcNow;

        dbContext.Ingredients.Add(ingredient);
        await dbContext.SaveChangesAsync();

        await Clients.Group(GroupName(listId)).OnIngredientAdded(listId, ingredient);
    }

    public async Task RemoveIngredient(Guid listId, Guid ingredientId)
    {
        await EnsureUserHasAccess(listId);

        var ingredient = await dbContext.Ingredients
            .FirstOrDefaultAsync(i => i.Id == ingredientId && i.IngredientListId == listId);

        if (ingredient is null)
        {
            throw new HubException("Ingredient not found.");
        }

        dbContext.Ingredients.Remove(ingredient);
        await dbContext.SaveChangesAsync();

        await Clients.Group(GroupName(listId)).OnIngredientRemoved(listId, ingredientId);
    }

    public async Task UpdateIngredientDetails(Guid listId, Guid ingredientId, string name, string? quantity, string? unit)
    {
        await EnsureUserHasAccess(listId);

        var ingredient = await dbContext.Ingredients
            .FirstOrDefaultAsync(i => i.Id == ingredientId && i.IngredientListId == listId);

        if (ingredient is null)
        {
            throw new HubException("Ingredient not found.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new HubException("Ingredient name is required.");
        }

        ingredient.Name = name.Trim();
        ingredient.Quantity = string.IsNullOrWhiteSpace(quantity) ? null : quantity.Trim();
        ingredient.Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();

        await dbContext.SaveChangesAsync();

        await Clients.Group(GroupName(listId)).OnIngredientUpdated(listId, ingredient);
    }

    public async Task AddRecipeToList(Guid listId, int recipeId)
    {
        var userId = await EnsureUserHasAccess(listId);

        var recipeExists = await dbContext.Set<Recipe>().AnyAsync(r => r.Id == recipeId);
        if (!recipeExists)
        {
            throw new HubException("Recipe not found.");
        }

        var existing = await dbContext.RecipeIngredientLists
            .AnyAsync(r => r.IngredientListId == listId && r.RecipeId == recipeId);

        if (!existing)
        {
            dbContext.RecipeIngredientLists.Add(new RecipeIngredientList
            {
                Id = Guid.NewGuid(),
                IngredientListId = listId,
                RecipeId = recipeId,
                AddedAt = DateTime.UtcNow,
                AddedByUserId = userId
            });

            await dbContext.SaveChangesAsync();
        }

        await Clients.Group(GroupName(listId)).OnRecipeAdded(listId, recipeId);
    }

    public async Task RemoveRecipeFromList(Guid listId, int recipeId)
    {
        await EnsureUserHasAccess(listId);

        var link = await dbContext.RecipeIngredientLists
            .FirstOrDefaultAsync(r => r.IngredientListId == listId && r.RecipeId == recipeId);

        if (link is null)
        {
            throw new HubException("Recipe link not found.");
        }

        dbContext.RecipeIngredientLists.Remove(link);
        await dbContext.SaveChangesAsync();

        await Clients.Group(GroupName(listId)).OnRecipeRemoved(listId, recipeId);
    }

    private async Task<Guid> EnsureUserHasAccess(Guid listId)
    {
        var userId = ResolveUserId();
        if (!userId.HasValue)
        {
            throw new HubException("Unauthorized.");
        }

        var hasAccess = await dbContext.IngredientLists
            .AnyAsync(l => l.Id == listId && l.OwnerId == userId.Value)
            || await dbContext.ListSharings
                .AnyAsync(s => s.IngredientListId == listId && s.SharedWithUserId == userId.Value);

        if (!hasAccess)
        {
            throw new HubException("Forbidden.");
        }

        return userId.Value;
    }

    private Guid? ResolveUserId()
    {
        var user = Context.User;
        if (user is null)
        {
            return null;
        }

        var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("userId")
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(idClaim, out var userId) ? userId : null;
    }
}
