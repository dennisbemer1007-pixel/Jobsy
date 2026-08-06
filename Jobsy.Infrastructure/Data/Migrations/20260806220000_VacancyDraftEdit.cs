using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260806220000_VacancyDraftEdit")]
    public partial class VacancyDraftEdit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ContentModerationPassed",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Existing drafts were never moderated under the new flag — require a fresh check before publish.
            migrationBuilder.Sql(
                """
                UPDATE "Vacancies"
                SET "ContentModerationPassed" = FALSE
                WHERE "Status" = 0;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "VacancyCategories"
                SET "IsActive" = FALSE,
                    "ShowInMapFilter" = FALSE,
                    "ShowInLegend" = FALSE,
                    "HighlightCostTokens" = CASE
                        WHEN "HighlightCostTokens" < 2 THEN 2
                        ELSE "HighlightCostTokens"
                    END,
                    "UpdatedAtUtc" = NOW()
                WHERE "Id" = 'c1000001-0000-4000-8000-000000000003'
                   OR lower("Slug") = 'highlight';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "TokenSpendCosts"
                SET "CostTokens" = 2
                WHERE "Reason" = 2
                  AND "IsActive" = TRUE
                  AND "CostTokens" < 2;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentModerationPassed",
                table: "Vacancies");
        }
    }
}
