using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260730100000_VatDeclarationWizard")]
    public partial class VatDeclarationWizard : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VatDeclarationStatusLabel",
                table: "TokenPurchaseInvoices",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VatDeclarationId",
                table: "TokenPurchaseInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VatTreatment",
                table: "SelfBillingInvoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VatDeclarationStatusLabel",
                table: "SelfBillingInvoices",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VatDeclarationId",
                table: "SelfBillingInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VatDeclarations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Quarter = table.Column<int>(type: "integer", nullable: false),
                    PeriodLabel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Rubriek1OmzetExVatCents = table.Column<int>(type: "integer", nullable: false),
                    Rubriek1VatCents = table.Column<int>(type: "integer", nullable: false),
                    TokenInvoiceCount = table.Column<int>(type: "integer", nullable: false),
                    GoodwillCount = table.Column<int>(type: "integer", nullable: false),
                    Rubriek5VoorbelastingCents = table.Column<int>(type: "integer", nullable: false),
                    Rubriek5CostExVatCents = table.Column<int>(type: "integer", nullable: false),
                    SalesManagerInvoiceCount = table.Column<int>(type: "integer", nullable: false),
                    AmountDueCents = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    GeneratedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PdfBytes = table.Column<byte[]>(type: "bytea", nullable: true),
                    PdfFileName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PlatformCompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PlatformKvkNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PlatformVatNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PlatformAddress = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatDeclarations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VatDeclarations_Users_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VatDeclarations_GeneratedByUserId",
                table: "VatDeclarations",
                column: "GeneratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VatDeclarations_PeriodLabel",
                table: "VatDeclarations",
                column: "PeriodLabel");

            migrationBuilder.CreateIndex(
                name: "IX_VatDeclarations_Year_Quarter",
                table: "VatDeclarations",
                columns: new[] { "Year", "Quarter" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenPurchaseInvoices_VatDeclarationId",
                table: "TokenPurchaseInvoices",
                column: "VatDeclarationId");

            migrationBuilder.CreateIndex(
                name: "IX_SelfBillingInvoices_VatDeclarationId",
                table: "SelfBillingInvoices",
                column: "VatDeclarationId");

            migrationBuilder.AddForeignKey(
                name: "FK_TokenPurchaseInvoices_VatDeclarations_VatDeclarationId",
                table: "TokenPurchaseInvoices",
                column: "VatDeclarationId",
                principalTable: "VatDeclarations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SelfBillingInvoices_VatDeclarations_VatDeclarationId",
                table: "SelfBillingInvoices",
                column: "VatDeclarationId",
                principalTable: "VatDeclarations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TokenPurchaseInvoices_VatDeclarations_VatDeclarationId",
                table: "TokenPurchaseInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_SelfBillingInvoices_VatDeclarations_VatDeclarationId",
                table: "SelfBillingInvoices");

            migrationBuilder.DropTable(name: "VatDeclarations");

            migrationBuilder.DropIndex(
                name: "IX_TokenPurchaseInvoices_VatDeclarationId",
                table: "TokenPurchaseInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SelfBillingInvoices_VatDeclarationId",
                table: "SelfBillingInvoices");

            migrationBuilder.DropColumn(name: "VatDeclarationStatusLabel", table: "TokenPurchaseInvoices");
            migrationBuilder.DropColumn(name: "VatDeclarationId", table: "TokenPurchaseInvoices");
            migrationBuilder.DropColumn(name: "VatTreatment", table: "SelfBillingInvoices");
            migrationBuilder.DropColumn(name: "VatDeclarationStatusLabel", table: "SelfBillingInvoices");
            migrationBuilder.DropColumn(name: "VatDeclarationId", table: "SelfBillingInvoices");
        }
    }
}
