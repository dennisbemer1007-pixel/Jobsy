namespace Jobsy.Tests;

public sealed class WatDrijftJouSsrTests
{
    [Fact]
    public void Wat_drijft_jou_skips_blazor_runtime()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Candidate/WatDrijftJou.razor"));
        Assert.Contains("[NoBlazorRuntime]", source, StringComparison.Ordinal);
        Assert.Contains("[ExcludeFromInteractiveRouting]", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveServerRenderMode", source, StringComparison.Ordinal);
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
