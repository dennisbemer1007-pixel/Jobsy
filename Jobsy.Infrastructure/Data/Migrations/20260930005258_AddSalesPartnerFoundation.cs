using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesPartnerFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommissionLedgerEntries_SourceTokenCheckoutId_SalesManagerU~",
                table: "CommissionLedgerEntries");

            migrationBuilder.AddColumn<Guid>(
                name: "SalesPayoutRequestId",
                table: "SelfBillingInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SelfBillingConsentId",
                table: "SelfBillingInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailPrefsJson",
                table: "SalesManagerProfiles",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IbanChangedAtUtc",
                table: "SalesManagerProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IbanPayoutHoldUntilUtc",
                table: "SalesManagerProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutAccountHolderName",
                table: "SalesManagerProfiles",
                type: "character varying(70)",
                maxLength: 70,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VatTreatment",
                table: "SalesManagerProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "VatTreatmentChangedAtUtc",
                table: "SalesManagerProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CandidateEmailSha256",
                table: "SalesManagerApplications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PersonalDataClearedAtUtc",
                table: "SalesManagerApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReferrerConfirmedPermission",
                table: "SalesManagerApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubjectNotifiedAtUtc",
                table: "SalesManagerApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubjectObjectedAtUtc",
                table: "SalesManagerApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttributionCookieDays",
                table: "SalesCommercialSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CommissionHoldDays",
                table: "SalesCommercialSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IbanChangeHoldDays",
                table: "SalesCommercialSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PayoutMinimumEuro",
                table: "SalesCommercialSettings",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "AmbassadorsEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CommissionStartsAtUtc",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionYear2RateSnapshot",
                table: "Companies",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionYear3RateSnapshot",
                table: "Companies",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LegalForm",
                table: "Companies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SalesAttributedAtUtc",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalesAttributionSource",
                table: "Companies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableFromUtc",
                table: "CommissionLedgerEntries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CorrectsEntryId",
                table: "CommissionLedgerEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "CommissionLedgerEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "CommissionLedgerEntries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalesPayoutRequestId",
                table: "CommissionLedgerEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceRefundKey",
                table: "CommissionLedgerEntries",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailPrefsJson",
                table: "AmbassadeurProfiles",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IbanChangedAtUtc",
                table: "AmbassadeurProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IbanPayoutHoldUntilUtc",
                table: "AmbassadeurProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutAccountHolderName",
                table: "AmbassadeurProfiles",
                type: "character varying(70)",
                maxLength: 70,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VatTreatment",
                table: "AmbassadeurProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "VatTreatmentChangedAtUtc",
                table: "AmbassadeurProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SalesAttributionChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesAttributionChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesAttributionChanges_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesLinkClickDailies",
                columns: table => new
                {
                    BeneficiaryUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesLinkClickDailies", x => new { x.BeneficiaryUserId, x.Date, x.Channel });
                });

            migrationBuilder.CreateTable(
                name: "SalesPayoutRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsExtra = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExportedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExportFileSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProviderKey = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesPayoutRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesSelfBillingConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TextSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesSelfBillingConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesSelfBillingConsents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesPayoutRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BeneficiaryUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountExVat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VatTreatment = table.Column<int>(type: "integer", nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalInclVat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaskedIban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SalesPayoutRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    SelfBillingInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesPayoutRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesPayoutRequests_SalesPayoutRuns_SalesPayoutRunId",
                        column: x => x.SalesPayoutRunId,
                        principalTable: "SalesPayoutRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesPayoutRequests_SelfBillingInvoices_SelfBillingInvoiceId",
                        column: x => x.SelfBillingInvoiceId,
                        principalTable: "SelfBillingInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesPayoutRequests_Users_BeneficiaryUserId",
                        column: x => x.BeneficiaryUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SelfBillingInvoices_SelfBillingConsentId",
                table: "SelfBillingInvoices",
                column: "SelfBillingConsentId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesManagerApplications_CandidateEmailSha256",
                table: "SalesManagerApplications",
                column: "CandidateEmailSha256");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLedgerEntries_CorrectsEntryId",
                table: "CommissionLedgerEntries",
                column: "CorrectsEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLedgerEntries_SalesPayoutRequestId",
                table: "CommissionLedgerEntries",
                column: "SalesPayoutRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLedgerEntries_SourceTokenCheckoutId_SalesManager~1",
                table: "CommissionLedgerEntries",
                columns: new[] { "SourceTokenCheckoutId", "SalesManagerUserId", "Kind", "SourceRefundKey" },
                unique: true,
                filter: "\"SourceRefundKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLedgerEntries_SourceTokenCheckoutId_SalesManagerU~",
                table: "CommissionLedgerEntries",
                columns: new[] { "SourceTokenCheckoutId", "SalesManagerUserId", "Kind" },
                unique: true,
                filter: "\"SourceTokenCheckoutId\" IS NOT NULL AND \"SourceRefundKey\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesAttributionChanges_CompanyId",
                table: "SalesAttributionChanges",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesPayoutRequests_BeneficiaryUserId",
                table: "SalesPayoutRequests",
                column: "BeneficiaryUserId",
                unique: true,
                filter: "\"Status\" IN (0, 1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_SalesPayoutRequests_SalesPayoutRunId",
                table: "SalesPayoutRequests",
                column: "SalesPayoutRunId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesPayoutRequests_SelfBillingInvoiceId",
                table: "SalesPayoutRequests",
                column: "SelfBillingInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesPayoutRuns_RunDate",
                table: "SalesPayoutRuns",
                column: "RunDate",
                unique: true,
                filter: "\"IsExtra\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_SalesSelfBillingConsents_UserId_Version",
                table: "SalesSelfBillingConsents",
                columns: new[] { "UserId", "Version" });

            migrationBuilder.AddForeignKey(
                name: "FK_CommissionLedgerEntries_CommissionLedgerEntries_CorrectsEnt~",
                table: "CommissionLedgerEntries",
                column: "CorrectsEntryId",
                principalTable: "CommissionLedgerEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CommissionLedgerEntries_SalesPayoutRequests_SalesPayoutRequ~",
                table: "CommissionLedgerEntries",
                column: "SalesPayoutRequestId",
                principalTable: "SalesPayoutRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SelfBillingInvoices_SalesSelfBillingConsents_SelfBillingCon~",
                table: "SelfBillingInvoices",
                column: "SelfBillingConsentId",
                principalTable: "SalesSelfBillingConsents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Idempotent backfill (§D / 01.3) — AvailableFromUtc is new; set for all existing rows.
            migrationBuilder.Sql("""
                UPDATE "CommissionLedgerEntries"
                SET "AvailableFromUtc" = "CreatedAt" + INTERVAL '14 days'
                WHERE "Kind" IN (0, 1, 4);

                UPDATE "CommissionLedgerEntries"
                SET "AvailableFromUtc" = "CreatedAt"
                WHERE "Kind" IN (2, 3);

                UPDATE "Companies"
                SET "SalesAttributedAtUtc" = COALESCE("FirstYearStartedAt", "PartnerReferredAtUtc"),
                    "SalesAttributionSource" = 3
                WHERE ("ReferredBySalesManagerUserId" IS NOT NULL
                       OR "ReferredByAmbassadeurUserId" IS NOT NULL)
                  AND "SalesAttributedAtUtc" IS NULL
                  AND COALESCE("FirstYearStartedAt", "PartnerReferredAtUtc") IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommissionLedgerEntries_CommissionLedgerEntries_CorrectsEnt~",
                table: "CommissionLedgerEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_CommissionLedgerEntries_SalesPayoutRequests_SalesPayoutRequ~",
                table: "CommissionLedgerEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_SelfBillingInvoices_SalesSelfBillingConsents_SelfBillingCon~",
                table: "SelfBillingInvoices");

            migrationBuilder.DropTable(
                name: "SalesAttributionChanges");

            migrationBuilder.DropTable(
                name: "SalesLinkClickDailies");

            migrationBuilder.DropTable(
                name: "SalesPayoutRequests");

            migrationBuilder.DropTable(
                name: "SalesSelfBillingConsents");

            migrationBuilder.DropTable(
                name: "SalesPayoutRuns");

            migrationBuilder.DropIndex(
                name: "IX_SelfBillingInvoices_SelfBillingConsentId",
                table: "SelfBillingInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SalesManagerApplications_CandidateEmailSha256",
                table: "SalesManagerApplications");

            migrationBuilder.DropIndex(
                name: "IX_CommissionLedgerEntries_CorrectsEntryId",
                table: "CommissionLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_CommissionLedgerEntries_SalesPayoutRequestId",
                table: "CommissionLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_CommissionLedgerEntries_SourceTokenCheckoutId_SalesManager~1",
                table: "CommissionLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_CommissionLedgerEntries_SourceTokenCheckoutId_SalesManagerU~",
                table: "CommissionLedgerEntries");

            migrationBuilder.DropColumn(
                name: "SalesPayoutRequestId",
                table: "SelfBillingInvoices");

            migrationBuilder.DropColumn(
                name: "SelfBillingConsentId",
                table: "SelfBillingInvoices");

            migrationBuilder.DropColumn(
                name: "EmailPrefsJson",
                table: "SalesManagerProfiles");

            migrationBuilder.DropColumn(
                name: "IbanChangedAtUtc",
                table: "SalesManagerProfiles");

            migrationBuilder.DropColumn(
                name: "IbanPayoutHoldUntilUtc",
                table: "SalesManagerProfiles");

            migrationBuilder.DropColumn(
                name: "PayoutAccountHolderName",
                table: "SalesManagerProfiles");

            migrationBuilder.DropColumn(
                name: "VatTreatment",
                table: "SalesManagerProfiles");

            migrationBuilder.DropColumn(
                name: "VatTreatmentChangedAtUtc",
                table: "SalesManagerProfiles");

            migrationBuilder.DropColumn(
                name: "CandidateEmailSha256",
                table: "SalesManagerApplications");

            migrationBuilder.DropColumn(
                name: "PersonalDataClearedAtUtc",
                table: "SalesManagerApplications");

            migrationBuilder.DropColumn(
                name: "ReferrerConfirmedPermission",
                table: "SalesManagerApplications");

            migrationBuilder.DropColumn(
                name: "SubjectNotifiedAtUtc",
                table: "SalesManagerApplications");

            migrationBuilder.DropColumn(
                name: "SubjectObjectedAtUtc",
                table: "SalesManagerApplications");

            migrationBuilder.DropColumn(
                name: "AttributionCookieDays",
                table: "SalesCommercialSettings");

            migrationBuilder.DropColumn(
                name: "CommissionHoldDays",
                table: "SalesCommercialSettings");

            migrationBuilder.DropColumn(
                name: "IbanChangeHoldDays",
                table: "SalesCommercialSettings");

            migrationBuilder.DropColumn(
                name: "PayoutMinimumEuro",
                table: "SalesCommercialSettings");

            migrationBuilder.DropColumn(
                name: "AmbassadorsEnabled",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "CommissionStartsAtUtc",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CommissionYear2RateSnapshot",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CommissionYear3RateSnapshot",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "LegalForm",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SalesAttributedAtUtc",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SalesAttributionSource",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "AvailableFromUtc",
                table: "CommissionLedgerEntries");

            migrationBuilder.DropColumn(
                name: "CorrectsEntryId",
                table: "CommissionLedgerEntries");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "CommissionLedgerEntries");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "CommissionLedgerEntries");

            migrationBuilder.DropColumn(
                name: "SalesPayoutRequestId",
                table: "CommissionLedgerEntries");

            migrationBuilder.DropColumn(
                name: "SourceRefundKey",
                table: "CommissionLedgerEntries");

            migrationBuilder.DropColumn(
                name: "EmailPrefsJson",
                table: "AmbassadeurProfiles");

            migrationBuilder.DropColumn(
                name: "IbanChangedAtUtc",
                table: "AmbassadeurProfiles");

            migrationBuilder.DropColumn(
                name: "IbanPayoutHoldUntilUtc",
                table: "AmbassadeurProfiles");

            migrationBuilder.DropColumn(
                name: "PayoutAccountHolderName",
                table: "AmbassadeurProfiles");

            migrationBuilder.DropColumn(
                name: "VatTreatment",
                table: "AmbassadeurProfiles");

            migrationBuilder.DropColumn(
                name: "VatTreatmentChangedAtUtc",
                table: "AmbassadeurProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLedgerEntries_SourceTokenCheckoutId_SalesManagerU~",
                table: "CommissionLedgerEntries",
                columns: new[] { "SourceTokenCheckoutId", "SalesManagerUserId", "Kind" },
                unique: true,
                filter: "\"SourceTokenCheckoutId\" IS NOT NULL");
        }
    }
}
