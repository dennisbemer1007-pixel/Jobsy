using Jobsy.Web.Components.Candidate;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class CareerPlanEvidenceTests
{
    [Fact]
    public void Experience_completes_met_gaps_and_the_overview_counts_only_the_rest()
    {
        var plan = Plan();
        var evidence = "orderpicker Logistiek MBO Logistiek";
        var page = CareerPlanViewBuilder.BuildPage(plan, "nl", "orderpicker", evidence);

        Assert.Equal(CareerStepStatus.Completed, page.Steps[0].Status);
        Assert.Equal(CareerStepStatus.Active, page.Steps[1].Status);
        Assert.Equal(0, page.Steps[0].GapCount);
        Assert.Equal(1, page.Steps[1].GapCount);
        Assert.Equal(1, page.CompletedSteps);
        Assert.DoesNotContain(page.Steps[1].GapNames, name => name.Contains("Logistiek", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(page.Steps[1].GapNames, name => name.Contains("Planner", StringComparison.Ordinal));
    }

    [Fact]
    public void Eight_years_hides_the_one_year_hint_and_passport_uses_the_same_gaps()
    {
        var plan = Plan();
        var evidence = "orderpicker Logistiek";
        var detail = CareerPlanViewBuilder.BuildStep(plan, 2, evidence, experienceYears: 8);
        Assert.NotNull(detail);
        Assert.True(detail!.YearsMet);
        Assert.Contains(detail.Present, line => line.Met && line.Text.Contains("Logistiek", StringComparison.Ordinal));

        var dashboard = new CareerPathService().FromApi(plan);
        var passport = CareerPlanViewBuilder.BuildPassport(dashboard, evidence);
        Assert.Equal(CareerStepStatus.Completed, passport.Shells[0].Status);
        Assert.Contains(passport.Gaps, gap => gap.Met && gap.Text.Contains("Logistiek", StringComparison.Ordinal));
        Assert.Contains("Roosteren", passport.CourseNames);
    }

    [Fact]
    public void ExperienceYears_sums_stated_years()
    {
        var years = CareerPlanViewBuilder.ExperienceYears(
        [
            new CandidateEmployerHistory { Role = "Orderpicker", Years = 8 }
        ]);
        Assert.Equal(8, years);
    }

    private static CareerPathPlanApiModel Plan() => new()
    {
        DreamTitle = "Teamleider logistiek",
        Steps =
        [
            new CareerPathStepApiModel
            {
                Id = "a",
                Order = 1,
                Title = "Basiskennis Logistiek",
                Status = "Active",
                SkillsGap = ["Basiskennis Logistiek"],
                Courses = ["Logistiek basis"]
            },
            new CareerPathStepApiModel
            {
                Id = "b",
                Order = 2,
                Title = "Ervaring in Logistiek",
                Status = "Open",
                SkillsGap = ["Ervaring in Logistiek"],
                MinRequirements = ["Planner worden"],
                YearsExperienceNeeded = 1,
                Courses = ["Roosteren"]
            }
        ]
    };
}
