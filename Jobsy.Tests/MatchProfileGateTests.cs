using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class MatchProfileGateTests
{
    [Fact]
    public void IsProfileComplete_requires_basics_education_and_wizard()
    {
        Assert.False(MatchProfileCompleteness.IsProfileComplete(true, false, true));
        Assert.False(MatchProfileCompleteness.IsProfileComplete(true, true, false));
        Assert.False(MatchProfileCompleteness.IsProfileComplete(false, true, true));
        Assert.True(MatchProfileCompleteness.IsProfileComplete(true, true, true));
        Assert.Equal(3, MatchProfileCompleteness.RequiredStepCount);
        Assert.Equal(2, MatchProfileCompleteness.CompletedCount(true, true, false));
        Assert.Equal(3, MatchProfileCompleteness.CompletedCount(true, true, true));
    }

    [Fact]
    public void FitGateOpen_accepts_provisional_scores_after_wizard()
    {
        var gate = new MatchProfileGateViewModel
        {
            WizardCompleted = true,
            HasProvisionalScores = true,
            CultureCompleted = false,
            ValuesCompleted = false
        };
        Assert.True(gate.FitGateOpen);
    }

    [Fact]
    public void IsMatchBasicsFilled_requires_name_and_travel_preset()
    {
        Assert.True(MatchProfileCompleteness.IsMatchBasicsFilled(
            fullName: null,
            firstName: "Sanne",
            lastName: "Test",
            maxTravelMinutes: 30,
            preferredTransport: "Fiets"));
        Assert.False(MatchProfileCompleteness.IsMatchBasicsFilled(
            fullName: null,
            firstName: "",
            lastName: "",
            maxTravelMinutes: 30,
            preferredTransport: "Fiets"));
        Assert.False(MatchProfileCompleteness.IsMatchBasicsFilled(
            fullName: null,
            firstName: "Sanne",
            lastName: "Test",
            maxTravelMinutes: 0,
            preferredTransport: "Fiets"));
    }

    [Fact]
    public void HasEducationLevel_accepts_geen_as_answered()
    {
        Assert.True(MatchProfileCompleteness.HasEducationLevel(["Geen"]));
        Assert.True(MatchProfileCompleteness.HasEducationLevel(["Geen specifiek vereist"]));
        Assert.False(MatchProfileCompleteness.HasEducationLevel([" ", ""]));
        Assert.False(MatchProfileCompleteness.HasEducationLevel(Array.Empty<string>()));
        Assert.True(MatchProfileCompleteness.HasEducationLevel(["MBO"]));
        Assert.True(MatchProfileCompleteness.HasEducationLevel(["HAVO – E&M"]));
    }

    [Fact]
    public void Relevance_filters_education_and_travel()
    {
        Assert.False(MatchVacancyRelevance.IsRelevant(
            ["VMBO"], 30, "Fiets",
            vacancyRequiredEducation: "HBO",
            vacancyTravelMinutes: 20,
            vacancyRequiredTransport: null));

        Assert.True(MatchVacancyRelevance.IsRelevant(
            ["MBO", "HBO"], 30, "Fiets",
            vacancyRequiredEducation: "MBO",
            vacancyTravelMinutes: 20,
            vacancyRequiredTransport: null));

        Assert.True(MatchVacancyRelevance.IsRelevant(
            ["MBO 3"], 30, "Fiets",
            vacancyRequiredEducation: "MBO",
            vacancyTravelMinutes: 20,
            vacancyRequiredTransport: null));

        Assert.False(MatchVacancyRelevance.IsRelevant(
            ["MBO"], 20, "Fiets",
            vacancyRequiredEducation: null,
            vacancyTravelMinutes: 45,
            vacancyRequiredTransport: null));
    }

    [Fact]
    public void MatchVacancyService_FilterRelevant_keeps_fitting_rows()
    {
        var gate = new MatchProfileGateViewModel
        {
            IsProfileComplete = true,
            Educations = ["MBO"],
            MaxTravelMinutes = 30,
            PreferredTransport = "Fiets"
        };

        var vacancies = new List<VacancyListItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Fit", RequiredEducation = "MBO", TravelMinutes = 15, RequiredTransport = ["Fiets"] },
            new() { Id = Guid.NewGuid(), Title = "TooFar", RequiredEducation = "MBO", TravelMinutes = 55 },
            new() { Id = Guid.NewGuid(), Title = "WrongEdu", RequiredEducation = "WO", TravelMinutes = 10 }
        };

        var filtered = MatchVacancyService.FilterRelevant(vacancies, gate);
        Assert.Single(filtered);
        Assert.Equal("Fit", filtered[0].Title);
    }

    [Fact]
    public void MatchVacancyService_FilterRelevant_keeps_outside_travel_when_discovery_widened()
    {
        var gate = new MatchProfileGateViewModel
        {
            IsProfileComplete = true,
            Educations = ["MBO"],
            MaxTravelMinutes = 20,
            PreferredTransport = "Fiets"
        };

        var vacancies = new List<VacancyListItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Reachable", RequiredEducation = "MBO", TravelMinutes = 55 },
        };

        var strict = MatchVacancyService.FilterRelevant(vacancies, gate, take: 24, outsideTravelFallback: false);
        Assert.Empty(strict);

        var widened = MatchVacancyService.FilterRelevant(vacancies, gate, take: 24, outsideTravelFallback: true);
        Assert.Single(widened);
        Assert.Equal("Reachable", widened[0].Title);
    }

    [Fact]
    public void Match_page_wires_unlock_gate_and_services()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateMatchSsr.razor"));
        var gateHtml = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Match/MatchSsrGateHtml.cs"));
        var shells = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Match/MatchSsrShells.cs"));
        Assert.Contains("MatchVacancyService", page, StringComparison.Ordinal);
        Assert.Contains("CandidateMatchProfileService", page, StringComparison.Ordinal);
        Assert.Contains("IsProfileComplete", shells, StringComparison.Ordinal);
        Assert.Contains("FitGateOpen", shells, StringComparison.Ordinal);
        Assert.Contains("Match.Gate.Check.BasicsTitle", gateHtml, StringComparison.Ordinal);
        Assert.Contains("whoami-checklist", gateHtml, StringComparison.Ordinal);
        Assert.Contains("KbRoutes.Map", gateHtml, StringComparison.Ordinal);
        Assert.Contains("Match.Gate.AuthTitle", gateHtml, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Program.cs"));
        Assert.Contains("CandidateMatchProfileService", program, StringComparison.Ordinal);
        Assert.Contains("MatchVacancyService", program, StringComparison.Ordinal);

        var unlock = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Match/MatchUnlockPanel.razor"));
        Assert.Contains("WizardCompleted", unlock, StringComparison.Ordinal);
        Assert.Contains("OnboardingRoutes.StartPath", unlock, StringComparison.Ordinal);
        Assert.Contains("MatchUnlock.ContinueStart", unlock, StringComparison.Ordinal);
    }
}
