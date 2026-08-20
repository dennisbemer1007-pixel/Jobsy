using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

/// <summary>No-op index for unit tests that construct services without DI.</summary>
public sealed class NullVacancyDiscoveryIndex : IVacancyDiscoveryIndex
{
    public static NullVacancyDiscoveryIndex Instance { get; } = new();

    public void Invalidate()
    {
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<VacancyDiscoveryRecord>> GetActiveAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<VacancyDiscoveryRecord>>([]);

    public Task<VacancyMapView> GetMapViewAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(VacancyMapViewCalculator.Fallback);
}
