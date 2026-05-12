namespace RecipeManager.ApiService.Data;

/// <summary>
/// Access level for shared ingredient lists.
/// </summary>
public enum AccessLevel
{
    Viewer = 0,    // Read-only access
    Editor = 1     // Can modify ingredients and checkboxes
}

/// <summary>
/// Represents a single ingredient list owned by a user.
/// </summary>
public class IngredientList
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    /// <summary>
    /// ID of the user who owns this list.
    /// </summary>
    public Guid OwnerId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
    
    public ICollection<RecipeIngredientList> RecipeLinks { get; set; } = new List<RecipeIngredientList>();
    
    public ICollection<ListSharing> Sharings { get; set; } = new List<ListSharing>();
    
    public ICollection<ListShareToken> ShareTokens { get; set; } = new List<ListShareToken>();
}

/// <summary>
/// Represents a single ingredient item in an ingredient list.
/// </summary>
public class Ingredient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid IngredientListId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Quantity { get; set; }
    
    public string? Unit { get; set; }
    
    /// <summary>
    /// Whether this ingredient has been checked off.
    /// Real-time synchronized across all viewing users.
    /// </summary>
    public bool IsChecked { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public IngredientList? IngredientList { get; set; }
}

/// <summary>
/// Junction table linking recipes to ingredient lists (N:N relationship).
/// </summary>
public class RecipeIngredientList
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid IngredientListId { get; set; }
    
    /// <summary>
    /// ID of the recipe (from RecipeDbContext).
    /// </summary>
    public int RecipeId { get; set; }
    
    /// <summary>
    /// When this recipe was added to the list.
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// ID of the user who added this recipe.
    /// </summary>
    public Guid AddedByUserId { get; set; }
    
    // Navigation property
    public IngredientList? IngredientList { get; set; }

    public Recipe? Recipe { get; set; }
}

/// <summary>
/// Represents a share of an ingredient list with another user.
/// </summary>
public class ListSharing
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid IngredientListId { get; set; }
    
    /// <summary>
    /// ID of the user this list is shared with.
    /// </summary>
    public Guid SharedWithUserId { get; set; }
    
    /// <summary>
    /// How the share was granted: Email or Link.
    /// </summary>
    public string ShareType { get; set; } = "Email"; // "Email" or "Link"
    
    /// <summary>
    /// Access level for the shared user.
    /// </summary>
    public AccessLevel AccessLevel { get; set; } = AccessLevel.Viewer;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public IngredientList? IngredientList { get; set; }
}

/// <summary>
/// Represents a shareable token/link for anonymous access to an ingredient list.
/// </summary>
public class ListShareToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid IngredientListId { get; set; }
    
    /// <summary>
    /// GUID token used as part of the shareable link URL.
    /// </summary>
    public Guid Token { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// When this share token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public IngredientList? IngredientList { get; set; }
}
