using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class MatchProfileGateTests
{
    [Fact]
    public void IsProfileComplete_requires_basics_education_and_three_tests()
    {
        Assert.False(MatchProfileCompleteness.IsProfileComplete(true, false, true, true, true));
        Assert.False(MatchProfileCompleteness.IsProfileComplete(true, true, false, true, true));
        Assert.True(MatchProfileCompleteness.IsProfileComplete(true, true, true, true, true));
        Assert.Equal(5, MatchProfileCompleteness.RequiredStepCount);
        Assert.Equal(3, MatchProfileCompleteness.CompletedCount(true, true, true, false, false));
    }

    [Fact]
    public void HasEducationLevel_ignores_geen_and_blank()
    {
        Assert.False(MatchProfileCompleteness.HasEducationLevel(["Geen"]));
        Assert.False(MatchProfileCompleteness.HasEducationLevel([" ", ""]));
        Assert.True(MatchProfileCompleteness.HasEducationLevel(["MBO"]));
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
    public void Match_page_wires_unlock_gate_and_services()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/MatchPage.razor"));
        Assert.Contains("MatchUnlockPanel", page, StringComparison.Ordinal);
        Assert.Contains("IsProfileComplete", page, StringComparison.Ordinal);
        Assert.Contains("MatchVacancyService", page, StringComparison.Ordinal);
        Assert.Contains("CandidateMatchProfileService", page, StringComparison.Ordinal);
        Assert.Contains("banenkaart", page, StringComparison.OrdinalIgnoreCase);

        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Program.cs"));
        Assert.Contains("CandidateMatchProfileService", program, StringComparison.Ordinal);
        Assert.Contains("MatchVacancyService", program, StringComparison.Ordinal);
    }
}
