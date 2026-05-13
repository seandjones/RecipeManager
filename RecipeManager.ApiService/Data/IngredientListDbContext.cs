using Microsoft.EntityFrameworkCore;

namespace RecipeManager.ApiService.Data;

/// <summary>
/// Database context for ingredient list management with real-time sharing.
/// </summary>
public class IngredientListDbContext : DbContext
{
    public IngredientListDbContext(DbContextOptions<IngredientListDbContext> options)
        : base(options)
    {
    }

    public DbSet<IngredientList> IngredientLists { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<RecipeIngredientList> RecipeIngredientLists { get; set; }
    public DbSet<ListSharing> ListSharings { get; set; }
    public DbSet<ListShareToken> ListShareTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // IngredientList configuration
        modelBuilder.Entity<IngredientList>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            // Indexes
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.CreatedAt);
            
            // Relationships
            entity.HasMany(e => e.Ingredients)
                .WithOne(i => i.IngredientList)
                .HasForeignKey(i => i.IngredientListId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.RecipeLinks)
                .WithOne(r => r.IngredientList)
                .HasForeignKey(r => r.IngredientListId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Sharings)
                .WithOne(s => s.IngredientList)
                .HasForeignKey(s => s.IngredientListId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ShareTokens)
                .WithOne(t => t.IngredientList)
                .HasForeignKey(t => t.IngredientListId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Ingredient configuration
        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Quantity).HasMaxLength(100);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.IsChecked).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Indexes
            entity.HasIndex(e => e.IngredientListId);
            entity.HasIndex(e => new { e.IngredientListId, e.IsChecked });
        });

        // RecipeIngredientList configuration (junction table)
        modelBuilder.Entity<RecipeIngredientList>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AddedAt).IsRequired();
            entity.Property(e => e.AddedByUserId).IsRequired();
            
            // Indexes
            entity.HasIndex(e => e.IngredientListId);
            entity.HasIndex(e => e.RecipeId);
            entity.HasIndex(e => new { e.IngredientListId, e.RecipeId }).IsUnique();

            entity.HasOne(e => e.Recipe)
                .WithMany()
                .HasForeignKey(e => e.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.ToTable("Recipes", t => t.ExcludeFromMigrations());
        });

        // ListSharing configuration
        modelBuilder.Entity<ListSharing>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShareType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.AccessLevel).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Indexes
            entity.HasIndex(e => e.IngredientListId);
            entity.HasIndex(e => e.SharedWithUserId);
            entity.HasIndex(e => new { e.IngredientListId, e.SharedWithUserId }).IsUnique();
        });

        // ListShareToken configuration
        modelBuilder.Entity<ListShareToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.AccessLevel).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Indexes
            entity.HasIndex(e => e.IngredientListId);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
        });
    }
}
