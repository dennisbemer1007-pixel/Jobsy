namespace Jobsy.Api.Models;

public sealed class AssistantChatRequestDto
{
    public List<AssistantChatMessageDto> Messages { get; set; } = [];

    /// <summary>Current Blazor/SSR path (e.g. /banenkaart) for page-aware replies.</summary>
    public string? PagePath { get; set; }

    /// <summary>Localized line shown in the assistant panel (e.g. "Je bekijkt nu je paspoort…").</summary>
    public string? PageContextLine { get; set; }
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
