using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260807060000_PartnerReferralTokenRewards")]
    public partial class PartnerReferralTokenRewards : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PartnerReferralRewardedAtUtc",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PartnerReferralStatus",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PartnerReferredAtUtc",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WelcomeTokenLedgerCredited",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Backfill partner referrals that already had attribution.
            migrationBuilder.Sql(
                """
                UPDATE "Companies"
                SET "PartnerReferralStatus" = 1,
                    "PartnerReferredAtUtc" = COALESCE("FirstYearStartedAt", NOW())
                WHERE "ReferredByPartnerUserId" IS NOT NULL
                  AND "PartnerReferralStatus" = 0;
                """);

            // Mark welcome ledger credit when a welcome grant transaction exists.
            migrationBuilder.Sql(
                """
                UPDATE "Companies" c
                SET "WelcomeTokenLedgerCredited" = TRUE
                WHERE EXISTS (
                    SELECT 1
                    FROM "TokenTransactions" t
                    WHERE t."CompanyId" = c."Id"
                      AND t."Kind" = 2
                      AND t."Note" ILIKE 'Welkomsttoken%'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartnerReferralRewardedAtUtc",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PartnerReferralStatus",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PartnerReferredAtUtc",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "WelcomeTokenLedgerCredited",
                table: "Companies");
        }
    }
}
