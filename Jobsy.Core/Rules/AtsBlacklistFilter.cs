using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>
/// Hard reject for agencies / flex language — ATS only scrapes direct local employers.
/// </summary>
public static class AtsBlacklistFilter
{
    private static readonly string[] Keywords =
    [
        "uitzendbureau", "uitzend", "flexbureau", "flexwerk", "flex ",
        "interim", "recruitment", "recruiter", "detachering", "detacheer",
        "payroll", "indeed", "linkedin", "nationale vacaturebank",
        "werk.nl", "jobbird", "monsterboard", "randstad", "timing",
        "tempo-team", "manpower", "unique", "start people", "olympia",
        "youngcapital", "undutchables", "undutchable"
    ];

    private static readonly string[] BlockedHostFragments =
    [
        "indeed.", "linkedin.", "nationalevacaturebank.", "werk.nl",
        "jobbird.", "monsterboard.", "randstad.", "timing.nl",
        "tempoteam.", "manpower.", "startpeople.", "olympia."
    ];

    public static bool IsBlocked(string? url, string? title, string? companyName = null)
    {
        if (IsBlockedHost(url))
        {
            return true;
        }

        var hay = $"{url} {title} {companyName}".ToLowerInvariant();
        foreach (var keyword in Keywords)
        {
            if (hay.Contains(keyword, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsBlockedHost(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        return BlockedHostFragments.Any(f => host.Contains(f, StringComparison.Ordinal));
    }

    public static bool IsDomainAllowed(string? url, string allowedDomain)
    {
        if (string.IsNullOrWhiteSpace(allowedDomain)
            || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.Trim().TrimStart('.').ToLowerInvariant();
        var allowed = allowedDomain.Trim().TrimStart('.').ToLowerInvariant();
        return host == allowed || host.EndsWith("." + allowed, StringComparison.Ordinal);
    }
}
