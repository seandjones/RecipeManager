using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeManager.ApiService.Migrations.IngredientListDb
{
    /// <inheritdoc />
    public partial class AddRecipeIngredientListJunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecipeIngredientLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientListId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredientLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeIngredientLists_IngredientLists_IngredientListId",
                        column: x => x.IngredientListId,
                        principalTable: "IngredientLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredientLists_IngredientListId",
                table: "RecipeIngredientLists",
                column: "IngredientListId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredientLists_IngredientListId_RecipeId",
                table: "RecipeIngredientLists",
                columns: new[] { "IngredientListId", "RecipeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredientLists_RecipeId",
                table: "RecipeIngredientLists",
                column: "RecipeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeIngredientLists");
        }
    }
}
