using Jobsy.Core.Contracts;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Xunit;

namespace Jobsy.Tests;

public sealed class CoachTonePreferenceTests
{
    [Fact]
    public void Normalize_rejects_unknown_keys()
    {
        var raw = new CoachTonePreferencesDto(
            Answers: new Dictionary<string, string[]>
            {
                ["certainty_helps"] = ["duidelijke_volgende_stap"],
                ["unknown"] = ["x"],
            });
        var normalized = CoachTonePreferenceRules.Normalize(raw);
        Assert.NotNull(normalized);
        Assert.True(normalized!.Answers!.ContainsKey("certainty_helps"));
        Assert.False(normalized.Answers.ContainsKey("unknown"));
    }

    [Fact]
    public void Wipe_clears_all_answers()
    {
        var wiped = CoachTonePreferenceRules.Wipe();
        Assert.Empty(wiped.Answers!);
    }

    [Fact]
    public void BuildToneAddendum_lists_numbered_steps_for_certainty()
    {
        var prefs = new CoachTonePreferencesDto(
            Answers: new Dictionary<string, string[]>
            {
                ["certainty_helps"] = ["duidelijke_volgende_stap"],
                ["thinking_learn"] = [CoachToneCatalog.ThinkingStappen],
            });
        var addendum = CoachTonePreferenceRules.BuildToneAddendum(prefs);
        Assert.Contains("numbered", addendum, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bullet", addendum, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildToneAddendum_empty_without_answers()
    {
        Assert.Equal(string.Empty, CoachTonePreferenceRules.BuildToneAddendum(null));
    }

    [Fact]
    public void WithoutCoachTone_strips_from_preferences_dto()
    {
        var dto = new CandidatePreferencesDto([], null, null, CoachTonePreferences: new CoachTonePreferencesDto(
            Answers: new Dictionary<string, string[]> { ["certainty_helps"] = ["duidelijke_volgende_stap"] }));
        var stripped = CoachTonePreferenceRules.WithoutCoachTone(dto);
        Assert.Null(stripped.CoachTonePreferences);
    }

    [Fact]
    public void MatchingProfileMapper_does_not_surface_coach_tone()
    {
        var json = """
                   {"roles":[],"coachTonePreferences":{"version":1,"answers":{"certainty_helps":["duidelijke_volgende_stap"]}}}
                   """;
        var prefs = MatchingProfileMapper.DeserializePrefs(json);
        Assert.Null(prefs.CoachTonePreferences);
    }

    [Fact]
    public void EmployerFacing_strips_coach_tone_from_json_summary()
    {
        var dto = new CandidatePreferencesDto([], null, null, CoachTonePreferences: new CoachTonePreferencesDto(
            Answers: new Dictionary<string, string[]> { ["thinking_learn"] = [CoachToneCatalog.ThinkingPlaatje] }));
        var employer = EmployerFacingCandidateProfile.FilterPreferences(dto);
        Assert.Null(employer.CoachTonePreferences);
    }

    [Fact]
    public void FeatureFlag_defaults_off_in_snapshot()
    {
        var snap = FeatureFlagSnapshot.Defaults;
        Assert.False(snap.CandidateCoachTonePreferencesEnabled);
        Assert.False(snap.IsEnabled(PlatformFeature.CandidateCoachTonePreferences));
    }

    [Fact]
    public void Passport_ssr_shells_reference_coach_tone_when_flag_on()
    {
        var root = FindRepoRoot();
        var shells = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Passport/PassportSsrShells.cs"));
        Assert.Contains("CandidateCoachTonePreferencesEnabled", shells, StringComparison.Ordinal);
        Assert.Contains("PassportCoachToneSsrHtml", shells, StringComparison.Ordinal);
        Assert.Contains("passport-coach-tone", shells, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        throw new InvalidOperationException("Jobsy.sln not found");
    }
}
