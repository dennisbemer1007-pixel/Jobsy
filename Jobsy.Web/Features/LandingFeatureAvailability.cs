using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Features;

/// <summary>
/// "Binnenkort" pills: true while candidate feature routes are not yet registered as @page.
/// </summary>
public static class LandingFeatureAvailability
{
    private static readonly string[] PassportTemplates =
    [
        "/paspoort",
        "/candidate/paspoort",
        "/candidate/passport"
    ];

    private static readonly string[] DiscoveryTemplates =
    [
        "/ontdekkingsreis",
        "/candidate/ontdekkingsreis"
    ];

    private static readonly Lazy<HashSet<string>> Routes = new(DiscoverRoutes);

    public static bool PassportLive => AnyRouteExists(PassportTemplates);

    public static bool DiscoveryJourneyLive => AnyRouteExists(DiscoveryTemplates);

    public static bool ShowPassportSoon => !PassportLive;

    public static bool ShowDiscoverySoon => !DiscoveryJourneyLive;

    /// <summary>Diagnostic for tests: which template matched.</summary>
    public static string DescribeMatches()
    {
        var hits = new List<string>();
        foreach (var t in PassportTemplates.Concat(DiscoveryTemplates))
        {
            if (Routes.Value.Contains(Normalize(t)))
            {
                hits.Add(t);
            }
        }

        return hits.Count == 0 ? "(none)" : string.Join(", ", hits);
    }

    private static bool AnyRouteExists(IEnumerable<string> templates)
        => templates.Any(t => Routes.Value.Contains(Normalize(t)));

    private static HashSet<string> DiscoverRoutes()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var asm = typeof(LandingFeatureAvailability).Assembly;
        foreach (var type in asm.GetExportedTypes())
        {
            foreach (var attr in type.GetCustomAttributes<RouteAttribute>(inherit: true))
            {
                if (!string.IsNullOrWhiteSpace(attr.Template))
                {
                    set.Add(Normalize(attr.Template));
                }
            }
        }

        return set;
    }

    private static string Normalize(string template)
    {
        var t = template.Trim();
        if (!t.StartsWith('/'))
        {
            t = "/" + t;
        }

        var q = t.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0)
        {
            t = t[..q];
        }

        if (t.Length > 1)
        {
            t = t.TrimEnd('/');
        }

        return t.ToLowerInvariant();
    }
}
