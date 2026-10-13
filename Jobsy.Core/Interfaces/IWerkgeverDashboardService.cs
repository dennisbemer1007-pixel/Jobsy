namespace Jobsy.Core.Interfaces;

public enum WerkgeverTodoKind
{
    PublishRequests = 0,
    ApplicationsOverdue = 1,
    VacanciesExpiring = 2,
    LowTokens = 3,
    NoManager = 4,
    Takeovers = 5,
    TokenRequests = 6,
    InsightsRequests = 7
}

public enum WerkgeverTodoSeverity
{
    Info = 0,
    Warning = 1,
    Danger = 2
}

public enum WerkgeverTodoActionKind
{
    None = 0,
    Beoordelen = 1,
    Bekijken = 2,
    Verlengen = 3,
    TokensVerdelen = 4,
    TokensAanvragen = 5,
    IemandUitnodigen = 6
}

public enum WerkgeverDashboardRole
{
    Bedrijfsmanager = 0,
    Regiomanager = 1,
    Vestigingsmanager = 2,
    Intermediair = 3
}

public sealed record WerkgeverKpiSparkDto(IReadOnlyList<decimal> Points);

public sealed record WerkgeverKpiDto(
    int ActiveVacancies,
    int ActiveVacanciesDelta,
    int NewApplications,
    double? NewApplicationsDeltaPercent,
    double? AvgFirstResponseHours,
    double? AvgFirstResponseHoursDelta,
    int Hired,
    int HiredDelta,
    decimal? TokenBalance,
    decimal? TokensUsed,
    decimal? TokensAllocated,
    double? TokenRunwayWeeks,
    WerkgeverKpiSparkDto ActiveVacanciesSpark,
    WerkgeverKpiSparkDto NewApplicationsSpark,
    WerkgeverKpiSparkDto AvgFirstResponseSpark,
    WerkgeverKpiSparkDto HiredSpark,
    WerkgeverKpiSparkDto TokensSpark);

public sealed record WerkgeverFunnelDto(
    int Applications,
    int Accepted,
    int EmployerContacting,
    int Hired);

public sealed record WerkgeverBranchRowDto(
    Guid CompanyId,
    string Name,
    string? RegionName,
    int LiveVacancies,
    int NewApplications,
    double? AvgFirstResponseHours,
    string Status);

public sealed record WerkgeverDashboardDto(
    string Period,
    int VestigingCount,
    int RegionCount,
    string? ScopeLabel,
    WerkgeverKpiDto Kpis,
    WerkgeverFunnelDto Funnel,
    IReadOnlyList<WerkgeverBranchRowDto> Branches,
    bool ShowTokenBalance,
    bool ShowTokenUsage);

public sealed record WerkgeverTodoItemDto(
    string Kind,
    string Severity,
    string TitleKey,
    IReadOnlyList<string> TitleArgs,
    string? MetaKey,
    IReadOnlyList<string>? MetaArgs,
    string ActionKind,
    string Href,
    int Count,
    IReadOnlyList<Guid> CompanyIds);

public interface IWerkgeverDashboardService
{
    Task<WerkgeverDashboardDto> GetDashboardAsync(
        IReadOnlyList<Guid> companyIds,
        string period,
        WerkgeverDashboardRole role,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WerkgeverTodoItemDto>> GetTodoAsync(
        IReadOnlyList<Guid> companyIds,
        WerkgeverDashboardRole role,
        int take = 50,
        CancellationToken cancellationToken = default);
}

/// <summary>One Te-doen / Signalen source. 06 and 07 add classes only.</summary>
public interface ITodoSource
{
    WerkgeverTodoKind Kind { get; }

    Task<WerkgeverTodoItemDto?> BuildAsync(
        IReadOnlyList<Guid> companyIds,
        WerkgeverDashboardRole role,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
