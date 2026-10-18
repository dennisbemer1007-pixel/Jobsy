using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReferenceVerificationEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "WorkExperienceReferenceRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkExperienceEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferentName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Relation = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ReferentEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    EmployerName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RoleTitle = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    StartMonth = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    EndMonth = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    DutiesText = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    ConfirmedEmployer = table.Column<bool>(type: "boolean", nullable: true),
                    ConfirmedPeriod = table.Column<bool>(type: "boolean", nullable: true),
                    ConfirmedRole = table.Column<bool>(type: "boolean", nullable: true),
                    ConfirmedDuties = table.Column<bool>(type: "boolean", nullable: true),
                    ReferentComment = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    UnknownPersonReported = table.Column<bool>(type: "boolean", nullable: false),
                    EndorsedCompetenceCodesJson = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CandidateConsentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsentVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RemovedByCandidate = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkExperienceReferenceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkExperienceReferenceRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkExperienceReferenceTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReminderSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkExperienceReferenceTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkExperienceReferenceTokens_WorkExperienceReferenceReques~",
                        column: x => x.RequestId,
                        principalTable: "WorkExperienceReferenceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkExperienceReferenceRequests_UserId",
                table: "WorkExperienceReferenceRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkExperienceReferenceRequests_UserId_WorkExperienceEntryId",
                table: "WorkExperienceReferenceRequests",
                columns: new[] { "UserId", "WorkExperienceEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkExperienceReferenceTokens_RequestId",
                table: "WorkExperienceReferenceTokens",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkExperienceReferenceTokens_TokenHash",
                table: "WorkExperienceReferenceTokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkExperienceReferenceTokens");

            migrationBuilder.DropTable(
                name: "WorkExperienceReferenceRequests");

            migrationBuilder.DropColumn(
                name: "ReferenceVerificationEnabled",
                table: "PlatformFeatureSettings");
        }
    }
}
