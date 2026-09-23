using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260923210000_DropDiscAddCulturePersonality")]
    public class DropDiscAddCulturePersonality : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CandidateDiscProfiles");

            migrationBuilder.CreateTable(
                name: "CandidateCulturePersonalityProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AnswersJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AutonomyPercent = table.Column<int>(type: "integer", nullable: true),
                    InformalPercent = table.Column<int>(type: "integer", nullable: true),
                    CollaborationPercent = table.Column<int>(type: "integer", nullable: true),
                    FlexibilityPercent = table.Column<int>(type: "integer", nullable: true),
                    InnovationPercent = table.Column<int>(type: "integer", nullable: true),
                    PeopleFirstPercent = table.Column<int>(type: "integer", nullable: true),
                    OpennessPercent = table.Column<int>(type: "integer", nullable: true),
                    ConscientiousnessPercent = table.Column<int>(type: "integer", nullable: true),
                    ExtraversionPercent = table.Column<int>(type: "integer", nullable: true),
                    AgreeablenessPercent = table.Column<int>(type: "integer", nullable: true),
                    EmotionalStabilityPercent = table.Column<int>(type: "integer", nullable: true),
                    MatchTagsJson = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateCulturePersonalityProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateCulturePersonalityProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCulturePersonalityProfiles_UserId",
                table: "CandidateCulturePersonalityProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateTable(
                name: "CompanyCultureProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AnswersJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AutonomyPercent = table.Column<int>(type: "integer", nullable: true),
                    InformalPercent = table.Column<int>(type: "integer", nullable: true),
                    CollaborationPercent = table.Column<int>(type: "integer", nullable: true),
                    FlexibilityPercent = table.Column<int>(type: "integer", nullable: true),
                    InnovationPercent = table.Column<int>(type: "integer", nullable: true),
                    PeopleFirstPercent = table.Column<int>(type: "integer", nullable: true),
                    OpennessPercent = table.Column<int>(type: "integer", nullable: true),
                    ConscientiousnessPercent = table.Column<int>(type: "integer", nullable: true),
                    ExtraversionPercent = table.Column<int>(type: "integer", nullable: true),
                    AgreeablenessPercent = table.Column<int>(type: "integer", nullable: true),
                    EmotionalStabilityPercent = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyCultureProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyCultureProfiles_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCultureProfiles_CompanyId",
                table: "CompanyCultureProfiles",
                column: "CompanyId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CandidateCulturePersonalityProfiles");
            migrationBuilder.DropTable(name: "CompanyCultureProfiles");

            migrationBuilder.CreateTable(
                name: "CandidateDiscProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AnswersJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    DominantPercent = table.Column<int>(type: "integer", nullable: true),
                    InvloedPercent = table.Column<int>(type: "integer", nullable: true),
                    StabielPercent = table.Column<int>(type: "integer", nullable: true),
                    NauwkeurigPercent = table.Column<int>(type: "integer", nullable: true),
                    MatchTagsJson = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateDiscProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateDiscProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateDiscProfiles_UserId",
                table: "CandidateDiscProfiles",
                column: "UserId",
                unique: true);
        }
    }
}
