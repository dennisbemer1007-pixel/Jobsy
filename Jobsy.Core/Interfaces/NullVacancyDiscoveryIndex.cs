using Jobsy.Core.Contracts;

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
}
