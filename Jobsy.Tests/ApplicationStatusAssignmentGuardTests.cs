using System.Text.RegularExpressions;

namespace Jobsy.Tests;

/// <summary>
/// Guard: every Application.Status write goes through ApplicationStatusTransitions / recorder / seeders.
/// </summary>
public class ApplicationStatusAssignmentGuardTests
{
    private static readonly Regex StatusAssign = new(
        @"\b(?:application|chosen|other|existing|app|onVacancy|promote)\.Status\s*=",
        RegexOptions.Compiled);

    private static readonly Regex SetPropertyStatus = new(
        @"SetProperty\s*\(\s*a\s*=>\s*a\.Status",
        RegexOptions.Compiled);

    private static readonly HashSet<string> AllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        "ApplicationStatusTransitions.cs",
        "ApplicationStatusRecorder.cs",
        "ApplicationsAndWagesSeeder.cs",
        "Sprint8MetricsSeeder.cs",
        "JobsyDbSeeder.cs",
        "MediaBackfillSeeder.cs"
    };

    [Fact]
    public void No_direct_Application_Status_writes_outside_recorder_and_seeders()
    {
        var root = FindRepoRoot();
        var roots = new[]
        {
            Path.Combine(root, "Jobsy.Api"),
            Path.Combine(root, "Jobsy.Infrastructure")
        };

        var violations = new List<string>();
        foreach (var dir in roots)
        {
            foreach (var file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var name = Path.GetFileName(file);
                if (AllowList.Contains(name) || name.Contains("Seeder", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                foreach (Match m in StatusAssign.Matches(text))
                {
                    violations.Add($"{Relative(root, file)}:{LineOf(text, m.Index)} {m.Value}");
                }

                foreach (Match m in SetPropertyStatus.Matches(text))
                {
                    violations.Add($"{Relative(root, file)}:{LineOf(text, m.Index)} {m.Value}");
                }
            }
        }

        Assert.True(violations.Count == 0, "Direct Application.Status writes:\n" + string.Join("\n", violations));
    }

    private static int LineOf(string text, int index)
        => text[..index].Count(c => c == '\n') + 1;

    private static string Relative(string root, string file)
        => Path.GetRelativePath(root, file);

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

        throw new InvalidOperationException("Jobsy.sln not found");
    }
}
