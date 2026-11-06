using System.Reflection;
using System.Text.RegularExpressions;
using Jobsy.Core.Email.Localization;
using Jobsy.Core.Passport;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

/// <summary>
/// Product brand is Lobsy in all user-visible copy (ADR 0001). Code/infra may keep the Jobsy name.
/// </summary>
public sealed class UserFacingBrandGuardTests
{
    private static readonly Regex QuotedJobsy = new(@"""[^""\\]*(?:\\.[^""\\]*)*Jobsy[^""\\]*(?:\\.[^""\\]*)*""", RegexOptions.Compiled);

    private static readonly HashSet<string> ExemptFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "MockVacancyCopy.cs",
        "MockVacancyCopyBackfill.cs"
    };

    [Fact]
    public void Catalogs_do_not_contain_legacy_brand_Jobsy()
    {
        var hits = new List<string>();
        hits.AddRange(CollectUiStringsHits());
        hits.AddRange(CollectEmailStringHits());
        hits.AddRange(CollectPassportPdfHits());
        hits.AddRange(CollectPupilVerhaalHits());

        Assert.True(
            hits.Count == 0,
            "User-visible catalog values still contain 'Jobsy' (use 'Lobsy'): "
            + string.Join("; ", hits.Take(25)));
    }

    [Fact]
    public void User_facing_resource_files_do_not_contain_quoted_Jobsy()
    {
        var root = FindRepoRoot();
        var hits = new List<string>();

        foreach (var rel in ResourceFileGlobs)
        {
            var full = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(full))
            {
                ScanFile(full, hits);
                continue;
            }

            var dir = Path.GetDirectoryName(full);
            var pattern = Path.GetFileName(full);
            if (dir is null || !Directory.Exists(dir))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(dir, pattern, SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                ScanFile(file, hits);
            }
        }

        Assert.True(
            hits.Count == 0,
            "User-facing resource/text files contain quoted 'Jobsy'. Fix or add a deliberate exempt: "
            + string.Join("; ", hits.Take(25)));
    }

    private static IEnumerable<string> CollectUiStringsHits()
    {
        var catalog = typeof(UiStrings).GetField("Catalog", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null) as Dictionary<string, Dictionary<string, string>>;
        foreach (var (lang, map) in catalog!)
        {
            foreach (var (key, value) in map)
            {
                if (value.Contains("Jobsy", StringComparison.Ordinal))
                {
                    yield return $"UiStrings:{lang}:{key}";
                }
            }
        }
    }

    private static IEnumerable<string> CollectEmailStringHits()
    {
        foreach (var (lang, map) in EmailStrings.All)
        {
            foreach (var (key, value) in map)
            {
                if (value.Contains("Jobsy", StringComparison.Ordinal))
                {
                    yield return $"EmailStrings:{lang}:{key}";
                }
            }
        }
    }

    private static IEnumerable<string> CollectPassportPdfHits()
    {
        foreach (var key in PassportPdfStrings.Keys)
        {
            foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
            {
                var value = PassportPdfStrings.T(lang, key);
                if (value.Contains("Jobsy", StringComparison.Ordinal))
                {
                    yield return $"PassportPdfStrings:{lang}:{key}";
                }
            }
        }
    }

    private static IEnumerable<string> CollectPupilVerhaalHits()
    {
        foreach (var (key, value) in PupilVerhaalCopy.All)
        {
            if (value.Contains("Jobsy", StringComparison.Ordinal))
            {
                yield return $"PupilVerhaalCopy:{key}";
            }
        }
    }

    private static void ScanFile(string path, List<string> hits)
    {
        if (ExemptFiles.Contains(Path.GetFileName(path)))
        {
            return;
        }

        var rel = path.Replace('\\', '/');
        var lines = File.ReadAllLines(path);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (!line.Contains("Jobsy", StringComparison.Ordinal))
            {
                continue;
            }

            if (IsIgnoredCodeLine(line))
            {
                continue;
            }

            if (rel.EndsWith("CookieTable.razor", StringComparison.OrdinalIgnoreCase)
                && line.Contains("new(\"Jobsy.", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (Match m in QuotedJobsy.Matches(line))
            {
                var quoted = m.Value;
                if (quoted.Contains("Jobsy.", StringComparison.Ordinal)
                    && (quoted.Contains(".Auth", StringComparison.Ordinal)
                        || quoted.Contains(".Culture", StringComparison.Ordinal)
                        || quoted.Contains(".Cookie", StringComparison.Ordinal)
                        || quoted.Contains(".LastActivity", StringComparison.Ordinal)
                        || quoted.Contains(".Pwa", StringComparison.Ordinal)
                        || quoted.Contains(".Mfa", StringComparison.Ordinal)
                        || quoted.Contains(".Email", StringComparison.Ordinal)
                        || quoted.Contains(".Login", StringComparison.Ordinal)
                        || quoted.Contains(".Verification", StringComparison.Ordinal)
                        || quoted.Contains("jobsy.gratis", StringComparison.Ordinal)))
                {
                    continue;
                }

                hits.Add($"{rel}:{i + 1}:{quoted.Trim('"')[..Math.Min(60, quoted.Length - 2)]}");
            }
        }
    }

    private static bool IsIgnoredCodeLine(string line)
    {
        var t = line.TrimStart();
        return t.StartsWith("using ", StringComparison.Ordinal)
               || t.StartsWith("namespace ", StringComparison.Ordinal)
               || t.StartsWith("@using ", StringComparison.Ordinal)
               || t.StartsWith("//", StringComparison.Ordinal)
               || t.StartsWith("*", StringComparison.Ordinal)
               || t.StartsWith("///", StringComparison.Ordinal)
               || t.Contains("JobsyLanguages", StringComparison.Ordinal)
               || t.Contains("JobsyCultures", StringComparison.Ordinal)
               || t.Contains("JobsyApi", StringComparison.Ordinal)
               || t.Contains("JobsyRoles", StringComparison.Ordinal)
               || t.Contains("JobsyDb", StringComparison.Ordinal)
               || t.Contains("JobsyAuth", StringComparison.Ordinal)
               || t.Contains("JobsyPublicUrl", StringComparison.Ordinal)
               || t.Contains("JobsyPublic", StringComparison.Ordinal)
               || t.Contains("JobsyClaim", StringComparison.Ordinal)
               || t.Contains("JobsyCookie", StringComparison.Ordinal)
               || t.Contains("JobsyJs", StringComparison.Ordinal)
               || t.Contains("JobsyPasswordHasher", StringComparison.Ordinal)
               || t.Contains("JobsyDbContext", StringComparison.Ordinal)
               || t.Contains("Jobsy.Core.", StringComparison.Ordinal)
               || t.Contains("Jobsy.Web.", StringComparison.Ordinal)
               || t.Contains("Jobsy.Infrastructure.", StringComparison.Ordinal)
               || t.Contains("Jobsy.Api.", StringComparison.Ordinal)
               || t.Contains("X-Jobsy", StringComparison.Ordinal)
               || t.Contains("jobsy.local", StringComparison.Ordinal)
               || t.Contains("Jobsy123", StringComparison.Ordinal)
               || t.Contains("localStorage", StringComparison.Ordinal)
               || t.Contains("getItem(", StringComparison.Ordinal)
               || t.Contains("LogicalName", StringComparison.Ordinal)
               || t.Contains("HttpClientName", StringComparison.Ordinal)
               || t.Contains("CreateClient(", StringComparison.Ordinal)
               || t.Contains("configuration[", StringComparison.Ordinal)
               || t.Contains("GetConnectionString", StringComparison.Ordinal)
               || t.Contains("LogInformation(", StringComparison.Ordinal)
               || t.Contains("FindRepoFile(", StringComparison.Ordinal)
               || t.Contains("global.Jobsy", StringComparison.Ordinal)
               || t.Contains("window.Jobsy", StringComparison.Ordinal);
    }

    private static readonly string[] ResourceFileGlobs =
    [
        "Jobsy.Web/Localization/*.cs",
        "Jobsy.Core/Email/Localization/*.cs",
        "Jobsy.Core/Passport/PassportPdfStrings.cs",
        "Jobsy.Core/Reports/CompactPdfCopy.cs",
        "Jobsy.Core/Scholen/PupilVerhaalCopy.cs",
        "Jobsy.Infrastructure/Data/DemoUsersSeeder.cs",
        "Jobsy.Web/wwwroot/manifest.webmanifest",
        "ops/maintenance/index.html",
        "Jobsy.Web/Components/App.razor",
        "Jobsy.Web/Components/Legal/CookieTable.razor",
        "Jobsy.Web/Components/Legal/Docs/*.razor",
        "Jobsy.Web/Components/Errors/*.razor",
        "Jobsy.Web/Components/LobsyLogo.razor"
    ];

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

        throw new InvalidOperationException("Could not find repo root (Jobsy.sln).");
    }
}
