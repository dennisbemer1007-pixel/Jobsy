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
    /// <summary>Null when locked — value never leaves the server.</summary>
    SuppressedCount? Candidates32PlusHours,
    /// <summary>Null when locked.</summary>
    SuppressedCount? Active30d,
    /// <summary>Null when locked.</summary>
    SuppressedCount? MatchingYourVacancies);

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

public sealed record InsightsCoverageDto(
    bool IsFull,
    int CoveredCount,
    int TotalCount,
    DateTime? ExpiresAtUtc,
    bool CanRenew,
    string ScopeKind);

public sealed record InsightsOfferDto(
    decimal PriceTokens,
    int DurationDays,
    string ScopeKind,
    bool CanUnlock,
    string? CannotUnlockReason,
    decimal? WalletBalance);

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
    InsightsTrend? Trend,
    IReadOnlyList<string> LockedSections,
    InsightsCoverageDto Coverage,
    InsightsOfferDto Offer);

public sealed record CandidateInsightsBranchDto(Guid Id, string Name, bool IsLocked);

public sealed record CandidateInsightsUnlockResultDto(
    Guid UnlockId,
    DateTime ExpiresAtUtc,
    decimal SpentTokens,
    decimal BalanceAfter);

public sealed record CandidateInsightsUnlockRequestDto(
    Guid Id,
    Guid BranchCompanyId,
    string BranchName,
    Guid RequestedByUserId,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? HandledAtUtc);

/// <summary>Stable locked-section keys used in <see cref="CandidateInsightsDto.LockedSections"/> and JSON leak tests.</summary>
public static class InsightsLockedKeys
{
    public const string MatchingYourVacancies = "MatchingYourVacancies";
    public const string Candidates32PlusHours = "Candidates32PlusHours";
    public const string Active30d = "Active30d";
    public const string Density = "Density";
    public const string WorkFields = "WorkFields";
    public const string Priorities = "Priorities";
    public const string WorkKinds = "WorkKinds";
    public const string DreamJobsTop = "DreamJobsTop";
    public const string DnaRiasec = "DnaRiasec";
    public const string Competences = "Competences";
    public const string Personality = "Personality";
    public const string Trend = "Trend";
    public const string Vacancies = "Vacancies";

    public static readonly IReadOnlyList<string> All =
    [
        MatchingYourVacancies,
        Candidates32PlusHours,
        Active30d,
        Density,
        WorkFields,
        Priorities,
        WorkKinds,
        DreamJobsTop,
        DnaRiasec,
        Competences,
        Personality,
        Trend,
        Vacancies
    ];
}
