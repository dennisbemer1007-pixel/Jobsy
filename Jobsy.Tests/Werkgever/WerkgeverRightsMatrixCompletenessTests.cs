using System.Text.RegularExpressions;

namespace Jobsy.Tests.Werkgever;

public class WerkgeverRightsMatrixCompletenessTests
{
    private static readonly Regex PageDirective = new(
        @"@page\s+""(/werkgever[^""]*)""",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex RouteAttr = new(
        @"\[Route\(""([^""]+)""\)\]",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex HttpMutate = new(
        @"\[Http(Post|Put|Patch|Delete)(?:\(""([^""]*)""\))?\]",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly string[] EmployerControllerFiles =
    [
        "VacanciesController.cs",
        "ApplicationsController.cs",
        "CompanyUsersController.cs",
        "RegionsController.cs",
        "CompaniesController.cs",
        "TokensController.cs",
        "SalaryTablesController.cs",
        "CandidateInsightsController.cs",
        "WerkgeverTokenRequestsController.cs",
        "RegistrationController.cs",
        "TalentPoolController.cs",
        "CompanyCultureController.cs",
        "CompanyApiKeysController.cs",
        "VacancyCsvImportController.cs",
    ];

    [Fact]
    public void Matrix_covers_every_werkgever_page_route()
    {
        var root = FindRepoRoot();
        var dir = Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Werkgever");
        var routes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(dir, "*.razor"))
        {
            if (Path.GetFileName(file).Contains("Legacy", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (Match m in PageDirective.Matches(text))
            {
                routes.Add(m.Groups[1].Value);
            }
        }

        var matrixRoutes = WerkgeverRightsMatrix.Pages
            .Select(p => p.Route)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = routes.Where(r => !matrixRoutes.Contains(r)).OrderBy(r => r).ToList();
        Assert.True(missing.Count == 0,
            "WerkgeverRightsMatrix.Pages missing routes:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void Matrix_covers_mutating_employer_endpoints()
    {
        var root = FindRepoRoot();
        var dir = Path.Combine(root, "Jobsy.Api", "Controllers");
        var discovered = new List<string>();

        foreach (var fileName in EmployerControllerFiles)
        {
            var path = Path.Combine(dir, fileName);
            Assert.True(File.Exists(path), path);
            var text = File.ReadAllText(path);
            var controllerRoute = RouteAttr.Match(text).Groups[1].Value;
            if (string.IsNullOrWhiteSpace(controllerRoute))
            {
                continue;
            }

            // Walk lines; track last [Route] that may override for subsequent actions (TalentPool).
            var activeRoute = controllerRoute;
            var lines = text.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var routeMatch = RouteAttr.Match(line);
                if (routeMatch.Success && i > 0)
                {
                    // Class-level already captured; method-level route attributes are rare — TalentPool has a second [Route] on class? 
                    // Actually TalentPool has [Route] on a nested region — detect route before Http attrs.
                    if (!line.Contains("ApiController", StringComparison.Ordinal)
                        && !IsClassLevelRoute(lines, i))
                    {
                        activeRoute = routeMatch.Groups[1].Value;
                    }
                }

                var http = HttpMutate.Match(line);
                if (!http.Success)
                {
                    continue;
                }

                var action = http.Groups[2].Success ? http.Groups[2].Value : "";
                var full = Combine(activeRoute, action);

                if (ShouldSkip(fileName, full))
                {
                    continue;
                }

                discovered.Add(Normalize(full));
            }
        }

        var matrix = WerkgeverRightsMatrix.Apis.Select(a => Normalize(a.Path)).ToList();
        var missing = discovered
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(d => !matrix.Any(m => PathsMatch(m, d)))
            .OrderBy(d => d)
            .ToList();

        Assert.True(missing.Count == 0,
            "WerkgeverRightsMatrix.Apis missing mutating endpoints:\n" + string.Join("\n", missing)
            + "\n\nDiscovered:\n" + string.Join("\n", discovered.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x)));
    }

    [Fact]
    public void Docs_roles_matrix_werkgever_section_matches_generator()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "docs", "security", "roles-matrix.md");
        Assert.True(File.Exists(path));
        var onDisk = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
        var generated = WerkgeverRightsMatrix.GenerateMarkdown();

        var update = string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_UPDATE_ROLES_MATRIX_DOC"),
            "1",
            StringComparison.Ordinal);

        const string marker = "## Werkgever redesign (BM / RM / VM)";
        var idx = onDisk.IndexOf(marker, StringComparison.Ordinal);
        if (update || idx < 0)
        {
            onDisk = idx >= 0
                ? onDisk[..idx].TrimEnd() + "\n\n" + generated
                : onDisk.TrimEnd() + "\n\n" + generated;
            File.WriteAllText(path, onDisk.Replace("\r\n", "\n", StringComparison.Ordinal));
            onDisk = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
        }

        Assert.Contains(marker, onDisk, StringComparison.Ordinal);
        var start = onDisk.IndexOf(marker, StringComparison.Ordinal);
        var section = onDisk[start..].TrimStart();
        Assert.True(
            section.StartsWith(generated.Trim(), StringComparison.Ordinal),
            "docs/security/roles-matrix.md Werkgever section is outdated. Regenerate with:\n" +
            "JOBSY_UPDATE_ROLES_MATRIX_DOC=1 dotnet test --filter FullyQualifiedName~Docs_roles_matrix");
    }

    private static bool IsClassLevelRoute(string[] lines, int index)
    {
        for (var j = index; j < Math.Min(index + 6, lines.Length); j++)
        {
            if (lines[j].Contains("class ", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldSkip(string fileName, string full)
    {
        var n = full.ToLowerInvariant();

        // Candidate-facing
        if (fileName == "ApplicationsController.cs"
            && (n is "api/applications" || n.EndsWith("/withdraw", StringComparison.Ordinal)))
        {
            return true;
        }

        if (fileName == "TokensController.cs"
            && (n.Contains("/grant", StringComparison.Ordinal)
                || n.Contains("/goodwill", StringComparison.Ordinal)
                || n.Contains("/checkout/complete", StringComparison.Ordinal)))
        {
            return true;
        }

        if (fileName == "RegistrationController.cs" && !n.Contains("takeover", StringComparison.Ordinal))
        {
            return true;
        }

        if (fileName == "TalentPoolController.cs"
            && (n.Contains("api/me/", StringComparison.Ordinal)
                || n.EndsWith("/respond", StringComparison.Ordinal)))
        {
            return true;
        }

        if (fileName == "CompaniesController.cs" && n.Contains("intermediary-clients", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static string Combine(string baseRoute, string action)
    {
        var b = baseRoute.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(action))
        {
            return b;
        }

        return b + "/" + action.TrimStart('/');
    }

    private static string Normalize(string path)
    {
        var p = path.Replace("[controller]", "vacancies", StringComparison.OrdinalIgnoreCase);
        p = Regex.Replace(p, @"\{([^}:]+)(?::[^}]+)?\}", "{$1}", RegexOptions.IgnoreCase);
        return p.Trim('/').ToLowerInvariant();
    }

    private static bool PathsMatch(string matrix, string discovered)
    {
        if (string.Equals(matrix, discovered, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var mParts = matrix.Split('/');
        var dParts = discovered.Split('/');
        if (mParts.Length != dParts.Length)
        {
            return false;
        }

        for (var i = 0; i < mParts.Length; i++)
        {
            var a = mParts[i];
            var b = dParts[i];
            if (a.StartsWith('{') && b.StartsWith('{'))
            {
                continue;
            }

            if (!string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
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

        throw new InvalidOperationException("Repo root not found");
    }
}
