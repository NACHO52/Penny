using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Penny.Infrastructure.Data.Migrations;

[DbContext(typeof(PennyDbContext))]
[Migration("20261128000100_RequireUniqueUserEmail")]
public sealed class RequireUniqueUserEmail : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("EmailIndex", "AspNetUsers");
        migrationBuilder.CreateIndex(
            "EmailIndex", "AspNetUsers", "NormalizedEmail",
            unique: true, filter: "[NormalizedEmail] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("EmailIndex", "AspNetUsers");
        migrationBuilder.CreateIndex("EmailIndex", "AspNetUsers", "NormalizedEmail");
    }
}
