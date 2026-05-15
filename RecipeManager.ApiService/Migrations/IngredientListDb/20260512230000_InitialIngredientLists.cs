using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RecipeManager.ApiService.Data;

#nullable disable

namespace RecipeManager.ApiService.Migrations.IngredientListDb;

[DbContext(typeof(IngredientListDbContext))]
[Migration("20260512230000_InitialIngredientLists")]
public partial class InitialIngredientLists : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    migrationBuilder.Sql("SELECT 1");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
