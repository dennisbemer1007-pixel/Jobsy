namespace Jobsy.Web.Dashboard;

/// <summary>
/// Groups KPI metric keys into scannable dashboard categories with visual hierarchy hints.
/// </summary>
public static class MetricDashboardCatalog
{
    public const string OtherCategoryId = "other";

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

    /// <summary>Keys that render a compact sparkline when trend points are available.</summary>
    public static bool SupportsSparkline(string key)
        => key.Equals("clicks", StringComparison.OrdinalIgnoreCase)
           || key.Equals("impressions", StringComparison.OrdinalIgnoreCase)
           || key.Equals("applications", StringComparison.OrdinalIgnoreCase)
           || key.Equals("shares", StringComparison.OrdinalIgnoreCase)
           || key.Equals("likes", StringComparison.OrdinalIgnoreCase)
           || key.Equals("site_visits", StringComparison.OrdinalIgnoreCase)
           || key.Equals("site_visits_unique", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("tokens_", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Ratio progress (0–100) for ring/meter visuals: conversion, token usage, reengagement.
    /// </summary>
    public static double? RatioProgress(string key, decimal value, IEnumerable<(string Key, decimal Value)> all)
    {
        if (IsPercent(key))
        {
            return (double)Math.Clamp(value, 0, 100);
        }

        var map = all
            .GroupBy(m => m.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.OrdinalIgnoreCase);

        if (key.Equals("applications", StringComparison.OrdinalIgnoreCase)
            && map.TryGetValue("clicks", out var clicks)
            && clicks > 0)
        {
            return (double)Math.Clamp(100m * value / clicks, 0, 100);
        }

        if (key.Equals("tokens_spent", StringComparison.OrdinalIgnoreCase)
            && map.TryGetValue("tokens_purchased", out var purchased)
            && purchased > 0)
        {
            return (double)Math.Clamp(100m * value / purchased, 0, 100);
        }

        if (key.Equals("clicks", StringComparison.OrdinalIgnoreCase)
            && map.TryGetValue("impressions", out var impressions)
            && impressions > 0)
        {
            return (double)Math.Clamp(100m * value / impressions, 0, 100);
        }

        return null;
    }

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

    /// <summary>
    /// Resolves a category id for a metric key, including the dynamic "Overig" bucket when present.
    /// </summary>
    public static string? CategoryIdFor<T>(
        string key,
        IReadOnlyList<(Category Category, IReadOnlyList<T> Metrics)> groups,
        Func<T, string> keySelector)
    {
        var known = CategoryIdFor(key);
        if (known is not null)
        {
            return known;
        }

        foreach (var (category, metrics) in groups)
        {
            if (metrics.Any(m => string.Equals(keySelector(m), key, StringComparison.OrdinalIgnoreCase)))
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
                new Category(OtherCategoryId, "Overig", "Overige meetwaarden.", orphaned.Select(keySelector).ToArray()),
                orphaned));
        }

        return result;
    }

    public static IReadOnlyList<T> HeroMetrics<T>(
        IEnumerable<T> metrics,
        Func<T, string> keySelector,
        Func<T, decimal> valueSelector,
        int max = 4)
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

        var list = metrics as IList<T> ?? metrics.ToList();
        var byKey = list
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

        // Non-zero errors must always surface in the hero so warning styling is never buried.
        if (byKey.TryGetValue("errors", out var errors)
            && IsWarning("errors", valueSelector(errors))
            && !hero.Any(h => string.Equals(keySelector(h), "errors", StringComparison.OrdinalIgnoreCase)))
        {
            if (hero.Count >= max)
            {
                hero[^1] = errors;
            }
            else
            {
                hero.Add(errors);
            }
        }

        return hero;
    }

    public static bool CategoryHasWarning<T>(
        IReadOnlyList<T> metrics,
        Func<T, string> keySelector,
        Func<T, decimal> valueSelector)
        => metrics.Any(m => IsWarning(keySelector(m), valueSelector(m)));
}
