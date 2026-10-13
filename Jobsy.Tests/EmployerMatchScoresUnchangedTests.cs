using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Tests.Uat;

namespace Jobsy.Tests;

/// <summary>
/// 04.6 — employer-side scores must stay byte-identical to the pre-calibration calculator.
/// </summary>
public class EmployerMatchScoresUnchangedTests
{
    // Golden TotalPercent values captured for fixed inputs (calculator weights unchanged).
    // Re-capture only if ProfileVacancyMatchCalculator weights intentionally change.
    private static readonly (string Key, int TotalPercent)[] Goldens =
    [
        ("near-strong", 91),
        ("far-weak", 64),
        ("mid-competency", 87)
    ];

    [Fact]
    public void StrongMatchThreshold_employer_remains_70()
        => Assert.Equal(70, MatchScoreWeights.StrongMatchThreshold);

    [Fact]
    public void Candidate_strong_threshold_is_separate_75()
        => Assert.Equal(75, CandidateFitDisplay.StrongThreshold);

    [Fact]
    public void Calculator_TotalPercent_matches_golden_values()
    {
        var actual = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["near-strong"] = Calculate("Barista", 8, 30, 85).TotalPercent,
            ["far-weak"] = Calculate("Ver", 90, 20, null).TotalPercent,
            ["mid-competency"] = Calculate("Winkel", 18, 30, 70).TotalPercent
        };

        foreach (var (key, expected) in Goldens)
        {
            Assert.True(actual.ContainsKey(key), key);
            Assert.Equal(expected, actual[key]);
        }
    }

    [Fact]
    public void ApplicationsController_still_stores_raw_TotalPercent()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Api/Controllers/ApplicationsController.cs"));
        Assert.Contains("application.MatchPercent = match.TotalPercent", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Snapshot_service_still_uses_raw_match_computation()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Infrastructure/Services/CandidateMatchSnapshotService.cs"));
        Assert.DoesNotContain("CandidateFitDisplay", src, StringComparison.Ordinal);
        Assert.DoesNotContain("FitPercent", src, StringComparison.Ordinal);
    }

    private static ProfileVacancyMatch Calculate(string title, int travel, int maxTravel, int? competency)
        => ProfileVacancyMatchCalculator.Calculate(new ProfileVacancyMatchInput
        {
            VacancyId = Guid.NewGuid(),
            VacancyTitle = title,
            VacancyDescription = "Team, samenwerken.",
            WorkTypes = ["Winkel"],
            CandidateRoles = ["Winkel"],
            CandidateCompetencies = competency is int c
                ? new CompetencyScores(c, c, c, c)
                : null,
            Core = new MatchScoreInput
            {
                EstimatedTravelMinutes = travel,
                MaxTravelMinutes = maxTravel,
                CandidateHours = new HoursRange(16, 24),
                VacancyHours = new HoursRange(16, 24),
                VacancySchedule = SchedulePayload.Flexible(FlexibleScheduleSource.Manual),
                CandidateSchedule = new SchedulePayload
                {
                    Slots = new Dictionary<string, List<string>> { ["Ma"] = ["Ochtend"] }
                }.Normalize(),
                CandidateAgeYears = 22
            }
        });
}
