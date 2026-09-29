using Jobsy.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Append-only guard: tracked <see cref="AdminAuditEvent"/> rows may not be Modified or Deleted.
/// Retention purge uses ExecuteDeleteAsync (bypasses change tracking).
/// </summary>
public sealed class AdminAuditAppendOnlyInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Guard(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Guard(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Guard(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<AdminAuditEvent>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException(
                    "AdminAuditEvent is append-only and cannot be modified or deleted via the change tracker.");
            }
        }
    }
}
