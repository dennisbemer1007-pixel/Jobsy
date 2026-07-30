using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260729200000_EmployerContactPreferences")]
    public partial class EmployerContactPreferences : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DirectContactEnabled",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ContactPreferMail",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ContactPreferPhone",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ContactPreferWhatsApp",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Companies",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Companies",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactWhatsApp",
                table: "Companies",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OverrideContactPreference",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DirectContactEnabled",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ContactPreferMail",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ContactPreferPhone",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ContactPreferWhatsApp",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DirectContactEnabled", table: "Companies");
            migrationBuilder.DropColumn(name: "ContactPreferMail", table: "Companies");
            migrationBuilder.DropColumn(name: "ContactPreferPhone", table: "Companies");
            migrationBuilder.DropColumn(name: "ContactPreferWhatsApp", table: "Companies");
            migrationBuilder.DropColumn(name: "ContactEmail", table: "Companies");
            migrationBuilder.DropColumn(name: "ContactPhone", table: "Companies");
            migrationBuilder.DropColumn(name: "ContactWhatsApp", table: "Companies");

            migrationBuilder.DropColumn(name: "OverrideContactPreference", table: "Vacancies");
            migrationBuilder.DropColumn(name: "DirectContactEnabled", table: "Vacancies");
            migrationBuilder.DropColumn(name: "ContactPreferMail", table: "Vacancies");
            migrationBuilder.DropColumn(name: "ContactPreferPhone", table: "Vacancies");
            migrationBuilder.DropColumn(name: "ContactPreferWhatsApp", table: "Vacancies");
        }
    }
}
