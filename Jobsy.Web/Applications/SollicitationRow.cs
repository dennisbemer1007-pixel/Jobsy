namespace Jobsy.Web.Applications;

internal sealed record SollicitationRow(
    Guid RowId,
    Guid? ApplicationId,
    Guid? ExternalVacancyId,
    Guid? VacancyId,
    string Title,
    string CompanyName,
    string? Place,
    string Status,
    bool IsInitiative,
    bool IsExternal,
    bool IsDone,
    string? PictureUrl,
    string SourceTagKey,
    string StatusLabel,
    string StatusModifier,
    string? CardHint,
    DateTime SortAtUtc,
    IReadOnlyList<SollicitationTimelineStep> Timeline,
    string? NextStepKey);

internal sealed record SollicitationTimelineStep(
    string Key,
    string State,
    string Label,
    DateTime? OccurredAtUtc);
