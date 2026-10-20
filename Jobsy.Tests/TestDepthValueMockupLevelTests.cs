using System.Text.RegularExpressions;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Features;
using Jobsy.Core.Reports;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Rules;
using Jobsy.Core.Entities;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Admin;
using Jobsy.Web.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jobsy.Tests;

/// <summary>
/// Production gate for test-depth mockup: levels 5/10/25/150, words-only copy, NL+EN strings.
/// </summary>
public sealed class TestDepthValueMockupLevelTests
{
    private static readonly Regex ScoreWord = new(@"\bscore\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private const int QuickScanV3 = AssessmentTestVersions.V3;

    private static (Dictionary<string, string> Nl, Dictionary<string, string> En) LoadTestResultStrings()
    {
        var nl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var en = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ro = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ar = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        UiStringsTestResults.MergeAll(nl, en, pl, ro, ar);
        return (nl, en);
    }

    private static void AssertCandidateCopy(string text, string context)
    {
        Assert.False(string.IsNullOrWhiteSpace(text), context);
        Assert.DoesNotContain('%', text);
        Assert.False(ScoreWord.IsMatch(text), $"{context} must not contain the word 'score'. Text: {text}");
        Assert.DoesNotContain('—', text);
    }

    public static TheoryData<int, TestDepthLevel, TestDepthLevel?> LevelCases { get; } = new()
    {
        { 5, TestDepthLevel.First, TestDepthLevel.Deeper },
        { 10, TestDepthLevel.Deeper, TestDepthLevel.Full },
        { 25, TestDepthLevel.Full, null },
        { 150, TestDepthLevel.Full, null },
    };

    [Theory]
    [MemberData(nameof(LevelCases))]
    public void Depth_level_reached_and_preview_match_milestones(
        int answered,
        TestDepthLevel expectedReached,
        TestDepthLevel? expectedNext)
    {
        var reached = TestDepthRules.Reached(AssessmentKind.Competence, answered, QuickScanV3);
        var next = TestDepthRules.Next(AssessmentKind.Competence, answered, QuickScanV3);
        Assert.Equal(expectedReached, reached);
        Assert.Equal(expectedNext, next);

        if (next is not null)
        {
            var preview = TestDepthValuePreviewCopy.NextPreview(next, QuickScanV3);
            Assert.NotNull(preview);
            AssertCandidateCopy(preview!.PromiseNl, $"preview NL n{answered}");
            AssertCandidateCopy(preview.PromiseEn, $"preview EN n{answered}");
            Assert.NotEmpty(preview.ThemeTitleKeys);

            if (next == TestDepthLevel.Full)
            {
                Assert.True(preview.FullMirrorSections);
            }
        }

        var themes = TestDepthValueInsight.MiniThemeCount(answered, QuickScanV3);
        if (answered is >= 5 and < 25)
        {
            Assert.True(themes is 1 or 3);
        }
    }

    [Fact]
    public void Level150_deep_report_words_only_has_no_score_or_percent()
    {
        var deepAnswers = Enumerable.Range(1, 150).ToDictionary(i => i, i =>
        {
            var band = (i - 1) / 30;
            var bias = band switch { 0 => 4, 1 => 2, 2 => 5, 3 => 1, _ => 3 };
            return ((i * 11 + bias * 17) % 5) + 1;
        });

        var report = CompetenceDeepReportBuilder.Build(
            deepAnswers,
            new FakeNormProvider(available: true),
            jobTitle: null,
            occupations: null,
            aiSummary: null,
            aiPlan: null,
            fromOpenAi: false,
            generatedAtUtc: DateTime.UtcNow);

        CompetenceDeepReportTexts.ApplyCandidateWordsOnly(report);

        AssertCandidateCopy(report.Summary, "deep summary");
        foreach (var step in report.ActionPlan)
        {
            AssertCandidateCopy(step.Title, "action title");
            AssertCandidateCopy(step.Body, "action body");
        }

        foreach (var occ in report.Occupations)
        {
            AssertCandidateCopy(occ.Reason, $"occupation {occ.Title}");
        }
    }

    [Fact]
    public void Mini_spiegel_copy_nl_and_en_are_words_only_for_all_domains()
    {
        foreach (var code in new[]
                 {
                     CompetencyTestCatalog.Samenwerken,
                     CompetencyTestCatalog.Resultaatgerichtheid,
                     CompetencyTestCatalog.Stressbestendigheid,
                     CompetencyTestCatalog.Innovatie,
                     CompetencyTestCatalog.Extraversie
                 })
        {
            var nl = TestDepthValueMiniThemeCopy.TryGet(code, "nl");
            var en = TestDepthValueMiniThemeCopy.TryGet(code, "en");
            Assert.NotNull(nl);
            Assert.NotNull(en);
            foreach (var (label, text) in new[] { ("nl-meaning", nl!.Meaning), ("nl-example", nl.Example), ("nl-energy", nl.Energy) })
            {
                AssertCandidateCopy(text, $"{code} {label}");
            }

            foreach (var (label, text) in new[] { ("en-meaning", en!.Meaning), ("en-example", en.Example), ("en-energy", en.Energy) })
            {
                AssertCandidateCopy(text, $"{code} {label}");
            }
        }
    }

    [Fact]
    public void TestDepthValue_ui_strings_nl_and_en_are_words_only()
    {
        var (nl, en) = LoadTestResultStrings();
        var keys = nl.Keys.Where(k => k.StartsWith("TestDepthValue.", StringComparison.Ordinal)).OrderBy(k => k).ToList();
        Assert.NotEmpty(keys);
        foreach (var key in keys)
        {
            Assert.True(en.ContainsKey(key), $"Missing EN for {key}");
            AssertCandidateCopy(nl[key], $"NL {key}");
            AssertCandidateCopy(en[key], $"EN {key}");
        }
    }

    [Fact]
    public void Competence_locked_card_title_has_no_score_wording_nl_en()
    {
        var (nl, en) = LoadTestResultStrings();
        AssertCandidateCopy(nl["TestResult.Card.Facets.Competence"], "NL facets competence card");
        AssertCandidateCopy(en["TestResult.Card.Facets.Competence"], "EN facets competence card");
        var card = DeepReportCapabilities.For(AssessmentKind.Competence).Cards
            .First(c => c.Key == DeepReportCardKey.Facets);
        Assert.Equal("TestResult.Card.Facets.Competence", card.TitleKey);
    }

    [Fact]
    public void TestDepthValueMockup_flag_is_admin_switchable_and_on_public_api()
    {
        var def = Assert.Single(
            FeatureFlagRegistry.All,
            d => d.Key == "TestDepthValueMockupEnabled");
        Assert.Equal(PlatformFeature.TestDepthValueMockup, def.PlatformFeature);
        Assert.True(def.DefaultValue);
        Assert.Contains(
            PlatformSettingsCatalog.Entries.Select(e => e.Key),
            k => k == "TestDepthValueMockupEnabled");
        Assert.Contains(
            FeatureFlagRegistry.PublicEndpointFlags.Select(d => d.Key),
            k => k == "TestDepthValueMockupEnabled");
    }

    [Fact]
    public void PlatformFeatureSettings_test_depth_mockup_defaults_on()
    {
        Assert.True(new PlatformFeatureSettings().TestDepthValueMockupEnabled);
        Assert.True(FeatureFlagSnapshot.Defaults.TestDepthValueMockupEnabled);
        Assert.True(FeatureFlagSnapshot.Defaults.IsEnabled(PlatformFeature.TestDepthValueMockup));
    }

    [Fact]
    public async Task Admin_can_turn_test_depth_mockup_off()
    {
        await using var db = CreateFeatureDb();
        var entry = PlatformSettingsCatalog.Entries.Single(e => e.Key == "TestDepthValueMockupEnabled");
        var sut = CreateFeatureService(db);

        var snap = await sut.GetAsync();
        Assert.True(snap.TestDepthValueMockupEnabled);
        Assert.True(entry.Read(snap) is true);

        snap = await sut.UpdateAsync(entry.Write(false));
        Assert.False(snap.TestDepthValueMockupEnabled);
        Assert.False(entry.Read(snap) is true);

        var again = await sut.GetAsync();
        Assert.False(again.TestDepthValueMockupEnabled);
    }

    private static JobsyDbContext CreateFeatureDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static PlatformFeatureService CreateFeatureService(JobsyDbContext db)
        => new(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "http://localhost:5201",
                ["TestDepthValueMockupEnabled"] = "true"
            }).Build());

    private sealed class FakeNormProvider(bool available) : INormProvider
    {
        public bool IsAvailable => available;
        public BigFiveNormSet? Norms => null;
        public string? BandLabel(double score, string traitOrFacetKey) => "Vergelijkbaar";
        public double? Mean(string traitOrFacetKey) => 50.0;
        public string SourceDisclaimer => "Test.";
    }
}
