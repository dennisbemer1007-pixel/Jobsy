using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateExternalVacancies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CandidateExternalVacanciesEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FirstEmployerAcceptanceFreeEnabled",
                table: "FlexCommercialSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CandidateExternalVacancies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    SourceHost = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Place = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    HoursText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PayText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StartText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TrainingText = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    RequirementsBulletsJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    StructuredFactsJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    MatchInsightsJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    TravelMinutesEstimate = table.Column<int>(type: "integer", nullable: true),
                    SavedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LinkedVacancyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateExternalVacancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateExternalVacancies_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CandidateExternalVacancies_Users_CandidateUserId",
                        column: x => x.CandidateUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CandidateExternalVacancies_Vacancies_LinkedVacancyId",
                        column: x => x.LinkedVacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OutboundRecipientSuppressions",
                columns: table => new
                {
                    NormalizedEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    NormalizedDomain = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundRecipientSuppressions", x => x.NormalizedEmail);
                });

            migrationBuilder.CreateTable(
                name: "CandidateExternalVacancyOutbounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalVacancyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerEmailNormalized = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Motivation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SharedFactsJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    InitialSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReminderSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OpenedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClickedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmployerAccountCreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OneTimeLinkId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateExternalVacancyOutbounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateExternalVacancyOutbounds_CandidateExternalVacancie~",
                        column: x => x.ExternalVacancyId,
                        principalTable: "CandidateExternalVacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CandidateExternalVacancyOutbounds_OneTimeLinks_OneTimeLinkId",
                        column: x => x.OneTimeLinkId,
                        principalTable: "OneTimeLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExternalVacancies_ApplicationId",
                table: "CandidateExternalVacancies",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExternalVacancies_CandidateUserId_SavedAtUtc",
                table: "CandidateExternalVacancies",
                columns: new[] { "CandidateUserId", "SavedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExternalVacancies_LinkedVacancyId",
                table: "CandidateExternalVacancies",
                column: "LinkedVacancyId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExternalVacancyOutbounds_ExternalVacancyId_Employe~",
                table: "CandidateExternalVacancyOutbounds",
                columns: new[] { "ExternalVacancyId", "EmployerEmailNormalized" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExternalVacancyOutbounds_OneTimeLinkId",
                table: "CandidateExternalVacancyOutbounds",
                column: "OneTimeLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundRecipientSuppressions_NormalizedDomain",
                table: "OutboundRecipientSuppressions",
                column: "NormalizedDomain");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateExternalVacancyOutbounds");

            migrationBuilder.DropTable(
                name: "OutboundRecipientSuppressions");

            migrationBuilder.DropTable(
                name: "CandidateExternalVacancies");

            migrationBuilder.DropColumn(
                name: "CandidateExternalVacanciesEnabled",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "FirstEmployerAcceptanceFreeEnabled",
                table: "FlexCommercialSettings");
        }
    }
}
