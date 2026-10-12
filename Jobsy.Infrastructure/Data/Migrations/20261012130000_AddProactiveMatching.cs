using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20261012130000_AddProactiveMatching")]
    public partial class AddProactiveMatching : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ProactiveMatchingEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TokenChargeOnAccept",
                table: "TalentContactRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "VacancyId",
                table: "TalentContactRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CandidateProactiveMatchingSettings",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    PausedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsentAcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsentVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LastProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateProactiveMatchingSettings", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_CandidateProactiveMatchingSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProactiveMatchSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VacancyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalVacancyId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchPercent = table.Column<int>(type: "integer", nullable: false),
                    Rationale = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CompanyLabel = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Feedback = table.Column<int>(type: "integer", nullable: true),
                    FeedbackAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProactiveMatchSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProactiveMatchSuggestions_CandidateExternalVacancies_External~",
                        column: x => x.ExternalVacancyId,
                        principalTable: "CandidateExternalVacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProactiveMatchSuggestions_Users_CandidateUserId",
                        column: x => x.CandidateUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProactiveMatchSuggestions_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProactiveMatchSuggestions_CandidateUserId_DeliveredAtUtc",
                table: "ProactiveMatchSuggestions",
                columns: new[] { "CandidateUserId", "DeliveredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProactiveMatchSuggestions_VacancyId",
                table: "ProactiveMatchSuggestions",
                column: "VacancyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProactiveMatchSuggestions_ExternalVacancyId",
                table: "ProactiveMatchSuggestions",
                column: "ExternalVacancyId");

            migrationBuilder.CreateIndex(
                name: "IX_TalentContactRequests_VacancyId",
                table: "TalentContactRequests",
                column: "VacancyId");

            migrationBuilder.AddForeignKey(
                name: "FK_TalentContactRequests_Vacancies_VacancyId",
                table: "TalentContactRequests",
                column: "VacancyId",
                principalTable: "Vacancies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TalentContactRequests_Vacancies_VacancyId",
                table: "TalentContactRequests");

            migrationBuilder.DropTable(name: "ProactiveMatchSuggestions");
            migrationBuilder.DropTable(name: "CandidateProactiveMatchingSettings");

            migrationBuilder.DropIndex(
                name: "IX_TalentContactRequests_VacancyId",
                table: "TalentContactRequests");

            migrationBuilder.DropColumn(name: "VacancyId", table: "TalentContactRequests");
            migrationBuilder.DropColumn(name: "TokenChargeOnAccept", table: "TalentContactRequests");
            migrationBuilder.DropColumn(name: "ProactiveMatchingEnabled", table: "PlatformFeatureSettings");
        }
    }
}
