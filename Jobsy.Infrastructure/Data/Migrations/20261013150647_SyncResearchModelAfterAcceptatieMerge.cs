using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncResearchModelAfterAcceptatieMerge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Acceptatie can apply this migration when AddQuickScanVersionAndTestsV2Enabled was skipped
            // (out-of-order merges #647 / #650). Ensure columns exist before dropping DB defaults.
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

            migrationBuilder.AlterColumn<int>(
                name: "QuickScanVersion",
                table: "CandidateValuesProfiles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "QuickScanHighestAnswered",
                table: "CandidateValuesProfiles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "QuickScanVersion",
                table: "CandidateCulturePersonalityProfiles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "QuickScanHighestAnswered",
                table: "CandidateCulturePersonalityProfiles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "QuickScanVersion",
                table: "CandidateCompetencies",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "QuickScanHighestAnswered",
                table: "CandidateCompetencies",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "QuickScanVersion",
                table: "CandidateCareerInterests",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "QuickScanHighestAnswered",
                table: "CandidateCareerInterests",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "QuickScanVersion",
                table: "CandidateValuesProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "QuickScanHighestAnswered",
                table: "CandidateValuesProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "QuickScanVersion",
                table: "CandidateCulturePersonalityProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "QuickScanHighestAnswered",
                table: "CandidateCulturePersonalityProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "QuickScanVersion",
                table: "CandidateCompetencies",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "QuickScanHighestAnswered",
                table: "CandidateCompetencies",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "QuickScanVersion",
                table: "CandidateCareerInterests",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "QuickScanHighestAnswered",
                table: "CandidateCareerInterests",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
