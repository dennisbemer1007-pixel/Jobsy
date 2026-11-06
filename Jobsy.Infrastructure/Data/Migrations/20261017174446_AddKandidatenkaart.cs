using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKandidatenkaart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PositionsFilled",
                table: "Vacancies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PositionsTarget",
                table: "Vacancies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "KandidatenkaartEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "KandidatenkaartActiveUntilUtc",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CandidateKandidatenkaartSettings",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitePreference = table.Column<int>(type: "integer", nullable: false),
                    ReAvailabilityPromptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InactivityCheckSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateKandidatenkaartSettings", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_CandidateKandidatenkaartSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KandidatenkaartInviteCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VacancyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AreaLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MinMatchPercent = table.Column<int>(type: "integer", nullable: false),
                    MaxTravelKm = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentRound = table.Column<int>(type: "integer", nullable: false),
                    NextRoundAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KandidatenkaartInviteCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KandidatenkaartInviteCampaigns_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KandidatenkaartInviteCampaigns_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KandidatenkaartInviteDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchPercent = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReminderSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RespondedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KandidatenkaartInviteDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KandidatenkaartInviteDeliveries_KandidatenkaartInviteCampai~",
                        column: x => x.CampaignId,
                        principalTable: "KandidatenkaartInviteCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KandidatenkaartInviteDeliveries_Users_CandidateUserId",
                        column: x => x.CandidateUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KandidatenkaartInviteCampaigns_CompanyId",
                table: "KandidatenkaartInviteCampaigns",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_KandidatenkaartInviteCampaigns_VacancyId_CreatedAtUtc",
                table: "KandidatenkaartInviteCampaigns",
                columns: new[] { "VacancyId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_KandidatenkaartInviteDeliveries_CampaignId",
                table: "KandidatenkaartInviteDeliveries",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_KandidatenkaartInviteDeliveries_CandidateUserId_SentAtUtc",
                table: "KandidatenkaartInviteDeliveries",
                columns: new[] { "CandidateUserId", "SentAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateKandidatenkaartSettings");

            migrationBuilder.DropTable(
                name: "KandidatenkaartInviteDeliveries");

            migrationBuilder.DropTable(
                name: "KandidatenkaartInviteCampaigns");

            migrationBuilder.DropColumn(
                name: "PositionsFilled",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "PositionsTarget",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "KandidatenkaartEnabled",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "KandidatenkaartActiveUntilUtc",
                table: "Companies");
        }
    }
}
