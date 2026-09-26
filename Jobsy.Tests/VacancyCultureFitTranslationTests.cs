using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Jobsy.Tests.Uat;

namespace Jobsy.Tests;

public class VacancyCultureFitTranslationTests
{
    private static readonly CompetencyScores Competency = new(85, 55, 80, 50, 88);
    private static readonly CulturePersonalityScores Culture = new(
        70, 60, 80, 55, 50, 65, 55, 70, 60, 75, 70);

    [Fact]
    public void Culture_fit_fingerprint_changes_only_on_relevant_input()
    {
        var pillars = new[] { "informeel", "samen", "kalm" };
        var a = CultureFitFingerprint.For(pillars, Competency, Culture);
        var b = CultureFitFingerprint.For(pillars, Competency, Culture);
        Assert.Equal(a, b);

        var scoreChange = CultureFitFingerprint.For(
            pillars,
            Competency with { Samenwerken = 40 },
            Culture);
        Assert.NotEqual(a, scoreChange);

        var pillarChange = CultureFitFingerprint.For(["stabiel", "zorgvuldig", "zelfstandig"], Competency, Culture);
        Assert.NotEqual(a, pillarChange);

        var cultureChange = CultureFitFingerprint.For(pillars, Competency, Culture with { Autonomy = 10 });
        Assert.NotEqual(a, cultureChange);
    }

    [Fact]
    public void Translation_source_hash_invalidates_when_dutch_text_changes()
    {
        var a = VacancyTranslationHash.ForSource("Barista", "Koffie zetten in Den Haag");
        var b = VacancyTranslationHash.ForSource("Barista", "Koffie zetten in Den Haag");
        Assert.Equal(a, b);
        Assert.NotEqual(a, VacancyTranslationHash.ForSource("Barista", "Thee zetten in Den Haag"));
        Assert.NotEqual(a, VacancyTranslationHash.ForSource("Chef", "Koffie zetten in Den Haag"));
    }

    [Fact]
    public void Culture_fit_fallback_retry_reuses_one_hour_rule()
    {
        var now = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(CultureFitFingerprint.ShouldRetryFallback(false, now.AddMinutes(-30), now));
        Assert.True(CultureFitFingerprint.ShouldRetryFallback(false, now.AddMinutes(-90), now));
        Assert.False(CultureFitFingerprint.ShouldRetryFallback(true, now.AddHours(-2), now));
    }

    [Fact]
    public void Culture_fit_refine_queue_deduplicates_pairs()
    {
        var queue = new CultureFitRefineQueue();
        var user = Guid.NewGuid();
        var vacancy = Guid.NewGuid();
        Assert.True(queue.TryEnqueue(user, vacancy));
        Assert.False(queue.TryEnqueue(user, vacancy));
        Assert.True(queue.TryEnqueue(user, Guid.NewGuid()));
        queue.MarkCompleted(user, vacancy);
        Assert.True(queue.TryEnqueue(user, vacancy));
    }

    [Fact]
    public void Vacancy_detail_loads_once_and_polls_culture_fit()
    {
        var root = RepoRoot.Find();
        var detail = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/VacancyDetail.razor"));
        Assert.Contains("RefreshCultureFitLaterAsync", detail, StringComparison.Ordinal);
        Assert.Contains("GetVacancyCultureFitAsync", detail, StringComparison.Ordinal);
        Assert.Contains("RefreshTravelAsync", detail, StringComparison.Ordinal);
        Assert.Contains("Exactly one full vacancy load", detail, StringComparison.Ordinal);
        Assert.Contains("Insights.Updating", detail, StringComparison.Ordinal);
        // No second full LoadAsync after likes/age.
        Assert.DoesNotContain("await LoadAsync(origin);", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Training_offers_fetch_only_when_inputs_change()
    {
        var root = RepoRoot.Find();
        var block = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/TrainingOffersBlock.razor"));
        Assert.Contains("_lastFetchKey", block, StringComparison.Ordinal);
        Assert.Contains("string.Equals(key, _lastFetchKey", block, StringComparison.Ordinal);
    }

    [Fact]
    public void Translation_service_no_longer_uses_process_dictionary_cache()
    {
        var root = RepoRoot.Find();
        var src = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/OpenAiTranslationService.cs"));
        Assert.DoesNotContain("ConcurrentDictionary", src, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxCacheEntries", src, StringComparison.Ordinal);
        Assert.Contains("VacancyTranslations", src, StringComparison.Ordinal);
        Assert.Contains("IMemoryCache", src, StringComparison.Ordinal);
        Assert.Contains("TranslateVacanciesBatchAsync", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Vacancy_get_uses_stored_culture_fit_service_not_inline_ai()
    {
        var root = RepoRoot.Find();
        var controller = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/VacanciesController.cs"));
        Assert.Contains("ICandidateVacancyCultureFitService", controller, StringComparison.Ordinal);
        Assert.Contains("ResolveForGetAsync", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("_cultureFitAi.TryRefineAsync", controller, StringComparison.Ordinal);
        Assert.Contains("InvalidateForVacancyAsync", controller, StringComparison.Ordinal);
        Assert.Contains("InvalidateVacancyAsync", controller, StringComparison.Ordinal);
        Assert.Contains("/culture-fit", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Applications_and_shares_use_batch_translation()
    {
        var root = RepoRoot.Find();
        var me = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/MeController.cs"));
        Assert.Contains("TranslateVacanciesBatchAsync", me, StringComparison.Ordinal);
        Assert.Contains("VacancyTranslationRequest", me, StringComparison.Ordinal);
    }
}
