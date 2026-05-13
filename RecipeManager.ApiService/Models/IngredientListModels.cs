using System.ComponentModel.DataAnnotations;

namespace RecipeManager.ApiService.Models;

public record IngredientListRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }
}

public record IngredientRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? Quantity { get; init; }

    [MaxLength(50)]
    public string? Unit { get; init; }

    public bool IsChecked { get; init; }
}

public record RecipeLinkRequest
{
    public int RecipeId { get; init; }
}

public record IngredientListSummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record IngredientItemResponse(
    Guid Id,
    string Name,
    string? Quantity,
    string? Unit,
    bool IsChecked,
    DateTime CreatedAt);

public record RecipeSummaryResponse(
    int Id,
    string Name,
    string? Description);

public record IngredientListDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<IngredientItemResponse> Ingredients,
    List<RecipeSummaryResponse> Recipes);
