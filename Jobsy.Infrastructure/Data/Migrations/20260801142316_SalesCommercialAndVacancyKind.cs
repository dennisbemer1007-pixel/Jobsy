using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SalesCommercialAndVacancyKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Vacancies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PendingStartHighlightBonus",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SalesCommercialSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseTokenValueEuro = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    HighlightCarouselTokens = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    HighlightPulseTokens = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    HighlightCarouselDays = table.Column<int>(type: "integer", nullable: false),
                    StartHighlightBonusTokens = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesCommercialSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    TokenAmount = table.Column<int>(type: "integer", nullable: false),
                    PriceEuro = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesPackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VacancyTypeTokenCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    CostTokens = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacancyTypeTokenCosts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_Status_Kind",
                table: "Vacancies",
                columns: new[] { "Status", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesPackages_Category_SortOrder",
                table: "SalesPackages",
                columns: new[] { "Category", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesPackages_Code",
                table: "SalesPackages",
                column: "Code",
                unique: true,
                filter: "\"Code\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyTypeTokenCosts_Kind",
                table: "VacancyTypeTokenCosts",
                column: "Kind",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesCommercialSettings");

            migrationBuilder.DropTable(
                name: "SalesPackages");

            migrationBuilder.DropTable(
                name: "VacancyTypeTokenCosts");

            migrationBuilder.DropIndex(
                name: "IX_Vacancies_Status_Kind",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "PendingStartHighlightBonus",
                table: "Companies");
        }
    }
}
