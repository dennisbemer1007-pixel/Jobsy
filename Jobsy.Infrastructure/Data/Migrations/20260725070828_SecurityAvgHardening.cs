using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SecurityAvgHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StubPassword",
                table: "CompanyRegistrations");

            // Keep existing values; AuthController rehashes plaintext on next successful login.
            migrationBuilder.RenameColumn(
                name: "Password",
                table: "LocalAuthCredentials",
                newName: "PasswordHash");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "LocalAuthCredentials",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<string>(
                name: "ConsentVersion",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClientSecret",
                table: "IntegrationCredentials",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApiKey",
                table: "IntegrationCredentials",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentAcceptedAt",
                table: "CompanyRegistrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsentVersion",
                table: "CompanyRegistrations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentAcceptedAt",
                table: "Applications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsentVersion",
                table: "Applications",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ConsentAcceptedAt",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "ConsentVersion",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "ConsentAcceptedAt",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ConsentVersion",
                table: "Applications");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "LocalAuthCredentials",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "LocalAuthCredentials",
                newName: "Password");

            migrationBuilder.AlterColumn<string>(
                name: "ClientSecret",
                table: "IntegrationCredentials",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApiKey",
                table: "IntegrationCredentials",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StubPassword",
                table: "CompanyRegistrations",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }
    }
}
