using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCareerPlanArchiveAndGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CandidateCareerPlans_UserId",
                table: "CandidateCareerPlans");

            migrationBuilder.AddColumn<string>(
                name: "UndoFingerprint",
                table: "CandidateCareerStepProgress",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "CandidateCareerPlans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DreamCatalogKey",
                table: "CandidateCareerPlans",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DreamSource",
                table: "CandidateCareerPlans",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Wizard");

            migrationBuilder.AddColumn<bool>(
                name: "FromAi",
                table: "CandidateCareerPlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PlanLanguage",
                table: "CandidateCareerPlans",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "nl");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CandidateCareerPlans",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateTable(
                name: "CandidateCareerGenerations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DreamKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Outcome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "Ok")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateCareerGenerations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateCareerGenerations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCareerPlans_UserId_Active",
                table: "CandidateCareerPlans",
                column: "UserId",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCareerPlans_UserId_Status_ArchivedAtUtc",
                table: "CandidateCareerPlans",
                columns: new[] { "UserId", "Status", "ArchivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCareerGenerations_UserId_StartedAtUtc",
                table: "CandidateCareerGenerations",
                columns: new[] { "UserId", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateCareerGenerations");

            migrationBuilder.DropIndex(
                name: "IX_CandidateCareerPlans_UserId_Active",
                table: "CandidateCareerPlans");

            migrationBuilder.DropIndex(
                name: "IX_CandidateCareerPlans_UserId_Status_ArchivedAtUtc",
                table: "CandidateCareerPlans");

            migrationBuilder.DropColumn(
                name: "UndoFingerprint",
                table: "CandidateCareerStepProgress");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "CandidateCareerPlans");

            migrationBuilder.DropColumn(
                name: "DreamCatalogKey",
                table: "CandidateCareerPlans");

            migrationBuilder.DropColumn(
                name: "DreamSource",
                table: "CandidateCareerPlans");

            migrationBuilder.DropColumn(
                name: "FromAi",
                table: "CandidateCareerPlans");

            migrationBuilder.DropColumn(
                name: "PlanLanguage",
                table: "CandidateCareerPlans");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CandidateCareerPlans");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCareerPlans_UserId",
                table: "CandidateCareerPlans",
                column: "UserId",
                unique: true);
        }
    }
}
