namespace Jobsy.Api.Models;

public sealed class AssistantChatRequestDto
{
    public List<AssistantChatMessageDto> Messages { get; set; } = [];
}

public sealed class AssistantChatMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public sealed record AssistantChatResponseDto(
    string Reply,
    bool UsedAi,
    IReadOnlyList<AssistantChatActionDto> Actions);

public sealed record AssistantChatActionDto(
    string Type,
    string? Url,
    string? WorkType,
    string? SearchQuery,
    int? Count,
    string? Label,
    Guid? ApplicationId,
    Guid? VacancyId,
    int? MaxTravelMinutes = null,
    string? Transport = null);
