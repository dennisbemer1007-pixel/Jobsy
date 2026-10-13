using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

/// <summary>
/// Single writer for application status + history rows (kandidaat-banen 07).
/// Callers must SaveChanges (or use the atomic react helper).
/// </summary>
public interface IApplicationStatusRecorder
{
    /// <summary>
    /// Sets status + adds a StatusChanged row when the status actually changes.
    /// Clears RespondedAt when <paramref name="clearRespondedAt"/> is true (re-apply).
    /// </summary>
    bool SetStatus(
        Application application,
        ApplicationStatus newStatus,
        ApplicationStatusActorKind actorKind,
        Guid? actorUserId,
        DateTime nowUtc,
        bool setRespondedAt = true,
        bool clearRespondedAt = false);

    /// <summary>Adds a Created row and ensures Pending. Idempotent if a Created row already exists.</summary>
    void RecordCreated(
        Application application,
        ApplicationStatusActorKind actorKind,
        Guid? actorUserId,
        DateTime nowUtc);

    /// <summary>
    /// Atomic Pending→Accepted/Rejected via ExecuteUpdate (relational) or tracked update (InMemory),
    /// with a history row in the same unit of work. Returns false when no row was updated.
    /// </summary>
    Task<bool> TryReactAsync(
        Application application,
        ApplicationStatus newStatus,
        Guid? actorUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records EmployerViewed once per application. Returns true when a new row was inserted.
    /// Concurrent unique-index races are ignored.
    /// </summary>
    Task<bool> TryRecordEmployerViewedAsync(
        Guid applicationId,
        Guid? actorUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
}
