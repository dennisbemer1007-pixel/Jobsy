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
    public void Eight_years_completes_an_internship_and_hides_a_repeated_gap()
    {
        var plan = Plan();
        plan.Steps.Add(new CareerPathStepApiModel
        {
            Id = "c",
            Order = 3,
            Title = "Ervaring in Logistiek – Stage of traineeship",
            Status = "Open",
            SkillsGap = ["Basiskennis Logistiek", "Stage of traineeship in de logistiek"],
            YearsExperienceNeeded = 1
        });
        plan.Steps.Add(new CareerPathStepApiModel
        {
            Id = "d",
            Order = 4,
            Title = "Leidinggeven",
            Status = "Open",
            SkillsGap = ["Een team aansturen"]
        });

        var evidence = "orderpicker Logistiek MBO Logistiek";
        var page = CareerPlanViewBuilder.BuildPage(plan, "nl", "orderpicker", evidence, experienceYears: 8);
        Assert.Equal(CareerStepStatus.Completed, page.Steps[2].Status);
        Assert.Equal(CareerStepStatus.Active, page.Steps[1].Status);
        Assert.Equal(CareerStepStatus.Open, page.Steps[3].Status);
        Assert.DoesNotContain(page.Steps[2].GapNames, name => name.Contains("Stage", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(page.Steps[2].GapNames, name => name.Contains("Basiskennis", StringComparison.OrdinalIgnoreCase));

        var detail = CareerPlanViewBuilder.BuildStep(plan, 3, evidence, experienceYears: 8);
        Assert.NotNull(detail);
        Assert.DoesNotContain(detail!.Present, line => line.Text.Contains("Basiskennis", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("Nog 1 stap laten groeien", Jobsy.Web.Localization.UiStrings.Get("Career.Fact.ClawOne", "nl"));
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
