using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint3ApplicationUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Applications_VacancyId",
                table: "Applications");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_VacancyId_CandidateEmail",
                table: "Applications",
                columns: new[] { "VacancyId", "CandidateEmail" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_VacancyId_CandidateUserId",
                table: "Applications",
                columns: new[] { "VacancyId", "CandidateUserId" },
                unique: true,
                filter: "\"CandidateUserId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Applications_VacancyId_CandidateEmail",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_VacancyId_CandidateUserId",
                table: "Applications");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_VacancyId",
                table: "Applications",
                column: "VacancyId");
        }
    }
}
