using System.Text.RegularExpressions;

namespace Jobsy.Tests;

/// <summary>
/// Candidate-facing Web surfaces must not send people to company KvK <c>/register</c>.
/// Allow-listed employer / admin / SEO paths may still mention it.
/// </summary>
public class CandidateRegisterLinkGuardTests
{
    private static readonly HashSet<string> AllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        "Register.razor",
        "Login.razor",
        "PartnerSales.razor",
        "WestlandTeaser.razor",
        "PageSeoCatalog.cs",
        "PageHelpDocs.cs",
        "SeoEndpoints.cs",
        "Admin.cs",
        "UiStrings.cs",
        "UiStringsExtras.cs",
        "UiStringsLanding.cs",
        "PublicRoutes.cs",
        "RegisterOntdekRedirectMiddleware.cs",
        "JobsyApiAuthHandler.cs",
        "HowLobsyRoleGuides.cs", // historical string may linger in comments only
    };

    [Fact]
    public void Candidate_facing_razor_and_guides_do_not_link_to_register()
    {
        var root = FindRepoRoot();
        var web = Path.Combine(root, "Jobsy.Web");
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(web, "*.*", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var ext = Path.GetExtension(file);
            if (ext is not (".razor" or ".cs"))
            {
                continue;
            }

            var name = Path.GetFileName(file);
            if (AllowList.Contains(name))
            {
                continue;
            }

            // Employer / admin / partner marketing may keep /register.
            var rel = Path.GetRelativePath(web, file).Replace('\\', '/');
            if (rel.StartsWith("Components/Pages/Branch/", StringComparison.OrdinalIgnoreCase)
                || rel.StartsWith("Components/Pages/Employer/", StringComparison.OrdinalIgnoreCase)
                || rel.StartsWith("Components/Pages/Admin/", StringComparison.OrdinalIgnoreCase)
                || rel.StartsWith("Components/Pages/Regional/", StringComparison.OrdinalIgnoreCase)
                || rel.StartsWith("Components/Pages/Intermediary/", StringComparison.OrdinalIgnoreCase)
                || rel.StartsWith("Components/Pages/SalesManager/", StringComparison.OrdinalIgnoreCase)
                || rel.StartsWith("Components/Pages/Partner/", StringComparison.OrdinalIgnoreCase)
                || rel.StartsWith("Components/Admin/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            if (Regex.IsMatch(text, """href\s*=\s*["']/register""", RegexOptions.IgnoreCase)
                || Regex.IsMatch(text, """["']/register\?van=ontdek["']""", RegexOptions.IgnoreCase)
                || text.Contains("=> \"/register?van=ontdek\"", StringComparison.Ordinal)
                || text.Contains("= \"/register?van=ontdek\"", StringComparison.Ordinal))
            {
                offenders.Add(rel);
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Candidate-facing /register links remain:\n" + string.Join("\n", offenders));
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

        return Directory.GetCurrentDirectory();
    }
}
