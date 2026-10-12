using Jobsy.Core.Entities;
using Jobsy.Core.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jobsy.Infrastructure.Data;

/// <summary>Maintains e-mail blind indexes when encrypted e-mail columns change.</summary>
public sealed class SensitiveDataSaveInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Stamp(DbContext? context)
    {
        if (context is not JobsyDbContext db)
        {
            return;
        }

        foreach (var entry in db.ChangeTracker.Entries<User>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified
                && entry.Property(u => u.Email).IsModified)
            {
                entry.Entity.EmailLookupHash = EmailLookupHash.Compute(entry.Entity.Email);
            }
        }

        foreach (var entry in db.ChangeTracker.Entries<LocalAuthCredential>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified
                && entry.Property(c => c.Email).IsModified)
            {
                entry.Entity.EmailLookupHash = EmailLookupHash.Compute(entry.Entity.Email);
            }
        }

        foreach (var entry in db.ChangeTracker.Entries<Application>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified
                && entry.Property(a => a.CandidateEmail).IsModified)
            {
                entry.Entity.CandidateEmailLookupHash =
                    EmailLookupHash.ComputeOrNull(entry.Entity.CandidateEmail);
            }
        }
    }
}
