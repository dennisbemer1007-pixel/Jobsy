using Jobsy.Core.Enums;

namespace Jobsy.Core.Contracts;

public sealed record JobSearchActivityItemDto(
    Guid Id,
    DateOnly ActivityDate,
    JobSearchActivityKind Kind,
    string Employer,
    string FunctionTitle,
    string? ContactMethod,
    string? Result,
    JobSearchActivitySource Source,
    string? SourceEntityType,
    Guid? SourceEntityId,
    bool IsExternal,
    string? StatusLabelKey,
    bool HasAttachment);

public sealed record JobSearchActivityWeekDto(
    DateOnly WeekStart,
    string WeekLabel,
    int Count,
    IReadOnlyList<JobSearchActivityItemDto> Items);

public sealed record JobSearchActivityOverviewDto(
    DateOnly PeriodStart,
    DateOnly PeriodEndInclusive,
    string PeriodLabel,
    int ActivitiesDoneInPeriod,
    int ActivitiesTarget,
    int DaysRemainingInPeriod,
    bool TargetMet,
    IReadOnlyList<JobSearchActivityWeekDto> Weeks);

public sealed record CreateManualJobSearchActivityRequest(
    JobSearchActivityKind Kind,
    DateOnly ActivityDate,
    string Employer,
    string FunctionTitle,
    string? Result);

public sealed record UpdateActivityAgreementRequest(int ActivitiesPerPeriod);
