using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Jobsy.Core;
using Jobsy.Core.Features;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Web.Features;

/// <summary>
/// Web-side feature flags: fetches public GET api/settings/feature-flags with a 30s cache.
/// </summary>
public sealed class WebFeatureFlags : IFeatureFlags
{
    public const string CacheKey = "jobsy.web.feature-flags";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WebFeatureFlags> _logger;
    private FeatureFlagSnapshot? _lastKnown;

    public WebFeatureFlags(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<WebFeatureFlags> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
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
            var apiBase = JobsyPublicUrl.NormalizeBaseUrl(
                _configuration["ApiBaseUrl"],
                "http://localhost:5200/");
            var client = _httpClientFactory.CreateClient("JobsyFeatureFlags");
            client.BaseAddress ??= new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(5);

            var dto = await client.GetFromJsonAsync<FeatureFlagsResponse>(
                "api/settings/feature-flags",
                cancellationToken);
            var flags = new FeatureFlagSnapshot(
                dto?.EmployersEnabled ?? false,
                dto?.CandidatePassportEnabled ?? true,
                dto?.PassportPartnersEnabled ?? false,
                dto?.PassportPdfV2Enabled ?? false,
                dto?.PhoneVerificationEnabled ?? false,
                dto?.SchoolsEnabled ?? false,
                dto?.AmbassadorsEnabled ?? false);
            _lastKnown = flags;
            _cache.Set(CacheKey, flags, CacheTtl);
            return flags;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load feature flags from API; using last known or defaults.");
            var fallback = _lastKnown ?? FeatureFlagSnapshot.Defaults;
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

    private sealed class FeatureFlagsResponse
    {
        [JsonPropertyName("employersEnabled")]
        public bool EmployersEnabled { get; set; }

        [JsonPropertyName("candidatePassportEnabled")]
        public bool CandidatePassportEnabled { get; set; } = true;

        [JsonPropertyName("passportPartnersEnabled")]
        public bool PassportPartnersEnabled { get; set; }

        [JsonPropertyName("passportPdfV2Enabled")]
        public bool PassportPdfV2Enabled { get; set; }

        [JsonPropertyName("phoneVerificationEnabled")]
        public bool PhoneVerificationEnabled { get; set; }

        [JsonPropertyName("schoolsEnabled")]
        public bool SchoolsEnabled { get; set; }

        [JsonPropertyName("ambassadorsEnabled")]
        public bool AmbassadorsEnabled { get; set; }
    }
}
