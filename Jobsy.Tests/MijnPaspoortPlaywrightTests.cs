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
