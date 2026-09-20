using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class LobsyCommercialAdminAmounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AgencyAnnualPriceEuro",
                table: "FlexCommercialSettings",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 4000m);

            migrationBuilder.AddColumn<decimal>(
                name: "ContactUnlockCostTokens",
                table: "FlexCommercialSettings",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "DeepAnalysisPriceEuro",
                table: "FlexCommercialSettings",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 2.99m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceEuro",
                table: "AgencyAnnualSubscriptions",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 4000m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgencyAnnualPriceEuro",
                table: "FlexCommercialSettings");

            migrationBuilder.DropColumn(
                name: "ContactUnlockCostTokens",
                table: "FlexCommercialSettings");

            migrationBuilder.DropColumn(
                name: "DeepAnalysisPriceEuro",
                table: "FlexCommercialSettings");

            migrationBuilder.DropColumn(
                name: "PriceEuro",
                table: "AgencyAnnualSubscriptions");
        }
    }
}
