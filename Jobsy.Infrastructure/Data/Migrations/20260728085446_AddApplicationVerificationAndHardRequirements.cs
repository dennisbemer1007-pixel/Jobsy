using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationVerificationAndHardRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FulfilledByApplicationId",
                table: "Vacancies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumEmployers",
                table: "Vacancies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredDrivingLicense",
                table: "Vacancies",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredEducation",
                table: "Vacancies",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CandidateEmployerCount",
                table: "Applications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationCode",
                table: "Applications",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationExpiresAt",
                table: "Applications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifiedAt",
                table: "Applications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotAboutMe",
                table: "Applications",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotAvailabilityJson",
                table: "Applications",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotDrivingLicenses",
                table: "Applications",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotEducations",
                table: "Applications",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WorkPermitConfirmed",
                table: "Applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FulfilledByApplicationId",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "MinimumEmployers",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "RequiredDrivingLicense",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "RequiredEducation",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "CandidateEmployerCount",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "EmailVerificationCode",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "EmailVerificationExpiresAt",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAt",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "SnapshotAboutMe",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "SnapshotAvailabilityJson",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "SnapshotDrivingLicenses",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "SnapshotEducations",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "WorkPermitConfirmed",
                table: "Applications");
        }
    }
}
