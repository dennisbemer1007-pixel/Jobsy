using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncNotificationsSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EngagementGoodwillExtendedAtUtc",
                table: "Vacancies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EngagementReminderSentAtUtc",
                table: "Vacancies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EngagementReminderTip",
                table: "Vacancies",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequireEmailVerification",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireEmailVerificationForApplications",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CandidateActionTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RelatedApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateActionTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateActionTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    DeepLink = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActionLabel = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ActionUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_Status_EngagementReminderSentAtUtc_PublishedAtUtc",
                table: "Vacancies",
                columns: new[] { "Status", "EngagementReminderSentAtUtc", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateActionTokens_TokenHash",
                table: "CandidateActionTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateActionTokens_UserId_Purpose_ExpiresAtUtc",
                table: "CandidateActionTokens",
                columns: new[] { "UserId", "Purpose", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_CreatedAtUtc",
                table: "UserNotifications",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_IsRead_CreatedAtUtc",
                table: "UserNotifications",
                columns: new[] { "UserId", "IsRead", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateActionTokens");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropIndex(
                name: "IX_Vacancies_Status_EngagementReminderSentAtUtc_PublishedAtUtc",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "EngagementGoodwillExtendedAtUtc",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "EngagementReminderSentAtUtc",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "EngagementReminderTip",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "RequireEmailVerification",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "RequireEmailVerificationForApplications",
                table: "Companies");
        }
    }
}
