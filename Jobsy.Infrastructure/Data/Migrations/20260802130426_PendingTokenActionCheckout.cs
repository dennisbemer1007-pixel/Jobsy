using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PendingTokenActionCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingTokenActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenPurchaseCheckoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    VacancyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionKind = table.Column<int>(type: "integer", nullable: false),
                    OptionHighlight = table.Column<bool>(type: "boolean", nullable: false),
                    OptionPushBom = table.Column<bool>(type: "boolean", nullable: false),
                    OptionExtend = table.Column<bool>(type: "boolean", nullable: false),
                    RequiredTokens = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingTokenActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingTokenActions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PendingTokenActions_TokenPurchaseCheckouts_TokenPurchaseChe~",
                        column: x => x.TokenPurchaseCheckoutId,
                        principalTable: "TokenPurchaseCheckouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PendingTokenActions_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingTokenActions_CompanyId",
                table: "PendingTokenActions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingTokenActions_Status",
                table: "PendingTokenActions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PendingTokenActions_TokenPurchaseCheckoutId",
                table: "PendingTokenActions",
                column: "TokenPurchaseCheckoutId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingTokenActions_VacancyId",
                table: "PendingTokenActions",
                column: "VacancyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingTokenActions");
        }
    }
}
