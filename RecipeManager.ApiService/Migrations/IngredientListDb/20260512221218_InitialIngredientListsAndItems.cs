using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeManager.ApiService.Migrations.IngredientListDb
{
    /// <inheritdoc />
    public partial class InitialIngredientListsAndItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IngredientLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ingredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientListId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Quantity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsChecked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ingredients_IngredientLists_IngredientListId",
                        column: x => x.IngredientListId,
                        principalTable: "IngredientLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_IngredientLists_CreatedAt",
                table: "IngredientLists",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientLists_OwnerId",
                table: "IngredientLists",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_IngredientListId",
                table: "Ingredients",
                column: "IngredientListId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_IngredientListId_IsChecked",
                table: "Ingredients",
                columns: new[] { "IngredientListId", "IsChecked" });

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
                name: "Ingredients");

            migrationBuilder.DropTable(
                name: "ListShareTokens");

            migrationBuilder.DropTable(
                name: "ListSharings");

            migrationBuilder.DropTable(
                name: "RecipeIngredientLists");

            migrationBuilder.DropTable(
                name: "IngredientLists");
        }
    }
}
