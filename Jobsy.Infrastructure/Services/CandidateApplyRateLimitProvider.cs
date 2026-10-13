using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateApplyRateLimitProvider : ICandidateApplyRateLimitProvider
{
    public const string CacheKey = "jobsy.candidate-apply-rate-limits";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CandidateApplyRateLimitProvider> _logger;

    public CandidateApplyRateLimitProvider(
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache,
        ILogger<CandidateApplyRateLimitProvider> logger)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _logger = logger;
    }

    public CandidateApplyRateLimitValues GetCurrent()
    {
        if (_cache.TryGetValue(CacheKey, out CandidateApplyRateLimitValues? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var features = scope.ServiceProvider.GetRequiredService<IPlatformFeatureService>();
            var snap = features.GetAsync().GetAwaiter().GetResult();
            var values = new CandidateApplyRateLimitValues(
                snap.CandidateApplyPerMinuteLimit,
                snap.CandidateApplyPerHourLimit,
                snap.CandidateApplyPerDayLimit);
            _cache.Set(CacheKey, values, CacheTtl);
            return values;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load candidate apply rate limits; using product defaults.");
            var fallback = CandidateApplyRateLimitValues.ProductDefaults;
            _cache.Set(CacheKey, fallback, TimeSpan.FromSeconds(10));
            return fallback;
        }
    }

    public void Invalidate() => _cache.Remove(CacheKey);
}
