using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Pure helpers for application status writes + history rows.
/// Controllers must go through <c>IApplicationStatusRecorder</c> (Infrastructure).
/// </summary>
public static class ApplicationStatusTransitions
{
    /// <summary>
    /// Sets <see cref="Application.Status"/> (and <see cref="Application.RespondedAt"/> when provided)
    /// and returns a <see cref="ApplicationStatusEventKind.StatusChanged"/> row, or null when unchanged.
    /// </summary>
    public static ApplicationStatusHistory? SetStatus(
        Application application,
        ApplicationStatus newStatus,
        ApplicationStatusActorKind actorKind,
        Guid? actorUserId,
        DateTime nowUtc,
        bool setRespondedAt = true)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (application.Status == newStatus)
        {
            return null;
        }

        var from = application.Status;
        application.Status = newStatus;
        if (setRespondedAt)
        {
            application.RespondedAt = nowUtc;
        }

        return NewRow(
            application.Id,
            ApplicationStatusEventKind.StatusChanged,
            from,
            newStatus,
            nowUtc,
            actorKind,
            actorUserId);
    }

    public static ApplicationStatusHistory CreateCreated(
        Application application,
        ApplicationStatusActorKind actorKind,
        Guid? actorUserId,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(application);
        application.Status = ApplicationStatus.Pending;
        return NewRow(
            application.Id,
            ApplicationStatusEventKind.Created,
            fromStatus: null,
            toStatus: ApplicationStatus.Pending,
            nowUtc,
            actorKind,
            actorUserId);
    }

    public static ApplicationStatusHistory NewEmployerViewed(
        Guid applicationId,
        Guid? actorUserId,
        DateTime nowUtc)
        => NewRow(
            applicationId,
            ApplicationStatusEventKind.EmployerViewed,
            fromStatus: null,
            toStatus: null,
            nowUtc,
            ApplicationStatusActorKind.Employer,
            actorUserId);

    public static ApplicationStatusHistory NewStatusChangedRow(
        Guid applicationId,
        ApplicationStatus fromStatus,
        ApplicationStatus toStatus,
        ApplicationStatusActorKind actorKind,
        Guid? actorUserId,
        DateTime nowUtc)
        => NewRow(
            applicationId,
            ApplicationStatusEventKind.StatusChanged,
            fromStatus,
            toStatus,
            nowUtc,
            actorKind,
            actorUserId);

    private static ApplicationStatusHistory NewRow(
        Guid applicationId,
        ApplicationStatusEventKind kind,
        ApplicationStatus? fromStatus,
        ApplicationStatus? toStatus,
        DateTime nowUtc,
        ApplicationStatusActorKind actorKind,
        Guid? actorUserId)
        => new()
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            Kind = kind,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            OccurredAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc),
            ActorKind = actorKind,
            ActorUserId = actorUserId
        };
}
