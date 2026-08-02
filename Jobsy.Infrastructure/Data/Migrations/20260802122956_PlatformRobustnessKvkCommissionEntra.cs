using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlatformRobustnessKvkCommissionEntra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KvkVerificationStatus",
                table: "CompanyRegistrations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionDirectRateSnapshot",
                table: "Companies",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommissionDurationDaysSnapshot",
                table: "Companies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionIndirectRateSnapshot",
                table: "Companies",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CommissionIndirectSalesManagerUserId",
                table: "Companies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CommissionTermsSnapshottedAtUtc",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "KvkLastVerificationAttemptAtUtc",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KvkVerificationAttempts",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "KvkVerificationStatus",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "KvkVerifiedAtUtc",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserExternalLogins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EmailAtLink = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    LinkedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExternalLogins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserExternalLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CommissionIndirectSalesManagerUserId",
                table: "Companies",
                column: "CommissionIndirectSalesManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_KvkVerificationStatus",
                table: "Companies",
                column: "KvkVerificationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_UserExternalLogins_Provider_ProviderSubject",
                table: "UserExternalLogins",
                columns: new[] { "Provider", "ProviderSubject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserExternalLogins_UserId_Provider",
                table: "UserExternalLogins",
                columns: new[] { "UserId", "Provider" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Users_CommissionIndirectSalesManagerUserId",
                table: "Companies",
                column: "CommissionIndirectSalesManagerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Users_CommissionIndirectSalesManagerUserId",
                table: "Companies");

            migrationBuilder.DropTable(
                name: "UserExternalLogins");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CommissionIndirectSalesManagerUserId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_KvkVerificationStatus",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "KvkVerificationStatus",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "CommissionDirectRateSnapshot",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CommissionDurationDaysSnapshot",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CommissionIndirectRateSnapshot",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CommissionIndirectSalesManagerUserId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CommissionTermsSnapshottedAtUtc",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "KvkLastVerificationAttemptAtUtc",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "KvkVerificationAttempts",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "KvkVerificationStatus",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "KvkVerifiedAtUtc",
                table: "Companies");
        }
    }
}
