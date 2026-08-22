using System.Text.RegularExpressions;
using Jobsy.Core.Authorization;

namespace Jobsy.Tests.Uat;

public sealed record RazorPageInfo(
    string FilePath,
    IReadOnlyList<string> Templates,
    bool AllowAnonymous,
    bool Authorize,
    IReadOnlyList<string> Roles);

/// <summary>
/// Indexes every Blazor <c>@page</c> plus <c>[Authorize]</c> / <c>[AllowAnonymous]</c>
/// so each UAT scenario can assert that a knop/link landt op een bestaande, correct beveiligde route.
/// </summary>
public sealed class RazorRouteIndex
{
    private static readonly Regex PageRx = new(
        @"@page\s+""([^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex AttrRx = new(
        @"@attribute\s+\[(?:Microsoft\.AspNetCore\.Authorization\.)?(AllowAnonymous|Authorize)(?:\(([^]]*)\))?\]",
        RegexOptions.Compiled);

    private static readonly Regex RolesRx = new(
        @"Roles\s*=\s*""([^""]+)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IReadOnlyList<RazorPageInfo> Pages { get; }

    public static RazorRouteIndex Load()
    {
        var root = Path.Combine(RepoRoot.Find(), "Jobsy.Web", "Components");
        var pages = new List<RazorPageInfo>();
        foreach (var file in Directory.EnumerateFiles(root, "*.razor", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var templates = PageRx.Matches(text).Select(m => m.Groups[1].Value).ToList();
            if (templates.Count == 0)
            {
                continue;
            }

            var allowAnonymous = false;
            var authorize = false;
            var roles = new List<string>();
            foreach (Match attr in AttrRx.Matches(text))
            {
                var name = attr.Groups[1].Value;
                if (string.Equals(name, "AllowAnonymous", StringComparison.Ordinal))
                {
                    allowAnonymous = true;
                    continue;
                }

                authorize = true;
                var args = attr.Groups[2].Value;
                var rolesMatch = RolesRx.Match(args);
                if (rolesMatch.Success)
                {
                    roles.AddRange(rolesMatch.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                }
            }

            pages.Add(new RazorPageInfo(file, templates, allowAnonymous, authorize, roles));
        }

        return new RazorRouteIndex(pages);
    }

    private RazorRouteIndex(IReadOnlyList<RazorPageInfo> pages) => Pages = pages;

    public RazorPageInfo? Find(string path)
    {
        var candidates = Pages.Where(p => p.Templates.Any(t => TemplateMatches(t, path))).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        // Prefer the most specific template (fewest parameters, longest static prefix).
        return candidates
            .OrderByDescending(p => p.Templates.Max(t => StaticPrefixLength(t)))
            .ThenBy(p => p.Templates.Min(t => t.Count(c => c == '{')))
            .First();
    }

    public IReadOnlyList<RazorPageInfo> Under(string prefix)
    {
        var norm = CanonicalPath(prefix).TrimEnd('/');
        if (string.IsNullOrEmpty(norm))
        {
            norm = "/";
        }

        return Pages
            .Where(p => p.Templates.Any(t =>
            {
                var c = CanonicalPath(t);
                return c == norm || c.StartsWith(norm + "/", StringComparison.OrdinalIgnoreCase);
            }))
            .ToList();
    }

    public static bool RoleMayOpen(RazorPageInfo page, string? jobsyRole)
    {
        if (page.AllowAnonymous)
        {
            return true;
        }

        if (jobsyRole is null)
        {
            return false;
        }

        if (!page.Authorize)
        {
            // Web has no FallbackPolicy — unannotated pages are reachable.
            return true;
        }

        if (page.Roles.Count == 0)
        {
            return true; // [Authorize] any authenticated user
        }

        return page.Roles.Contains(jobsyRole, StringComparer.OrdinalIgnoreCase);
    }

    public static bool TemplateMatches(string template, string requestPath)
    {
        var path = CanonicalPath(requestPath);
        if (path.Contains('*', StringComparison.Ordinal))
        {
            return false;
        }

        var tmpl = CanonicalPath(template);
        if (string.Equals(tmpl, path, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var tParts = Split(tmpl);
        var pParts = Split(path);
        if (tParts.Length != pParts.Length)
        {
            // Optional parameter OR catalog path without the last segment
            // (/partner, /home/metrics vs /home/metrics/{Key}).
            if (tParts.Length == pParts.Length + 1 && tParts[^1].StartsWith('{'))
            {
                return HeadEquals(tParts, pParts, tParts.Length - 1);
            }

            return false;
        }

        return HeadEquals(tParts, pParts, tParts.Length);
    }

    private static bool HeadEquals(string[] tmpl, string[] path, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (tmpl[i].StartsWith('{') && tmpl[i].EndsWith('}'))
            {
                if (!ParameterMatches(tmpl[i], path[i]))
                {
                    return false;
                }

                continue;
            }

            if (!string.Equals(tmpl[i], path[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ParameterMatches(string token, string value)
    {
        if (value.StartsWith('{') && value.Contains('}', StringComparison.Ordinal))
        {
            return true;
        }

        var inner = token.Trim('{', '}');
        var optional = inner.EndsWith('?');
        inner = inner.TrimEnd('?');

        if (inner.Contains("regex", StringComparison.OrdinalIgnoreCase))
        {
            var start = inner.IndexOf('(');
            var end = inner.LastIndexOf(')');
            if (start >= 0 && end > start)
            {
                var pattern = inner[(start + 1)..end];
                pattern = pattern.Replace("{{", "{", StringComparison.Ordinal).Replace("}}", "}", StringComparison.Ordinal);
                try
                {
                    return Regex.IsMatch(value, pattern);
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }
        }

        if (inner.Contains("guid", StringComparison.OrdinalIgnoreCase))
        {
            return Guid.TryParse(value, out _);
        }

        if (inner.Contains("int", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(value, out _);
        }

        return value.Length > 0 || optional;
    }

    private static string[] Split(string path)
        => path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

    public static string CanonicalPath(string raw)
    {
        var path = raw.Trim();
        var q = IndexOfQueryMarker(path);
        if (q >= 0)
        {
            path = path[..q];
        }

        path = path.Trim();
        if (string.IsNullOrEmpty(path) || path == "/")
        {
            return "/";
        }

        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        return path.TrimEnd('/');
    }

    private static int IndexOfQueryMarker(string path)
    {
        var depth = 0;
        for (var i = 0; i < path.Length; i++)
        {
            switch (path[i])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth = Math.Max(0, depth - 1);
                    break;
                case '?' when depth == 0:
                    return i;
            }
        }

        return -1;
    }

    private static int StaticPrefixLength(string template)
    {
        var brace = template.IndexOf('{', StringComparison.Ordinal);
        return brace < 0 ? template.Length : brace;
    }

    public static string? ToJobsyRole(string catalogRole)
    {
        var key = catalogRole.Trim();
        return key switch
        {
            "Gast" => null,
            "Kandidaat" => JobsyRoles.Candidate,
            "Filiaalmanager" or "BranchManager" => JobsyRoles.BranchManager,
            "Regiomanager" or "RegionalManager" => JobsyRoles.RegionalManager,
            "Bedrijfsmanager" or "EnterpriseManager" or "Enterprise" => JobsyRoles.EnterpriseManager,
            "Intermediair" or "Intermediary" => JobsyRoles.Intermediary,
            "Salesmanager" or "SalesManager" => JobsyRoles.SalesManager,
            "Ambassadeur" => JobsyRoles.Ambassadeur,
            "Admin" => JobsyRoles.Admin,
            _ => null
        };
    }
}
