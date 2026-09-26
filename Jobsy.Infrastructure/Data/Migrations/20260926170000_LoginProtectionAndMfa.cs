using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class LoginProtectionAndMfa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "LocalAuthCredentials",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutUntil",
                table: "LocalAuthCredentials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AuthenticatorEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AuthenticatorEnrolledAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthenticatorSecret",
                table: "Users",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryCodesHash",
                table: "Users",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MfaVerifiedUntilUtc",
                table: "UserDeviceSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalAuthCredentials_LockoutUntil",
                table: "LocalAuthCredentials",
                column: "LockoutUntil");

            // Existing installations did not expose the feature as a security setting.
            migrationBuilder.Sql("""UPDATE "PlatformFeatureSettings" SET "AuthenticatorEnabled" = TRUE;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LocalAuthCredentials_LockoutUntil",
                table: "LocalAuthCredentials");

            migrationBuilder.DropColumn(name: "FailedLoginCount", table: "LocalAuthCredentials");
            migrationBuilder.DropColumn(name: "LockoutUntil", table: "LocalAuthCredentials");
            migrationBuilder.DropColumn(name: "AuthenticatorEnabled", table: "Users");
            migrationBuilder.DropColumn(name: "AuthenticatorEnrolledAtUtc", table: "Users");
            migrationBuilder.DropColumn(name: "AuthenticatorSecret", table: "Users");
            migrationBuilder.DropColumn(name: "RecoveryCodesHash", table: "Users");
            migrationBuilder.DropColumn(name: "MfaVerifiedUntilUtc", table: "UserDeviceSessions");
        }
    }
}
