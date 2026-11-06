namespace Jobsy.Core.Interfaces;

public sealed record PersonalDataAccessEntry(
    Guid ActorUserId,
    string ActorRole,
    string Resource,
    string Action,
    Guid? SubjectUserId = null,
    Guid? SubjectCompanyId = null,
    string? Reason = null,
    Guid? SupportAccessGrantId = null,
    string? CorrelationId = null,
    string? IpAddress = null,
    Guid? SubjectPupilCodeId = null);

public interface IPersonalDataAccessLogger
{
    /// <summary>
    /// Persist an access event. Failures are logged but must not throw to callers.
    /// </summary>
    Task LogAsync(PersonalDataAccessEntry entry, CancellationToken cancellationToken = default);
}
