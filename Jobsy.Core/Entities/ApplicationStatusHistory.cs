using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Dated application status event. No backfill of invented dates for older applications (D4).
/// </summary>
public class ApplicationStatusHistory
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    public ApplicationStatusEventKind Kind { get; set; }
    public ApplicationStatus? FromStatus { get; set; }
    public ApplicationStatus? ToStatus { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public ApplicationStatusActorKind ActorKind { get; set; }
    /// <summary>Never exposed on candidate-facing payloads.</summary>
    public Guid? ActorUserId { get; set; }
}
