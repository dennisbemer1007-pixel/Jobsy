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
        migrationBuilder.DropColumn(name: "VoFavoriteSubjectsJson", table: "PupilProgress");
        migrationBuilder.DropColumn(name: "VoProfileKey", table: "PupilProgress");
        migrationBuilder.DropColumn(name: "VoSideJobAvailabilityJson", table: "PupilProgress");
        migrationBuilder.DropColumn(name: "VoTravelModeKey", table: "PupilProgress");
        migrationBuilder.DropColumn(name: "VoOwnWords", table: "PupilProgress");
        migrationBuilder.DropColumn(name: "VoLearningGoalsJson", table: "PupilProgress");
    }
}
