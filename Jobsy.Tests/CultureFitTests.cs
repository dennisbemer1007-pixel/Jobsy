using Jobsy.Core.Rules;
using Jobsy.Tests.Uat;

namespace Jobsy.Tests;

public class CultureFitTests
{
    [Fact]
    public void Catalog_normalizes_unique_known_pillars_and_rejects_short_sets()
    {
        Assert.Equal(["informeel", "groei", "stabiel"], CulturePillarCatalog.Normalize(
            [" informeel ", "GROEI", "unknown", "stabiel", "informeel"]));
        Assert.False(CulturePillarCatalog.HasProfile(["informeel", "groei"]));
        Assert.True(CulturePillarCatalog.HasProfile(["informeel", "groei", "stabiel"]));
        Assert.Equal(3, CulturePillarCatalog.Deserialize("""["informeel","groei","stabiel"]""").Count);
        Assert.Empty(CulturePillarCatalog.Deserialize("not-json"));
    }

    [Fact]
    public void Informal_high_extraversion_scores_high_without_jargon()
    {
        var result = CultureFitBuilder.Evaluate(
            ["informeel", "samen", "kalm"],
            new CompetencyScores(85, 55, 80, 50, 88));
        Assert.NotNull(result);
        Assert.InRange(result!.Percent, 70, 100);
        Assert.Equal("high", result.Band);
        Assert.Equal("Cultuur Fit: Hoog", result.Label);
        Assert.False(string.IsNullOrWhiteSpace(result.Why));
        Assert.False(CareerCompassBuilder.ContainsForbiddenJargon(result.Why));
        Assert.False(CareerCompassBuilder.ContainsForbiddenJargon(CultureFitPrompt.User(
            CulturePillarCatalog.Labels(["informeel", "samen", "kalm"]),
            new CompetencyScores(85, 55, 80, 50, 88))));
        Assert.Contains("cultureFitPercent", CultureFitPrompt.System, StringComparison.Ordinal);
        Assert.Contains("OCEAN", CultureFitPrompt.System, StringComparison.Ordinal);
    }

    [Fact]
    public void Structured_team_mismatches_a_highly_exploratory_profile()
    {
        var result = CultureFitBuilder.Evaluate(
            ["stabiel", "zorgvuldig", "zelfstandig"],
            new CompetencyScores(40, 40, 40, 95, 80));
        Assert.NotNull(result);
        Assert.True(result!.Percent < 75);
        Assert.False(CareerCompassBuilder.ContainsForbiddenJargon(result.Why));
    }

    [Fact]
    public void Hard_criteria_gate_skips_culture_until_core_match()
    {
        var weak = MakeInput(travelMinutes: 90, maxTravel: 20, competencies: new CompetencyScores(90, 90, 90, 90, 90));
        var weakMatch = ProfileVacancyMatchCalculator.Calculate(weak);
        Assert.Null(weakMatch.CultureFit);

        var strong = MakeInput(travelMinutes: 10, maxTravel: 30, competencies: new CompetencyScores(90, 90, 90, 90, 90));
        var strongMatch = ProfileVacancyMatchCalculator.Calculate(strong);
        Assert.NotNull(strongMatch.CultureFit);
        Assert.Contains(strongMatch.Why.Concat(strongMatch.Gaps), p => p.Kind == "culture");
        Assert.True(strongMatch.TotalPercent >= 50);
    }

    [Fact]
    public void OpenAi_json_is_clamped_to_local_score()
    {
        var local = new CultureFitResult(80, "high", "Cultuur Fit: Hoog", "Sluit goed aan bij het team.", false);
        var parsed = CultureFitJson.TryParse("""{"cultureFitPercent":10,"why":"Dit team is veel te wild en chaotisch voor jou."}""", local);
        Assert.NotNull(parsed);
        Assert.True(parsed!.FromOpenAi);
        Assert.InRange(parsed.Percent, 68, 80);
        Assert.Contains("wild", parsed.Why, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_form_and_map_wire_culture_ui()
    {
        var root = RepoRoot.Find();
        var create = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Branch/CreateVacancy.razor"));
        Assert.Contains("Employer.Culture.Legend", create, StringComparison.Ordinal);
        Assert.Contains("CulturePillars:", create, StringComparison.Ordinal);

        var discovery = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/VacancyDiscovery.razor"));
        Assert.Contains("CultureFitLabel", discovery, StringComparison.Ordinal);
        Assert.Contains("cultureFitLabel", discovery, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/VacancyDetail.razor"));
        Assert.Contains("CultureFitLabel", detail, StringComparison.Ordinal);

        Assert.Contains("ICultureFitAiService", File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/DependencyInjection.cs")), StringComparison.Ordinal);
        Assert.Contains("CulturePillarsJson", File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Data/JobsyDbContext.cs")), StringComparison.Ordinal);
    }

    private static ProfileVacancyMatchInput MakeInput(int travelMinutes, int maxTravel, CompetencyScores competencies)
        => new()
        {
            VacancyId = Guid.NewGuid(),
            VacancyTitle = "Vulploeg",
            WorkTypes = ["Winkel"],
            CandidateRoles = ["Winkel"],
            CandidateLicenses = [],
            CandidateEducations = [],
            CandidateEmployerCount = 1,
            CandidateCompetencies = competencies,
            CulturePillars = ["informeel", "samen", "kalm"],
            Core = new MatchScoreInput
            {
                EstimatedTravelMinutes = travelMinutes,
                MaxTravelMinutes = maxTravel,
                CandidateHours = new HoursRange(16, 24),
                VacancyHours = new HoursRange(16, 24),
                VacancySchedule = SchedulePayload.Flexible(FlexibleScheduleSource.Manual),
                CandidateAgeYears = 28
            }
        };
}
