using RecipeManager.Web.Models;
using System.Net.Http.Json;

namespace RecipeManager.Web.Services;

public class RecipeApiClient(HttpClient httpClient)
{
    public async Task<Recipe[]> GetRecipesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<Recipe[]>("/api/recipes/", cancellationToken) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<Recipe?> GetRecipeAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/recipes/{id}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<Recipe>(cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<Recipe?> CreateRecipeAsync(RecipeFormModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/recipes/", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Recipe>(cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<Recipe?> UpdateRecipeAsync(int id, RecipeFormModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"/api/recipes/{id}", model, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<Recipe>(cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> DeleteRecipeAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"/api/recipes/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
