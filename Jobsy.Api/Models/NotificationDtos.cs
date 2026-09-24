namespace Jobsy.Api.Models;

public record UserNotificationDto(
    Guid Id,
    string Title,
    string Body,
    string Category,
    string? DeepLink,
    string? ActionLabel,
    string? ActionUrl,
    bool IsRead,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc,
    string? RelatedEntityType,
    Guid? RelatedEntityId);

public record UnreadNotificationCountDto(int Count);

public record CandidateActionRequest(string Token);

public record WithdrawOthersAuthenticatedRequest(Guid HiredApplicationId);

public record CandidateActionResultDto(bool Succeeded, string Message, int? WithdrawnCount = null);

public record WebPushVapidPublicKeyDto(string PublicKey);

public record WebPushSubscribeRequest(string Endpoint, WebPushKeysRequest Keys);

public record WebPushKeysRequest(string P256dh, string Auth);

public record WebPushUnsubscribeRequest(string? Endpoint);

public record WebPushSubscriptionDto(Guid Id, string Endpoint, DateTime CreatedAtUtc, DateTime? LastUsedAtUtc);
