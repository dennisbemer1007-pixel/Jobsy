using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentResearchCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResearchAgeBand",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResearchConsentAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResearchConsentVersion",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResearchEducationLevel",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResearchGender",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResearchNativeLanguage",
                table: "Users",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResearchTestLanguage",
                table: "Users",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ResearchConsentEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AssessmentResearchAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PilotSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RespondentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TestKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TestVersion = table.Column<int>(type: "integer", nullable: false),
                    TestLanguage = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AgeBand = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Gender = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    EducationLevel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    NativeLanguage = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalDurationMs = table.Column<int>(type: "integer", nullable: true),
                    PilotEndComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsPilot = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentResearchAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResearchPilotCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchPilotCampaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentResearchItemResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    ResponseRaw = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    ItemUnclear = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentResearchItemResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentResearchItemResponses_AssessmentResearchAttempts_~",
                        column: x => x.AttemptId,
                        principalTable: "AssessmentResearchAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResearchPilotSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    RespondentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AgeBand = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Gender = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    EducationLevel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    NativeLanguage = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    TestLanguage = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchPilotSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchPilotSessions_ResearchPilotCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "ResearchPilotCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResearchAttempts_RespondentHash",
                table: "AssessmentResearchAttempts",
                column: "RespondentHash");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResearchAttempts_UserId_TestKey_CompletedAtUtc",
                table: "AssessmentResearchAttempts",
                columns: new[] { "UserId", "TestKey", "CompletedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResearchItemResponses_AttemptId_ItemId",
                table: "AssessmentResearchItemResponses",
                columns: new[] { "AttemptId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchPilotCampaigns_Code",
                table: "ResearchPilotCampaigns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchPilotSessions_CampaignId",
                table: "ResearchPilotSessions",
                column: "CampaignId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentResearchItemResponses");

            migrationBuilder.DropTable(
                name: "ResearchPilotSessions");

            migrationBuilder.DropTable(
                name: "AssessmentResearchAttempts");

            migrationBuilder.DropTable(
                name: "ResearchPilotCampaigns");

            migrationBuilder.DropColumn(
                name: "ResearchAgeBand",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResearchConsentAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResearchConsentVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResearchEducationLevel",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResearchGender",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResearchNativeLanguage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResearchTestLanguage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResearchConsentEnabled",
                table: "PlatformFeatureSettings");
        }
    }
}
