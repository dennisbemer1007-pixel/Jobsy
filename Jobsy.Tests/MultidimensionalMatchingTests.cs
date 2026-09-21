using Jobsy.Core.Rules;
using Jobsy.Tests.Uat;

namespace Jobsy.Tests;

public class MultidimensionalMatchingTests
{
    [Fact]
    public void Transferable_skills_score_related_domains_without_exact_title()
    {
        var exact = TransferableSkillRules.Score01(
            ["Winkel"], ["Winkel"], "Verkoopmedewerker", "Werk in de winkel.");
        var transferable = TransferableSkillRules.Score01(
            ["Horeca"], ["Winkel"], "Kassamedewerker", "Klantcontact in retail.");
        var unrelated = TransferableSkillRules.Score01(
            ["Software"], ["Zorg"], "Verpleegkundige", "Thuiszorg.");

        Assert.True(exact > transferable);
        Assert.True(transferable > unrelated);
        Assert.True(transferable >= 0.5);
    }

    [Fact]
    public void Broad_match_includes_ai_style_rationale_beyond_job_title()
    {
        var match = ProfileVacancyMatchCalculator.Calculate(new ProfileVacancyMatchInput
        {
            VacancyId = Guid.NewGuid(),
            VacancyTitle = "Business Controller",
            VacancyDescription = "Financiële planning, rapportages en cijfers bewaken.",
            WorkTypes = ["Administratie"],
            CandidateRoles = ["Administratief medewerker", "Boekhoudkundig assistent"],
            CandidateEducations = ["HBO"],
            RequiredEducation = "HBO",
            CandidateEmployerCount = 2,
            CandidateCompetencies = new CompetencyScores(70, 88, 75, 60),
            CandidateDiscScores = new DiscScores(55, 45, 60, 90),
            CandidateRiasecScores = new RiasecScores(20, 40, 20, 25, 50, 95),
            CareerDeepCompleted = true,
            CareerOccupations =
            [
                new CareerOccupationMatch(
                    "Financieel administratief medewerker",
                    88,
                    CareerCompassBuilder.BandStrong,
                    "Cijfers.",
                    ["financieel", "administratief", "boekhoud"])
            ],
            Core = new MatchScoreInput
            {
                EstimatedTravelMinutes = 12,
                MaxTravelMinutes = 30,
                CandidateHours = new HoursRange(32, 40),
                VacancyHours = new HoursRange(32, 40),
                VacancySchedule = SchedulePayload.Flexible(FlexibleScheduleSource.Manual),
                CandidateAgeYears = 28
            }
        });

        Assert.True(match.TotalPercent >= ProfileVacancyMatchCalculator.DisplayThreshold);
        Assert.True(match.IsBroadMatch);
        Assert.False(string.IsNullOrWhiteSpace(match.MatchRationale));
        Assert.Equal(match.MatchRationale, ProfileVacancyMatchCalculator.SummaryLine(match));
        Assert.True(
            match.MatchRationale!.Contains("opleiding", StringComparison.OrdinalIgnoreCase)
            || match.MatchRationale.Contains("overdraagbare", StringComparison.OrdinalIgnoreCase)
            || match.MatchRationale.Contains("beroepen-kompas", StringComparison.OrdinalIgnoreCase)
            || match.MatchRationale.Contains("Wie ben ik?", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(match.Why, w => CareerCompassBuilder.ContainsForbiddenJargon(w.Text));
        Assert.False(CareerCompassBuilder.ContainsForbiddenJargon(match.MatchRationale!));
    }

    [Fact]
    public void Exact_title_hit_is_not_marked_broad()
    {
        var match = ProfileVacancyMatchCalculator.Calculate(new ProfileVacancyMatchInput
        {
            VacancyId = Guid.NewGuid(),
            VacancyTitle = "Verpleegkundige thuiszorg",
            VacancyDescription = "Zorg bij mensen thuis.",
            WorkTypes = ["Zorg"],
            CandidateRoles = ["Verpleegkundige"],
            CandidateEducations = ["MBO"],
            CandidateEmployerCount = 1,
            CandidateCompetencies = new CompetencyScores(85, 70, 80, 50),
            CandidateRiasecScores = new RiasecScores(20, 30, 20, 95, 30, 25),
            CareerOccupations =
            [
                new CareerOccupationMatch(
                    "Verpleegkundige",
                    97,
                    CareerCompassBuilder.BandSuper,
                    "Zorg.",
                    ["verpleegkundige", "zorg"])
            ],
            Core = new MatchScoreInput
            {
                EstimatedTravelMinutes = 10,
                MaxTravelMinutes = 30,
                CandidateHours = new HoursRange(24, 32),
                VacancyHours = new HoursRange(24, 32),
                VacancySchedule = SchedulePayload.Flexible(FlexibleScheduleSource.Manual),
                CandidateAgeYears = 30
            }
        });

        Assert.False(match.IsBroadMatch);
        Assert.Null(match.MatchRationale);
    }

    [Fact]
    public void Training_deeplinks_never_resolve_to_a_homepage()
    {
        Assert.True(TrainingDeepLinkRules.IsCourseDeepLink("https://www.loi.nl/opleidingen/mbo/zorg-welzijn"));
        Assert.False(TrainingDeepLinkRules.IsCourseDeepLink("https://www.loi.nl/"));
        Assert.False(TrainingDeepLinkRules.IsCourseDeepLink("https://www.loi.nl"));
        Assert.Throws<ArgumentException>(() => TrainingDeepLinkRules.Combine("https://www.loi.nl/", null));
        Assert.Throws<ArgumentException>(() => TrainingDeepLinkRules.Combine("https://www.loi.nl/", "/"));
        var url = TrainingDeepLinkRules.Combine("https://www.loi.nl/", "/opleidingen/mbo/zorg-welzijn");
        Assert.Contains("/opleidingen/mbo/zorg-welzijn", url, StringComparison.OrdinalIgnoreCase);
        Assert.True(TrainingDeepLinkRules.IsCourseDeepLink(url));
    }

    [Fact]
    public void Training_ui_is_subtle_in_context_without_shouting_cta()
    {
        var block = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Web/Components/Pages/Candidate/TrainingOffersBlock.razor"));
        Assert.Contains("training-offers--subtle", block, StringComparison.Ordinal);
        Assert.Contains("training-offers__link", block, StringComparison.Ordinal);
        Assert.DoesNotContain("login-submit", block, StringComparison.Ordinal);
        Assert.Contains("IsCourseDeepLink", block, StringComparison.Ordinal);
        Assert.Equal("Passende cursus", Jobsy.Web.Localization.UiStrings.Get("Fit.TrainingTitle", "nl"));
        Assert.Contains("korte cursus", Jobsy.Web.Localization.UiStrings.Get("Fit.TrainingLead", "nl"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("holistisch", RoleFitCheckPrompt.System, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("overdraagbare", RoleFitCheckPrompt.System, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(TrainingCopy.GapAdvice, RoleFitCheckPrompt.System, StringComparison.Ordinal);
    }
}
