namespace Jobsy.Tests;

public sealed class KreeftHeaderStripGuardTests
{
    [Fact]
    public void Sollicitaties_tabs_use_distinct_kreeft_strips()
    {
        var root = FindRepoRoot();
        var shells = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Applications", "ApplicationsSsrShells.cs"));
        Assert.Contains("KreeftHeaderStripAssets.ActiviteitenDesk", shells, StringComparison.Ordinal);
        Assert.Contains("KreeftHeaderStripAssets.BerichtenDesk", shells, StringComparison.Ordinal);
        Assert.Contains("KreeftHeaderStripAssets.SollicitatiesDesk", shells, StringComparison.Ordinal);
        Assert.DoesNotContain("sollicitaties-hero-strip-desktop", shells, StringComparison.Ordinal);
    }

    [Fact]
    public void After_match_panels_use_kreeft_strips_not_shared_sollicitaties_hero()
    {
        var root = FindRepoRoot();
        Assert.Contains("KreeftHeaderStripAssets.NaDeMatchDesk", ReadApp(root, "AfterMatchConfirmedSsrShells.cs"), StringComparison.Ordinal);
        Assert.Contains("KreeftHeaderStripAssets.MomentDesk", ReadApp(root, "AfterMatchPickSlotSsrShells.cs"), StringComparison.Ordinal);
        Assert.Contains("KreeftHeaderStripAssets.BerichtenDesk", ReadApp(root, "AfterMatchMessagesSsrShells.cs"), StringComparison.Ordinal);
        Assert.Contains("KreeftHeaderStripAssets.NuNietDesk", ReadApp(root, "AfterMatchOutcomeSsrShells.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public void Career_tabs_use_carriere_and_omscholen_strips()
    {
        var root = FindRepoRoot();
        var career = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Career", "CareerSsrShells.cs"));
        Assert.Contains("KreeftHeaderStripAssets.CarriereDesk", career, StringComparison.Ordinal);
        Assert.Contains("KreeftHeaderStripAssets.OmscholenDesk", career, StringComparison.Ordinal);
        Assert.DoesNotContain("droombaan-profession-wide", career, StringComparison.Ordinal);
    }

    [Fact]
    public void New_kreeft_header_pages_reference_strip_assets()
    {
        var root = FindRepoRoot();
        Assert.Contains("KreeftHeaderStripAssets.ToevoegenDesk", ReadWeb(root, "Components/Pages/Candidate/ExternalVacancyAdd.razor"), StringComparison.Ordinal);
        Assert.Contains("KreeftHeaderStripAssets.AdviseurDesk", ReadWeb(root, "Components/Pages/Candidate/AdviseurWerk.razor"), StringComparison.Ordinal);
        Assert.Contains("KreeftHeaderStripAssets.MatchDesk", ReadWeb(root, "Match/MatchSsrShells.cs"), StringComparison.Ordinal);
        Assert.Contains("KreeftHeaderStripAssets.OndernemerDesk", ReadWeb(root, "Components/Pages/Candidate/OndernemersCheck.razor"), StringComparison.Ordinal);
    }

    [Fact]
    public void Error_views_use_kreeft_art_not_lobsy_mascot()
    {
        var root = FindRepoRoot();
        var notFound = ReadWeb(root, "Components/Errors/NotFoundView.razor");
        Assert.Contains("KreeftHeaderStrip", notFound, StringComparison.Ordinal);
        Assert.Contains("RouteKaartDesk", notFound, StringComparison.Ordinal);
        Assert.DoesNotContain("LobsyMascot", notFound, StringComparison.Ordinal);

        var errorPage = ReadWeb(root, "Components/Pages/Error.razor");
        Assert.Contains("KreeftErrorIllustrations", errorPage, StringComparison.Ordinal);
        Assert.DoesNotContain("LobsyMascot", errorPage, StringComparison.Ordinal);
    }

    private static string ReadApp(string root, string fileName)
        => File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Applications", fileName));

    private static string ReadWeb(string root, string relativePath)
        => File.ReadAllText(Path.Combine(root, "Jobsy.Web", relativePath));

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
