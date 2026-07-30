using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260730090000_TokenBillingVatBuffer")]
    public partial class TokenBillingVatBuffer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VatBufferIban",
                table: "PlatformCompanySettings",
                type: "character varying(34)",
                maxLength: 34,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AmountExVatCents",
                table: "TokenPurchaseCheckouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VatAmountCents",
                table: "TokenPurchaseCheckouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalAmountCents",
                table: "TokenPurchaseCheckouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TokenTransactionId",
                table: "TokenPurchaseCheckouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TokenPurchaseInvoiceId",
                table: "TokenPurchaseCheckouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AmountExVatCents",
                table: "TokenTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VatAmountCents",
                table: "TokenTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalAmountCents",
                table: "TokenTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TokenPurchaseCheckoutId",
                table: "TokenTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TokenPurchaseInvoiceId",
                table: "TokenTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TokenPurchaseInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TokenPurchaseCheckoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MolliePaymentId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PackSize = table.Column<int>(type: "integer", nullable: false),
                    AmountExVatCents = table.Column<int>(type: "integer", nullable: false),
                    VatAmountCents = table.Column<int>(type: "integer", nullable: false),
                    TotalAmountCents = table.Column<int>(type: "integer", nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CompanyKvkNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CompanyAddress = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenPurchaseInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenPurchaseInvoices_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TokenPurchaseInvoices_TokenPurchaseCheckouts_TokenPurchaseCheckoutId",
                        column: x => x.TokenPurchaseCheckoutId,
                        principalTable: "TokenPurchaseCheckouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TokenPurchaseInvoices_TokenTransactions_TokenTransactionId",
                        column: x => x.TokenTransactionId,
                        principalTable: "TokenTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VatBufferTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenPurchaseInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DestinationIban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    AmountCents = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatBufferTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VatBufferTransfers_TokenPurchaseInvoices_TokenPurchaseInvoiceId",
                        column: x => x.TokenPurchaseInvoiceId,
                        principalTable: "TokenPurchaseInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_Kind",
                table: "TokenTransactions",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_TokenPurchaseCheckoutId",
                table: "TokenTransactions",
                column: "TokenPurchaseCheckoutId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_TokenPurchaseInvoiceId",
                table: "TokenTransactions",
                column: "TokenPurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenPurchaseInvoices_CompanyId",
                table: "TokenPurchaseInvoices",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenPurchaseInvoices_InvoiceNumber",
                table: "TokenPurchaseInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenPurchaseInvoices_IssuedAt",
                table: "TokenPurchaseInvoices",
                column: "IssuedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TokenPurchaseInvoices_TokenPurchaseCheckoutId",
                table: "TokenPurchaseInvoices",
                column: "TokenPurchaseCheckoutId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenPurchaseInvoices_TokenTransactionId",
                table: "TokenPurchaseInvoices",
                column: "TokenTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_VatBufferTransfers_CreatedAt",
                table: "VatBufferTransfers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VatBufferTransfers_Status",
                table: "VatBufferTransfers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VatBufferTransfers_TokenPurchaseInvoiceId",
                table: "VatBufferTransfers",
                column: "TokenPurchaseInvoiceId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TokenTransactions_TokenPurchaseCheckouts_TokenPurchaseCheckoutId",
                table: "TokenTransactions",
                column: "TokenPurchaseCheckoutId",
                principalTable: "TokenPurchaseCheckouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TokenTransactions_TokenPurchaseInvoices_TokenPurchaseInvoiceId",
                table: "TokenTransactions",
                column: "TokenPurchaseInvoiceId",
                principalTable: "TokenPurchaseInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Backfill cents on existing checkouts from AmountEuro (incl. BTW).
            migrationBuilder.Sql("""
                UPDATE "TokenPurchaseCheckouts"
                SET
                    "TotalAmountCents" = ROUND("AmountEuro" * 100)::int,
                    "AmountExVatCents" = ROUND(("AmountEuro" * 100) / 1.21)::int,
                    "VatAmountCents" = ROUND("AmountEuro" * 100)::int - ROUND(("AmountEuro" * 100) / 1.21)::int
                WHERE "TotalAmountCents" = 0 AND "AmountEuro" > 0;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TokenTransactions_TokenPurchaseInvoices_TokenPurchaseInvoiceId",
                table: "TokenTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_TokenTransactions_TokenPurchaseCheckouts_TokenPurchaseCheckoutId",
                table: "TokenTransactions");

            migrationBuilder.DropTable(name: "VatBufferTransfers");
            migrationBuilder.DropTable(name: "TokenPurchaseInvoices");

            migrationBuilder.DropIndex(name: "IX_TokenTransactions_Kind", table: "TokenTransactions");
            migrationBuilder.DropIndex(name: "IX_TokenTransactions_TokenPurchaseCheckoutId", table: "TokenTransactions");
            migrationBuilder.DropIndex(name: "IX_TokenTransactions_TokenPurchaseInvoiceId", table: "TokenTransactions");

            migrationBuilder.DropColumn(name: "VatBufferIban", table: "PlatformCompanySettings");
            migrationBuilder.DropColumn(name: "AmountExVatCents", table: "TokenPurchaseCheckouts");
            migrationBuilder.DropColumn(name: "VatAmountCents", table: "TokenPurchaseCheckouts");
            migrationBuilder.DropColumn(name: "TotalAmountCents", table: "TokenPurchaseCheckouts");
            migrationBuilder.DropColumn(name: "TokenTransactionId", table: "TokenPurchaseCheckouts");
            migrationBuilder.DropColumn(name: "TokenPurchaseInvoiceId", table: "TokenPurchaseCheckouts");
            migrationBuilder.DropColumn(name: "AmountExVatCents", table: "TokenTransactions");
            migrationBuilder.DropColumn(name: "VatAmountCents", table: "TokenTransactions");
            migrationBuilder.DropColumn(name: "TotalAmountCents", table: "TokenTransactions");
            migrationBuilder.DropColumn(name: "TokenPurchaseCheckoutId", table: "TokenTransactions");
            migrationBuilder.DropColumn(name: "TokenPurchaseInvoiceId", table: "TokenTransactions");
        }
    }
}
