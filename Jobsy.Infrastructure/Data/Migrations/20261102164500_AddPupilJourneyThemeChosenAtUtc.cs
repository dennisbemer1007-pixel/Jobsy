using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddPupilJourneyThemeChosenAtUtc : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "JourneyThemeChosenAtUtc",
            table: "PupilProgresses",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE "PupilProgresses"
            SET "JourneyThemeChosenAtUtc" = "UpdatedAtUtc"
            WHERE "JourneyThemeKey" IS NOT NULL
              AND "JourneyThemeChosenAtUtc" IS NULL;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "JourneyThemeChosenAtUtc",
            table: "PupilProgresses");
    }
}
