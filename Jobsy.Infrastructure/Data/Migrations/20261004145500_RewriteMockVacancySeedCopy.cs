using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Rewrites leftover demo sentences on vacancies that were seeded before the copy change.
    /// Idempotent: a second run matches no rows. Real text that only mentions Jobsy is left alone.
    /// The same rewrite also runs from MockVacancyCopyBackfill after migrate.
    /// </summary>
    [DbContext(typeof(global::Jobsy.Infrastructure.Data.JobsyDbContext))]
    [Migration("20261004145500_RewriteMockVacancySeedCopy")]
    public class RewriteMockVacancySeedCopy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Vacancies"
                SET "Description" = regexp_replace(
                    "Description",
                    'Solliciteer via Jobsy[^.]{0,180}\.(\s*\((Mock vacaturetekst|Haaglanden testdata|testdata)[^)]*\))?',
                    'Je kunt direct solliciteren. We reageren doorgaans binnen één werkdag.',
                    'gi')
                WHERE "Description" ILIKE '%Solliciteer via Jobsy%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Demo sentences are not restored.
        }
    }
}
