using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeManager.ApiService.Migrations.IngredientListDb
{
    /// <inheritdoc />
    public partial class AddListShareTokenAccessLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "ListShareTokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "ListShareTokens");
        }
    }
}
