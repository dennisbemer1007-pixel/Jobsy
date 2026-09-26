namespace Jobsy.Core.Interfaces;

public interface IWebPushSubscriptionService
{
    Task UpsertAsync(Guid userId, WebPushSubscriptionInput input, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid userId, string endpoint, CancellationToken cancellationToken = default);
    Task RemoveAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RemoveByDeviceSessionAsync(Guid deviceSessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebPushSubscriptionRecord>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record WebPushSubscriptionInput(
    string Endpoint,
    string P256dh,
    string Auth,
    string? UserAgent,
    Guid? DeviceSessionId = null);

public sealed record WebPushSubscriptionRecord(
    Guid Id,
    string Endpoint,
    DateTime CreatedAtUtc,
    DateTime? LastUsedAtUtc,
    Guid? DeviceSessionId = null);
