using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixSprint0DataDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Users" SET "IsActive" = TRUE WHERE "IsActive" = FALSE;
                UPDATE "Vacancies" SET "MaxApplications" = 5 WHERE "MaxApplications" = 0;
                UPDATE "TokenTransactions"
                SET "Kind" = 2,
                    "OldBalance" = 0,
                    "NewBalance" = "Amount",
                    "Note" = COALESCE("Note", 'Backfilled grant')
                WHERE "Amount" > 0
                  AND "Kind" = 0
                  AND "OldBalance" = 0
                  AND "NewBalance" = 0;
                UPDATE "TokenTransactions"
                SET "Kind" = 1,
                    "Reason" = CASE WHEN "Reason" = 0 THEN 1 ELSE "Reason" END,
                    "OldBalance" = ABS("Amount"),
                    "NewBalance" = 0
                WHERE "Amount" < 0
                  AND "OldBalance" = 0
                  AND "NewBalance" = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
