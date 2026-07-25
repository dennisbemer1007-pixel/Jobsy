namespace Jobsy.Core.Interfaces;

public interface IVacancyContentModerationService
{
    /// <summary>
    /// Checks vacancy title and description for discriminatory or unnecessarily harsh wording.
    /// </summary>
    Task<VacancyContentModerationResult> CheckAsync(
        string title,
        string description,
        CancellationToken cancellationToken = default);
}

public sealed record VacancyContentModerationResult(
    bool IsAllowed,
    string? Warning = null,
    string? Suggestion = null)
{
    public static VacancyContentModerationResult Allowed() => new(true);

    public static VacancyContentModerationResult Blocked(string warning, string suggestion) =>
        new(false, warning, suggestion);
}

/// <summary>API / client contract for moderation feedback when save is blocked.</summary>
public static class VacancyModerationCodes
{
    public const string ContentModeration = "content_moderation";
}
