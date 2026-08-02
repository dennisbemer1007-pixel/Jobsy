using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260802141500_TokenCheckoutIdempotencyIndexes")]
    public partial class TokenCheckoutIdempotencyIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deduplicate any pre-index race rows (keep oldest Id per checkout+kind).
            migrationBuilder.Sql(
                """
                DELETE FROM "TokenTransactions" t
                USING "TokenTransactions" newer
                WHERE t."TokenPurchaseCheckoutId" IS NOT NULL
                  AND newer."TokenPurchaseCheckoutId" = t."TokenPurchaseCheckoutId"
                  AND newer."Kind" = t."Kind"
                  AND newer."Id" > t."Id";
                """);

            migrationBuilder.DropIndex(
                name: "IX_TokenTransactions_TokenPurchaseCheckoutId",
                table: "TokenTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_Checkout_Kind",
                table: "TokenTransactions",
                columns: new[] { "TokenPurchaseCheckoutId", "Kind" },
                unique: true,
                filter: "\"TokenPurchaseCheckoutId\" IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TokenTransactions_Checkout_Kind",
                table: "TokenTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_TokenPurchaseCheckoutId",
                table: "TokenTransactions",
                column: "TokenPurchaseCheckoutId");
        }
    }
}
