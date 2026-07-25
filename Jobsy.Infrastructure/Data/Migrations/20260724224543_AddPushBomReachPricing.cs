using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPushBomReachPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PushBomPricingTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MinCandidates = table.Column<int>(type: "integer", nullable: false),
                    MaxCandidates = table.Column<int>(type: "integer", nullable: true),
                    CostTokens = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushBomPricingTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PushBomSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RadiusKm = table.Column<double>(type: "double precision", nullable: false),
                    MaxTravelMinutes = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushBomSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PushBomPricingTiers_MinCandidates_MaxCandidates",
                table: "PushBomPricingTiers",
                columns: new[] { "MinCandidates", "MaxCandidates" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PushBomPricingTiers");

            migrationBuilder.DropTable(
                name: "PushBomSettings");
        }
    }
}
