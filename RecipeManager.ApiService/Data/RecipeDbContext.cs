using Microsoft.EntityFrameworkCore;

namespace RecipeManager.ApiService.Data;

public class RecipeDbContext(DbContextOptions<RecipeDbContext> options) : DbContext(options)
{
    public DbSet<Recipe> Recipes => Set<Recipe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Ingredients).IsRequired();
            entity.Property(e => e.Instructions).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });
    }
}
