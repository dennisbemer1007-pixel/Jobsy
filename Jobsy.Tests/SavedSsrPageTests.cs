namespace Jobsy.Tests;

public sealed class SavedSsrPageTests
{
    [Fact]
    public void Saved_page_is_static_ssr_with_post_unsave()
    {
        var page = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Components", "Pages", "Candidate", "CandidateLikedSsr.razor"));
        var shells = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Saved", "SavedSsrShells.cs"));
        Assert.Contains("@page \"/candidate/liked\"", page, StringComparison.Ordinal);
        Assert.Contains("[NoBlazorRuntime]", page, StringComparison.Ordinal);
        Assert.Contains("/api/saved-client/unsave", shells, StringComparison.Ordinal);
        Assert.Contains("data-enhance-nav=", shells, StringComparison.Ordinal);
        Assert.Contains("/carriere", shells, StringComparison.Ordinal);
    }
}
