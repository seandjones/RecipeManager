using System.ComponentModel.DataAnnotations;

namespace RecipeManager.ApiService.Models;

public record RecipeRequest
{
    [Required]
    [MaxLength(256)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Required]
    public string Ingredients { get; init; } = string.Empty;

    public string Instructions { get; init; } = string.Empty;

    public int? PrepTimeMinutes { get; init; }

    public int? CookTimeMinutes { get; init; }

    public int? Servings { get; init; }
}
