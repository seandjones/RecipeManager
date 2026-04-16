using System.ComponentModel.DataAnnotations;

namespace RecipeManager.ApiService.Data;

public class User
{
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    
    public DateTime? LastLoginAt { get; set; }

    public ICollection<LoginCode> LoginCodes { get; set; } = [];
}
