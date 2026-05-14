using System.Net.Http.Json;
using RecipeManager.Web.Models;

namespace RecipeManager.Web.Services;

public class IngredientListApiClient(HttpClient httpClient)
{
    public async Task<IngredientList[]> GetListsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var summaries = await httpClient.GetFromJsonAsync<IngredientListSummaryResponse[]>("/api/ingredient-lists", cancellationToken) ?? [];
            return summaries.Select(MapSummaryToIngredientList).ToArray();
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<IngredientList?> GetListAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/ingredient-lists/{id}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var detail = await response.Content.ReadFromJsonAsync<IngredientListDetailResponse>(cancellationToken);
            if (detail is null)
            {
                return null;
            }

            return MapDetailToIngredientList(detail);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<IngredientList?> CreateListAsync(IngredientListRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/ingredient-lists", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var summary = await response.Content.ReadFromJsonAsync<IngredientListSummaryResponse>(cancellationToken);
            return summary is null ? null : MapSummaryToIngredientList(summary);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<IngredientList?> UpdateListAsync(Guid id, IngredientListRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"/api/ingredient-lists/{id}", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var summary = await response.Content.ReadFromJsonAsync<IngredientListSummaryResponse>(cancellationToken);
            return summary is null ? null : MapSummaryToIngredientList(summary);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> DeleteListAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"/api/ingredient-lists/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<IngredientItem?> AddIngredientAsync(Guid listId, IngredientRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"/api/ingredient-lists/{listId}/ingredients", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<IngredientItem>(cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<IngredientItem?> UpdateIngredientAsync(Guid listId, Guid ingredientId, IngredientRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"/api/ingredient-lists/{listId}/ingredients/{ingredientId}", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<IngredientItem>(cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> DeleteIngredientAsync(Guid listId, Guid ingredientId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"/api/ingredient-lists/{listId}/ingredients/{ingredientId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> AddRecipeToListAsync(Guid listId, int recipeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"/api/ingredient-lists/{listId}/recipes", new { RecipeId = recipeId }, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> RemoveRecipeFromListAsync(Guid listId, int recipeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"/api/ingredient-lists/{listId}/recipes/{recipeId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> ShareListViaEmailAsync(Guid listId, string email, string accessLevel, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ShareIngredientListByEmailRequest
            {
                Email = email,
                AccessLevel = accessLevel
            };

            var response = await httpClient.PostAsJsonAsync($"/api/ingredient-lists/{listId}/share/email", request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<IngredientListShareLink?> GenerateShareLinkAsync(Guid listId, string accessLevel, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new CreateIngredientListShareLinkRequest
            {
                AccessLevel = accessLevel,
                ExpiresInDays = 7
            };

            var response = await httpClient.PostAsJsonAsync($"/api/ingredient-lists/{listId}/share/link", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<IngredientListShareLink>(cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<SharedIngredientListAccess?> GetSharedAccessAsync(Guid token, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/ingredient-lists/shared/{token}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<SharedIngredientListAccess>(cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<IngredientListSharingEntry[]> GetSharingAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<IngredientListSharingEntry[]>($"/api/ingredient-lists/{listId}/sharing", cancellationToken) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static IngredientList MapSummaryToIngredientList(IngredientListSummaryResponse summary)
    {
        return new IngredientList
        {
            Id = summary.Id,
            Name = summary.Name,
            Description = summary.Description,
            OwnerId = summary.OwnerId,
            CreatedAt = summary.CreatedAt,
            UpdatedAt = summary.UpdatedAt,
            AccessLevel = summary.AccessLevel,
            SharedByUserId = summary.SharedByUserId,
            RecipeCount = 0,
            SharedCount = 0,
            Ingredients = [],
            Recipes = []
        };
    }

    private static IngredientList MapDetailToIngredientList(IngredientListDetailResponse detail)
    {
        return new IngredientList
        {
            Id = detail.Id,
            Name = detail.Name,
            Description = detail.Description,
            OwnerId = detail.OwnerId,
            CreatedAt = detail.CreatedAt,
            UpdatedAt = detail.UpdatedAt,
            AccessLevel = "Owner",
            SharedByUserId = null,
            RecipeCount = detail.Recipes.Count,
            SharedCount = 0,
            Ingredients = detail.Ingredients,
            Recipes = detail.Recipes
        };
    }
}
