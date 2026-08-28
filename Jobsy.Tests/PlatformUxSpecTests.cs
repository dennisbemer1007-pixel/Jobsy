using Jobsy.Core.Rules;
using Jobsy.Web.Auth;

namespace Jobsy.Tests;

public class PlatformUxSpecTests
{
    [Fact]
    public void ManagedVacancyMapFilter_requires_active_and_unfulfilled()
    {
        Assert.True(ManagedVacancyMapFilter.IsPublishedOpen("Active", null));
        Assert.False(ManagedVacancyMapFilter.IsPublishedOpen("Draft", null));
        Assert.False(ManagedVacancyMapFilter.IsPublishedOpen("Active", Guid.NewGuid()));
    }

    [Theory]
    [InlineData("Pending", 0, false)]
    [InlineData("Accepted", 1, false)]
    [InlineData("EmployerContacting", 2, false)]
    [InlineData("Hired", 3, false)]
    [InlineData("Rejected", 1, true)]
    [InlineData("Withdrawn", 1, true)]
    public void Application_wizard_steps(string status, int current, bool rejected)
    {
        Assert.Equal(rejected, ApplicationStatusWizard.IsRejectedTrack(status));
        Assert.Equal(current, ApplicationStatusWizard.CurrentStepIndex(status));
        Assert.Equal(rejected ? 2 : 4, ApplicationStatusWizard.StepsFor(status).Length);
    }

    [Fact]
    public void Auth_preserves_vacancy_return_url()
    {
        Assert.Equal("/vacancies/123", AuthRedirects.ResolveCandidateReturnUrl("/vacancies/123", true));
        Assert.Equal(AuthRedirects.BanenkaartPath, AuthRedirects.ResolveCandidateReturnUrl("/home", false));
    }

    [Fact]
    public void Job_map_highlight_toggles_class_without_rewriting_marker_html()
    {
        var js = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        var highlightIdx = js.IndexOf("function highlight(id)", StringComparison.Ordinal);
        Assert.True(highlightIdx > 0);
        var nextFn = js.IndexOf("function focus(id)", highlightIdx, StringComparison.Ordinal);
        var highlight = js[highlightIdx..nextFn];
        Assert.Contains("applyMarkerSelected", highlight);
        Assert.Contains("classList.toggle", highlight);
        Assert.DoesNotContain("fillMarkerElement", highlight);
        Assert.DoesNotContain("innerHTML", highlight);
        Assert.DoesNotContain("style.zIndex", highlight);
    }

    [Fact]
    public void Job_map_css_keeps_popups_above_travel_labels()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css"));
        Assert.Contains(".job-map .maplibregl-popup", css);
        Assert.Contains("z-index: 40 !important", css);
        Assert.Contains(".travel-ring-label", css);
        Assert.Contains("z-index: 1 !important", css);
        Assert.DoesNotContain("z-index: 1200 !important", css);
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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
