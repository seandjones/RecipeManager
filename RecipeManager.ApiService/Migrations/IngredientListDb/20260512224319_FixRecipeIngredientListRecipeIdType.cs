using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeManager.ApiService.Migrations.IngredientListDb
{
    /// <inheritdoc />
    public partial class FixRecipeIngredientListRecipeIdType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredientLists_IngredientListId_RecipeId",
                table: "RecipeIngredientLists");

            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredientLists_RecipeId",
                table: "RecipeIngredientLists");

            migrationBuilder.DropColumn(
                name: "RecipeId",
                table: "RecipeIngredientLists");

            migrationBuilder.AddColumn<int>(
                name: "RecipeId",
                table: "RecipeIngredientLists",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredientLists_IngredientListId_RecipeId",
                table: "RecipeIngredientLists",
                columns: new[] { "IngredientListId", "RecipeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredientLists_RecipeId",
                table: "RecipeIngredientLists",
                column: "RecipeId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeIngredientLists_Recipes_RecipeId",
                table: "RecipeIngredientLists",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecipeIngredientLists_Recipes_RecipeId",
                table: "RecipeIngredientLists");

            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredientLists_IngredientListId_RecipeId",
                table: "RecipeIngredientLists");

            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredientLists_RecipeId",
                table: "RecipeIngredientLists");

            migrationBuilder.DropColumn(
                name: "RecipeId",
                table: "RecipeIngredientLists");

            migrationBuilder.AddColumn<Guid>(
                name: "RecipeId",
                table: "RecipeIngredientLists",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

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
    }
}
