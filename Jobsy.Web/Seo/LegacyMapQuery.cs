namespace Jobsy.Web.Seo;

/// <summary>
/// Detects legacy map deep-link query keys that historically lived on "/".
/// Landing 05 uses this to 301 <c>/?company=…</c> etc. to <c>/banenkaart?…</c>.
/// </summary>
public static class LegacyMapQuery
{
    /// <summary>
    /// Query keys read by <c>VacancyDiscovery</c> for map deep links.
    /// Keep in sync — <c>LegacyMapQueryTests</c> greps the razor source.
    /// </summary>
    public static readonly IReadOnlyList<string> MapQueryKeys =
    [
        "company",
        "companies",
        "maxHours",
        "maxMinutes",
        "minHours",
        "q",
        "transport",
        "weergave",
        "workType"
    ];

    public static bool IsMapDeepLink(IQueryCollection query)
    {
        if (query.Count == 0)
        {
            return false;
        }

        foreach (var key in MapQueryKeys)
        {
            if (query.ContainsKey(key))
            {
                return true;
            }
        }

        return false;
    }
}
