using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddPupilJourneyThemeKey : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "JourneyThemeKey",
            table: "PupilProgresses",
            type: "character varying(16)",
            maxLength: 16,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "JourneyThemeKey",
            table: "PupilProgresses");
    }
}
