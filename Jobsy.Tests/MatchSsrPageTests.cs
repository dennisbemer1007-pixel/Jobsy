using Jobsy.Web.Banenkaart;

namespace Jobsy.Tests;

public sealed class MatchSsrPageTests
{
    [Fact]
    public void Match_page_is_static_ssr_with_client_swipe_script()
    {
        var page = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Components", "Pages", "Candidate", "CandidateMatchSsr.razor"));
        var shells = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Match", "MatchSsrShells.cs"));
        var swipe = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Match", "MatchSsrSwipeHtml.cs"));
        Assert.Contains("@page \"/candidate/match\"", page, StringComparison.Ordinal);
        Assert.Contains("[NoBlazorRuntime]", page, StringComparison.Ordinal);
        Assert.Contains("match-ssr.js", shells, StringComparison.Ordinal);
        Assert.Contains("data-match-action", swipe, StringComparison.Ordinal);
        Assert.Contains(BanenkaartSsrQuery.SavePath, shells, StringComparison.Ordinal);
    }
}
