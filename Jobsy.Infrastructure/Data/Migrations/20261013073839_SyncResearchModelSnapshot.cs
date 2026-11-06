using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncResearchModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: acceptatie may already have research columns/tables (partial #650 deploy)
            // or this migration runs after later timestamps (QuickScan) were applied first.
            migrationBuilder.Sql(ResearchSchemaIdempotentSql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentResearchItemResponses");

            migrationBuilder.DropTable(
                name: "ResearchPilotSessions");

            migrationBuilder.DropTable(
                name: "AssessmentResearchAttempts");

            migrationBuilder.DropTable(
                name: "ResearchPilotCampaigns");

            migrationBuilder.DropColumn(
                name: "ResearchAgeBand",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResearchConsentAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResearchConsentVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResearchEducationLevel",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResearchGender",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResearchNativeLanguage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResearchTestLanguage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResearchConsentEnabled",
                table: "PlatformFeatureSettings");
        }
    }
}
