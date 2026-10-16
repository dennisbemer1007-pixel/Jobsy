using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

public partial class AddPassportPdfDnaBlocksEnabled : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "PassportPdfDnaBlocksEnabled" boolean NOT NULL DEFAULT true;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PassportPdfDnaBlocksEnabled",
            table: "PlatformFeatureSettings");
    }
}
