using Jobsy.Core.Interfaces;

namespace Jobsy.Core.Contracts;

/// <summary>Payload returned after a manual dashboard cache flush for the caller's current scope.</summary>
public sealed record DashboardRefreshResultDto(
    DateTime GeneratedAtUtc,
    DateTime CachedUntilUtc,
    IReadOnlyList<MetricCountDto>? Metrics,
    VacancyPerformanceBoardDto? VacancyPerformance,
    ClientPerformanceBoardDto? ClientPerformance,
    SalesManagerDashboardDto? Sales,
    AmbassadeurDashboardDto? Ambassadeur);
