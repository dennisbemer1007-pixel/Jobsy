using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJobSearchActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ActivityLogEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ActivityAgreements",
                columns: table => new
                {
                    CandidateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivitiesPerPeriod = table.Column<int>(type: "integer", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityAgreements", x => x.CandidateUserId);
                    table.ForeignKey(
                        name: "FK_ActivityAgreements_Users_CandidateUserId",
                        column: x => x.CandidateUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobSearchActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Employer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FunctionTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactMethod = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Result = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    SourceEntityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SourceEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    AttachmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSearchActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobSearchActivities_Users_CandidateUserId",
                        column: x => x.CandidateUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobSearchActivities_CandidateUserId_ActivityDate",
                table: "JobSearchActivities",
                columns: new[] { "CandidateUserId", "ActivityDate" });

            migrationBuilder.CreateIndex(
                name: "IX_JobSearchActivities_CandidateUserId_Source_SourceEntityType~",
                table: "JobSearchActivities",
                columns: new[] { "CandidateUserId", "Source", "SourceEntityType", "SourceEntityId" },
                unique: true,
                filter: "\"Source\" = 0 AND \"SourceEntityType\" IS NOT NULL AND \"SourceEntityId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityAgreements");

            migrationBuilder.DropTable(
                name: "JobSearchActivities");

            migrationBuilder.DropColumn(
                name: "ActivityLogEnabled",
                table: "PlatformFeatureSettings");
        }
    }
}
