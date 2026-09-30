using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Logs the count of companies backfilled to Verified/Backfill once after the
/// <c>AddCompanyVerificationStatus</c> migration (not inside the migration itself).
/// </summary>
internal static class CompanyVerificationBackfillLog
{
    public const string Category = "CompanyVerification";
    public const string Message = "Company verification backfill applied";

    public static async Task EnsureLoggedAsync(JobsyDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        var alreadyLogged = await db.PlatformLogs.AsNoTracking()
            .AnyAsync(
                l => l.Category == Category && l.Message == Message,
                cancellationToken);
        if (alreadyLogged)
        {
            return;
        }

        // Only meaningful after the verification columns exist.
        if (!await HasVerificationColumnsAsync(db, cancellationToken))
        {
            return;
        }

        var backfilled = await db.Companies.AsNoTracking()
            .CountAsync(
                c => c.VerificationMethod == CompanyVerificationMethod.Backfill
                     && c.VerificationStatus == CompanyVerificationStatus.Verified,
                cancellationToken);

        db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = Category,
            Message = Message,
            DetailsJson = $"{{\"backfilledCount\":{backfilled}}}",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Logged company verification backfill count ({Count}) to PlatformLogs.",
            backfilled);
    }

    private static async Task<bool> HasVerificationColumnsAsync(JobsyDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            _ = await db.Companies.AsNoTracking()
                .Select(c => c.VerificationStatus)
                .Take(1)
                .ToListAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
