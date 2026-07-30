using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hand-written hotpath migration added IX_Vacancies_CompanyId_Status but left the
            // snapshot with the old FK-only IX_Vacancies_CompanyId. Model now only declares the
            // composite index (left-prefix covers CompanyId lookups).
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Vacancies_CompanyId";""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_CompanyId",
                table: "Vacancies",
                column: "CompanyId");
        }
    }
}
