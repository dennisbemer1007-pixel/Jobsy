using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

[DbContext(typeof(JobsyDbContext))]
[Migration("20261016120000_AddPupilPassportV2Flags")]
public partial class AddPupilPassportV2Flags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "PupilPassportV2Enabled",
            table: "PlatformFeatureSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "StudentPassportEnabled",
            table: "PlatformFeatureSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "VoFavoriteSubjectsJson",
            table: "PupilProgress",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "VoProfileKey",
            table: "PupilProgress",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "VoSideJobAvailabilityJson",
            table: "PupilProgress",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "VoTravelModeKey",
            table: "PupilProgress",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "VoOwnWords",
            table: "PupilProgress",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "VoLearningGoalsJson",
            table: "PupilProgress",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PupilPassportV2Enabled", table: "PlatformFeatureSettings");
        migrationBuilder.DropColumn(name: "StudentPassportEnabled", table: "PlatformFeatureSettings");
        migrationBuilder.DropColumn(name: "VoFavoriteSubjectsJson", table: "PupilProgress");
        migrationBuilder.DropColumn(name: "VoProfileKey", table: "PupilProgress");
        migrationBuilder.DropColumn(name: "VoSideJobAvailabilityJson", table: "PupilProgress");
        migrationBuilder.DropColumn(name: "VoTravelModeKey", table: "PupilProgress");
        migrationBuilder.DropColumn(name: "VoOwnWords", table: "PupilProgress");
        migrationBuilder.DropColumn(name: "VoLearningGoalsJson", table: "PupilProgress");
    }
}
