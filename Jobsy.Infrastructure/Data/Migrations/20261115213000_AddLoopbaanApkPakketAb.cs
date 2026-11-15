using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoopbaanApkPakketAb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LoopbaanApkPackage",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LoopbaanApkBasisPrice",
                table: "PlatformFeatureSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoopbaanApkPriceUnit",
                table: "PlatformFeatureSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoopbaanApkUitgebreidPrice",
                table: "PlatformFeatureSettings",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WerkgeverApkUitgebreidRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Handled = table.Column<bool>(type: "boolean", nullable: false),
                    HandledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WerkgeverApkUitgebreidRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WerkgeverApkUitgebreidRequests_CompanyId_Handled",
                table: "WerkgeverApkUitgebreidRequests",
                columns: new[] { "CompanyId", "Handled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WerkgeverApkUitgebreidRequests");

            migrationBuilder.DropColumn(
                name: "LoopbaanApkPackage",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "LoopbaanApkBasisPrice",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "LoopbaanApkPriceUnit",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "LoopbaanApkUitgebreidPrice",
                table: "PlatformFeatureSettings");
        }
    }
}