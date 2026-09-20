using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class LobsyPlatformMasterSpec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AnswersJson",
                table: "CandidateCompetencies",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<string>(
                name: "MatchTagsJson",
                table: "CandidateCompetencies",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "RiasecTagsJson",
                table: "CandidateCompetencies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "AgencyAnnualSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgencyAnnualSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgencyAnnualSubscriptions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CandidateDeepAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AnswersJson = table.Column<string>(type: "text", nullable: false),
                    TagsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    UnlockedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReportGeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateDeepAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateDeepAnalyses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeepAnalysisCheckouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AmountEuro = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeepAnalysisCheckouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeepAnalysisCheckouts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlexCommercialSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarginPerHourEuro = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    BackofficePartnerName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlexCommercialSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TalentContactRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SpendTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefundTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondByUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WithdrawnAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContactSharedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentContactRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TalentContactRequests_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TalentContactRequests_TokenTransactions_RefundTransactionId",
                        column: x => x.RefundTransactionId,
                        principalTable: "TokenTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TalentContactRequests_TokenTransactions_SpendTransactionId",
                        column: x => x.SpendTransactionId,
                        principalTable: "TokenTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TalentContactRequests_Users_CandidateUserId",
                        column: x => x.CandidateUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TalentContactRequests_Users_EmployerUserId",
                        column: x => x.EmployerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgencyAnnualSubscriptions_CompanyId_IsActive_EndsAtUtc",
                table: "AgencyAnnualSubscriptions",
                columns: new[] { "CompanyId", "IsActive", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateDeepAnalyses_UserId",
                table: "CandidateDeepAnalyses",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeepAnalysisCheckouts_PaymentId",
                table: "DeepAnalysisCheckouts",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeepAnalysisCheckouts_UserId",
                table: "DeepAnalysisCheckouts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TalentContactRequests_CandidateUserId",
                table: "TalentContactRequests",
                column: "CandidateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TalentContactRequests_CompanyId_CandidateUserId_Status",
                table: "TalentContactRequests",
                columns: new[] { "CompanyId", "CandidateUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TalentContactRequests_EmployerUserId",
                table: "TalentContactRequests",
                column: "EmployerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TalentContactRequests_RefundTransactionId",
                table: "TalentContactRequests",
                column: "RefundTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TalentContactRequests_RespondByUtc",
                table: "TalentContactRequests",
                column: "RespondByUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TalentContactRequests_SpendTransactionId",
                table: "TalentContactRequests",
                column: "SpendTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgencyAnnualSubscriptions");

            migrationBuilder.DropTable(
                name: "CandidateDeepAnalyses");

            migrationBuilder.DropTable(
                name: "DeepAnalysisCheckouts");

            migrationBuilder.DropTable(
                name: "FlexCommercialSettings");

            migrationBuilder.DropTable(
                name: "TalentContactRequests");

            migrationBuilder.DropColumn(
                name: "MatchTagsJson",
                table: "CandidateCompetencies");

            migrationBuilder.DropColumn(
                name: "RiasecTagsJson",
                table: "CandidateCompetencies");

            migrationBuilder.AlterColumn<string>(
                name: "AnswersJson",
                table: "CandidateCompetencies",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);
        }
    }
}
