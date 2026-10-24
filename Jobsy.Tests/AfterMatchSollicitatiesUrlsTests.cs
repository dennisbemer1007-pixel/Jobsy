using Jobsy.Web.Applications;

namespace Jobsy.Tests;

public sealed class AfterMatchSollicitatiesUrlsTests
{
    [Fact]
    public void PickSlot_includes_panel_and_return()
    {
        var appId = Guid.Parse("77777771-0002-4000-8000-000000000002");
        var url = AfterMatchSollicitatiesUrls.PickSlot(appId, "/candidate/applications?tab=berichten&thread=abc");
        Assert.Contains("panel=pick-slot", url, StringComparison.Ordinal);
        Assert.Contains("return=", url, StringComparison.Ordinal);
        Assert.Contains(Uri.EscapeDataString("/candidate/applications?tab=berichten&thread=abc"), url, StringComparison.Ordinal);
    }

    [Fact]
    public void Candidate_after_match_messages_has_seo_and_direct_back_nav()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatch.razor"));
        Assert.Contains("<PageSeo Title=\"@_pageTitle\" />", razor, StringComparison.Ordinal);
        Assert.Contains("data-enhance-nav=\"false\"", razor, StringComparison.Ordinal);
        Assert.Contains("AfterMatch.Link.BackBerichten", razor, StringComparison.Ordinal);
        Assert.Contains("ResolveInitialPageTitle", razor, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Repo root not found");
    }
}
