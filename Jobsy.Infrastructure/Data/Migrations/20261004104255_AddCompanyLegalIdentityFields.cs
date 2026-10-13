using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyLegalIdentityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "PlatformCompanySettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCity",
                table: "PlatformCompanySettings",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalPostalCode",
                table: "PlatformCompanySettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalStreet",
                table: "PlatformCompanySettings",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivacyEmail",
                table: "PlatformCompanySettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportEmail",
                table: "PlatformCompanySettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TradeName",
                table: "PlatformCompanySettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "PlatformCompanySettings");

            migrationBuilder.DropColumn(
                name: "PostalCity",
                table: "PlatformCompanySettings");

            migrationBuilder.DropColumn(
                name: "PostalPostalCode",
                table: "PlatformCompanySettings");

            migrationBuilder.DropColumn(
                name: "PostalStreet",
                table: "PlatformCompanySettings");

            migrationBuilder.DropColumn(
                name: "PrivacyEmail",
                table: "PlatformCompanySettings");

            migrationBuilder.DropColumn(
                name: "SupportEmail",
                table: "PlatformCompanySettings");

            migrationBuilder.DropColumn(
                name: "TradeName",
                table: "PlatformCompanySettings");
        }
    }
}
