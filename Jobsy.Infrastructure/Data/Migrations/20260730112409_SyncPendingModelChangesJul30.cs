using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Snapshot sync only. Schema changes already live in
    /// MatchingScheduleHoursLegalAndIndexes + VatDeclarationWizard.
    /// </summary>
    public partial class SyncPendingModelChangesJul30 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: keeps __EFMigrationsHistory in sync with the regenerated model snapshot.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
