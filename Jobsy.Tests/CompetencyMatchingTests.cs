using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Jobsy.Tests.Uat;

namespace Jobsy.Tests;

public class CompetencyMatchingTests
{
    [Fact]
    public void Catalog_has_25_big_five_items_without_riasec()
    {
        Assert.Equal(25, CompetencyTestCatalog.QuestionCount);
        Assert.Equal(25, CompetencyTestCatalog.Questions.Count);
        Assert.Equal(4, CompetencyTestCatalog.CategoryCodes.Length);
        Assert.Equal(5, CompetencyTestCatalog.QuickScanCategories.Length);
        foreach (var category in CompetencyTestCatalog.QuickScanCategories)
        {
            Assert.Equal(5, CompetencyTestCatalog.Questions.Count(q => q.Category == category));
        }

        Assert.DoesNotContain(CompetencyTestCatalog.Questions, q => q.IsRiasec);
        Assert.Contains(CompetencyTestCatalog.Questions, q => q.Reverse);
        Assert.True(CompetencyTestCatalog.Questions.Count(q => q.Reverse) >= 8);
    }

    [Fact]
    public void Score_maps_likert_to_0_100_and_reverses_items()
    {
        var high = Enumerable.Range(1, 25).ToDictionary(
            i => i,
            i => CompetencyTestCatalog.Questions.First(q => q.Id == i).Reverse ? 1 : 5);
        var scores = CompetencyTestCatalog.Score(high);
        Assert.NotNull(scores);
        Assert.True(scores!.IsComplete);
        Assert.Equal(100, scores.Samenwerken);
        Assert.Equal(100, scores.Resultaatgerichtheid);
        Assert.Equal(100, scores.Stressbestendigheid);
        Assert.Equal(100, scores.Innovatie);
        Assert.Equal(100, scores.Extraversie);

        var low = Enumerable.Range(1, 25).ToDictionary(
            i => i,
            i => CompetencyTestCatalog.Questions.First(q => q.Id == i).Reverse ? 5 : 1);
        var lowScores = CompetencyTestCatalog.Score(low)!;
        Assert.Equal(0, lowScores.Samenwerken);
        Assert.Equal(0, lowScores.Innovatie);
    }

    [Fact]
    public void Draft_may_be_partial_complete_requires_all_25()
    {
        var partial = new Dictionary<int, int> { [1] = 4, [2] = 3 };
        Assert.Null(CompetencyTestCatalog.ValidateAnswers(partial, requireComplete: false));
        Assert.NotNull(CompetencyTestCatalog.ValidateAnswers(partial, requireComplete: true));
        Assert.False(CompetencyTestCatalog.IsComplete(partial));

        var full = Enumerable.Range(1, 25).ToDictionary(i => i, _ => 3);
        Assert.Null(CompetencyTestCatalog.ValidateAnswers(full, requireComplete: true));
        Assert.True(CompetencyTestCatalog.IsComplete(full));
        Assert.Equal(50, CompetencyTestCatalog.Score(full)!.Samenwerken);
    }

    [Fact]
    public void Career_catalog_has_25_riasec_items()
    {
        Assert.Equal(25, CareerTestCatalog.QuestionCount);
        Assert.Equal(25, CareerTestCatalog.Questions.Count);
        Assert.Equal(6, CareerTestCatalog.RiasecCodes.Length);
        Assert.Equal(5, CareerTestCatalog.Questions.Count(q => q.Category == CareerTestCatalog.Realistic));
        Assert.Equal(4, CareerTestCatalog.Questions.Count(q => q.Category == CareerTestCatalog.Social));

        var high = Enumerable.Range(1, 25).ToDictionary(
            i => i,
            i => CareerTestCatalog.Questions.First(q => q.Id == i).Reverse ? 1 : 5);
        var scores = CareerTestCatalog.Score(high);
        Assert.NotNull(scores);
        Assert.True(scores!.IsComplete);
        Assert.Equal(100, scores.Realistic);
        Assert.Equal("ACE", CareerTestCatalog.HollandCode(scores));
        var tags = CareerTestCatalog.DeriveRiasecTags(scores);
        Assert.Equal(3, tags.Count);
    }

    [Fact]
    public void Deep_analysis_catalogs_are_separated_150()
    {
        Assert.Equal(150, DeepAnalysisCatalog.QuestionCount);
        Assert.Equal(150, DeepAnalysisCatalog.Questions.Count);
        Assert.Equal(150, DeepAnalysisCatalog.CareerQuestions.Count);
        Assert.Contains(DeepAnalysisCatalog.Questions, q => q.Family == "BigFive");
        Assert.DoesNotContain(DeepAnalysisCatalog.Questions, q => q.Family == "RIASEC");
        Assert.Contains(DeepAnalysisCatalog.CareerQuestions, q => q.Family == "RIASEC");
        Assert.DoesNotContain(DeepAnalysisCatalog.CareerQuestions, q => q.Family == "BigFive");
    }

    [Fact]
    public void Talent_contact_rules_48h_refund_window()
    {
        var created = new DateTime(2026, 9, 20, 8, 0, 0, DateTimeKind.Utc);
        var deadline = TalentContactRules.ComputeRespondByUtc(created);
        Assert.Equal(created.AddHours(48), deadline);
        Assert.False(TalentContactRules.CanEmployerWithdraw(
            Jobsy.Core.Enums.TalentContactStatus.Pending, created.AddHours(12), deadline));
        Assert.True(TalentContactRules.CanEmployerWithdraw(
            Jobsy.Core.Enums.TalentContactStatus.Pending, created.AddHours(49), deadline));
        Assert.True(TalentContactRules.IsRefundBlockedAfterContactShared(
            Jobsy.Core.Enums.TalentContactStatus.ContactShared));
    }

    [Fact]
    public void Flex_and_agency_commercial_defaults()
    {
        Assert.Equal(2.00m, FlexCommercialSettings.DefaultMarginPerHourEuro);
        Assert.Equal(2.99m, FlexCommercialSettings.DefaultDeepAnalysisPriceEuro);
        Assert.Equal(4000m, FlexCommercialSettings.DefaultAgencyAnnualPriceEuro);
        Assert.Equal(1m, FlexCommercialSettings.DefaultContactUnlockCostTokens);
        Assert.Equal(VacancyKind.Flex, VacancyKindLabels.ParseOrDefault("flex"));
        Assert.Equal("Flex-inzet", VacancyKindLabels.ToDutch(VacancyKind.Flex));
        Assert.Equal(1m, TalentContactRules.DefaultUnlockCostTokens);
    }

    [Fact]
    public void Legacy_compact_riasec_maps_q21_to_q25_and_keeps_top_three()
    {
        var answers = new Dictionary<int, int>
        {
            [21] = 5,
            [22] = 1,
            [23] = 1,
            [24] = 5,
            [25] = 4
        };
        var tags = CareerTestCatalog.DeriveLegacyCompactRiasecTags(answers);
        Assert.Equal(
            new[] { CareerTestCatalog.Realistic, CareerTestCatalog.Social, CareerTestCatalog.Enterprising },
            tags);

        var crowded = new Dictionary<int, int>
        {
            [21] = 5,
            [22] = 5,
            [23] = 5,
            [24] = 5,
            [25] = 5
        };
        var top = CareerTestCatalog.DeriveLegacyCompactRiasecTags(crowded);
        Assert.Equal(3, top.Count);
        Assert.Equal(CareerTestCatalog.Artistic, top[0]);
    }

    [Fact]
    public void Deep_analysis_upsell_copy_is_kind_specific()
    {
        var competence = DeepAnalysisService.FormatUpsellCopy(2.99m, AssessmentKind.Competence);
        Assert.Contains("€ 2,99", competence, StringComparison.Ordinal);
        Assert.Contains("competentie-analyse", competence, StringComparison.OrdinalIgnoreCase);

        var career = DeepAnalysisService.FormatUpsellCopy(2.99m, AssessmentKind.Career);
        Assert.Contains("€ 2,99", career, StringComparison.Ordinal);
        Assert.Contains("beroepentest", career, StringComparison.OrdinalIgnoreCase);
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
    public void ScoreAll_includes_matches_below_display_threshold()
    {
        var strong = MakeInput(
            Guid.NewGuid(),
            "Sterke match",
            travelMinutes: 8,
            maxTravel: 30,
            candidateHours: new HoursRange(16, 24),
            vacancyHours: new HoursRange(16, 24),
            competencies: new CompetencyScores(80, 80, 80, 80));
        var weak = MakeInput(
            Guid.NewGuid(),
            "Zwakke match",
            travelMinutes: 90,
            maxTravel: 20,
            candidateHours: new HoursRange(4, 8),
            vacancyHours: new HoursRange(32, 40),
            competencies: null);

        var scored = ProfileVacancyMatchCalculator.ScoreAll([strong, weak]);
        Assert.Equal(2, scored.Count);
        Assert.Contains(scored, m => m.TotalPercent < ProfileVacancyMatchCalculator.DisplayThreshold);
        Assert.Contains(ProfileVacancyMatchCalculator.WhyHeadlines(scored[0]), h => h.Length > 0);
    }

    [Fact]
    public void E_bike_routes_as_bike_and_satisfies_fiets_requirement()
    {
        Assert.Equal(TransportMode.Bike, TransportLabels.Parse("E-bike"));
        Assert.Equal(TransportLabels.EBike, TransportLabels.Canonical("ebike"));
        Assert.True(TransportLabels.MatchesRequired([TransportLabels.Bike], TransportLabels.EBike));
        Assert.False(TransportLabels.MatchesRequired([TransportLabels.Car], TransportLabels.EBike));
    }

    [Fact]
    public void Availability_presets_mutate_hours_and_flexibility()
    {
        var immediate = CandidateAvailabilityPresets.Apply(CandidateAvailabilityPresets.Immediate);
        Assert.True(immediate.FlexibleTimes);
        Assert.Equal(8, immediate.MinHoursPerWeek);
        Assert.Equal(40, immediate.MaxHoursPerWeek);

        var parttime = CandidateAvailabilityPresets.Apply(CandidateAvailabilityPresets.PartTime);
        Assert.False(parttime.FlexibleTimes);
        Assert.Equal(24, parttime.MaxHoursPerWeek);
        Assert.Contains("Ma", parttime.Availability.Keys);

        var seasonal = CandidateAvailabilityPresets.Apply(CandidateAvailabilityPresets.Seasonal);
        Assert.Equal(
            CandidateAvailabilityPresets.Seasonal,
            CandidateAvailabilityPresets.Detect(seasonal.MinHoursPerWeek, seasonal.MaxHoursPerWeek, seasonal.FlexibleTimes));
        Assert.Equal(
            CandidateAvailabilityPresets.PartTime,
            CandidateAvailabilityPresets.Detect(8, 24, false));
    }

    [Fact]
    public void Riasec_overlap_feeds_score_and_why_headline()
    {
        var match = ProfileVacancyMatchCalculator.Calculate(MakeInput(
            Guid.NewGuid(),
            "Verkoopmedewerker",
            travelMinutes: 10,
            maxTravel: 30,
            candidateHours: new HoursRange(16, 24),
            vacancyHours: new HoursRange(16, 24),
            competencies: new CompetencyScores(80, 80, 80, 80),
            candidateRiasec: [CareerTestCatalog.Enterprising, CareerTestCatalog.Social],
            vacancyRiasec: [CareerTestCatalog.Enterprising, CareerTestCatalog.Conventional]));

        Assert.NotNull(match.InterestScore01);
        Assert.True(match.InterestScore01 >= 0.4);
        Assert.Contains(match.Why, w => w.Kind == "interest");
        Assert.Contains(ProfileVacancyMatchCalculator.WhyHeadlines(match), h => h == "Beroepsinteresse past");
    }

    [Fact]
    public void Discover_attaches_match_fields_only_for_candidates()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Api/Controllers/VacanciesController.cs"));
        Assert.Contains("if (_companyAuth.IsCandidate(User))", src, StringComparison.Ordinal);
        Assert.Contains("MatchPercent = match.TotalPercent", src, StringComparison.Ordinal);
        Assert.Contains("minMatchPercent", src, StringComparison.Ordinal);
        Assert.Contains("LegalAgeKnown && !match.Core.LegalEligible", src, StringComparison.Ordinal);
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
        string? vacancyDescription = null,
        IReadOnlyList<string>? candidateRiasec = null,
        IReadOnlyList<string>? vacancyRiasec = null)
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
            CandidateRiasecTags = candidateRiasec,
            VacancyRiasecTags = vacancyRiasec,
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
