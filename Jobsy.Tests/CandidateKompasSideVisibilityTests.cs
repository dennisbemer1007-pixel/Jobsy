namespace Jobsy.Tests;

/// <summary>Acc 27-09 §3: Top 10 side panel depends only on viewport width.</summary>
public class CandidateKompasSideVisibilityTests
{
    [Fact]
    public void CandidateKompas_source_gates_side_on_viewport_only()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(
            Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateKompas.razor"));
        Assert.Contains("private bool ShouldShowSide => _isWideViewport;", razor, StringComparison.Ordinal);
        Assert.DoesNotContain("_tab != CandidateKompasTabs.Dna || _isWideViewport", razor, StringComparison.Ordinal);
        Assert.Contains("private bool _isWideViewport;", razor, StringComparison.Ordinal);
        Assert.Contains("watchKompasWide", razor, StringComparison.Ordinal);
        Assert.Contains("nameof(SetWide)", razor, StringComparison.Ordinal);

        var profile = File.ReadAllText(
            Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/Profile.razor"));
        Assert.Contains("_isWideViewport", profile, StringComparison.Ordinal);
        Assert.Contains("OnKompasWideChanged", profile, StringComparison.Ordinal);

        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".kompas-workspace__side", css, StringComparison.Ordinal);
        Assert.Contains("display: none !important;", css, StringComparison.Ordinal);

        // Questionnaire match cards live in the feature stylesheet (step 1 extract).
        var qCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/questionnaire.css"));
        Assert.Contains(".questionnaire-matches", qCss, StringComparison.Ordinal);
        Assert.Contains("display: none !important;", qCss, StringComparison.Ordinal);

        var min = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.min.css"));
        Assert.Contains(".kompas-workspace__side", min, StringComparison.Ordinal);
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
