namespace Jobsy.Tests;

public sealed class CareerSsrPageTests
{
    [Fact]
    public void Career_ssr_page_is_static_without_blazor_circuit()
    {
        var page = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Components", "Pages", "Candidate", "CandidateCareerSsr.razor"));
        var shells = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Career", "CareerSsrShells.cs"));
        Assert.Contains("@page \"/carriere\"", page, StringComparison.Ordinal);
        Assert.Contains("[NoBlazorRuntime]", page, StringComparison.Ordinal);
        Assert.Contains("data-enhance-nav=", shells, StringComparison.Ordinal);
        Assert.Contains("carriere-segmented__tab", shells, StringComparison.Ordinal);
        Assert.Contains("/api/career-client/save-dream", shells, StringComparison.Ordinal);
        Assert.Contains("career-ssr.js", shells, StringComparison.Ordinal);
        Assert.Contains("data-career-tab", shells, StringComparison.Ordinal);
        Assert.Contains("data-career-panel", shells, StringComparison.Ordinal);
    }
}
