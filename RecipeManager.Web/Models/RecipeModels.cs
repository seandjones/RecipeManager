using System.ComponentModel.DataAnnotations;

namespace RecipeManager.Web.Models;

public class Recipe
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Ingredients { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RecipeFormModel
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(256, ErrorMessage = "Name cannot exceed 256 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Ingredients are required.")]
    public string Ingredients { get; set; } = string.Empty;

    [Required(ErrorMessage = "Instructions are required.")]
    public string Instructions { get; set; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Prep time must be between 1 and 1440 minutes.")]
    public int? PrepTimeMinutes { get; set; }

    [Range(1, 1440, ErrorMessage = "Cook time must be between 1 and 1440 minutes.")]
    public int? CookTimeMinutes { get; set; }

    [Range(1, 100, ErrorMessage = "Servings must be between 1 and 100.")]
    public int? Servings { get; set; }
}
