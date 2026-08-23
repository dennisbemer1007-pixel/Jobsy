namespace Jobsy.Core.Interfaces;

public enum DashboardCacheKind
{
    MetricsSummary = 1,
    VacancyPerformance = 2,
    ClientPerformance = 3,
    SalesDashboard = 4,
    AmbassadeurDashboard = 5
}

public sealed record DashboardCacheDescriptor(
    DashboardCacheKind Kind,
    string ScopeKey,
    string Period,
    bool IncludePlatformOnly,
    Guid[]? CompanyIds,
    Guid? UserId,
    int Take);

/// <summary>In-process dashboard snapshot cache (10-minute TTL) with tracked keys for background refresh.</summary>
public interface IDashboardCache
{
    TimeSpan TimeToLive { get; }

    bool TryGet<T>(string key, out T? value) where T : class;

    void Set<T>(string key, T value, DashboardCacheDescriptor descriptor) where T : class;

    void Remove(string key);

    void RemoveByPrefix(string prefix);

    IReadOnlyList<(string Key, DashboardCacheDescriptor Descriptor)> GetTracked(TimeSpan maxIdle);
}
