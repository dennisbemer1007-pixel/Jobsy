namespace Jobsy.Web.Dashboard;

/// <summary>
/// Groups KPI metric keys into scannable dashboard categories with visual hierarchy hints.
/// </summary>
public static class MetricDashboardCatalog
{
    public sealed record Category(string Id, string Title, string Lead, string[] Keys);

    public static readonly Category[] Categories =
    [
        new(
            "growth",
            "Kernplatform & Groei",
            "Actieve vacatures, gebruikers, bedrijven en kernconversie.",
            [
                "active_vacancies",
                "active_vacancies_employers",
                "active_vacancies_intermediaries",
                "users_active",
                "users_open_for_work",
                "companies_employers",
                "companies_intermediaries",
                "applications",
                "clicks",
                "tokens_purchased",
                "tokens_spent"
            ]),
        new(
            "engagement",
            "Engagement & Activiteit",
            "Delen, likes, pushboms, verlengingen en bereik.",
            [
                "shares",
                "likes",
                "pushboms",
                "extensions",
                "impressions",
                "site_visits",
                "site_visits_unique"
            ]),
        new(
            "marketing",
            "Marketing & Retentie",
            "We-missen-je campagnes en conversie.",
            [
                "reengagement_emails_sent",
                "reengagement_reactivated"
            ]),
        new(
            "system",
            "Systeem & Beheer",
            "Fouten, concepten en integratiekoppelingen.",
            [
                "errors",
                "unpublished_vacancies",
                "companies_with_api",
                "companies_with_csv"
            ])
    ];

    /// <summary>Keys that deserve larger visual weight in the hero / category grids.</summary>
    public static readonly HashSet<string> PrimaryKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "active_vacancies",
        "companies_employers",
        "users_active",
        "applications",
        "clicks",
        "errors"
    };

    public static bool IsPrimary(string key)
        => PrimaryKeys.Contains(key);

    public static bool IsWarning(string key, decimal value)
        => string.Equals(key, "errors", StringComparison.OrdinalIgnoreCase) && value > 0;

    public static bool IsPercent(string key)
        => string.Equals(key, "reengagement_reactivated", StringComparison.OrdinalIgnoreCase);

    public static string? CategoryIdFor(string key)
    {
        foreach (var category in Categories)
        {
            if (category.Keys.Any(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase)))
            {
                return category.Id;
            }
        }

        return null;
    }

    public static IReadOnlyList<(Category Category, IReadOnlyList<T> Metrics)> GroupPresent<T>(
        IEnumerable<T> metrics,
        Func<T, string> keySelector)
    {
        var byKey = metrics
            .GroupBy(m => keySelector(m), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var result = new List<(Category, IReadOnlyList<T>)>();
        foreach (var category in Categories)
        {
            var items = new List<T>();
            foreach (var key in category.Keys)
            {
                if (byKey.TryGetValue(key, out var metric))
                {
                    items.Add(metric);
                }
            }

            if (items.Count > 0)
            {
                result.Add((category, items));
            }
        }

        // Any unexpected keys land in a catch-all so nothing disappears.
        var known = new HashSet<string>(
            Categories.SelectMany(c => c.Keys),
            StringComparer.OrdinalIgnoreCase);
        var orphaned = byKey
            .Where(kv => !known.Contains(kv.Key))
            .Select(kv => kv.Value)
            .ToList();
        if (orphaned.Count > 0)
        {
            result.Add((
                new Category("other", "Overig", "Overige meetwaarden.", orphaned.Select(keySelector).ToArray()),
                orphaned));
        }

        return result;
    }

    public static IReadOnlyList<T> HeroMetrics<T>(IEnumerable<T> metrics, Func<T, string> keySelector, int max = 4)
    {
        var preferred = new[]
        {
            "active_vacancies",
            "companies_employers",
            "users_active",
            "applications",
            "clicks",
            "errors"
        };

        var byKey = metrics
            .GroupBy(m => keySelector(m), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var hero = new List<T>();
        foreach (var key in preferred)
        {
            if (byKey.TryGetValue(key, out var metric))
            {
                hero.Add(metric);
            }

            if (hero.Count >= max)
            {
                break;
            }
        }

        return hero;
    }
}
