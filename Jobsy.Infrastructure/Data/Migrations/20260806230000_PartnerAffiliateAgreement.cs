using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260806230000_PartnerAffiliateAgreement")]
    public partial class PartnerAffiliateAgreement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AgreementSignedAt",
                table: "PartnerAffiliateProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgreementVersion",
                table: "PartnerAffiliateProfiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            // Safety net if an earlier draft-moderation migration already ran without the backfill.
            migrationBuilder.Sql(
                """
                UPDATE "Vacancies"
                SET "ContentModerationPassed" = FALSE
                WHERE "Status" = 0
                  AND "ContentModerationPassed" = TRUE;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgreementSignedAt",
                table: "PartnerAffiliateProfiles");

            migrationBuilder.DropColumn(
                name: "AgreementVersion",
                table: "PartnerAffiliateProfiles");
        }
    }
}
