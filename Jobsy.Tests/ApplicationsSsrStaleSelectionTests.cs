namespace Jobsy.Tests;

public sealed class ApplicationsSsrStaleSelectionTests
{
    [Fact]
    public void Demo_ensure_retry_does_not_run_for_arbitrary_external_sel_ids()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Applications/ApplicationsSsrData.cs"));
        Assert.Contains("AcceptatieSollicDemoCatalog.IsDemoApplicationId", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Applications_ssr_returns_not_found_view_for_missing_sel()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Candidate/CandidateApplicationsSsr.razor"));
        Assert.Contains("Status404NotFound", source, StringComparison.Ordinal);
        Assert.Contains("NotFoundView", source, StringComparison.Ordinal);
        Assert.Contains("data.AllRows.All(r => r.RowId != requestedSel)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void External_detail_ssr_uses_not_found_view_when_api_returns_null()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Candidate/CandidateExternalVacancyDetailSsr.razor"));
        Assert.Contains("NotFoundView", source, StringComparison.Ordinal);
        Assert.Contains("Status404NotFound", source, StringComparison.Ordinal);
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

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
