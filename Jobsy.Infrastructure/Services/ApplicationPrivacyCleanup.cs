using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

/// <summary>AVG helpers for application-scoped blobs that <see cref="Core.Rules.ApplicationRules.ScrubPersonalDataOnWithdraw"/> cannot delete.</summary>
public static class ApplicationPrivacyCleanup
{
    public static async Task RemoveUploadedCvSnapshotsAsync(
        JobsyDbContext db,
        IEnumerable<Guid> applicationIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        var ids = applicationIds as ICollection<Guid> ?? applicationIds.ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var rows = await db.ApplicationUploadedCvs
            .Where(c => ids.Contains(c.ApplicationId))
            .ToListAsync(cancellationToken);
        if (rows.Count > 0)
        {
            db.ApplicationUploadedCvs.RemoveRange(rows);
        }
    }
}
