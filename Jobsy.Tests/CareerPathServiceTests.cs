using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class CareerPathServiceTests
{
    [Fact]
    public void Default_dashboard_has_horizon_content_without_positional_completion()
    {
        var svc = new CareerPathService();
        var dash = svc.GetDashboard();

        Assert.DoesNotContain("Magazijnmedewerker", dash.MatchSummary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Magazijnmedewerker", dash.DreamRoleTitle, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(CareerPathService.DefaultDreamId, dash.DreamRoleId);
        Assert.Equal("Teamleider logistiek", dash.DreamRoleTitle);
        Assert.InRange(dash.MatchPercent, 15, 55);
        Assert.NotEmpty(dash.MatchSummary);
        Assert.True(dash.Steps.Count >= 3);
        Assert.Contains(dash.Steps, s => s.Status == CareerStepStatus.Active);
        Assert.DoesNotContain(dash.Steps, s => s.Status == CareerStepStatus.Completed);
        Assert.Contains(dash.Steps, s => s.Courses.Count > 0 || s.MinRequirements.Count > 0);
        Assert.Contains(dash.Steps, s => s.SkillsGap.Count > 0);
        Assert.Contains(dash.Steps, s => s.YearsExperienceNeeded > 0);
        Assert.All(dash.Steps, s => Assert.False(string.IsNullOrWhiteSpace(s.Id)));
    }

    [Fact]
    public void Switching_known_dream_role_changes_path_and_match()
    {
        var svc = new CareerPathService();
        var logistics = svc.GetDashboard("teamleider-logistiek");
        var retail = svc.GetDashboard("Filiaalmanager");
        var hr = svc.GetDashboard("hr-adviseur");

        Assert.NotEqual(logistics.DreamRoleTitle, retail.DreamRoleTitle);
        Assert.Equal("HR-adviseur", hr.DreamRoleTitle);
        Assert.All(new[] { logistics, retail, hr }, d =>
        {
            Assert.NotEmpty(d.Steps);
            Assert.Contains(d.Steps, s => s.SkillsGap.Count > 0 || s.Status == CareerStepStatus.Active);
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
        Assert.Equal(CareerPathService.DefaultDreamId, dash.DreamRoleId);
        Assert.Equal(CareerPathService.DefaultDreamTitle, dash.DreamRoleTitle);
    }

    [Fact]
    public void Career_dashboard_page_is_horizon_layout_with_progress_actions()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CareerDashboard.razor"));
        Assert.Contains("@page \"/carriere\"", page);
        Assert.Contains("CareerPathService", page);
        Assert.Contains("horizon-hero", page);
        Assert.Contains("horizon-stepper", page);
        Assert.Contains("horizon-card", page);
        Assert.Contains("career-dream-input", page);
        Assert.Contains("GetCareerPathAsync", page);
        Assert.Contains("GenerateCareerPathAsync", page);
        Assert.Contains("CompleteCareerStepAsync", page);
        Assert.Contains("MarkCareerCourseOwnedAsync", page);
        Assert.Contains("CareerDash.Courses", page);
        Assert.Contains("CareerDash.MarkComplete", page);
        Assert.Contains("CareerDash.CourseHaveIt", page);
        Assert.Contains("LobsyToast", page);
        Assert.Contains("CareerDash.MatchWithProfile", page);
        Assert.DoesNotContain("CareerDash.CurrentRole", page);
        Assert.DoesNotContain("CareerDash.PlanLead", page);
        Assert.DoesNotContain("Magazijnmedewerker", page);
        Assert.DoesNotContain("career-gauge", page);
        Assert.DoesNotContain("career-accordion", page);
        Assert.DoesNotContain("career-dash__bar", page);

        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".career-dash", css);
        Assert.Contains(".horizon-hero", css);
        Assert.Contains(".horizon-stepper", css);
        Assert.Contains(".horizon-card", css);
        Assert.Contains(".lobsy-toast", css);
        Assert.Contains("--gold", css);
        Assert.DoesNotContain(".career-gauge", css);
        Assert.DoesNotContain(".career-accordion {", css);
        Assert.DoesNotContain(".career-dash__bar", css);
    }

    [Fact]
    public void BuildLocal_uses_stable_step_keys_without_positional_statuses()
    {
        var plan = HorizonCareerPathBuilder.BuildLocal("Teamleider logistiek");
        Assert.Equal(4, plan.Steps.Count);
        Assert.All(plan.Steps, s => Assert.Equal(HorizonCareerStepKind.Open, s.Status));
        Assert.All(plan.Steps, s => Assert.Equal(0, s.StepMatchPercent));
        Assert.All(plan.Steps, s => Assert.Equal(CareerStepKey.ForStep(s.Title, s.Order), s.Id));
        var again = HorizonCareerPathBuilder.BuildLocal("Teamleider logistiek");
        Assert.Equal(plan.Steps.Select(s => s.Id), again.Steps.Select(s => s.Id));
    }
}
