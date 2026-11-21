using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penny.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subcategory_CategoryId",
                table: "Subcategory");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Subcategory_CategoryId_Id",
                table: "Subcategory",
                columns: new[] { "CategoryId", "Id" });

            migrationBuilder.CreateTable(
                name: "Movement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    SubcategoryId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movement", x => x.Id);
                    table.CheckConstraint("CK_Movement_Amount_Positive", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_Movement_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movement_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movement_Subcategory_CategoryId_SubcategoryId",
                        columns: x => new { x.CategoryId, x.SubcategoryId },
                        principalTable: "Subcategory",
                        principalColumns: new[] { "CategoryId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Movement_AccountId",
                table: "Movement",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Movement_CategoryId_SubcategoryId",
                table: "Movement",
                columns: new[] { "CategoryId", "SubcategoryId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Movement");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Subcategory_CategoryId_Id",
                table: "Subcategory");

            migrationBuilder.CreateIndex(
                name: "IX_Subcategory_CategoryId",
                table: "Subcategory",
                column: "CategoryId");
        }
    }
}
