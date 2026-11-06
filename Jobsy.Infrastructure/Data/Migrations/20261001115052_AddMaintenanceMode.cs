using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MaintenanceEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MaintenanceExpectedEndUtc",
                table: "PlatformFeatureSettings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaintenanceNote",
                table: "PlatformFeatureSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaintenanceEnabled",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "MaintenanceExpectedEndUtc",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "MaintenanceNote",
                table: "PlatformFeatureSettings");
        }
    }
}
