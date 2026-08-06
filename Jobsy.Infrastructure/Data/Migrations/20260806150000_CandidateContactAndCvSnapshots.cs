using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CandidateContactAndCvSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppContactAllowed",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "SnapshotAvailabilityJson",
                table: "Applications",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SnapshotHomeLatitude",
                table: "Applications",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SnapshotHomeLongitude",
                table: "Applications",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotPhoneNumber",
                table: "Applications",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SnapshotWhatsAppAllowed",
                table: "Applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE "Users"
                SET "FirstName" = CASE
                        WHEN position(' ' in trim("FullName")) > 0
                            THEN left(trim("FullName"), position(' ' in trim("FullName")) - 1)
                        ELSE trim("FullName")
                    END,
                    "LastName" = CASE
                        WHEN position(' ' in trim("FullName")) > 0
                            THEN trim(substring(trim("FullName") from position(' ' in trim("FullName")) + 1))
                        ELSE NULL
                    END
                WHERE coalesce(trim("FullName"), '') <> ''
                  AND "FirstName" IS NULL
                  AND "LastName" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WhatsAppContactAllowed",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SnapshotHomeLatitude",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "SnapshotHomeLongitude",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "SnapshotPhoneNumber",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "SnapshotWhatsAppAllowed",
                table: "Applications");

            migrationBuilder.AlterColumn<string>(
                name: "SnapshotAvailabilityJson",
                table: "Applications",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);
        }
    }
}
