using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Structured logging when <see cref="JobsyDbContext"/> EF migrate fails on startup (Render lobsy-acc-api).
/// </summary>
internal static class MigrationStartupDiagnostics
{
    internal static async Task LogPreflightAsync(
        JobsyDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var applied = (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        var pendingModelChanges = db.Database.HasPendingModelChanges();

        logger.LogInformation(
            "EF migrate preflight: appliedCount={AppliedCount}, pendingCount={PendingCount}, pendingModelChanges={PendingModelChanges}, lastApplied={LastApplied}, nextPending={NextPending}",
            applied.Count,
            pending.Count,
            pendingModelChanges,
            applied.Count > 0 ? applied[^1] : "(none)",
            pending.Count > 0 ? pending[0] : "(none)");

        if (pendingModelChanges)
        {
            logger.LogError(
                "EF model has pending changes vs JobsyDbContextModelSnapshot (MigrateAsync will fail before running pending migrations). "
                + "Typical after #1376 MboHboProgressJson: entity property added without snapshot sync. "
                + "Pending migrations that would run after snapshot fix: {PendingMigrations}",
                string.Join(", ", pending));
        }
        else if (pending.Count > 0)
        {
            logger.LogInformation(
                "EF pending migrations to apply: {PendingMigrations}",
                string.Join(", ", pending));
        }
    }

    internal static void LogMigrateFailure(
        Exception ex,
        IReadOnlyList<string> pendingMigrations,
        string? lastAppliedMigration,
        ILogger logger)
    {
        var pendingModelChanges = ex.Message.Contains("pending changes", StringComparison.OrdinalIgnoreCase)
                                  || (ex.InnerException?.Message.Contains("pending changes", StringComparison.OrdinalIgnoreCase) ?? false);

        logger.LogError(
            ex,
            "EF Database.MigrateAsync failed. lastApplied={LastApplied}, pendingCount={PendingCount}, pendingModelChanges={PendingModelChanges}, nextPending={NextPending}",
            lastAppliedMigration ?? "(none)",
            pendingMigrations.Count,
            pendingModelChanges,
            pendingMigrations.Count > 0 ? pendingMigrations[0] : "(none)");

        logger.LogError(
            "EF Database.MigrateAsync exception detail (full): {ExceptionDetail}",
            ex.ToString());

        if (pendingModelChanges)
        {
            logger.LogError(
                "Migrate failed due to pending model changes (not a Postgres SQL error). "
                + "Fix JobsyDbContextModelSnapshot / dotnet ef migrations has-pending-model-changes — not #1375 G78 UI.");
        }
        else if (pendingMigrations.Count > 0)
        {
            logger.LogError(
                "Migrate likely failed while applying migration {MigrationId} (check Postgres SQL above in the same log window).",
                pendingMigrations[0]);
        }
    }
}
