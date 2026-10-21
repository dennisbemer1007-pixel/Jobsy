namespace Jobsy.Tests;

public sealed class CandidateSettingsDangerLinkTests
{
    [Fact]
    public void Settings_calm_links_target_distinct_danger_intents()
    {
        var root = FindRepoRoot();
        var settings = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateSettings.razor"));
        Assert.Contains("/candidate/paspoort?tab=data&danger=reset", settings, StringComparison.Ordinal);
        Assert.Contains("/candidate/paspoort?tab=data&danger=delete", settings, StringComparison.Ordinal);
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
