using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmployerPhase2SnapshotSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PublicMapRadiusKm",
                table: "Vacancies",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<bool>(
                name: "EmployerPhase2Enabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "AcceptCandidatePilotCostTokens",
                table: "FlexCommercialSettings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateOnly>(
                name: "AcceptCandidatePilotEndsOn",
                table: "FlexCommercialSettings",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AcceptCandidateStandardCostTokens",
                table: "FlexCommercialSettings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ApplicationPlacements",
                columns: table => new
                {
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptSpendTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptCostTokens = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    EmploymentMode = table.Column<int>(type: "integer", nullable: true),
                    EmploymentModeChosenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaqqieCreditGrantedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaqqieCreditGrantTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationPlacements", x => x.ApplicationId);
                    table.ForeignKey(
                        name: "FK_ApplicationPlacements_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationPlacements_TokenTransactions_AcceptSpendTransact~",
                        column: x => x.AcceptSpendTransactionId,
                        principalTable: "TokenTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ApplicationPlacements_TokenTransactions_MaqqieCreditGrantTr~",
                        column: x => x.MaqqieCreditGrantTransactionId,
                        principalTable: "TokenTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MaqqieHoursWeeks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekStart = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DailyHoursJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TotalHours = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmployerApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentToMaqqieAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaqqieHoursWeeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaqqieHoursWeeks_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationPlacements_AcceptSpendTransactionId",
                table: "ApplicationPlacements",
                column: "AcceptSpendTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationPlacements_MaqqieCreditGrantTransactionId",
                table: "ApplicationPlacements",
                column: "MaqqieCreditGrantTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_MaqqieHoursWeeks_ApplicationId_WeekStart",
                table: "MaqqieHoursWeeks",
                columns: new[] { "ApplicationId", "WeekStart" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationPlacements");

            migrationBuilder.DropTable(
                name: "MaqqieHoursWeeks");

            migrationBuilder.DropColumn(
                name: "PublicMapRadiusKm",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "EmployerPhase2Enabled",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "AcceptCandidatePilotCostTokens",
                table: "FlexCommercialSettings");

            migrationBuilder.DropColumn(
                name: "AcceptCandidatePilotEndsOn",
                table: "FlexCommercialSettings");

            migrationBuilder.DropColumn(
                name: "AcceptCandidateStandardCostTokens",
                table: "FlexCommercialSettings");
        }
    }
}
