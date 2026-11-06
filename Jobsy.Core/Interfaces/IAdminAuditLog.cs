namespace Jobsy.Core.Interfaces;

public sealed record AdminAuditEntry(
    string Action,
    string TargetType,
    string? TargetId = null,
    string? TargetLabel = null,
    string? Reason = null,
    string? DetailsJson = null,
    string Result = "success",
    Guid? ActorUserId = null,
    string? ActorRole = null,
    string ActorKind = "admin",
    string? CorrelationId = null,
    string? IpAddress = null,
    DateTime? OccurredAtUtc = null);

public interface IAdminAuditLog
{
    /// <summary>
    /// Persist an audit event in its own SaveChanges. Failures are logged (warning + Sentry)
    /// but must not throw to callers — except when the caller uses <see cref="Stage"/> for
    /// critical same-transaction writes (2FA reset, role change).
    /// </summary>
    Task WriteAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a row to the current DbContext without saving. The caller must include it in the
    /// same SaveChanges as the business mutation (2FA reset / role change). Throws on
    /// validation failure so the request can return 500 before committing.
    /// </summary>
    void Stage(AdminAuditEntry entry);
}
