using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Rewrites existing seeded vacancy texts that still say "Solliciteer via Jobsy … (Mock vacaturetekst #N.)".
/// Runs after migrate, including when Seed:Enabled is false.
/// </summary>
internal static class MockVacancyCopyBackfill
{
    public static async Task BackfillAsync(JobsyDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        var rows = await db.Vacancies
            .Where(v => v.Description.Contains("Solliciteer via Jobsy"))
            .ToListAsync(cancellationToken);

        var changed = 0;
        foreach (var row in rows)
        {
            var next = MockVacancyCopy.Rewrite(row.Description);
            if (string.Equals(next, row.Description, StringComparison.Ordinal))
            {
                continue;
            }

            row.Description = next;
            changed++;
        }

        if (changed == 0)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Rewrote demo vacancy copy on {Count} existing rows.", changed);
    }
}
