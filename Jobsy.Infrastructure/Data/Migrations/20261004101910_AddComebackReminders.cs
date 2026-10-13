using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddComebackReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppRemindersEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CandidateReminderPreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailOptedInAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WhatsAppOptedInAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WhatsAppPhone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateReminderPreferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_CandidateReminderPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComebackReminderLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Channels = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComebackReminderLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComebackReminderLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComebackReminderLogs_UserId_SentAtUtc",
                table: "ComebackReminderLogs",
                columns: new[] { "UserId", "SentAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateReminderPreferences");

            migrationBuilder.DropTable(
                name: "ComebackReminderLogs");

            migrationBuilder.DropColumn(
                name: "WhatsAppRemindersEnabled",
                table: "PlatformFeatureSettings");
        }
    }
}
