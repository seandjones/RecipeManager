namespace RecipeManager.Web.Models;

public class IngredientList
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string AccessLevel { get; set; } = "Owner";
    public Guid? SharedByUserId { get; set; }
    public int RecipeCount { get; set; }
    public int SharedCount { get; set; }
    public List<IngredientItem> Ingredients { get; set; } = [];
    public List<IngredientListRecipe> Recipes { get; set; } = [];
}

public class IngredientListSharingEntry
{
    public Guid ShareId { get; set; }
    public Guid IngredientListId { get; set; }
    public string ShareType { get; set; } = string.Empty;
    public string AccessLevel { get; set; } = "Viewer";
    public Guid? SharedWithUserId { get; set; }
    public string? SharedWithEmail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ShareUrl { get; set; }
}

public class IngredientItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public string? Unit { get; set; }
    public bool IsChecked { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class IngredientListRecipe
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class IngredientListRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class IngredientRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public string? Unit { get; set; }
    public bool IsChecked { get; set; }
}

public class IngredientListShareLink
{
    public Guid Token { get; set; }
    public string Url { get; set; } = string.Empty;
    public string AccessLevel { get; set; } = "Viewer";
    public DateTime ExpiresAt { get; set; }
}

public class SharedIngredientListAccess
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<IngredientItem> Ingredients { get; set; } = [];
    public List<IngredientListRecipe> Recipes { get; set; } = [];
    public string AccessLevel { get; set; } = "Viewer";
    public bool CanEdit { get; set; }
    public DateTime ExpiresAt { get; set; }
}

internal class IngredientListSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string AccessLevel { get; set; } = "Owner";
    public Guid? SharedByUserId { get; set; }
}

internal class IngredientListDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<IngredientItem> Ingredients { get; set; } = [];
    public List<IngredientListRecipe> Recipes { get; set; } = [];
}

internal class ShareIngredientListByEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string AccessLevel { get; set; } = "Viewer";
}

internal class CreateIngredientListShareLinkRequest
{
    public string AccessLevel { get; set; } = "Viewer";
    public int ExpiresInDays { get; set; } = 7;
}
