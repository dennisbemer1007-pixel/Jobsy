namespace Jobsy.Core.Entities;

/// <summary>
/// Append-only admin action audit trail. Retention:
/// <see cref="Privacy.PrivacyConstants.AdminAuditRetentionDays"/> (default 7 years).
/// </summary>
public class AdminAuditEvent
{
    public Guid Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    /// <summary>Null for system actors.</summary>
    public Guid? ActorUserId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    /// <summary>admin | system | self</summary>
    public string ActorKind { get; set; } = "admin";
    /// <summary>Stable action key, e.g. settings.platform.update, user.mfa.reset.</summary>
    public string Action { get; set; } = string.Empty;
    /// <summary>user | company | setting | vacancy | invoice | grant | export | …</summary>
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    /// <summary>Masked label, ≤ 200 chars. Never clear-text candidate PII.</summary>
    public string TargetLabel { get; set; } = string.Empty;
    public string? Reason { get; set; }
    /// <summary>Non-PII diffs only, ≤ 4 kB.</summary>
    public string? DetailsJson { get; set; }
    /// <summary>success | denied | failed</summary>
    public string Result { get; set; } = "success";
    public string CorrelationId { get; set; } = string.Empty;
    /// <summary>SHA-256(salt + IP); never raw IP.</summary>
    public string? IpHash { get; set; }
}
