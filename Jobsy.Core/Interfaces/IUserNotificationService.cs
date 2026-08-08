using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public sealed record NotificationCreateRequest(
    Guid UserId,
    string Title,
    string Body,
    string Category,
    string? DeepLink = null,
    string? ActionLabel = null,
    string? ActionUrl = null,
    string? RelatedEntityType = null,
    Guid? RelatedEntityId = null);

public interface IUserNotificationService
{
    Task<UserNotification> CreateAsync(NotificationCreateRequest request, CancellationToken cancellationToken = default);

    Task CreateForEmailAsync(
        string userEmail,
        string title,
        string body,
        string category,
        string? deepLink = null,
        string? actionLabel = null,
        string? actionUrl = null,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserNotification>> ListForUserAsync(
        Guid userId,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserNotification?> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);

    Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
