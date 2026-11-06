namespace Jobsy.Tests;

public sealed class KreeftHeaderStripGuardTests
{
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
