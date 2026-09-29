namespace Jobsy.Web.Navigation;

/// <summary>
/// Old employer URLs → new /werkgever URLs (D2). Query is preserved; optional tab= is merged.
/// </summary>
public static class WerkgeverLegacyRoutes
{
    public sealed record Redirect(string OldPath, string NewPath, string? Tab = null);

    public static readonly IReadOnlyList<Redirect> Table =
    [
        new("/employer/vacancies", "/werkgever/vacatures"),
        new("/branch/vacancies", "/werkgever/vacatures"),
        new("/branch/vacancies/new", "/werkgever/vacatures/nieuw"),
        new("/branch/applicants", "/werkgever/sollicitaties"),
        new("/employer/talent", "/werkgever/talentpool"),
        new("/employer/talent-contacts", "/werkgever/talentpool", "contact"),
        new("/employer/kandidaatinzichten", "/werkgever/kandidaatinzichten"),
        new("/employer/branches", "/werkgever/organisatie/vestigingen"),
        new("/regional/branches", "/werkgever/organisatie/vestigingen"),
        new("/employer/regions", "/werkgever/organisatie/vestigingen", "regios"),
        new("/employer/organization", "/werkgever/organisatie/vestigingen"),
        new("/employer/users", "/werkgever/organisatie/team"),
        new("/employer/company", "/werkgever/organisatie/profiel"),
        new("/employer/culture", "/werkgever/organisatie/profiel", "cultuur"),
        new("/branch/culture", "/werkgever/organisatie/profiel", "cultuur"),
        new("/employer/salary-tables", "/werkgever/organisatie/salaristabellen"),
        new("/employer/tokens", "/werkgever/tokens"),
        new("/branch/tokens", "/werkgever/tokens"),
        new("/regional/tokens", "/werkgever/tokens"),
        new("/employer/csv-import", "/werkgever/koppelingen", "csv"),
        new("/employer/takeovers", "/werkgever/overnames"),
        new("/employer/sales", "/werkgever/partner"),
        new("/employer/sales/payout-checkout", "/werkgever/partner/uitbetalen"),
        new("/branch", "/werkgever"),
        new("/regional", "/werkgever"),
    ];

    /// <summary>Payment / checkout return URLs that must NOT move.</summary>
    public static readonly IReadOnlyList<string> UntouchedPaymentPaths =
    [
        "/employer/onboarding-checkout",
        "/tokens/checkout-return",
        "/tokens/checkout-stub",
    ];

    public static bool TryMap(string path, out string newPath, out string? tab)
    {
        newPath = path;
        tab = null;
        var normalized = Normalize(path);
        foreach (var row in Table)
        {
            if (string.Equals(Normalize(row.OldPath), normalized, StringComparison.OrdinalIgnoreCase))
            {
                newPath = row.NewPath;
                tab = row.Tab;
                return true;
            }
        }

        // /employer/salary-tables/{id}
        const string salaryPrefix = "/employer/salary-tables/";
        if (normalized.StartsWith(salaryPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var id = normalized[salaryPrefix.Length..];
            newPath = "/werkgever/organisatie/salaristabellen/" + id;
            return true;
        }

        return false;
    }

    public static string BuildTarget(string newPath, string? existingQuery, string? tab)
    {
        var query = existingQuery?.TrimStart('?') ?? "";
        if (!string.IsNullOrWhiteSpace(tab))
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                query = "tab=" + Uri.EscapeDataString(tab);
            }
            else if (!query.Contains("tab=", StringComparison.OrdinalIgnoreCase))
            {
                query += "&tab=" + Uri.EscapeDataString(tab);
            }
        }

        return string.IsNullOrWhiteSpace(query) ? newPath : newPath + "?" + query;
    }

    public static bool IsUntouchedPaymentPath(string path)
    {
        var n = Normalize(path);
        return UntouchedPaymentPaths.Any(p =>
            string.Equals(Normalize(p), n, StringComparison.OrdinalIgnoreCase));
    }

    public static string Normalize(string relativePath)
    {
        var path = relativePath.Split('?', 2)[0].Split('#', 2)[0].Trim('/');
        return string.IsNullOrEmpty(path) ? "/" : "/" + path;
    }
}
