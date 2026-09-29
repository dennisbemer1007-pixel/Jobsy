namespace Jobsy.Web.Navigation;

/// <summary>
/// Old admin URLs → new Dutch URLs (D5). Middleware answers 301; Blazor safety net navigates in-circuit.
/// </summary>
public static class AdminLegacyRoutes
{
    public sealed record Entry(string OldPath, string NewPath, string? Tab = null);

    /// <summary>Every legacy admin path that must 301 (plus cockpit / placeholders).</summary>
    public static readonly IReadOnlyList<Entry> All =
    [
        new("/admin/cockpit", "/admin"),
        new("/admin/users", "/admin/gebruikers"),
        new("/admin/sales-managers", "/admin/gebruikers/sales"),
        new("/admin/ambassadeurs", "/admin/gebruikers/sales", "ambassadeurs"),
        new("/admin/companies", "/admin/organisaties"),
        new("/admin/cnames", "/admin/organisaties/regios"),
        new("/admin/vacancies", "/admin/vacatures"),
        new("/admin/ats-vacancies", "/admin/vacatures/ats"),
        // Until 02 retargets to /admin/vacatures/moderatie:
        new("/admin/moderation", "/admin/vacatures/moderatie"),
        new("/admin/vacancy-categories", "/admin/vacatures/categorieen"),
        new("/admin/wages", "/admin/vacatures/categorieen", "salaris"),
        new("/admin/finance", "/admin/financien"),
        new("/admin/sales", "/admin/financien/prijzen", "sales"),
        new("/admin/tokens", "/admin/financien/goodwill"),
        new("/admin/token-finance", "/admin/financien/uitbetalingen"),
        new("/admin/about", "/admin/content/paginas"),
        new("/admin/marketing-flyer", "/admin/content/paginas", "flyer"),
        new("/admin/training", "/admin/content/opleidingen"),
        new("/admin/masterdata", "/admin/content/stamgegevens"),
        new("/admin/exclusivity", "/admin/content/stamgegevens", "exclusiviteit"),
        new("/admin/mail-test", "/admin/content/emails"),
        new("/admin/notifications", "/admin/content/emails"),
        new("/admin/settings", "/admin/instellingen"),
        new("/admin/company", "/admin/instellingen/algemeen"),
        new("/admin/integrations", "/admin/instellingen/integraties"),
        new("/admin/api-keys", "/admin/instellingen/integraties", "api"),
        new("/admin/personal-data-access-log", "/admin/beveiliging/gegevensinzage"),
        new("/admin/logging", "/admin/beveiliging/systeemlogs"),
    ];

    private static readonly Dictionary<string, Entry> ByOld =
        All.ToDictionary(e => Normalize(e.OldPath), e => e, StringComparer.OrdinalIgnoreCase);

    public static bool TryMap(string relativePath, out string destinationPathAndQuery)
    {
        destinationPathAndQuery = "";
        var raw = relativePath ?? "";
        var qIndex = raw.IndexOf('?', StringComparison.Ordinal);
        var path = qIndex >= 0 ? raw[..qIndex] : raw;
        var query = qIndex >= 0 ? raw[(qIndex + 1)..] : "";

        // Same-path tab remaps (goodwill left Uitbetalingen; lives under Goodwill & tokens).
        if (string.Equals(Normalize(path), "/admin/financien/uitbetalingen", StringComparison.OrdinalIgnoreCase)
            && HasTab(query, "goodwill"))
        {
            destinationPathAndQuery = MergeQuery("/admin/financien/goodwill", StripTab(query), tab: null);
            return true;
        }

        if (!ByOld.TryGetValue(Normalize(path), out var entry))
        {
            return false;
        }

        destinationPathAndQuery = MergeQuery(entry.NewPath, query, entry.Tab);
        return true;
    }

    private static bool HasTab(string query, string tab)
    {
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.StartsWith("tab=", StringComparison.OrdinalIgnoreCase)
                && string.Equals(part[4..], tab, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string StripTab(string query)
    {
        var parts = query.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !p.StartsWith("tab=", StringComparison.OrdinalIgnoreCase));
        return string.Join('&', parts);
    }

    public static bool IsLegacyPath(string path)
        => ByOld.ContainsKey(Normalize(path));

    public static IEnumerable<string> OldPaths => All.Select(e => e.OldPath);

    public static string MergeQuery(string newPath, string existingQuery, string? tab)
    {
        var pairs = new List<string>();
        if (!string.IsNullOrEmpty(existingQuery))
        {
            foreach (var part in existingQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                if (tab is not null
                    && part.StartsWith("tab=", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                pairs.Add(part);
            }
        }

        if (tab is not null)
        {
            pairs.Insert(0, "tab=" + Uri.EscapeDataString(tab));
        }

        return pairs.Count == 0 ? newPath : newPath + "?" + string.Join('&', pairs);
    }

    public static string Normalize(string path)
    {
        var p = path.Split('?', 2)[0].Split('#', 2)[0].Trim('/');
        return string.IsNullOrEmpty(p) ? "/" : "/" + p;
    }
}
