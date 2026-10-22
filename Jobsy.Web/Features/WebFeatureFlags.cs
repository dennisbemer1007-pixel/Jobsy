using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Jobsy.Core;
using Jobsy.Core.Features;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Web.Features;

/// <summary>
/// Web-side feature flags: fetches public GET api/settings/feature-flags with a 10s cache.
/// The API cache is cleared as soon as an admin saves a flag.
/// </summary>
public sealed class WebFeatureFlags : IFeatureFlags
{
    public const string CacheKey = "jobsy.web.feature-flags";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(10);

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
                    cancellationToken)
                .ConfigureAwait(false);
            var flags = new FeatureFlagSnapshot(
                dto?.EmployersEnabled ?? false,
                dto?.CandidatePassportEnabled ?? true,
                dto?.PassportPartnersEnabled ?? false,
                dto?.PassportPdfV2Enabled ?? false,
                dto?.PupilPassportV2Enabled ?? false,
                dto?.StudentPassportEnabled ?? false,
                dto?.PhoneVerificationEnabled ?? false,
                dto?.SchoolsEnabled ?? false,
                dto?.AmbassadorsEnabled ?? false,
                dto?.WhatsAppRemindersEnabled ?? false,
                dto?.CompactTestPdfEnabled ?? false,
                dto?.HonestAdviceEnabled ?? false,
                dto?.FutureJobsForYouEnabled ?? false,
                dto?.EmployerPhase2Enabled ?? false,
                dto?.CandidateExternalVacanciesEnabled ?? false,
                dto?.ProactiveMatchingEnabled ?? false,
                dto?.TestsV2Enabled ?? false,
                TestItemExamplesEnabled: dto?.TestItemExamplesEnabled ?? false,
                TestProgressInsightEnabled: dto?.TestProgressInsightEnabled ?? false,
                dto?.EmployerFitQuestionnairesEnabled ?? false,
                dto?.KandidatenkaartEnabled ?? false,
                dto?.CompanyMapInitiativeEnabled ?? true,
                dto?.AfterMatchEnabled ?? false,
                dto?.ReferenceVerificationEnabled ?? false,
                dto?.SensitiveExperienceEnabled ?? false,
                dto?.TrainingPartnersEnabled ?? false,
                dto?.TestJourneyIllustrationEnabled ?? false,
                dto?.OnboardingFinaleV2Enabled ?? false,
                dto?.OccupationDayInLifeEnabled ?? true,
                dto?.TestEditModeEnabled ?? false,
                dto?.SamplePdfEnabled ?? false,
                dto?.TestDepthValueMockupEnabled ?? true,
                dto?.UwvFeaturesEnabled ?? true);
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

        [JsonPropertyName("whatsAppRemindersEnabled")]
        public bool WhatsAppRemindersEnabled { get; set; }

        [JsonPropertyName("compactTestPdfEnabled")]
        public bool CompactTestPdfEnabled { get; set; }

        [JsonPropertyName("honestAdviceEnabled")]
        public bool HonestAdviceEnabled { get; set; }

        [JsonPropertyName("futureJobsForYouEnabled")]
        public bool FutureJobsForYouEnabled { get; set; }

        [JsonPropertyName("employerPhase2Enabled")]
        public bool EmployerPhase2Enabled { get; set; }

        [JsonPropertyName("candidateExternalVacanciesEnabled")]
        public bool CandidateExternalVacanciesEnabled { get; set; }

        [JsonPropertyName("proactiveMatchingEnabled")]
        public bool ProactiveMatchingEnabled { get; set; }

        [JsonPropertyName("testsV2Enabled")]
        public bool TestsV2Enabled { get; set; }

        [JsonPropertyName("testItemExamplesEnabled")]
        public bool TestItemExamplesEnabled { get; set; }

        [JsonPropertyName("testProgressInsightEnabled")]
        public bool TestProgressInsightEnabled { get; set; }

        [JsonPropertyName("employerFitQuestionnairesEnabled")]
        public bool EmployerFitQuestionnairesEnabled { get; set; }

        [JsonPropertyName("kandidatenkaartEnabled")]
        public bool KandidatenkaartEnabled { get; set; }

        [JsonPropertyName("companyMapInitiativeEnabled")]
        public bool CompanyMapInitiativeEnabled { get; set; } = true;

        [JsonPropertyName("afterMatchEnabled")]
        public bool AfterMatchEnabled { get; set; }

        [JsonPropertyName("referenceVerificationEnabled")]
        public bool ReferenceVerificationEnabled { get; set; }

        [JsonPropertyName("sensitiveExperienceEnabled")]
        public bool SensitiveExperienceEnabled { get; set; }

        [JsonPropertyName("trainingPartnersEnabled")]
        public bool TrainingPartnersEnabled { get; set; }

        [JsonPropertyName("pupilPassportV2Enabled")]
        public bool PupilPassportV2Enabled { get; set; }

        [JsonPropertyName("studentPassportEnabled")]
        public bool StudentPassportEnabled { get; set; }

        [JsonPropertyName("testJourneyIllustrationEnabled")]
        public bool TestJourneyIllustrationEnabled { get; set; }

        [JsonPropertyName("onboardingFinaleV2Enabled")]
        public bool OnboardingFinaleV2Enabled { get; set; }

        [JsonPropertyName("occupationDayInLifeEnabled")]
        public bool OccupationDayInLifeEnabled { get; set; } = true;

        [JsonPropertyName("testEditModeEnabled")]
        public bool TestEditModeEnabled { get; set; }

        [JsonPropertyName("samplePdfEnabled")]
        public bool SamplePdfEnabled { get; set; }

        [JsonPropertyName("testDepthValueMockupEnabled")]
        public bool TestDepthValueMockupEnabled { get; set; } = true;

        [JsonPropertyName("uwvFeaturesEnabled")]
        public bool UwvFeaturesEnabled { get; set; } = true;
    }
}
