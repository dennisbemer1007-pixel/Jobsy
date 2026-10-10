using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGolf2Westland : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CandidateConversationSheets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StrengthsText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MotivationText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CustomText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateConversationSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateConversationSheets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CandidateOutsideWorkExperiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityTitle = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    HoursPerWeek = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateOutsideWorkExperiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateOutsideWorkExperiences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PassportFourTestsFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    HelpfulnessRating = table.Column<int>(type: "integer", nullable: false),
                    OpenAnswer = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ShareWithPilot = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassportFourTestsFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PassportFourTestsFeedbacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WestlandOccupations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscoId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TitleNl = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WestlandOccupations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WestlandPilotCohorts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    OpensAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosesAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WestlandPilotCohorts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WestlandOccupationTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccupationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TitleNl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Gecontroleerd = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WestlandOccupationTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WestlandOccupationTasks_WestlandOccupations_OccupationId",
                        column: x => x.OccupationId,
                        principalTable: "WestlandOccupations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WestlandPilotEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrolledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WestlandPilotEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WestlandPilotEnrollments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WestlandPilotEnrollments_WestlandPilotCohorts_CohortId",
                        column: x => x.CohortId,
                        principalTable: "WestlandPilotCohorts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CandidateWestlandTaskChoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChosenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateWestlandTaskChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateWestlandTaskChoices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CandidateWestlandTaskChoices_WestlandOccupationTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "WestlandOccupationTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateConversationSheets_UserId",
                table: "CandidateConversationSheets",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateOutsideWorkExperiences_UserId_SortOrder",
                table: "CandidateOutsideWorkExperiences",
                columns: new[] { "UserId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateWestlandTaskChoices_TaskId",
                table: "CandidateWestlandTaskChoices",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateWestlandTaskChoices_UserId_TaskId",
                table: "CandidateWestlandTaskChoices",
                columns: new[] { "UserId", "TaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PassportFourTestsFeedbacks_UserId",
                table: "PassportFourTestsFeedbacks",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WestlandOccupations_EscoId",
                table: "WestlandOccupations",
                column: "EscoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WestlandOccupationTasks_OccupationId_SortOrder",
                table: "WestlandOccupationTasks",
                columns: new[] { "OccupationId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WestlandPilotCohorts_Key",
                table: "WestlandPilotCohorts",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WestlandPilotEnrollments_CohortId_UserId",
                table: "WestlandPilotEnrollments",
                columns: new[] { "CohortId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WestlandPilotEnrollments_UserId",
                table: "WestlandPilotEnrollments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateConversationSheets");

            migrationBuilder.DropTable(
                name: "CandidateOutsideWorkExperiences");

            migrationBuilder.DropTable(
                name: "CandidateWestlandTaskChoices");

            migrationBuilder.DropTable(
                name: "PassportFourTestsFeedbacks");

            migrationBuilder.DropTable(
                name: "WestlandPilotEnrollments");

            migrationBuilder.DropTable(
                name: "WestlandOccupationTasks");

            migrationBuilder.DropTable(
                name: "WestlandPilotCohorts");

            migrationBuilder.DropTable(
                name: "WestlandOccupations");
        }
    }
}
