using Jobsy.Core.Contracts;

namespace Jobsy.Web.Services;

/// <summary>Circuit-scoped cache for discovery overview test counts (keyed by API profile revision).</summary>
public sealed class DiscoveryOverviewCache(MeGetCache meCache, JobsyApiClient api)
{
    private const string CachePrefix = "discovery-overview:";

    public async Task<CandidateDiscoveryOverviewPanelDto?> GetPanelAsync(CancellationToken ct = default)
    {
        var result = await meCache.GetOrCreateAsync(
            CachePrefix + "latest",
            async token =>
            {
                var panel = await api.GetDiscoveryOverviewPanelAsync(token);
                return panel is null
                    ? MeGetResult<CandidateDiscoveryOverviewPanelDto>.Unavailable()
                    : MeGetResult<CandidateDiscoveryOverviewPanelDto>.Ok(panel);
            },
            MeGetCache.DefaultTtl,
            ct);

        return result.TemporarilyUnavailable ? null : result.Value;
    }

    public void Invalidate() => meCache.Invalidate(CachePrefix + "latest");
}
