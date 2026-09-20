using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeparatedAssessmentArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CandidateDeepAnalyses_UserId",
                table: "CandidateDeepAnalyses");

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "DeepAnalysisCheckouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "CandidateDeepAnalyses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExtraversiePercent",
                table: "CandidateCompetencies",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CandidateCareerInterests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AnswersJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RealisticPercent = table.Column<int>(type: "integer", nullable: true),
                    InvestigativePercent = table.Column<int>(type: "integer", nullable: true),
                    ArtisticPercent = table.Column<int>(type: "integer", nullable: true),
                    SocialPercent = table.Column<int>(type: "integer", nullable: true),
                    EnterprisingPercent = table.Column<int>(type: "integer", nullable: true),
                    ConventionalPercent = table.Column<int>(type: "integer", nullable: true),
                    HollandCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    RiasecTagsJson = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MatchTagsJson = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateCareerInterests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateCareerInterests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeepAnalysisCheckouts_UserId_Kind_Status",
                table: "DeepAnalysisCheckouts",
                columns: new[] { "UserId", "Kind", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateDeepAnalyses_UserId_Kind",
                table: "CandidateDeepAnalyses",
                columns: new[] { "UserId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCareerInterests_UserId",
                table: "CandidateCareerInterests",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateCareerInterests");

            migrationBuilder.DropIndex(
                name: "IX_DeepAnalysisCheckouts_UserId_Kind_Status",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropIndex(
                name: "IX_CandidateDeepAnalyses_UserId_Kind",
                table: "CandidateDeepAnalyses");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "DeepAnalysisCheckouts");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "CandidateDeepAnalyses");

            migrationBuilder.DropColumn(
                name: "ExtraversiePercent",
                table: "CandidateCompetencies");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateDeepAnalyses_UserId",
                table: "CandidateDeepAnalyses",
                column: "UserId",
                unique: true);
        }
    }
}
