using System.Text.RegularExpressions;
using Jobsy.Core.Authorization;

namespace Jobsy.Tests;

/// <summary>
/// Lightweight Blazor guard: mutation-oriented pages must not list RegionalManager
/// in <c>[Authorize(Roles=...)]</c>. API remains the source of truth.
/// </summary>
public class BlazorPageRoleAttributesTests
{
    private static readonly Regex PageDirective = new(
        @"@page\s+""([^""]+)""",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex AuthorizeRoles = new(
        @"@attribute\s+\[\s*(?:Microsoft\.AspNetCore\.Authorization\.)?Authorize\s*\(\s*Roles\s*=\s*""([^""]+)""\s*\)\s*\]",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>Pages that look mutating but intentionally keep RegionalManager for read UI.</summary>
    private static readonly HashSet<string> RegionalManagerPageAllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        // Balance / logging read; purchase + allocate buttons gated in-page via CanPurchase/CanAllocate.
        "Tokens.razor",
    };

    [Fact]
    public void Mutation_oriented_pages_do_not_authorize_RegionalManager()
    {
        var pagesDir = Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages");
        Assert.True(Directory.Exists(pagesDir));

        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(pagesDir, "*.razor", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            if (!PageDirective.IsMatch(text))
            {
                continue;
            }

            var name = Path.GetFileName(file);
            if (!ImpliesMutation(name, text))
            {
                continue;
            }

            if (RegionalManagerPageAllowList.Contains(name))
            {
                continue;
            }

            var rolesMatch = AuthorizeRoles.Match(text);
            if (!rolesMatch.Success)
            {
                continue;
            }

            var roles = rolesMatch.Groups[1].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (roles.Any(r => string.Equals(r, JobsyRoles.RegionalManager, StringComparison.OrdinalIgnoreCase)))
            {
                offenders.Add($"{Relative(file)} roles={rolesMatch.Groups[1].Value}");
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Mutation-oriented Blazor pages still authorize RegionalManager:\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void OnboardingCheckout_excludes_RegionalManager()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "Jobsy.Web",
            "Components",
            "Pages",
            "Employer",
            "OnboardingCheckout.razor");
        var text = File.ReadAllText(path);
        var roles = AuthorizeRoles.Match(text).Groups[1].Value;
        Assert.DoesNotContain(JobsyRoles.RegionalManager, roles, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>05 §1 H2: the candidate how-to guide is candidate-only; other roles use /hoe-werkt-lobsy.</summary>
    [Fact]
    public void Candidate_how_to_guide_is_candidate_only()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "Jobsy.Web",
            "Components",
            "Pages",
            "Candidate",
            "HowLobsyWorks.razor");
        var text = File.ReadAllText(path);
        Assert.Contains("@page \"/candidate/hoe-werkt-lobsy\"", text, StringComparison.Ordinal);

        var roles = AuthorizeRoles.Match(text).Groups[1].Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal([JobsyRoles.Candidate], roles);
    }

    private static bool ImpliesMutation(string fileName, string text)
    {
        if (fileName.Contains("Create", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains("Edit", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains("Checkout", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains("OnboardingCheckout", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("Tokens.razor", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Route heuristics
        return text.Contains("@page \"/werkgever/vacatures/nieuw\"", StringComparison.Ordinal)
               || text.Contains("@page \"/employer/onboarding-checkout\"", StringComparison.Ordinal)
               || text.Contains("vacancies/create", StringComparison.OrdinalIgnoreCase);
    }

    private static string Relative(string fullPath)
    {
        var root = FindRepoRoot();
        return fullPath.StartsWith(root, StringComparison.Ordinal)
            ? fullPath[(root.Length + 1)..].Replace('\\', '/')
            : fullPath;
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
