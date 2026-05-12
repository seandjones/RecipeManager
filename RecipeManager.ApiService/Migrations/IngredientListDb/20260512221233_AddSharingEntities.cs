using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeManager.ApiService.Migrations.IngredientListDb
{
    /// <inheritdoc />
    public partial class AddSharingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListShareTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientListId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListShareTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListShareTokens_IngredientLists_IngredientListId",
                        column: x => x.IngredientListId,
                        principalTable: "IngredientLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListSharings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientListId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedWithUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShareType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AccessLevel = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListSharings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListSharings_IngredientLists_IngredientListId",
                        column: x => x.IngredientListId,
                        principalTable: "IngredientLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListShareTokens_ExpiresAt",
                table: "ListShareTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ListShareTokens_IngredientListId",
                table: "ListShareTokens",
                column: "IngredientListId");

            migrationBuilder.CreateIndex(
                name: "IX_ListShareTokens_Token",
                table: "ListShareTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListSharings_IngredientListId",
                table: "ListSharings",
                column: "IngredientListId");

            migrationBuilder.CreateIndex(
                name: "IX_ListSharings_IngredientListId_SharedWithUserId",
                table: "ListSharings",
                columns: new[] { "IngredientListId", "SharedWithUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListSharings_SharedWithUserId",
                table: "ListSharings",
                column: "SharedWithUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListShareTokens");

            migrationBuilder.DropTable(
                name: "ListSharings");
        }
    }
}
