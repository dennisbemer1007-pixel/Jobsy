using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

public partial class AddPassportPdfDnaBlocksEnabled : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "PassportPdfDnaBlocksEnabled",
            table: "PlatformFeatureSettings",
            type: "boolean",
            nullable: false,
            defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PassportPdfDnaBlocksEnabled",
            table: "PlatformFeatureSettings");
    }
}
