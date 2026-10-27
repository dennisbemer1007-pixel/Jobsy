using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

public partial class AddPupilVisualThemeKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "PupilProgresses" ADD COLUMN IF NOT EXISTS "VisualThemeKey" character varying(16) NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "PupilProgresses" DROP COLUMN IF EXISTS "VisualThemeKey";
            """);
    }
}
