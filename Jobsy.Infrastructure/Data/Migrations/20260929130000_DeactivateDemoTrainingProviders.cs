using System;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Deactivates the six fixed demo training providers (no deletes — clicks/FKs stay).
    /// Admin can re-activate after a real partnership. Seeds only run in Dev/Test.
    /// </summary>
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260929130000_DeactivateDemoTrainingProviders")]
    public partial class DeactivateDemoTrainingProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "TrainingProviders"
                SET "IsActive" = false
                WHERE "Id" IN (
                    'a11a0001-0001-4000-8000-000000000001',
                    'a11a0001-0001-4000-8000-000000000002',
                    'a11a0001-0001-4000-8000-000000000003',
                    'a11a0001-0001-4000-8000-000000000004',
                    'a11a0001-0001-4000-8000-000000000005',
                    'a11a0001-0001-4000-8000-000000000006'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "TrainingProviders"
                SET "IsActive" = true
                WHERE "Id" IN (
                    'a11a0001-0001-4000-8000-000000000001',
                    'a11a0001-0001-4000-8000-000000000002',
                    'a11a0001-0001-4000-8000-000000000003',
                    'a11a0001-0001-4000-8000-000000000004',
                    'a11a0001-0001-4000-8000-000000000005',
                    'a11a0001-0001-4000-8000-000000000006'
                );
                """);
        }
    }
}
