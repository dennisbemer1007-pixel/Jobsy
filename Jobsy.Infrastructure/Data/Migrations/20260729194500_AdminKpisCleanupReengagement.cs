using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260729194500_AdminKpisCleanupReengagement")]
    public partial class AdminKpisCleanupReengagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Vacancies",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAtUtc",
                table: "Vacancies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DraftCleanupWarningSentAtUtc",
                table: "Vacancies",
                type: "timestamp with time zone",
                nullable: true);

            // Never-published drafts keep PublishedAt null. Heuristic: Active/Archived/Fulfilled were live.
            migrationBuilder.Sql(
                """
                UPDATE "Vacancies"
                SET "PublishedAtUtc" = CURRENT_TIMESTAMP
                WHERE "Status" IN (1, 2, 4);
                """);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReengagementEmailSentAtUtc",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCsvImportAtUtc",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InactiveCompanyDays",
                table: "PlatformFeatureSettings",
                type: "integer",
                nullable: false,
                defaultValue: 120);

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_Status_PublishedAtUtc_CreatedAtUtc",
                table: "Vacancies",
                columns: new[] { "Status", "PublishedAtUtc", "CreatedAtUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vacancies_Status_PublishedAtUtc_CreatedAtUtc",
                table: "Vacancies");

            migrationBuilder.DropColumn(name: "CreatedAtUtc", table: "Vacancies");
            migrationBuilder.DropColumn(name: "PublishedAtUtc", table: "Vacancies");
            migrationBuilder.DropColumn(name: "DraftCleanupWarningSentAtUtc", table: "Vacancies");
            migrationBuilder.DropColumn(name: "ReengagementEmailSentAtUtc", table: "Companies");
            migrationBuilder.DropColumn(name: "LastCsvImportAtUtc", table: "Companies");
            migrationBuilder.DropColumn(name: "InactiveCompanyDays", table: "PlatformFeatureSettings");
        }
    }
}
