namespace Jobsy.Tests;

public sealed class SollicitatiesSsrHeadAssetTests
{
    [Fact]
    public void Applications_ssr_head_uses_slim_stylesheets_not_app_min_or_applications_css()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(),
            "Jobsy.Web/Components/Pages/Candidate/CandidateApplicationsSsr.razor"));
        Assert.Contains("SollicitatiesSsrStylesheets()", razor, StringComparison.Ordinal);
        Assert.DoesNotContain("StylesheetsFor(", razor, StringComparison.Ordinal);
        Assert.DoesNotContain("applications.css", razor, StringComparison.Ordinal);
        Assert.DoesNotContain("app.min.css", razor, StringComparison.Ordinal);
        Assert.Contains("layout9", razor, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Path.GetDirectoryName(dir)!;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
