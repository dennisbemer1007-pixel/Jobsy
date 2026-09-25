using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260925043000_AddCandidateValuesProfiles")]
    public class AddCandidateValuesProfiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CandidateValuesProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AnswersJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AutonomyPercent = table.Column<int>(type: "integer", nullable: true),
                    ConnectionPercent = table.Column<int>(type: "integer", nullable: true),
                    AchievementPercent = table.Column<int>(type: "integer", nullable: true),
                    StabilityPercent = table.Column<int>(type: "integer", nullable: true),
                    ImpactPercent = table.Column<int>(type: "integer", nullable: true),
                    MatchTagsJson = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateValuesProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateValuesProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateValuesProfiles_UserId",
                table: "CandidateValuesProfiles",
                column: "UserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CandidateValuesProfiles");
        }
    }
}
