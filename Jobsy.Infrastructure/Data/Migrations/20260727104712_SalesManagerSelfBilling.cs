using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SalesManagerSelfBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SalesManagerTrackingCode",
                table: "CompanyRegistrations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstYearStartedAt",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirstYearSupplierSlot",
                table: "Companies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReferredBySalesManagerUserId",
                table: "Companies",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SalesManagerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    KvkNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    VatNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    City = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Country = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Iban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: true),
                    TrackingCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    AgreementSignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AgreementVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    OnboardingCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesManagerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesManagerProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SelfBillingInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesManagerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SalesManagerCompanyName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SalesManagerKvkNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SalesManagerVatNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SalesManagerAddress = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SubtotalExVat = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalInclVat = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfBillingInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelfBillingInvoices_Users_SalesManagerUserId",
                        column: x => x.SalesManagerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierOnboardingCheckouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountEuro = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierOnboardingCheckouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierOnboardingCheckouts_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommissionLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesManagerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    AmountExVat = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourcePaymentId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    SourceTokenCheckoutId = table.Column<Guid>(type: "uuid", nullable: true),
                    SelfBillingInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionLedgerEntries_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CommissionLedgerEntries_SelfBillingInvoices_SelfBillingInvo~",
                        column: x => x.SelfBillingInvoiceId,
                        principalTable: "SelfBillingInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CommissionLedgerEntries_Users_SalesManagerUserId",
                        column: x => x.SalesManagerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SelfBillingInvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SelfBillingInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AmountExVat = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    SourceLedgerEntryId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfBillingInvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelfBillingInvoiceLines_SelfBillingInvoices_SelfBillingInvo~",
                        column: x => x.SelfBillingInvoiceId,
                        principalTable: "SelfBillingInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_FirstYearSupplierSlot",
                table: "Companies",
                column: "FirstYearSupplierSlot",
                unique: true,
                filter: "\"FirstYearSupplierSlot\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_ReferredBySalesManagerUserId",
                table: "Companies",
                column: "ReferredBySalesManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLedgerEntries_CompanyId",
                table: "CommissionLedgerEntries",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLedgerEntries_CreatedAt",
                table: "CommissionLedgerEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLedgerEntries_SalesManagerUserId",
                table: "CommissionLedgerEntries",
                column: "SalesManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLedgerEntries_SelfBillingInvoiceId",
                table: "CommissionLedgerEntries",
                column: "SelfBillingInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLedgerEntries_SourcePaymentId",
                table: "CommissionLedgerEntries",
                column: "SourcePaymentId",
                unique: true,
                filter: "\"SourcePaymentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLedgerEntries_SourceTokenCheckoutId",
                table: "CommissionLedgerEntries",
                column: "SourceTokenCheckoutId",
                unique: true,
                filter: "\"SourceTokenCheckoutId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesManagerProfiles_TrackingCode",
                table: "SalesManagerProfiles",
                column: "TrackingCode",
                unique: true,
                filter: "\"TrackingCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesManagerProfiles_UserId",
                table: "SalesManagerProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SelfBillingInvoiceLines_SelfBillingInvoiceId",
                table: "SelfBillingInvoiceLines",
                column: "SelfBillingInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SelfBillingInvoices_InvoiceNumber",
                table: "SelfBillingInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SelfBillingInvoices_SalesManagerUserId",
                table: "SelfBillingInvoices",
                column: "SalesManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOnboardingCheckouts_CompanyId",
                table: "SupplierOnboardingCheckouts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOnboardingCheckouts_PaymentId",
                table: "SupplierOnboardingCheckouts",
                column: "PaymentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Users_ReferredBySalesManagerUserId",
                table: "Companies",
                column: "ReferredBySalesManagerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Users_ReferredBySalesManagerUserId",
                table: "Companies");

            migrationBuilder.DropTable(
                name: "CommissionLedgerEntries");

            migrationBuilder.DropTable(
                name: "SalesManagerProfiles");

            migrationBuilder.DropTable(
                name: "SelfBillingInvoiceLines");

            migrationBuilder.DropTable(
                name: "SupplierOnboardingCheckouts");

            migrationBuilder.DropTable(
                name: "SelfBillingInvoices");

            migrationBuilder.DropIndex(
                name: "IX_Companies_FirstYearSupplierSlot",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_ReferredBySalesManagerUserId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SalesManagerTrackingCode",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "FirstYearStartedAt",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "FirstYearSupplierSlot",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ReferredBySalesManagerUserId",
                table: "Companies");
        }
    }
}
