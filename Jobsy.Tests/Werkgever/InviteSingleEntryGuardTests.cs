using System.Text.RegularExpressions;

namespace Jobsy.Tests.Werkgever;

public class InviteSingleEntryGuardTests
{
    [Fact]
    public void InviteCompanyUserAsync_is_only_called_from_WgInviteDrawer()
    {
        var root = FindRepoRoot();
        var web = Path.Combine(root, "Jobsy.Web");
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(web, "*.*", SearchOption.AllDirectories))
        {
            if (!file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                && !file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var rel = Path.GetRelativePath(web, file).Replace('\\', '/');
            if (rel.Equals("Components/Werkgever/Team/WgInviteDrawer.razor", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (rel.Equals("Services/ApiClient/JobsyApiClient.Employer.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            if (Regex.IsMatch(text, @"InviteCompanyUserAsync\s*\("))
            {
                offenders.Add(rel);
            }
        }

        Assert.True(offenders.Count == 0,
            "InviteCompanyUserAsync must only be called from WgInviteDrawer. Offenders: "
            + string.Join(", ", offenders));
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

        throw new InvalidOperationException("Repo root not found.");
    }
}
