using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public class CandidateInsightsFingerprintTests
{
    private static readonly CompetencyScores Competency = new(80, 70, 75, 40, 55);
    private static readonly RiasecScores Career = new(20, 30, 25, 95, 40, 35);
    private static readonly CulturePersonalityScores Culture = new(
        70, 60, 80, 55, 50, 65, 55, 70, 60, 75, 70);
    private static readonly SchwartzValuesScores Values = new(60, 70, 50, 55, 65);
    private static readonly CandidatePreferencesDto Prefs = new(
        ["Zorg"], 30, "Fiets", AboutMe: "Ik werk graag in de zorg.");

    [Fact]
    public void Match_fingerprint_changes_when_scores_change()
    {
        var a = CandidateInsightsFingerprint.ForMatches(Competency, Career, Culture, Values, Prefs, null);
        var b = CandidateInsightsFingerprint.ForMatches(
            Competency with { Samenwerken = 40 }, Career, Culture, Values, Prefs, null);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Match_fingerprint_changes_when_travel_preference_changes()
    {
        var a = CandidateInsightsFingerprint.ForMatches(Competency, Career, Culture, Values, Prefs, null);
        var other = Prefs with { MaxTravelMinutes = 60 };
        var b = CandidateInsightsFingerprint.ForMatches(Competency, Career, Culture, Values, other, null);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Match_fingerprint_stable_for_identical_input()
    {
        var a = CandidateInsightsFingerprint.ForMatches(Competency, Career, Culture, Values, Prefs, null);
        var b = CandidateInsightsFingerprint.ForMatches(Competency, Career, Culture, Values, Prefs, null);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Compass_fingerprint_changes_when_riasec_or_deep_flag_changes()
    {
        var a = CandidateInsightsFingerprint.ForCompass(Career, fromDeepAnalysis: false);
        var b = CandidateInsightsFingerprint.ForCompass(Career, fromDeepAnalysis: true);
        var c = CandidateInsightsFingerprint.ForCompass(Career with { Social = 10 }, fromDeepAnalysis: false);
        Assert.NotEqual(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void Role_fit_fingerprint_changes_when_job_title_changes()
    {
        var a = CandidateInsightsFingerprint.ForRoleFit("Verpleegkundige", null, Competency, Career, Culture, Prefs);
        var b = CandidateInsightsFingerprint.ForRoleFit("Magazijnmedewerker", null, Competency, Career, Culture, Prefs);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void WhoAmI_fingerprint_changes_only_on_relevant_input()
    {
        var highlights = WhoAmIProfileHighlights.FromPreferences(Prefs);
        var a = WhoAmICompleteness.Fingerprint(Competency, Career, Culture, highlights, Values);
        var b = WhoAmICompleteness.Fingerprint(
            Competency with { Innovatie = 10 }, Career, Culture, highlights, Values);
        Assert.NotEqual(a, b);
        Assert.Equal(a, WhoAmICompleteness.Fingerprint(Competency, Career, Culture, highlights, Values));
    }

    [Theory]
    [InlineData(false, -30, false)]
    [InlineData(false, -90, true)]
    [InlineData(true, -120, false)]
    [InlineData(false, null, false)]
    public void Fallback_retry_after_one_hour_only(bool fromOpenAi, int? minutesAgo, bool expected)
    {
        var now = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);
        DateTime? generated = minutesAgo is int m ? now.AddMinutes(m) : null;
        Assert.Equal(expected, CandidateInsightsFingerprint.ShouldRetryFallback(fromOpenAi, generated, now));
    }

    [Fact]
    public void Queue_deduplicates_pending_users()
    {
        var queue = new CandidateInsightsQueue();
        var id = Guid.NewGuid();
        Assert.True(queue.TryEnqueue(id));
        Assert.False(queue.TryEnqueue(id));
        queue.MarkCompleted(id);
        Assert.True(queue.TryEnqueue(id));
    }

    [Fact]
    public void Queue_rejects_empty_guid()
    {
        Assert.False(new CandidateInsightsQueue().TryEnqueue(Guid.Empty));
    }
}
