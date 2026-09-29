namespace Jobsy.Tests;

/// <summary>
/// Layout smoke for Mijn Paspoort (flag ON). Full browser checks need a live host + flag ON.
/// </summary>
public class MijnPaspoortPlaywrightTests
{
    [Fact]
    public void Passport_css_keeps_sticky_tabs_and_compact_overview()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "mijn-paspoort.css"));
        Assert.Contains("position: sticky", css, StringComparison.Ordinal);
        Assert.Contains(".passport-tabs", css, StringComparison.Ordinal);
        Assert.Contains("overflow-x: auto", css, StringComparison.Ordinal);
        Assert.Contains("@media (min-width: 1024px)", css, StringComparison.Ordinal);
        Assert.Contains(".passport-tests__", css, StringComparison.Ordinal);
        Assert.Contains(".passport-course", css, StringComparison.Ordinal);
        Assert.Contains("passport-tests__depth-seg--1", css, StringComparison.Ordinal);
        Assert.Contains("--accent-soft", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Passport_tests_tab_replaces_phase1_placeholder()
    {
        var page = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Candidate", "Passport.razor"));
        Assert.Contains("PassportTestsTab", page, StringComparison.Ordinal);
        Assert.DoesNotContain("<TestsOverviewPanel", page, StringComparison.Ordinal);
        var tab = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Candidate", "Passport", "PassportTestsTab.razor"));
        Assert.Contains("TestsOverviewBuilder", tab, StringComparison.Ordinal);
        Assert.Contains("CourseSuggestionBlock", tab, StringComparison.Ordinal);
        Assert.Contains("TestResultLockedPreview", tab, StringComparison.Ordinal);
        var panel = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Candidate", "TestsOverviewPanel.razor"));
        Assert.Contains("TestsOverviewBuilder", panel, StringComparison.Ordinal);
        Assert.Contains("test-tile-grid", panel, StringComparison.Ordinal);
    }

    [Fact]
    public void Passport_tests_layout_fits_desktop_and_mobile_breakpoints()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "mijn-paspoort.css"));
        Assert.Contains("@media (min-width: 1024px)", css, StringComparison.Ordinal);
        Assert.Contains("passport-tests__layout", css, StringComparison.Ordinal);
        Assert.Contains("@media (min-width: 640px)", css, StringComparison.Ordinal);
        // No vertical expansion beyond viewport intent: sticky aside + compact rows.
        Assert.Contains("passport-tests__row", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Passport_page_uses_requires_feature_and_candidate_role()
    {
        var page = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Candidate", "Passport.razor"));
        Assert.Contains("Authorize(Roles = \"Candidate\")", page, StringComparison.Ordinal);
        Assert.Contains("RequiresFeature(PlatformFeature.CandidatePassport", page, StringComparison.Ordinal);
        Assert.Contains("FallbackPath = \"/candidate/profile\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Deel mijn paspoort", page, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Repo root not found.");
    }
}
