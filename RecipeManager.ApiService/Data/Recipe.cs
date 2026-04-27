using System.ComponentModel.DataAnnotations;

namespace RecipeManager.ApiService.Data;

public class Recipe
{
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string Ingredients { get; set; } = string.Empty;

    public string Instructions { get; set; } = string.Empty;

    public int? PrepTimeMinutes { get; set; }

    public int? CookTimeMinutes { get; set; }

    public int? Servings { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
