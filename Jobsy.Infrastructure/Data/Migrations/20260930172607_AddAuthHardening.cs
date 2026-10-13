using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastMfaLockoutMailAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LastTotpTimeStep",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MfaFailedCount",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "MfaLockoutUntilUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLockoutAtUtc",
                table: "LocalAuthCredentials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLockoutMailAtUtc",
                table: "LocalAuthCredentials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LockoutCount",
                table: "LocalAuthCredentials",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastMfaLockoutMailAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastTotpTimeStep",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MfaFailedCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MfaLockoutUntilUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLockoutAtUtc",
                table: "LocalAuthCredentials");

            migrationBuilder.DropColumn(
                name: "LastLockoutMailAtUtc",
                table: "LocalAuthCredentials");

            migrationBuilder.DropColumn(
                name: "LockoutCount",
                table: "LocalAuthCredentials");
        }
    }
}
