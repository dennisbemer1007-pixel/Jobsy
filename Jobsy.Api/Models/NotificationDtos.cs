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
