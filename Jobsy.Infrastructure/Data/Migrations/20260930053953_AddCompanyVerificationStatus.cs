using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyVerificationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Temporary defaults so every existing row is Verified/Backfill (D6).
            // CompanyVerificationStatus.Verified = 2, CompanyVerificationMethod.Backfill = 4.
            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "VerificationMethod",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAtUtc",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationUpdatedAtUtc",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Companies"
                SET "VerifiedAtUtc" = NOW() AT TIME ZONE 'utc',
                    "VerificationUpdatedAtUtc" = NOW() AT TIME ZONE 'utc'
                WHERE "VerifiedAtUtc" IS NULL;
                """);

            // Drop temporary column defaults so new rows must set status explicitly in code.
            migrationBuilder.AlterColumn<int>(
                name: "VerificationStatus",
                table: "Companies",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 2,
                defaultValue: null);

            migrationBuilder.AlterColumn<int>(
                name: "VerificationMethod",
                table: "Companies",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 4,
                defaultValue: null);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_VerificationStatus",
                table: "Companies",
                column: "VerificationStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Companies_VerificationStatus",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VerificationMethod",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VerificationUpdatedAtUtc",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VerifiedAtUtc",
                table: "Companies");
        }
    }
}
