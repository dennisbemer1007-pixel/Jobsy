using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260730080000_MatchingScheduleHoursLegalAndIndexes")]
    public partial class MatchingScheduleHoursLegalAndIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinHoursPerWeek",
                table: "Vacancies",
                type: "numeric(5,1)",
                precision: 5,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxHoursPerWeek",
                table: "Vacancies",
                type: "numeric(5,1)",
                precision: 5,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleJson",
                table: "Vacancies",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FlexibleTimes",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FlexibleScheduleSource",
                table: "Vacancies",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LegalWorksAfter19",
                table: "Vacancies",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LegalNightShift23To06",
                table: "Vacancies",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LegalAdultSupervisorPresent",
                table: "Vacancies",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LegalHandlesMoneyOrClosing",
                table: "Vacancies",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LegalHeavyOrHazardousWork",
                table: "Vacancies",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Motivation",
                table: "Applications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ViaSafetyNet",
                table: "Applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MatchPercent",
                table: "Applications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchBreakdownJson",
                table: "Applications",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_OpenForWork_IsActive_Role",
                table: "Users",
                columns: new[] { "OpenForWork", "IsActive", "Role" },
                filter: "\"OpenForWork\" = TRUE AND \"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_HoursPerWeek",
                table: "Vacancies",
                columns: new[] { "MinHoursPerWeek", "MaxHoursPerWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_MatchPercent",
                table: "Applications",
                column: "MatchPercent");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ViaSafetyNet",
                table: "Applications",
                column: "ViaSafetyNet",
                filter: "\"ViaSafetyNet\" = TRUE");

            // Geometry-preserving DWithin helper expression index (avoids ::geography cast defeating GIST).
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Users_HomeLocation_geom_gist"
                ON "Users" USING GIST ("HomeLocation")
                WHERE "HomeLocation" IS NOT NULL AND "OpenForWork" = TRUE AND "IsActive" = TRUE;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Users_HomeLocation_geom_gist";""");

            migrationBuilder.DropIndex(
                name: "IX_Applications_ViaSafetyNet",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_MatchPercent",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Vacancies_HoursPerWeek",
                table: "Vacancies");

            migrationBuilder.DropIndex(
                name: "IX_Users_OpenForWork_IsActive_Role",
                table: "Users");

            migrationBuilder.DropColumn(name: "MatchBreakdownJson", table: "Applications");
            migrationBuilder.DropColumn(name: "MatchPercent", table: "Applications");
            migrationBuilder.DropColumn(name: "ViaSafetyNet", table: "Applications");
            migrationBuilder.DropColumn(name: "Motivation", table: "Applications");
            migrationBuilder.DropColumn(name: "LegalHeavyOrHazardousWork", table: "Vacancies");
            migrationBuilder.DropColumn(name: "LegalHandlesMoneyOrClosing", table: "Vacancies");
            migrationBuilder.DropColumn(name: "LegalAdultSupervisorPresent", table: "Vacancies");
            migrationBuilder.DropColumn(name: "LegalNightShift23To06", table: "Vacancies");
            migrationBuilder.DropColumn(name: "LegalWorksAfter19", table: "Vacancies");
            migrationBuilder.DropColumn(name: "FlexibleScheduleSource", table: "Vacancies");
            migrationBuilder.DropColumn(name: "FlexibleTimes", table: "Vacancies");
            migrationBuilder.DropColumn(name: "ScheduleJson", table: "Vacancies");
            migrationBuilder.DropColumn(name: "MaxHoursPerWeek", table: "Vacancies");
            migrationBuilder.DropColumn(name: "MinHoursPerWeek", table: "Vacancies");
        }
    }
}
