using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class CareerPathServiceTests
{
    [Fact]
    public void Default_dashboard_builds_content_only_local_preview()
    {
        var svc = new CareerPathService();
        var dash = svc.GetDashboard();

        Assert.DoesNotContain("Magazijnmedewerker", dash.MatchSummary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Magazijnmedewerker", dash.DreamRoleTitle, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("custom", dash.DreamRoleId);
        Assert.Equal("Teamleider logistiek", dash.DreamRoleTitle);
        Assert.InRange(dash.MatchPercent, 15, 55);
        Assert.NotEmpty(dash.MatchSummary);
        Assert.False(dash.HasPlan);
        Assert.True(dash.Steps.Count >= 3);
        Assert.All(dash.Steps, s => Assert.Equal(CareerStepStatus.Open, s.Status));
        Assert.Contains(dash.Steps, s => s.Courses.Count > 0 || s.MinRequirements.Count > 0);
        Assert.Contains(dash.Steps, s => s.SkillsGap.Count > 0);
        Assert.Contains(dash.Steps, s => s.YearsExperienceNeeded > 0);
        Assert.All(dash.Steps, s => Assert.False(string.IsNullOrWhiteSpace(s.Id)));
    }

    [Fact]
    public void Empty_dashboard_has_no_plan_until_saved()
    {
        var svc = new CareerPathService();
        var dash = svc.EmptyDashboard();
        Assert.False(dash.HasPlan);
        Assert.Empty(dash.Steps);
        Assert.Equal("", dash.DreamRoleTitle);
    }

    [Fact]
    public void FromApi_maps_statuses_courses_and_goal()
    {
        var svc = new CareerPathService();
        var api = new CareerPathPlanApiModel
        {
            DreamTitle = "Teamleider logistiek",
            MatchPercent = 42,
            MatchSummary = "test",
            GoalReached = false,
            Steps =
            [
                new CareerPathStepApiModel
                {
                    Id = "abcd1234",
                    Order = 1,
                    Title = "Basis",
                    Status = "Completed",
                    Summary = "ok",
                    Courses = ["Leiderschap"],
                    CourseStatuses = [new CareerPathCourseApiModel { Name = "Leiderschap", OnProfile = true }],
                    ActionLabel = "Verder",
                    ActionHref = "/opleidingen",
                    StepMatchPercent = 100,
                    MatchedCourseCount = 1
                },
                new CareerPathStepApiModel
                {
                    Id = "efgh5678",
                    Order = 2,
                    Title = "Nu",
                    Status = "Active",
                    Summary = "next",
                    Courses = ["Roosteren"],
                    CourseStatuses = [new CareerPathCourseApiModel { Name = "Roosteren", OnProfile = false }],
                    ActionLabel = "Verder",
                    ActionHref = "/opleidingen",
                    StepMatchPercent = 0,
                    MatchedCourseCount = 0
                }
            ]
        };

        var dash = svc.FromApi(api);
        Assert.True(dash.HasPlan);
        Assert.Equal(CareerStepStatus.Completed, dash.Steps[0].Status);
        Assert.Equal(CareerStepStatus.Active, dash.Steps[1].Status);
        Assert.True(dash.Steps[0].Courses[0].OnProfile);
        Assert.False(dash.Steps[1].Courses[0].OnProfile);
        Assert.False(dash.GoalReached);
    }

    [Fact]
    public void Switching_known_dream_role_changes_path_and_match()
    {
        var svc = new CareerPathService();
        var logistics = svc.GetDashboard("Teamleider logistiek");
        var retail = svc.GetDashboard("Filiaalmanager");
        var hr = svc.GetDashboard("HR-adviseur");

        Assert.NotEqual(logistics.DreamRoleTitle, retail.DreamRoleTitle);
        Assert.Equal("HR-adviseur", hr.DreamRoleTitle);
        Assert.All(new[] { logistics, retail, hr }, d =>
        {
            Assert.NotEmpty(d.Steps);
            Assert.Contains(d.Steps, s => s.SkillsGap.Count > 0);
            Assert.All(d.Steps, s => Assert.False(string.IsNullOrWhiteSpace(s.ActionHref)));
            Assert.DoesNotContain("Magazijnmedewerker", d.MatchSummary, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Free_text_horizon_builds_custom_path()
    {
        var svc = new CareerPathService();
        var dash = svc.GetDashboard("Chef-kok Westland");

        Assert.Equal("custom", dash.DreamRoleId);
        Assert.Equal("Chef-kok Westland", dash.DreamRoleTitle);
        Assert.InRange(dash.MatchPercent, 15, 55);
        Assert.Equal(4, dash.Steps.Count);
        Assert.Contains(dash.DreamRoleTitle, dash.Steps[^1].Title, StringComparison.Ordinal);
        Assert.All(dash.Steps, s => Assert.False(string.IsNullOrWhiteSpace(s.ActionHref)));
        Assert.DoesNotContain("Magazijnmedewerker", string.Join(' ', dash.Steps.Select(s => s.Summary)));
    }

    [Fact]
    public void Empty_horizon_falls_back_to_default_title()
    {
        var svc = new CareerPathService();
        var dash = svc.GetDashboard("   ");
        Assert.Equal("custom", dash.DreamRoleId);
        Assert.Equal(CareerPathService.DefaultDreamTitle, dash.DreamRoleTitle);
    }

    [Fact]
    public void Local_builder_returns_open_content_only()
    {
        var plan = HorizonCareerPathBuilder.BuildLocal("Teamleider logistiek");
        Assert.All(plan.Steps, s => Assert.Equal(HorizonCareerStepKind.Open, s.Status));
        Assert.All(plan.Steps, s => Assert.Equal(0, s.StepMatchPercent));
        Assert.All(plan.Steps, s => Assert.False(string.IsNullOrWhiteSpace(s.Id)));
    }

    [Fact]
    public void Career_dashboard_page_is_the_journey_style_climb()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CareerDashboard.razor"));
        Assert.Contains("@page \"/carriere\"", page);
        Assert.Contains("journey-page career-page", page);
        Assert.Contains("CareerStage", page);
        Assert.Contains("CareerRail", page);
        Assert.Contains("CareerEmptyCard", page);
        Assert.Contains("CareerOverviewCard", page);
        Assert.Contains("CareerDreamDialog", page);
        Assert.Contains("CareerArchivedPlans", page);
        Assert.Contains("CareerPlanViewBuilder.BuildPage", page);
        Assert.Contains("GetCareerPathAsync", page);
        Assert.Contains("GenerateCareerPathAsync", page);
        Assert.Contains("RestoreArchivedCareerPlanAsync", page);

        // B2/B3/B6/B10/B11/B12: the old horizon hero, confirm interop and % are gone.
        Assert.DoesNotContain("horizon-hero", page);
        Assert.DoesNotContain("horizon-stepper", page);
        Assert.DoesNotContain("horizon-card", page);
        Assert.DoesNotContain("HorizonArt", page);
        Assert.DoesNotContain("career-dream-input", page);
        Assert.DoesNotContain("datalist", page);
        Assert.DoesNotContain("window.confirm", page);
        Assert.DoesNotContain("@onblur", page);
        Assert.DoesNotContain("ClaimCareerCourseAsync", page);
        Assert.DoesNotContain("MatchWithProfile", page);
        Assert.DoesNotContain("ex.Message", page);
        Assert.DoesNotContain("<select", page);

        var overview = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/Career/CareerOverviewCard.razor"));
        Assert.Contains("GrowingShellsStepper", overview);

        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/carriere.css"));
        Assert.Contains(".career-page", css);
        Assert.Contains(".career-scene", css);
        Assert.Contains(".career-stage", css);
        Assert.Contains(".career-rail", css);
        Assert.Contains(".career-stepper", css);
        Assert.Contains("journey-page h1:focus:not(:focus-visible)", css);
        Assert.Contains("prefers-reduced-motion", css);
    }
}
