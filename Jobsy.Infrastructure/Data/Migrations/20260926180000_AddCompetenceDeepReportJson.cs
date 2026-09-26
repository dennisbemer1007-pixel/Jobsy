using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Jobsy.Infrastructure.Data;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260926180000_AddCompetenceDeepReportJson")]
    public class AddCompetenceDeepReportJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReportJson",
                table: "CandidateDeepAnalyses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReportVersion",
                table: "CandidateDeepAnalyses",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportJson",
                table: "CandidateDeepAnalyses");

            migrationBuilder.DropColumn(
                name: "ReportVersion",
                table: "CandidateDeepAnalyses");
        }
    }
}
