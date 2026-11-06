using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVacancyPublishOnVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PublishOnVerification",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadyMarkedAtUtc",
                table: "Vacancies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReadyMarkedByUserId",
                table: "Vacancies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentDay21AtUtc",
                table: "CompanyRegistrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentDay7AtUtc",
                table: "CompanyRegistrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManualVerificationClosedAtUtc",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManualVerificationOpenedAtUtc",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishOnVerification",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "ReadyMarkedAtUtc",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "ReadyMarkedByUserId",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "ReminderSentDay21AtUtc",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "ReminderSentDay7AtUtc",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "ManualVerificationClosedAtUtc",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ManualVerificationOpenedAtUtc",
                table: "Companies");
        }
    }
}
