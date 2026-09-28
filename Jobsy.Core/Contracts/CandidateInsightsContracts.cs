namespace Jobsy.Core.Contracts;

public sealed record SuppressedCount(string Status, int? Value);

public sealed record SuppressedPercent(string Status, int? Percent);

public sealed record InsightsBranchRef(Guid Id, string Name, double Lat, double Lng);

public sealed record InsightsScope(
    IReadOnlyList<InsightsBranchRef> Branches,
    int RadiusKm,
    int PeriodDays,
    DateTime GeneratedAtUtc,
    bool IsFullAccess);

public sealed record InsightsKpis(
    SuppressedCount CandidatesInRadius,
    SuppressedCount AvgHoursPerWeek,
    SuppressedCount Candidates32PlusHours,
    SuppressedCount Active30d,
    SuppressedCount MatchingYourVacancies);

public sealed record RankedItem(string Label, SuppressedCount Count);

public sealed record InsightsDistributionBucket(string Key, string Label, SuppressedPercent Share);

public sealed record InsightsDistribution(
    string Status,
    IReadOnlyList<InsightsDistributionBucket> Buckets);

public sealed record DensityCell(string CellId, double CenterLat, double CenterLng, int Band);

public sealed record InsightsVacancyChecklist(
    bool? SalaryMentioned,
    bool? FlexibleHours,
    bool? AtmosphereAndTeam);

public sealed record InsightsTips(
    IReadOnlyList<string> TipKeys,
    InsightsVacancyChecklist? Checklist);

public sealed record VacancyReach(
    Guid VacancyId,
    string Title,
    string BranchName,
    SuppressedCount MatchingCandidates,
    InsightsTips? Tips);

public sealed record InsightsTrend(string Status, string? MessageKey = null);

public sealed record CandidateInsightsDto(
    InsightsScope Scope,
    InsightsKpis Kpis,
    IReadOnlyList<RankedItem> DreamJobsTop,
    InsightsDistribution? WorkFields,
    InsightsDistribution? DnaRiasec,
    InsightsDistribution? Competences,
    InsightsDistribution? Personality,
    InsightsDistribution? Priorities,
    InsightsDistribution? WorkKinds,
    IReadOnlyList<DensityCell> Density,
    IReadOnlyList<VacancyReach> Vacancies,
    InsightsTrend Trend,
    IReadOnlyList<string> LockedSections);

public sealed record CandidateInsightsBranchDto(Guid Id, string Name, bool IsLocked);
