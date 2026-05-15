using RecipeManager.ApiService.Data;

namespace RecipeManager.ApiService.Services;

public interface IIngredientListClient
{
    Task OnIngredientAdded(Guid listId, Ingredient ingredient);
    Task OnIngredientRemoved(Guid listId, Guid ingredientId);
    Task OnIngredientUpdated(Guid listId, Ingredient ingredient);
    Task OnIngredientCheckStateUpdated(Guid listId, Guid ingredientId, bool isChecked);
    Task OnRecipeAdded(Guid listId, int recipeId);
    Task OnRecipeRemoved(Guid listId, int recipeId);
}
