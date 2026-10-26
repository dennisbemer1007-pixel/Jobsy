using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Ops;

/// <summary>
/// Cleans stale acceptatie activity-log copy (e.g. demo outcome rows) during seed reset.
/// </summary>
internal static class JobSearchActivityAcceptatieRepair
{
    internal static async Task PurgeStaleOutcomeCopyAsync(
        JobsyDbContext db,
        Guid candidateUserId,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.JobSearchActivities
            .Where(a => a.CandidateUserId == candidateUserId)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return;
        }

        var removed = 0;
        foreach (var row in rows)
        {
            if (ContainsOutcomeCopy(row.Result) || ContainsOutcomeCopy(row.FunctionTitle) || ContainsOutcomeCopy(row.ContactMethod))
            {
                db.JobSearchActivities.Remove(row);
                removed++;
            }
        }

        if (removed > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Acceptatie activity log repair: removed {Count} stale rows for candidate {CandidateId}.",
                removed,
                candidateUserId);
        }
    }

    private static bool ContainsOutcomeCopy(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && value.Contains("uitkomst", StringComparison.OrdinalIgnoreCase);
}
