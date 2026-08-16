using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    PageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    UserRole = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserDisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BrowserInfo = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DeviceInfo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ScreenshotBytes = table.Column<byte[]>(type: "bytea", nullable: true),
                    ScreenshotContentType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    GeneratedPrompt = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: true),
                    PromptEditedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CursorAgentId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BranchName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PullRequestUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    AutomationStatus = table.Column<int>(type: "integer", nullable: false),
                    AutomationError = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    AutomationLaunchedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AutomationFinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformFeedbacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformFeedbacks_CreatedAtUtc",
                table: "PlatformFeedbacks",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformFeedbacks_CursorAgentId",
                table: "PlatformFeedbacks",
                column: "CursorAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformFeedbacks_Status",
                table: "PlatformFeedbacks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformFeedbacks_UserId",
                table: "PlatformFeedbacks",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformFeedbacks");
        }
    }
}
