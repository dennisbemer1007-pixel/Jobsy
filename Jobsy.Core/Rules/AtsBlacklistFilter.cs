using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>
/// Hard reject for agencies / flex language — ATS only scrapes direct local employers.
/// </summary>
public static class AtsBlacklistFilter
{
    /// <summary>Substring hits (safe phrases / multi-word brands).</summary>
    private static readonly string[] PhraseKeywords =
    [
        "uitzendbureau", "uitzend", "flexbureau", "flexwerk", "flex worker",
        "staffing", "staff bureau", "detachering", "detacheer", "payroll",
        "recruitment", "recruiter", "headhunt", "headhunter",
        "nationale vacaturebank", "werk.nl", "jobbird", "monsterboard",
        "randstad", "timing", "tempo-team", "tempo team", "manpower",
        "start people", "startpeople", "olympia", "youngcapital", "young capital",
        "undutchables", "undutchable", "adecco", "unique uitzend",
        "brunel", "yacht ", "hays ", "robert half", "michael page",
        "indeed", "linkedin", "glassdoor", "stepstone", "jooble",
        "uitzendorganisatie", "bemiddelingsbureau", "personeelsbemiddeling",
        "payrolling", "zzp bemiddel", "freelancer platform"
    ];

    /// <summary>
    /// Short tokens matched as whole words only (avoid blocking "flexibel", "interimaris" edge cases
    /// still catch "flex", "interim", "agency").
    /// </summary>
    private static readonly Regex WholeWordBlocked = new(
        @"\b(flex|interim|agency|agencies|staffing|adecco|yacht|hays)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] BlockedHostFragments =
    [
        "indeed.", "linkedin.", "nationalevacaturebank.", "werk.nl",
        "jobbird.", "monsterboard.", "randstad.", "timing.nl",
        "tempoteam.", "manpower.", "startpeople.", "olympia.",
        "adecco.", "youngcapital.", "glassdoor.", "stepstone.",
        "jooble.", "brunel.", "hays.", "roberthalf.", "michaelpage.",
        "yacht.nl", "unique.nl"
    ];

    public static bool IsBlocked(string? url, string? title, string? companyName = null)
    {
        if (IsBlockedHost(url))
        {
            return true;
        }

        var hay = $"{url} {title} {companyName}".ToLowerInvariant();
        foreach (var keyword in PhraseKeywords)
        {
            if (hay.Contains(keyword, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return WholeWordBlocked.IsMatch(hay);
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
