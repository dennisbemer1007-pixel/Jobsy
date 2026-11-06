using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScholenFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SchoolPerCodeResultsEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SchoolRetentionCutoffDay",
                table: "PlatformFeatureSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SchoolRetentionCutoffMonth",
                table: "PlatformFeatureSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SchoolsEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SubjectPupilCodeId",
                table: "PersonalDataAccessLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SchoolRetentionRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RanAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CutoffDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ClassesDeleted = table.Column<int>(type: "integer", nullable: false),
                    CodesDeleted = table.Column<int>(type: "integer", nullable: false),
                    ResultsDeleted = table.Column<int>(type: "integer", nullable: false),
                    AggregatesWritten = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolRetentionRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    City = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BrinCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    AllowedEmailDomains = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessorAgreementSignedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ProcessorAgreementVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchoolClassAggregates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: true),
                    SchoolYearStart = table.Column<int>(type: "integer", nullable: false),
                    ClassLabel = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    PupilCount = table.Column<int>(type: "integer", nullable: false),
                    StartedCount = table.Column<int>(type: "integer", nullable: false),
                    CompletedCount = table.Column<int>(type: "integer", nullable: false),
                    RiasecTop3CountsJson = table.Column<string>(type: "text", nullable: false),
                    TopValueCountsJson = table.Column<string>(type: "text", nullable: false),
                    TopCultureCountsJson = table.Column<string>(type: "text", nullable: false),
                    CompetenceBandCountsJson = table.Column<string>(type: "text", nullable: false),
                    DreamJobCountsJson = table.Column<string>(type: "text", nullable: false),
                    SnapshotAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolClassAggregates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolClassAggregates_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SchoolClasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    SchoolYearStart = table.Column<int>(type: "integer", nullable: false),
                    PupilCount = table.Column<int>(type: "integer", nullable: false),
                    TestWindow = table.Column<int>(type: "integer", nullable: false),
                    TestWindowClosesOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ParentalInfoConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ParentalInfoConfirmedByInvitedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentalInfoTextVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LoginPausedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolClasses_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SchoolStaffInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ClassIdsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolStaffInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolStaffInvites_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SchoolYearAggregates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: true),
                    SchoolYearStart = table.Column<int>(type: "integer", nullable: false),
                    PupilCount = table.Column<int>(type: "integer", nullable: false),
                    StartedCount = table.Column<int>(type: "integer", nullable: false),
                    CompletedCount = table.Column<int>(type: "integer", nullable: false),
                    RiasecTop3CountsJson = table.Column<string>(type: "text", nullable: false),
                    TopValueCountsJson = table.Column<string>(type: "text", nullable: false),
                    TopCultureCountsJson = table.Column<string>(type: "text", nullable: false),
                    CompetenceBandCountsJson = table.Column<string>(type: "text", nullable: false),
                    DreamJobCountsJson = table.Column<string>(type: "text", nullable: false),
                    SnapshotAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolYearAggregates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolYearAggregates_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PupilCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    CodeLookupHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CodeProtected = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SessionVersion = table.Column<int>(type: "integer", nullable: false),
                    LockedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PupilCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PupilCodes_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeacherClassAssignments",
                columns: table => new
                {
                    TeacherUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherClassAssignments", x => new { x.TeacherUserId, x.SchoolClassId });
                    table.ForeignKey(
                        name: "FK_TeacherClassAssignments_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherClassAssignments_Users_TeacherUserId",
                        column: x => x.TeacherUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PupilProgresses",
                columns: table => new
                {
                    PupilCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnswersJson = table.Column<string>(type: "text", nullable: false),
                    CurrentIndex = table.Column<int>(type: "integer", nullable: false),
                    LikesJson = table.Column<string>(type: "text", nullable: false),
                    DislikesJson = table.Column<string>(type: "text", nullable: false),
                    LikeOtherWord = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    DislikeOtherWord = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    DreamJobKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PupilProgresses", x => x.PupilCodeId);
                    table.ForeignKey(
                        name: "FK_PupilProgresses_PupilCodes_PupilCodeId",
                        column: x => x.PupilCodeId,
                        principalTable: "PupilCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PupilResults",
                columns: table => new
                {
                    PupilCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompetenceScoresJson = table.Column<string>(type: "text", nullable: false),
                    RiasecScoresJson = table.Column<string>(type: "text", nullable: false),
                    HollandCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    ValuesScoresJson = table.Column<string>(type: "text", nullable: false),
                    TopValue = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CultureScoresJson = table.Column<string>(type: "text", nullable: false),
                    TopCulture = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ScoringVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StoryTemplateVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StoryKeysJson = table.Column<string>(type: "text", nullable: false),
                    DreamJobKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FitSnapshotJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PupilResults", x => x.PupilCodeId);
                    table.ForeignKey(
                        name: "FK_PupilResults_PupilCodes_PupilCodeId",
                        column: x => x.PupilCodeId,
                        principalTable: "PupilCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_SchoolId",
                table: "Users",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalDataAccessLogs_SubjectPupilCodeId",
                table: "PersonalDataAccessLogs",
                column: "SubjectPupilCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PupilCodes_SchoolClassId_CodeLookupHash",
                table: "PupilCodes",
                columns: new[] { "SchoolClassId", "CodeLookupHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PupilResults_SchoolClassId",
                table: "PupilResults",
                column: "SchoolClassId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolClassAggregates_SchoolId_SchoolYearStart",
                table: "SchoolClassAggregates",
                columns: new[] { "SchoolId", "SchoolYearStart" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolClasses_SchoolId_SchoolYearStart_Name",
                table: "SchoolClasses",
                columns: new[] { "SchoolId", "SchoolYearStart", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolClasses_TestWindow",
                table: "SchoolClasses",
                column: "TestWindow");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolRetentionRuns_RanAtUtc",
                table: "SchoolRetentionRuns",
                column: "RanAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Schools_IsActive",
                table: "Schools",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Schools_Name",
                table: "Schools",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolStaffInvites_SchoolId_Email",
                table: "SchoolStaffInvites",
                columns: new[] { "SchoolId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolStaffInvites_TokenHash",
                table: "SchoolStaffInvites",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolYearAggregates_SchoolId_SchoolYearStart",
                table: "SchoolYearAggregates",
                columns: new[] { "SchoolId", "SchoolYearStart" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherClassAssignments_SchoolClassId",
                table: "TeacherClassAssignments",
                column: "SchoolClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Schools_SchoolId",
                table: "Users",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Schools_SchoolId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "PupilProgresses");

            migrationBuilder.DropTable(
                name: "PupilResults");

            migrationBuilder.DropTable(
                name: "SchoolClassAggregates");

            migrationBuilder.DropTable(
                name: "SchoolRetentionRuns");

            migrationBuilder.DropTable(
                name: "SchoolStaffInvites");

            migrationBuilder.DropTable(
                name: "SchoolYearAggregates");

            migrationBuilder.DropTable(
                name: "TeacherClassAssignments");

            migrationBuilder.DropTable(
                name: "PupilCodes");

            migrationBuilder.DropTable(
                name: "SchoolClasses");

            migrationBuilder.DropTable(
                name: "Schools");

            migrationBuilder.DropIndex(
                name: "IX_Users_SchoolId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_PersonalDataAccessLogs_SubjectPupilCodeId",
                table: "PersonalDataAccessLogs");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SchoolPerCodeResultsEnabled",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "SchoolRetentionCutoffDay",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "SchoolRetentionCutoffMonth",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "SchoolsEnabled",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "SubjectPupilCodeId",
                table: "PersonalDataAccessLogs");
        }
    }
}
