using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandIntegrationSettingsAndPlatformFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseUrl",
                table: "IntegrationCredentials",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "IntegrationCredentials",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientSecret",
                table: "IntegrationCredentials",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromAddress",
                table: "IntegrationCredentials",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPingAtUtc",
                table: "IntegrationCredentials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastPingMessage",
                table: "IntegrationCredentials",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LastPingOk",
                table: "IntegrationCredentials",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "IntegrationCredentials",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlatformFeatureSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VacancyContentModerationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AuthenticatorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ExposeRegistrationActivationLinks = table.Column<bool>(type: "boolean", nullable: false),
                    PublicWebBaseUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformFeatureSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "BaseUrl",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "ClientSecret",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "FromAddress",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "LastPingAtUtc",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "LastPingMessage",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "LastPingOk",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "IntegrationCredentials");
        }
    }
}
