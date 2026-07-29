using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260729160000_AddPerformanceHotpathIndexes")]
    public partial class AddPerformanceHotpathIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_Status_EndDate_StartDate",
                table: "Vacancies",
                columns: new[] { "Status", "EndDate", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_CompanyId_Status",
                table: "Vacancies",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_Status",
                table: "Applications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_EmailVerifiedAt",
                table: "Applications",
                column: "EmailVerifiedAt",
                filter: "\"EmailVerifiedAt\" IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vacancies_Status_EndDate_StartDate",
                table: "Vacancies");

            migrationBuilder.DropIndex(
                name: "IX_Vacancies_CompanyId_Status",
                table: "Vacancies");

            migrationBuilder.DropIndex(
                name: "IX_Applications_Status",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_EmailVerifiedAt",
                table: "Applications");
        }
    }
}
