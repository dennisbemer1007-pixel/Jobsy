namespace Jobsy.Tests;

public sealed class CandidateSettingsDangerLinkTests
{
    [Fact]
    public void Settings_calm_links_target_distinct_danger_intents()
    {
        var root = FindRepoRoot();
        var shells = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Settings/SettingsSsrShells.cs"));
        Assert.Contains("data-settings-danger=", shells, StringComparison.Ordinal);
        Assert.Contains("reset", shells, StringComparison.Ordinal);
        Assert.Contains("delete", shells, StringComparison.Ordinal);
        Assert.Contains("CandidateSettings.NavAccount", shells, StringComparison.Ordinal);
        Assert.Contains("cand-settings__nav-action", shells, StringComparison.Ordinal);
        Assert.DoesNotContain("cand-settings__calm-links", shells, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
