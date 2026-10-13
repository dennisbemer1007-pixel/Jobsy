namespace Jobsy.Core.Entities;

/// <summary>
/// AVG access log: who accessed whose personal data, which resource, when, and why.
/// Retention: <see cref="Privacy.PrivacyConstants.PersonalDataAccessLogRetentionDays"/> (default 2 years).
/// </summary>
public class PersonalDataAccessLog
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid ActorUserId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public Guid? SubjectUserId { get; set; }
    public Guid? SubjectCompanyId { get; set; }
    /// <summary>
    /// Pseudonymous pupil code id when staff views per-code detail / PDF (no FK — survives retention).
    /// Resource key: <c>school.pupil-code</c>.
    /// </summary>
    public Guid? SubjectPupilCodeId { get; set; }
    /// <summary>Stable resource key, e.g. admin.users.list, application.cv.download.</summary>
    public string Resource { get; set; } = string.Empty;
    /// <summary>list | view | export | download | reveal</summary>
    public string Action { get; set; } = string.Empty;
    public string? Reason { get; set; }
    /// <summary>Filled by prompt 06 (temporary support access).</summary>
    public Guid? SupportAccessGrantId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    /// <summary>SHA-256(salt + IP); never store the raw IP.</summary>
    public string? IpHash { get; set; }
}
