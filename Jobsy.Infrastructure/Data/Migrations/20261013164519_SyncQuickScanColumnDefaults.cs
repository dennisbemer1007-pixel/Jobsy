using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Applies Quick-Scan v2 columns that were defined in a hand-written migration
    /// missing from the EF designer chain (see removed AddQuickScanVersionAndTestsV2Enabled file).
    /// </summary>
    public partial class SyncQuickScanColumnDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "CandidateValuesProfiles" ADD COLUMN IF NOT EXISTS "QuickScanHighestAnswered" integer NOT NULL DEFAULT 0;
                ALTER TABLE "CandidateValuesProfiles" ADD COLUMN IF NOT EXISTS "QuickScanVersion" integer NOT NULL DEFAULT 1;
                ALTER TABLE "CandidateCulturePersonalityProfiles" ADD COLUMN IF NOT EXISTS "QuickScanHighestAnswered" integer NOT NULL DEFAULT 0;
                ALTER TABLE "CandidateCulturePersonalityProfiles" ADD COLUMN IF NOT EXISTS "QuickScanVersion" integer NOT NULL DEFAULT 1;
                ALTER TABLE "CandidateCompetencies" ADD COLUMN IF NOT EXISTS "QuickScanHighestAnswered" integer NOT NULL DEFAULT 0;
                ALTER TABLE "CandidateCompetencies" ADD COLUMN IF NOT EXISTS "QuickScanVersion" integer NOT NULL DEFAULT 1;
                ALTER TABLE "CandidateCareerInterests" ADD COLUMN IF NOT EXISTS "QuickScanHighestAnswered" integer NOT NULL DEFAULT 0;
                ALTER TABLE "CandidateCareerInterests" ADD COLUMN IF NOT EXISTS "QuickScanVersion" integer NOT NULL DEFAULT 1;
                ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "TestsV2Enabled" boolean NOT NULL DEFAULT false;
                """);
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
