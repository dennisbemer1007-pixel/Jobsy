using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260926090000_AddCandidateCareerPlans")]
    public class AddCandidateCareerPlans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CandidateCareerPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DreamTitle = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DreamKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PlanJson = table.Column<string>(type: "text", nullable: false),
                    MatchPercent = table.Column<int>(type: "integer", nullable: false),
                    MatchSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateCareerPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateCareerPlans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CandidateCareerStepProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepKey = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StepOrder = table.Column<int>(type: "integer", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateCareerStepProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateCareerStepProgress_CandidateCareerPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "CandidateCareerPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CandidateCareerStepProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCareerPlans_UserId",
                table: "CandidateCareerPlans",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCareerStepProgress_PlanId_StepKey",
                table: "CandidateCareerStepProgress",
                columns: new[] { "PlanId", "StepKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCareerStepProgress_UserId",
                table: "CandidateCareerStepProgress",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CandidateCareerStepProgress");
            migrationBuilder.DropTable(name: "CandidateCareerPlans");
        }
    }
}
