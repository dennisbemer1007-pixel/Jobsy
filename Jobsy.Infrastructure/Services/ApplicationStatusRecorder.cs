using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class ApplicationStatusRecorder : IApplicationStatusRecorder
{
    private readonly JobsyDbContext _db;
    private readonly ILogger<ApplicationStatusRecorder> _logger;

    public ApplicationStatusRecorder(JobsyDbContext db, ILogger<ApplicationStatusRecorder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public bool SetStatus(
        Application application,
        ApplicationStatus newStatus,
        ApplicationStatusActorKind actorKind,
        Guid? actorUserId,
        DateTime nowUtc,
        bool setRespondedAt = true,
        bool clearRespondedAt = false)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (application.Status == newStatus)
        {
            if (clearRespondedAt)
            {
                application.RespondedAt = null;
            }

            return false;
        }

        var row = ApplicationStatusTransitions.SetStatus(
            application,
            newStatus,
            actorKind,
            actorUserId,
            nowUtc,
            setRespondedAt && !clearRespondedAt);
        if (clearRespondedAt)
        {
            application.RespondedAt = null;
        }

        if (row is not null)
        {
            _db.ApplicationStatusHistories.Add(row);
            return true;
        }

        return false;
    }

    public void RecordCreated(
        Application application,
        ApplicationStatusActorKind actorKind,
        Guid? actorUserId,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(application);
        application.Status = ApplicationStatus.Pending;

        if (HasLocalCreated(application.Id))
        {
            return;
        }

        // Existing applications (re-apply / draft verify) may already have a Created row.
        if (application.Id != Guid.Empty
            && _db.Entry(application).State is not (EntityState.Added or EntityState.Detached)
            && _db.ApplicationStatusHistories.Any(h =>
                h.ApplicationId == application.Id && h.Kind == ApplicationStatusEventKind.Created))
        {
            return;
        }

        if (application.Id != Guid.Empty
            && _db.Entry(application).State == EntityState.Detached
            && _db.ApplicationStatusHistories.Any(h =>
                h.ApplicationId == application.Id && h.Kind == ApplicationStatusEventKind.Created))
        {
            return;
        }

        var row = ApplicationStatusTransitions.CreateCreated(application, actorKind, actorUserId, nowUtc);
        _db.ApplicationStatusHistories.Add(row);
    }

    public async Task<bool> TryReactAsync(
        Application application,
        ApplicationStatus newStatus,
        Guid? actorUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (newStatus is not (ApplicationStatus.Accepted or ApplicationStatus.Rejected))
        {
            return false;
        }

        if (application.Status != ApplicationStatus.Pending)
        {
            return false;
        }

        var from = application.Status;
        if (_db.Database.IsRelational())
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
            var updated = await _db.Applications
                .Where(a => a.Id == application.Id && a.Status == ApplicationStatus.Pending)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(a => a.Status, newStatus)
                        .SetProperty(a => a.RespondedAt, nowUtc),
                    cancellationToken);
            if (updated == 0)
            {
                await tx.RollbackAsync(cancellationToken);
                return false;
            }

            var row = ApplicationStatusTransitions.NewStatusChangedRow(
                application.Id,
                from,
                newStatus,
                ApplicationStatusActorKind.Employer,
                actorUserId,
                nowUtc);
            _db.ApplicationStatusHistories.Add(row);
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            application.Status = newStatus;
            application.RespondedAt = nowUtc;
            var entry = _db.Entry(application);
            if (entry.State != EntityState.Detached)
            {
                entry.Property(a => a.Status).IsModified = false;
                entry.Property(a => a.RespondedAt).IsModified = false;
            }

            return true;
        }

        var tracked = ApplicationStatusTransitions.SetStatus(
            application,
            newStatus,
            ApplicationStatusActorKind.Employer,
            actorUserId,
            nowUtc);
        if (tracked is null)
        {
            return false;
        }

        _db.ApplicationStatusHistories.Add(tracked);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryRecordEmployerViewedAsync(
        Guid applicationId,
        Guid? actorUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.ApplicationStatusHistories.AsNoTracking()
            .AnyAsync(
                h => h.ApplicationId == applicationId && h.Kind == ApplicationStatusEventKind.EmployerViewed,
                cancellationToken);
        if (exists)
        {
            return false;
        }

        var row = ApplicationStatusTransitions.NewEmployerViewed(applicationId, actorUserId, nowUtc);
        _db.ApplicationStatusHistories.Add(row);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex)
        {
            _db.Entry(row).State = EntityState.Detached;
            _logger.LogDebug(ex, "EmployerViewed race for application {ApplicationId}", applicationId);
            return false;
        }
    }

    private bool HasLocalCreated(Guid applicationId)
        => _db.ApplicationStatusHistories.Local.Any(h =>
            h.ApplicationId == applicationId && h.Kind == ApplicationStatusEventKind.Created);
}
