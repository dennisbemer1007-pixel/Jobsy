using System.Reflection;
using System.Text.RegularExpressions;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

/// <summary>
/// Source-scan guard: every <c>new Company</c> in Api/Infrastructure (except Migrations)
/// must assign <c>VerificationStatus</c>. Allow-list: CandidateApplicationLocation.
/// </summary>
public class CompanyCreationSetsVerificationTests
{
    private static readonly string[] Roots =
    [
        Path.Combine(FindRepoRoot(), "Jobsy.Api"),
        Path.Combine(FindRepoRoot(), "Jobsy.Infrastructure")
    ];

    private static readonly HashSet<string> AllowListFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "CandidateApplicationLocation.cs"
    };

    [Fact]
    public void Every_new_Company_initializer_assigns_VerificationStatus()
    {
        var failures = new List<string>();
        var pattern = new Regex(@"new\s+Company\s*\{", RegexOptions.Compiled);

        foreach (var root in Roots)
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                if (AllowListFiles.Contains(Path.GetFileName(file)))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                foreach (Match match in pattern.Matches(text))
                {
                    var block = ExtractInitializer(text, match.Index);
                    if (block is null)
                    {
                        failures.Add($"{Relative(file)}: could not parse Company initializer");
                        continue;
                    }

                    if (!block.Contains("VerificationStatus", StringComparison.Ordinal))
                    {
                        failures.Add($"{Relative(file)}: new Company without VerificationStatus");
                    }
                }
            }
        }

        Assert.True(
            failures.Count == 0,
            "Company creation paths must set VerificationStatus:\n" + string.Join("\n", failures));
    }

    private static string? ExtractInitializer(string text, int start)
    {
        var open = text.IndexOf('{', start);
        if (open < 0)
        {
            return null;
        }

        var depth = 0;
        for (var i = open; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                depth++;
            }
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return text[open..(i + 1)];
                }
            }
        }

        return null;
    }

    private static string Relative(string path)
        => Path.GetRelativePath(FindRepoRoot(), path);

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
