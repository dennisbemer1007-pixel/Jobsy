namespace Jobsy.Tests;

public sealed class AdviseurWerkSsrTests
{
    [Fact]
    public void Adviseur_werk_skips_blazor_runtime()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Candidate/AdviseurWerk.razor"));
        Assert.Contains("[NoBlazorRuntime]", source, StringComparison.Ordinal);
        Assert.Contains("uwv-adviseur-panel.js", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UwvAdviseurSharePanel", source, StringComparison.Ordinal);
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
