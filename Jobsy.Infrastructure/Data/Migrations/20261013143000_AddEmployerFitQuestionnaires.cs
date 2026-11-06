using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20261013143000_AddEmployerFitQuestionnaires")]
    public partial class AddEmployerFitQuestionnaires : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmployerFitQuestionnairesEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VacancyCompetencyTargetsJson",
                table: "Vacancies",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VacancyCultureDeltaJson",
                table: "Vacancies",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VacancyUseInferredCompetencies",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "VacancyWorkContextJson",
                table: "Vacancies",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployerFitQuestionnairesEnabled",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "VacancyCompetencyTargetsJson",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "VacancyCultureDeltaJson",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "VacancyUseInferredCompetencies",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "VacancyWorkContextJson",
                table: "Vacancies");
        }
    }
}
