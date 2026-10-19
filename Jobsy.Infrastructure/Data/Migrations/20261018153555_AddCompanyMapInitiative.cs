using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyMapInitiative : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CompanyMapInitiativeEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HubAboutText",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HubCultureText",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HubVideoUrl",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InitiativeAvgResponseDays",
                table: "Companies",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyInitiativeSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RootCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ContactEmailConfirmationCodeHash = table.Column<string>(type: "text", nullable: true),
                    ContactEmailConfirmationExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContactEmailConfirmationFailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    ContactEmailConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxPerWeek = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyInitiativeSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyInitiativeSettings_Companies_RootCompanyId",
                        column: x => x.RootCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InitiativeTeams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RootCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ContactEmailConfirmationCodeHash = table.Column<string>(type: "text", nullable: true),
                    ContactEmailConfirmationExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContactEmailConfirmationFailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    ContactEmailConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxPerWeek = table.Column<int>(type: "integer", nullable: false),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitiativeTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InitiativeTeams_Companies_BranchCompanyId",
                        column: x => x.BranchCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InitiativeTeams_Companies_RootCompanyId",
                        column: x => x.RootCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkInitiativeDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RootCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiativeTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    Motivation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkInitiativeDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkInitiativeDrafts_Companies_RootCompanyId",
                        column: x => x.RootCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkInitiativeDrafts_Users_CandidateUserId",
                        column: x => x.CandidateUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkInitiatives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RootCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiativeTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MatchLabel = table.Column<int>(type: "integer", nullable: false),
                    Motivation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SnapshotAttachedDocument = table.Column<string>(type: "text", nullable: true),
                    SnapshotRoleTitle = table.Column<string>(type: "text", nullable: false),
                    SnapshotRegionLabel = table.Column<string>(type: "text", nullable: false),
                    SnapshotWorkDnaSummary = table.Column<string>(type: "text", nullable: false),
                    SnapshotAvailabilitySummary = table.Column<string>(type: "text", nullable: false),
                    SnapshotHoursPerWeek = table.Column<int>(type: "integer", nullable: true),
                    CandidateDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CandidateEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    SnapshotAboutMe = table.Column<string>(type: "text", nullable: true),
                    SnapshotExperienceSummary = table.Column<string>(type: "text", nullable: true),
                    CountsTowardWeeklyLimit = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmployerViewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RespondedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResponseDeadlineUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkInitiatives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkInitiatives_Companies_BranchCompanyId",
                        column: x => x.BranchCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkInitiatives_Companies_RootCompanyId",
                        column: x => x.RootCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkInitiatives_InitiativeTeams_InitiativeTeamId",
                        column: x => x.InitiativeTeamId,
                        principalTable: "InitiativeTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkInitiatives_Users_CandidateUserId",
                        column: x => x.CandidateUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyInitiativeSettings_RootCompanyId",
                table: "CompanyInitiativeSettings",
                column: "RootCompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeTeams_BranchCompanyId",
                table: "InitiativeTeams",
                column: "BranchCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeTeams_RootCompanyId_Name",
                table: "InitiativeTeams",
                columns: new[] { "RootCompanyId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkInitiativeDrafts_CandidateUserId_RootCompanyId_Initiati~",
                table: "WorkInitiativeDrafts",
                columns: new[] { "CandidateUserId", "RootCompanyId", "InitiativeTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkInitiativeDrafts_RootCompanyId",
                table: "WorkInitiativeDrafts",
                column: "RootCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkInitiatives_BranchCompanyId",
                table: "WorkInitiatives",
                column: "BranchCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkInitiatives_CandidateUserId_CreatedAtUtc",
                table: "WorkInitiatives",
                columns: new[] { "CandidateUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkInitiatives_InitiativeTeamId",
                table: "WorkInitiatives",
                column: "InitiativeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkInitiatives_RootCompanyId_Status_CreatedAtUtc",
                table: "WorkInitiatives",
                columns: new[] { "RootCompanyId", "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyInitiativeSettings");

            migrationBuilder.DropTable(
                name: "WorkInitiativeDrafts");

            migrationBuilder.DropTable(
                name: "WorkInitiatives");

            migrationBuilder.DropTable(
                name: "InitiativeTeams");

            migrationBuilder.DropColumn(
                name: "CompanyMapInitiativeEnabled",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "HubAboutText",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "HubCultureText",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "HubVideoUrl",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "InitiativeAvgResponseDays",
                table: "Companies");
        }
    }
}
