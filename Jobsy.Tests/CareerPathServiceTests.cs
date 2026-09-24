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
    public void Switching_dream_role_changes_path_and_match()
    {
        var svc = new CareerPathService();
        var logistics = svc.GetDashboard("teamleider-logistiek");
        var retail = svc.GetDashboard("filiaalmanager");
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
    public void Unknown_dream_id_falls_back_to_default()
    {
        var svc = new CareerPathService();
        var dash = svc.GetDashboard("onbekend-doel");
        Assert.Equal(CareerPathService.DefaultDreamId, dash.DreamRoleId);
    }

    [Fact]
    public void Career_dashboard_page_is_routed_and_uses_service()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CareerDashboard.razor"));
        Assert.Contains("@page \"/carriere\"", page);
        Assert.Contains("CareerPathService", page);
        Assert.Contains("career-timeline", page);
        Assert.Contains("career-dash__bar", page);

        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".career-dash", css);
        Assert.Contains(".career-timeline__toggle", css);
    }
}
