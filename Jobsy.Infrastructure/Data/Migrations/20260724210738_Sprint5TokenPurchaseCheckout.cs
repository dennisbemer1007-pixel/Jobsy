using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint5TokenPurchaseCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TokenPurchaseCheckouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackSize = table.Column<int>(type: "integer", nullable: false),
                    AmountEuro = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenPurchaseCheckouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenPurchaseCheckouts_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokenPurchaseCheckouts_CompanyId",
                table: "TokenPurchaseCheckouts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenPurchaseCheckouts_PaymentId",
                table: "TokenPurchaseCheckouts",
                column: "PaymentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenPurchaseCheckouts");
        }
    }
}
