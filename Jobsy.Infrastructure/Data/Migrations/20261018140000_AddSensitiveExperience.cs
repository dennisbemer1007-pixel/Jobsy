using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

[DbContext(typeof(JobsyDbContext))]
[Migration("20261018140000_AddSensitiveExperience")]
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
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RelationshipContext",
            table: "CandidateReferences",
            type: "text",
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
