using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class IntermediarySalesRevenueShareKpis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAtUtc",
                table: "Vacancies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RevenueShareLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenCheckoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipientKind = table.Column<int>(type: "integer", nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    AmountEuro = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Tokens = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevenueShareLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevenueShareLogs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevenueShareLogs_Companies_RecipientCompanyId",
                        column: x => x.RecipientCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RevenueShareLogs_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_ClosedAtUtc",
                table: "Vacancies",
                column: "ClosedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueShareLogs_CompanyId",
                table: "RevenueShareLogs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueShareLogs_CreatedAtUtc",
                table: "RevenueShareLogs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueShareLogs_RecipientCompanyId",
                table: "RevenueShareLogs",
                column: "RecipientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueShareLogs_RecipientUserId",
                table: "RevenueShareLogs",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueShareLogs_TokenCheckoutId",
                table: "RevenueShareLogs",
                column: "TokenCheckoutId");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueShareLogs_TokenCheckoutId_RecipientKind",
                table: "RevenueShareLogs",
                columns: new[] { "TokenCheckoutId", "RecipientKind" },
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RevenueShareLogs");

            migrationBuilder.DropIndex(
                name: "IX_Vacancies_ClosedAtUtc",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                table: "Vacancies");
        }
    }
}
