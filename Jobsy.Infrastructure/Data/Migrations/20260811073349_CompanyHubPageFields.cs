using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompanyHubPageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HubAboutText",
                table: "Companies",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HubCultureText",
                table: "Companies",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HubHighlightedUntil",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HubVideoUrl",
                table: "Companies",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_HubHighlightedUntil",
                table: "Companies",
                column: "HubHighlightedUntil");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Companies_HubHighlightedUntil",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "HubAboutText",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "HubCultureText",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "HubHighlightedUntil",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "HubVideoUrl",
                table: "Companies");
        }
    }
}
