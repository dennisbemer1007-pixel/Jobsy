using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddSensitiveExperience : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "SensitiveExperienceEnabled",
            table: "PlatformFeatureSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "ReferenceType",
            table: "CandidateReferences",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RelationshipContext",
            table: "CandidateReferences",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SensitiveExperienceEnabled",
            table: "PlatformFeatureSettings");

        migrationBuilder.DropColumn(
            name: "ReferenceType",
            table: "CandidateReferences");

        migrationBuilder.DropColumn(
            name: "RelationshipContext",
            table: "CandidateReferences");
    }
}
