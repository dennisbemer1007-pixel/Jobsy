using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SalesManagerReferralCommissionHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommissionLedgerEntries_SourceTokenCheckoutId",
                table: "CommissionLedgerEntries");

            // Existing salesmanagers were Admin-created (tier-0) and may recruit.
            migrationBuilder.AddColumn<bool>(
                name: "CanRecruitSalesManagers",
                table: "SalesManagerProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReferredBySalesManagerUserId",
                table: "SalesManagerProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommissionDurationDays",
                table: "SalesCommercialSettings",
                type: "integer",
                nullable: false,
                defaultValue: 365);

            migrationBuilder.AddColumn<decimal>(
                name: "DirectCommissionRate",
                table: "SalesCommercialSettings",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0.15m);

            migrationBuilder.AddColumn<decimal>(
                name: "IndirectCommissionRate",
                table: "SalesCommercialSettings",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0.03m);

            migrationBuilder.CreateTable(
                name: "SalesManagerApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferrerSalesManagerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferrerTrackingCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CandidateEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CandidateFullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Motivation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByAdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProvisionedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesManagerApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesManagerApplications_Users_ProvisionedUserId",
                        column: x => x.ProvisionedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesManagerApplications_Users_ReferrerSalesManagerUserId",
                        column: x => x.ReferrerSalesManagerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesManagerApplications_Users_ReviewedByAdminUserId",
                        column: x => x.ReviewedByAdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesManagerProfiles_ReferredBySalesManagerUserId",
                table: "SalesManagerProfiles",
                column: "ReferredBySalesManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLedgerEntries_SourceTokenCheckoutId_SalesManagerU~",
                table: "CommissionLedgerEntries",
                columns: new[] { "SourceTokenCheckoutId", "SalesManagerUserId", "Kind" },
                unique: true,
                filter: "\"SourceTokenCheckoutId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesManagerApplications_CandidateEmail",
                table: "SalesManagerApplications",
                column: "CandidateEmail");

            migrationBuilder.CreateIndex(
                name: "IX_SalesManagerApplications_CandidateEmail_Status",
                table: "SalesManagerApplications",
                columns: new[] { "CandidateEmail", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesManagerApplications_CreatedAtUtc",
                table: "SalesManagerApplications",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SalesManagerApplications_ProvisionedUserId",
                table: "SalesManagerApplications",
                column: "ProvisionedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesManagerApplications_ReferrerSalesManagerUserId",
                table: "SalesManagerApplications",
                column: "ReferrerSalesManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesManagerApplications_ReviewedByAdminUserId",
                table: "SalesManagerApplications",
                column: "ReviewedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesManagerApplications_Status",
                table: "SalesManagerApplications",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesManagerProfiles_Users_ReferredBySalesManagerUserId",
                table: "SalesManagerProfiles",
                column: "ReferredBySalesManagerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesManagerProfiles_Users_ReferredBySalesManagerUserId",
                table: "SalesManagerProfiles");

            migrationBuilder.DropTable(
                name: "SalesManagerApplications");

            migrationBuilder.DropIndex(
                name: "IX_SalesManagerProfiles_ReferredBySalesManagerUserId",
                table: "SalesManagerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CommissionLedgerEntries_SourceTokenCheckoutId_SalesManagerU~",
                table: "CommissionLedgerEntries");

            migrationBuilder.DropColumn(
                name: "CanRecruitSalesManagers",
                table: "SalesManagerProfiles");

            migrationBuilder.DropColumn(
                name: "ReferredBySalesManagerUserId",
                table: "SalesManagerProfiles");

            migrationBuilder.DropColumn(
                name: "CommissionDurationDays",
                table: "SalesCommercialSettings");

            migrationBuilder.DropColumn(
                name: "DirectCommissionRate",
                table: "SalesCommercialSettings");

            migrationBuilder.DropColumn(
                name: "IndirectCommissionRate",
                table: "SalesCommercialSettings");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLedgerEntries_SourceTokenCheckoutId",
                table: "CommissionLedgerEntries",
                column: "SourceTokenCheckoutId",
                unique: true,
                filter: "\"SourceTokenCheckoutId\" IS NOT NULL");
        }
    }
}
