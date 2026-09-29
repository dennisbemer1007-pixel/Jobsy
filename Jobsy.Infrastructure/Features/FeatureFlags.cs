using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Features;

/// <summary>
/// Cached platform feature flags backed by <see cref="IPlatformFeatureService"/>.
/// </summary>
public sealed class FeatureFlags : IFeatureFlags
{
    public const string CacheKey = "jobsy.feature-flags";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly IPlatformFeatureService _features;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FeatureFlags> _logger;

    public FeatureFlags(
        IPlatformFeatureService features,
        IMemoryCache cache,
        ILogger<FeatureFlags> logger)
    {
        _features = features;
        _cache = cache;
        _logger = logger;
    }

    public async ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out FeatureFlagSnapshot? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            var snap = await _features.GetAsync(cancellationToken);
            var flags = new FeatureFlagSnapshot(snap.EmployersEnabled, snap.CandidatePassportEnabled);
            _cache.Set(CacheKey, flags, CacheTtl);
            return flags;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to load platform feature flags; using defaults (Employers ON, Passport OFF).");
            var fallback = FeatureFlagSnapshot.Defaults;
            _cache.Set(CacheKey, fallback, TimeSpan.FromSeconds(10));
            return fallback;
        }
    }

    public async ValueTask<bool> IsEnabledAsync(
        PlatformFeature feature,
        CancellationToken cancellationToken = default)
    {
        var snap = await GetAsync(cancellationToken);
        return snap.IsEnabled(feature);
    }

    public void Invalidate() => _cache.Remove(CacheKey);
}
