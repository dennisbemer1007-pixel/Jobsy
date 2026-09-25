using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class CareerPathServiceTests
{
    [Fact]
    public void Default_dashboard_has_current_role_dream_and_steps()
    {
        var svc = new CareerPathService();
        var dash = svc.GetDashboard();

        Assert.Equal("Magazijnmedewerker", dash.CurrentRoleTitle);
        Assert.Equal(CareerPathService.DefaultDreamId, dash.DreamRoleId);
        Assert.Equal("Teamleider logistiek", dash.DreamRoleTitle);
        Assert.Equal(32, dash.MatchPercent);
        Assert.NotEmpty(dash.MatchSummary);
        Assert.True(dash.Steps.Count >= 3);
        Assert.Contains(dash.Steps, s => s.Status == CareerStepStatus.Completed);
        Assert.Contains(dash.Steps, s => s.Status == CareerStepStatus.Active);
        Assert.Contains(dash.Steps, s => s.Status == CareerStepStatus.Open);
    }

    [Fact]
    public void Switching_known_dream_role_changes_path_and_match()
    {
        var svc = new CareerPathService();
        var logistics = svc.GetDashboard("teamleider-logistiek");
        var retail = svc.GetDashboard("Filiaalmanager");
        var hr = svc.GetDashboard("hr-adviseur");

        Assert.NotEqual(logistics.DreamRoleTitle, retail.DreamRoleTitle);
        Assert.NotEqual(logistics.MatchPercent, retail.MatchPercent);
        Assert.Equal("HR-adviseur", hr.DreamRoleTitle);
        Assert.All(new[] { logistics, retail, hr }, d =>
        {
            Assert.NotEmpty(d.Steps);
            Assert.Contains(d.Steps, s => s.SkillsGap.Count > 0 || s.Status == CareerStepStatus.Completed);
            Assert.All(d.Steps, s => Assert.False(string.IsNullOrWhiteSpace(s.ActionHref)));
        });
    }

    [Fact]
    public void Free_text_horizon_builds_custom_path()
    {
        var svc = new CareerPathService();
        var dash = svc.GetDashboard("Chef-kok Westland");

        Assert.Equal("custom", dash.DreamRoleId);
        Assert.Equal("Chef-kok Westland", dash.DreamRoleTitle);
        Assert.InRange(dash.MatchPercent, 18, 34);
        Assert.Equal(4, dash.Steps.Count);
        Assert.Contains(dash.DreamRoleTitle, dash.Steps[^1].Title, StringComparison.Ordinal);
        Assert.All(dash.Steps, s => Assert.False(string.IsNullOrWhiteSpace(s.ActionHref)));
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
    public void Career_dashboard_page_is_routed_and_uses_service()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CareerDashboard.razor"));
        Assert.Contains("@page \"/carriere\"", page);
        Assert.Contains("CareerPathService", page);
        Assert.Contains("career-gauge", page);
        Assert.Contains("career-steps", page);
        Assert.Contains("career-dream-input", page);
        Assert.DoesNotContain("career-dash__bar", page);
        Assert.DoesNotContain("career-dream-select", page);
        Assert.DoesNotContain("<select", page);

        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".career-dash", css);
        Assert.Contains(".career-gauge", css);
        Assert.Contains(".career-steps", css);
        Assert.DoesNotContain(".career-dash__bar {", css);
    }
}
