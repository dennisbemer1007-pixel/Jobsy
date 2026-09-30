using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeepTestPaymentsAndPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VatBufferTransfers_TokenPurchaseInvoiceId",
                table: "VatBufferTransfers");

            migrationBuilder.DropIndex(
                name: "IX_DeepAnalysisCheckouts_PaymentId",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropIndex(
                name: "IX_DeepAnalysisCheckouts_UserId",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.AlterColumn<Guid>(
                name: "TokenPurchaseInvoiceId",
                table: "VatBufferTransfers",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ConsumerPurchaseInvoiceId",
                table: "VatBufferTransfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeepTestPriceCareerEuro",
                table: "FlexCommercialSettings",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DeepTestPriceCompetenceEuro",
                table: "FlexCommercialSettings",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DeepTestPriceCultureEuro",
                table: "FlexCommercialSettings",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DeepTestPriceValuesEuro",
                table: "FlexCommercialSettings",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "AmountExVatCents",
                table: "DeepAnalysisCheckouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                table: "DeepAnalysisCheckouts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FailedAtUtc",
                table: "DeepAnalysisCheckouts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InvoiceId",
                table: "DeepAnalysisCheckouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsStub",
                table: "DeepAnalysisCheckouts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Locale",
                table: "DeepAnalysisCheckouts",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "DeepAnalysisCheckouts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderStatus",
                table: "DeepAnalysisCheckouts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptSendAttempts",
                table: "DeepAnalysisCheckouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiptSentAtUtc",
                table: "DeepAnalysisCheckouts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalAmountCents",
                table: "DeepAnalysisCheckouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VatAmountCents",
                table: "DeepAnalysisCheckouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "WaiverAcceptedAtUtc",
                table: "DeepAnalysisCheckouts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "WaiverTextVersion",
                table: "DeepAnalysisCheckouts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            // Carry admin deep-analysis price into the four per-kind columns.
            migrationBuilder.Sql("""
                UPDATE "FlexCommercialSettings"
                SET "DeepTestPriceCompetenceEuro" = CASE WHEN "DeepAnalysisPriceEuro" > 0 THEN "DeepAnalysisPriceEuro" ELSE 2.99 END,
                    "DeepTestPriceCareerEuro" = CASE WHEN "DeepAnalysisPriceEuro" > 0 THEN "DeepAnalysisPriceEuro" ELSE 2.99 END,
                    "DeepTestPriceValuesEuro" = CASE WHEN "DeepAnalysisPriceEuro" > 0 THEN "DeepAnalysisPriceEuro" ELSE 2.99 END,
                    "DeepTestPriceCultureEuro" = CASE WHEN "DeepAnalysisPriceEuro" > 0 THEN "DeepAnalysisPriceEuro" ELSE 2.99 END;
                """);

            // Existing checkouts: cents from AmountEuro, stub flag, legacy waiver.
            migrationBuilder.Sql("""
                UPDATE "DeepAnalysisCheckouts"
                SET "TotalAmountCents" = ROUND("AmountEuro" * 100),
                    "AmountExVatCents" = ROUND(ROUND("AmountEuro" * 100) / 1.21),
                    "VatAmountCents" = ROUND("AmountEuro" * 100) - ROUND(ROUND("AmountEuro" * 100) / 1.21),
                    "IsStub" = CASE WHEN "PaymentId" LIKE 'stub_deep_%' THEN TRUE ELSE FALSE END,
                    "WaiverTextVersion" = 'legacy',
                    "Locale" = CASE WHEN "Locale" = '' OR "Locale" IS NULL THEN 'nl' ELSE "Locale" END;
                """);

            migrationBuilder.CreateTable(
                name: "ConsumerPurchaseInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DeepAnalysisCheckoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CustomerCountry = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    AmountExVatCents = table.Column<int>(type: "integer", nullable: false),
                    VatAmountCents = table.Column<int>(type: "integer", nullable: false),
                    TotalAmountCents = table.Column<int>(type: "integer", nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    MolliePaymentId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IsStub = table.Column<bool>(type: "boolean", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VatDeclarationId = table.Column<Guid>(type: "uuid", nullable: true),
                    VatDeclarationStatusLabel = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumerPurchaseInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumerPurchaseInvoices_DeepAnalysisCheckouts_DeepAnalysis~",
                        column: x => x.DeepAnalysisCheckoutId,
                        principalTable: "DeepAnalysisCheckouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsumerPurchaseInvoices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ConsumerPurchaseInvoices_VatDeclarations_VatDeclarationId",
                        column: x => x.VatDeclarationId,
                        principalTable: "VatDeclarations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VatBufferTransfers_ConsumerPurchaseInvoiceId",
                table: "VatBufferTransfers",
                column: "ConsumerPurchaseInvoiceId",
                unique: true,
                filter: "\"ConsumerPurchaseInvoiceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VatBufferTransfers_TokenPurchaseInvoiceId",
                table: "VatBufferTransfers",
                column: "TokenPurchaseInvoiceId",
                unique: true,
                filter: "\"TokenPurchaseInvoiceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeepAnalysisCheckouts_PaymentId",
                table: "DeepAnalysisCheckouts",
                column: "PaymentId",
                unique: true,
                filter: "\"PaymentId\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_DeepAnalysisCheckouts_Status_CreatedAtUtc",
                table: "DeepAnalysisCheckouts",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerPurchaseInvoices_DeepAnalysisCheckoutId",
                table: "ConsumerPurchaseInvoices",
                column: "DeepAnalysisCheckoutId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerPurchaseInvoices_InvoiceNumber",
                table: "ConsumerPurchaseInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerPurchaseInvoices_IssuedAt",
                table: "ConsumerPurchaseInvoices",
                column: "IssuedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerPurchaseInvoices_UserId",
                table: "ConsumerPurchaseInvoices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerPurchaseInvoices_VatDeclarationId",
                table: "ConsumerPurchaseInvoices",
                column: "VatDeclarationId");

            migrationBuilder.AddForeignKey(
                name: "FK_VatBufferTransfers_ConsumerPurchaseInvoices_ConsumerPurchas~",
                table: "VatBufferTransfers",
                column: "ConsumerPurchaseInvoiceId",
                principalTable: "ConsumerPurchaseInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VatBufferTransfers_ConsumerPurchaseInvoices_ConsumerPurchas~",
                table: "VatBufferTransfers");

            migrationBuilder.DropTable(
                name: "ConsumerPurchaseInvoices");

            migrationBuilder.DropIndex(
                name: "IX_VatBufferTransfers_ConsumerPurchaseInvoiceId",
                table: "VatBufferTransfers");

            migrationBuilder.DropIndex(
                name: "IX_VatBufferTransfers_TokenPurchaseInvoiceId",
                table: "VatBufferTransfers");

            migrationBuilder.DropIndex(
                name: "IX_DeepAnalysisCheckouts_PaymentId",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropIndex(
                name: "IX_DeepAnalysisCheckouts_Status_CreatedAtUtc",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropColumn(
                name: "ConsumerPurchaseInvoiceId",
                table: "VatBufferTransfers");

            migrationBuilder.DropColumn(
                name: "DeepTestPriceCareerEuro",
                table: "FlexCommercialSettings");

            migrationBuilder.DropColumn(
                name: "DeepTestPriceCompetenceEuro",
                table: "FlexCommercialSettings");

            migrationBuilder.DropColumn(
                name: "DeepTestPriceCultureEuro",
                table: "FlexCommercialSettings");

            migrationBuilder.DropColumn(
                name: "DeepTestPriceValuesEuro",
                table: "FlexCommercialSettings");

            migrationBuilder.DropColumn(
                name: "AmountExVatCents",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropColumn(
                name: "FailedAtUtc",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropColumn(
                name: "IsStub",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropColumn(
                name: "Locale",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropColumn(
                name: "ProviderStatus",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropColumn(
                name: "ReceiptSendAttempts",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropColumn(
                name: "ReceiptSentAtUtc",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropColumn(
                name: "TotalAmountCents",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropColumn(
                name: "VatAmountCents",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropColumn(
                name: "WaiverAcceptedAtUtc",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropColumn(
                name: "WaiverTextVersion",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.AlterColumn<Guid>(
                name: "TokenPurchaseInvoiceId",
                table: "VatBufferTransfers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VatBufferTransfers_TokenPurchaseInvoiceId",
                table: "VatBufferTransfers",
                column: "TokenPurchaseInvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeepAnalysisCheckouts_PaymentId",
                table: "DeepAnalysisCheckouts",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeepAnalysisCheckouts_UserId",
                table: "DeepAnalysisCheckouts",
                column: "UserId");
        }
    }
}
