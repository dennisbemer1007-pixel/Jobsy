using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CompetencyMatchingTests
{
    [Fact]
    public void Catalog_has_20_items_five_per_big_five_competency()
    {
        Assert.Equal(20, CompetencyTestCatalog.QuestionCount);
        Assert.Equal(20, CompetencyTestCatalog.Questions.Count);
        Assert.Equal(4, CompetencyTestCatalog.CategoryCodes.Length);
        foreach (var category in CompetencyTestCatalog.CategoryCodes)
        {
            Assert.Equal(5, CompetencyTestCatalog.Questions.Count(q => q.Category == category));
        }

        Assert.Contains(CompetencyTestCatalog.Questions, q => q.Reverse);
        Assert.True(CompetencyTestCatalog.Questions.Count(q => q.Reverse) >= 8);
    }

    [Fact]
    public void Score_maps_likert_to_0_100_and_reverses_items()
    {
        var high = Enumerable.Range(1, 20).ToDictionary(
            i => i,
            i => CompetencyTestCatalog.Questions.First(q => q.Id == i).Reverse ? 1 : 5);
        var scores = CompetencyTestCatalog.Score(high);
        Assert.NotNull(scores);
        Assert.True(scores!.IsComplete);
        Assert.Equal(100, scores.Samenwerken);
        Assert.Equal(100, scores.Resultaatgerichtheid);
        Assert.Equal(100, scores.Stressbestendigheid);
        Assert.Equal(100, scores.Innovatie);

        var low = Enumerable.Range(1, 20).ToDictionary(
            i => i,
            i => CompetencyTestCatalog.Questions.First(q => q.Id == i).Reverse ? 5 : 1);
        var lowScores = CompetencyTestCatalog.Score(low)!;
        Assert.Equal(0, lowScores.Samenwerken);
        Assert.Equal(0, lowScores.Innovatie);
    }

    [Fact]
    public void Draft_may_be_partial_complete_requires_all_20()
    {
        var partial = new Dictionary<int, int> { [1] = 4, [2] = 3 };
        Assert.Null(CompetencyTestCatalog.ValidateAnswers(partial, requireComplete: false));
        Assert.NotNull(CompetencyTestCatalog.ValidateAnswers(partial, requireComplete: true));
        Assert.False(CompetencyTestCatalog.IsComplete(partial));

        var full = Enumerable.Range(1, 20).ToDictionary(i => i, _ => 3);
        Assert.Null(CompetencyTestCatalog.ValidateAnswers(full, requireComplete: true));
        Assert.True(CompetencyTestCatalog.IsComplete(full));
        Assert.Equal(50, CompetencyTestCatalog.Score(full)!.Samenwerken);
    }

    [Fact]
    public void Vacancy_profile_boosts_horeca_stress_and_teamwork()
    {
        var profile = VacancyCompetencyProfile.Infer(["Horeca"], "Avondhulp in het team", "Drukke piekavonden, samenwerken in de keuken.");
        Assert.True(profile.Samenwerken >= 80);
        Assert.True(profile.Stressbestendigheid >= 85);
        Assert.True(profile.Innovatie < profile.Stressbestendigheid);
    }

    [Fact]
    public void Top_matches_are_sorted_desc_capped_at_10_and_hide_below_60()
    {
        var inputs = new List<ProfileVacancyMatchInput>();
        for (var i = 0; i < 12; i++)
        {
            var strong = i < 8;
            inputs.Add(MakeInput(
                Guid.Parse($"c1000000-0000-0000-0000-0000000000{i:00}"),
                $"Vacature {i:00}",
                travelMinutes: strong ? 10 : 90,
                maxTravel: 30,
                candidateHours: strong ? new HoursRange(16, 24) : new HoursRange(4, 8),
                vacancyHours: new HoursRange(16, 24),
                competencies: strong ? new CompetencyScores(80, 80, 80, 80) : null));
        }

        var ranked = ProfileVacancyMatchCalculator.Rank(inputs);
        Assert.True(ranked.Count <= 10);
        Assert.NotEmpty(ranked);
        Assert.All(ranked, m => Assert.True(m.TotalPercent >= 60));
        for (var i = 1; i < ranked.Count; i++)
        {
            Assert.True(ranked[i - 1].TotalPercent >= ranked[i].TotalPercent);
        }
    }

    [Fact]
    public void Explanation_covers_experience_and_competency_fit_and_gap()
    {
        var match = ProfileVacancyMatchCalculator.Calculate(MakeInput(
            Guid.NewGuid(),
            "Vulploegmedewerker",
            travelMinutes: 12,
            maxTravel: 30,
            candidateHours: new HoursRange(12, 20),
            vacancyHours: new HoursRange(16, 24),
            competencies: new CompetencyScores(90, 40, 50, 30),
            workTypes: ["Winkel"],
            candidateRoles: ["Winkel"],
            vacancyDescription: "Drukke winkel, samenwerken in het team."));

        Assert.True(match.TotalPercent >= 50);
        Assert.Contains(match.Why, w => w.Kind is "experience" or "competency" or "travel" or "hours");
        Assert.Contains(match.Gaps, g => g.Kind == "competency");
        Assert.Contains(match.Why, w => w.Text.Contains("winkel", StringComparison.OrdinalIgnoreCase)
                                        || w.Text.Contains("reistijd", StringComparison.OrdinalIgnoreCase)
                                        || w.Text.Contains("Samenwerken", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(match.Gaps, g => g.Text.Contains("uitdaging", StringComparison.OrdinalIgnoreCase)
                                         || g.Text.Contains("nog niet", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Completed_scores_helper_ignores_drafts()
    {
        Assert.Null(CompetencyTestCatalog.CompletedScoresOrNull("Draft", 80, 80, 80, 80));
        var done = CompetencyTestCatalog.CompletedScoresOrNull("Completed", 70, 60, 50, 40);
        Assert.NotNull(done);
        Assert.Equal(70, done!.Samenwerken);
    }

    private static ProfileVacancyMatchInput MakeInput(
        Guid id,
        string title,
        int travelMinutes,
        int maxTravel,
        HoursRange candidateHours,
        HoursRange vacancyHours,
        CompetencyScores? competencies,
        IReadOnlyList<string>? workTypes = null,
        IReadOnlyList<string>? candidateRoles = null,
        string? vacancyDescription = null)
        => new()
        {
            VacancyId = id,
            VacancyTitle = title,
            VacancyDescription = vacancyDescription,
            WorkTypes = workTypes ?? ["Winkel"],
            CandidateRoles = candidateRoles ?? ["Winkel"],
            CandidateLicenses = [],
            CandidateEducations = [],
            CandidateEmployerCount = 1,
            CandidateCompetencies = competencies,
            VacancyCompetencies = VacancyCompetencyProfile.Infer(workTypes ?? ["Winkel"], title, vacancyDescription),
            Core = new MatchScoreInput
            {
                EstimatedTravelMinutes = travelMinutes,
                MaxTravelMinutes = maxTravel,
                CandidateHours = candidateHours,
                VacancyHours = vacancyHours,
                VacancySchedule = SchedulePayload.Flexible(FlexibleScheduleSource.Manual),
                CandidateSchedule = new SchedulePayload
                {
                    Slots = new Dictionary<string, List<string>> { ["Ma"] = ["Ochtend"] }
                }.Normalize(),
                CandidateAgeYears = 22
            }
        };
}
