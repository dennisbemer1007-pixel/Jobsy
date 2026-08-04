using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260804070000_RegistrationEmailVerificationCode")]
    public partial class RegistrationEmailVerificationCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationCode",
                table: "CompanyRegistrations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationExpiresAt",
                table: "CompanyRegistrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailVerificationFailedAttempts",
                table: "CompanyRegistrations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRegistrations_EmailVerificationExpiresAt",
                table: "CompanyRegistrations",
                column: "EmailVerificationExpiresAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanyRegistrations_EmailVerificationExpiresAt",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "EmailVerificationCode",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "EmailVerificationExpiresAt",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "EmailVerificationFailedAttempts",
                table: "CompanyRegistrations");
        }
    }
}
