using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SalesManagerHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommissionLedgerEntries_CompanyId",
                table: "CommissionLedgerEntries");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLedgerEntries_CompanyId",
                table: "CommissionLedgerEntries",
                column: "CompanyId",
                unique: true,
                filter: "\"Kind\" = 0 AND \"CompanyId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommissionLedgerEntries_CompanyId",
                table: "CommissionLedgerEntries");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLedgerEntries_CompanyId",
                table: "CommissionLedgerEntries",
                column: "CompanyId");
        }
    }
}
