using Jobsy.Infrastructure.Data.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

[DbContext(typeof(JobsyDbContext))]
[Migration("20261016163000_AddPupilPassportV2Flags")]
public partial class AddPupilPassportV2Flags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PupilPassportV2SchemaIdempotentSql.EnsureColumns);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PupilPassportV2Enabled", table: "PlatformFeatureSettings");
        migrationBuilder.DropColumn(name: "StudentPassportEnabled", table: "PlatformFeatureSettings");
        migrationBuilder.DropColumn(name: "VoFavoriteSubjectsJson", table: "PupilProgresses");
        migrationBuilder.DropColumn(name: "VoProfileKey", table: "PupilProgresses");
        migrationBuilder.DropColumn(name: "VoSideJobAvailabilityJson", table: "PupilProgresses");
        migrationBuilder.DropColumn(name: "VoTravelModeKey", table: "PupilProgresses");
        migrationBuilder.DropColumn(name: "VoOwnWords", table: "PupilProgresses");
        migrationBuilder.DropColumn(name: "VoLearningGoalsJson", table: "PupilProgresses");
    }
}
