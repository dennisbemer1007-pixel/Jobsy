namespace Jobsy.Tests;

public sealed class PassportSsrR3GuardTests
{
    [Fact]
    public void Passport_ssr_shell_matches_r3_markers()
    {
        var root = FindRepoRoot();
        var shells = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Passport/PassportSsrShells.cs"));
        Assert.Contains("cand-compact-hero--onboarding", File.ReadAllText(Path.Combine(root, "Jobsy.Web/Passport/PassportSsrHeroHtml.cs")), StringComparison.Ordinal);
        Assert.Contains("passport-lobsy-tip", File.ReadAllText(Path.Combine(root, "Jobsy.Web/Passport/PassportSsrPanelsHtml.cs")), StringComparison.Ordinal);
        Assert.Contains("passport-dna-ring", File.ReadAllText(Path.Combine(root, "Jobsy.Web/Passport/PassportSsrRingHtml.cs")), StringComparison.Ordinal);
        Assert.Contains("data-passport-tab", shells, StringComparison.Ordinal);
        Assert.Contains("data-passport-panel", shells, StringComparison.Ordinal);
        Assert.Contains("passport-ssr-tabs.js", shells, StringComparison.Ordinal);
        Assert.DoesNotContain("KreeftReiskaartHtml", shells, StringComparison.Ordinal);
        Assert.Contains("passport-card__facts", File.ReadAllText(Path.Combine(root, "Jobsy.Web/Passport/PassportSsrPanelsHtml.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void Passport_ssr_tabs_script_is_registered()
    {
        var root = FindRepoRoot();
        var versions = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/wwwroot-asset-versions.json"));
        Assert.Contains("js/passport-ssr-tabs.js", versions, StringComparison.Ordinal);
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
