namespace Jobsy.Tests;

public class CandidateVacanciesTabTests
{
    [Fact]
    public void Recently_viewed_is_local_guid_storage_not_email()
    {
        var root = FindRepoRoot();
        var geo = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/geo.js"));
        var core = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/app-core.js"));
        foreach (var js in new[] { geo, core })
        {
            Assert.Contains("jobsy.recentlyViewedVacancies", js);
            Assert.Contains("rememberViewedVacancy", js);
            Assert.Contains("listRecentlyViewed", js);
            Assert.Contains("UUID_RE", js);
            Assert.Contains("RECENT_MAX = 20", js);
            Assert.DoesNotContain("FindFirst(ClaimTypes.Email)", js);
        }

        var helper = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Navigation/RecentlyViewedVacancies.cs"));
        Assert.Contains("Stores GUIDs, never emails", helper);
        Assert.DoesNotContain("ClaimTypes.Email", helper);
        Assert.DoesNotContain("ClaimTypes.NameIdentifier", helper);

        var detail = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/VacancyDetail.razor"));
        Assert.Contains("RecentlyViewedVacancies.RememberAsync", detail);

        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/Vacancies.razor"));
        Assert.Contains("@page \"/candidate/vacancies\"", page);
        Assert.Contains("Roles = \"Candidate\"", page);
        Assert.Contains("CandidateVacancies.RecentlyViewed", page);
        Assert.Contains("pb-28", page);
        Assert.Contains("DiscoverVacanciesAsync", page);
        Assert.Contains("aria-selected=\"@(_tab == \"overview\" ? \"true\" : \"false\")\"", page);
    }

    [Fact]
    public void Side_tabs_and_bottom_nav_clearance_stay_in_place()
    {
        var root = FindRepoRoot();
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".lobsy-assistant-tab {\n    position: fixed;\n    top: 26%;\n    right: 0;", css);
        Assert.Contains(".feedback-widget {\n    position: fixed;\n    top: 46%;\n    right: 0;", css);
        Assert.Contains(".pb-28 { padding-bottom: 7rem; }", css);
        Assert.Contains(".bottom-nav {\n    position: fixed;\n    bottom: 0;\n    left: 0;\n    right: 0;\n    z-index: 50;", css);
        Assert.Contains(".candidate-vacancies-page .candidate-vacancies__section", css);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find Jobsy.sln");
    }
}
