using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuickScanVersionAndTestsV2Enabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuickScanHighestAnswered",
                table: "CandidateValuesProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuickScanVersion",
                table: "CandidateValuesProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "QuickScanHighestAnswered",
                table: "CandidateCulturePersonalityProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuickScanVersion",
                table: "CandidateCulturePersonalityProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "QuickScanHighestAnswered",
                table: "CandidateCompetencies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuickScanVersion",
                table: "CandidateCompetencies",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "QuickScanHighestAnswered",
                table: "CandidateCareerInterests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuickScanVersion",
                table: "CandidateCareerInterests",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "TestsV2Enabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuickScanHighestAnswered",
                table: "CandidateValuesProfiles");

            migrationBuilder.DropColumn(
                name: "QuickScanVersion",
                table: "CandidateValuesProfiles");

            migrationBuilder.DropColumn(
                name: "QuickScanHighestAnswered",
                table: "CandidateCulturePersonalityProfiles");

            migrationBuilder.DropColumn(
                name: "QuickScanVersion",
                table: "CandidateCulturePersonalityProfiles");

            migrationBuilder.DropColumn(
                name: "QuickScanHighestAnswered",
                table: "CandidateCompetencies");

            migrationBuilder.DropColumn(
                name: "QuickScanVersion",
                table: "CandidateCompetencies");

            migrationBuilder.DropColumn(
                name: "QuickScanHighestAnswered",
                table: "CandidateCareerInterests");

            migrationBuilder.DropColumn(
                name: "QuickScanVersion",
                table: "CandidateCareerInterests");

            migrationBuilder.DropColumn(
                name: "TestsV2Enabled",
                table: "PlatformFeatureSettings");
        }
    }
}
