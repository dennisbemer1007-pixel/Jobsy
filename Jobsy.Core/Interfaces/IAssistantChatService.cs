namespace Jobsy.Core.Interfaces;

public interface IAssistantChatService
{
    Task<AssistantChatResult> ChatAsync(
        AssistantChatContext context,
        IReadOnlyList<AssistantChatMessage> history,
        CancellationToken cancellationToken = default);
}

public sealed record AssistantChatMessage(string Role, string Content);

public sealed record AssistantChatContext(
    Guid UserId,
    string Role,
    string Language,
    IReadOnlyCollection<Guid>? AccessibleCompanyIds);

public sealed record AssistantChatResult(
    string Reply,
    bool UsedAi,
    IReadOnlyList<AssistantChatAction> Actions);

public sealed record AssistantChatAction(
    string Type,
    string? Url = null,
    string? WorkType = null,
    string? SearchQuery = null,
    int? Count = null,
    string? Label = null,
    Guid? ApplicationId = null,
    Guid? VacancyId = null,
    int? MaxTravelMinutes = null,
    string? Transport = null);

public static class AssistantActionTypes
{
    public const string Navigate = "navigate";
    public const string SetFilters = "setFilters";
    public const string OpenApplication = "openApplication";
}
