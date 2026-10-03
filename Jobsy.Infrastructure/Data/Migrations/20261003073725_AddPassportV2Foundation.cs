using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPassportV2Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifiedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhoneVerifiedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneVerifiedE164",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Both AuthController candidate-creation paths prove the e-mail
            // (passwordless code, or an external IdP that rejects email_verified=false).
            // Stamp active non-test candidates who have already logged in.
            // Down drops the column; this one-shot stamp is not reversed.
            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "EmailVerifiedAtUtc" = COALESCE("LastLoginAtUtc", "TermsAcceptedAt")
                WHERE "Role" = 0
                  AND "IsActive" = TRUE
                  AND "IsTestAccount" = FALSE
                  AND "LastLoginAtUtc" IS NOT NULL
                  AND "EmailVerifiedAtUtc" IS NULL;
                """);

            migrationBuilder.AddColumn<bool>(
                name: "PassportPartnersEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PassportPdfV2Enabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneVerificationEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PhoneVerificationChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneE164 = table.Column<string>(type: "text", nullable: false),
                    CodeHash = table.Column<string>(type: "text", nullable: false),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneVerificationChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneVerificationChallenges_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerificationChallenges_UserId",
                table: "PhoneVerificationChallenges",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneVerificationChallenges");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneVerifiedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneVerifiedE164",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PassportPartnersEnabled",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "PassportPdfV2Enabled",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "PhoneVerificationEnabled",
                table: "PlatformFeatureSettings");
        }
    }
}
