using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penny.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowUncategorizedIncome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Movement",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Movement_Category_Required",
                table: "Movement",
                sql: "([Type] <> 2 AND [SubcategoryId] IS NULL) OR [CategoryId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [Movement] WHERE [CategoryId] IS NULL)
                    THROW 50001, 'Assign categories to uncategorized movements before reverting this migration.', 1;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_Movement_Category_Required",
                table: "Movement");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Movement",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
