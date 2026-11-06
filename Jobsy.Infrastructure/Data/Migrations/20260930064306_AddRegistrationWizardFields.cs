using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationWizardFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LocationUnknown",
                table: "CompanyRegistrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLoginProvider",
                table: "CompanyRegistrations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RepresentationConsentAtUtc",
                table: "CompanyRegistrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentationConsentVersion",
                table: "CompanyRegistrations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalesManagerUserId",
                table: "CompanyRegistrations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedEstablishmentIdsJson",
                table: "CompanyRegistrations",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationSource",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRegistrations_SalesManagerUserId",
                table: "CompanyRegistrations",
                column: "SalesManagerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanyRegistrations_SalesManagerUserId",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "LocationUnknown",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "PreferredLoginProvider",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "RepresentationConsentAtUtc",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "RepresentationConsentVersion",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "SalesManagerUserId",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "SelectedEstablishmentIdsJson",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "LocationSource",
                table: "Companies");
        }
    }
}
