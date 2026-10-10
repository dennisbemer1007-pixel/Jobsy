using System.Text.RegularExpressions;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

public static class CandidateExternalVacancyRules
{
    public const int MaxImportsPerUserPerDay = 20;
    public const int MaxMotivationLength = 2000;
    public const int ReminderAfterDays = 7;
    public const int MaxHtmlBytes = 512 * 1024;
    public const int MaxVisibleTextChars = 12_000;

    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsValidEmployerEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var trimmed = email.Trim();
        return trimmed.Length <= 254 && EmailRegex.IsMatch(trimmed);
    }

    public static string NormalizeEmployerEmail(string email)
        => email.Trim().ToLowerInvariant();

    public static string NormalizeDomain(string normalizedEmail)
    {
        var at = normalizedEmail.LastIndexOf('@');
        return at >= 0 ? normalizedEmail[(at + 1)..] : normalizedEmail;
    }

    public static bool IsSuppressed(
        string normalizedEmail,
        IReadOnlyCollection<string> suppressedEmails,
        IReadOnlyCollection<string> suppressedDomains)
    {
        if (suppressedEmails.Contains(normalizedEmail, StringComparer.Ordinal))
        {
            return true;
        }

        var domain = NormalizeDomain(normalizedEmail);
        return suppressedDomains.Contains(domain, StringComparer.Ordinal);
    }

    public static string? ValidateMotivation(string? motivation)
    {
        if (string.IsNullOrWhiteSpace(motivation))
        {
            return "Motivatie is verplicht.";
        }

        if (motivation.Trim().Length > MaxMotivationLength)
        {
            return $"Motivatie mag maximaal {MaxMotivationLength} tekens zijn.";
        }

        return null;
    }

    public static bool TryNormalizeSourceUrl(string? url, out Uri normalized, out string? error)
    {
        normalized = null!;
        error = null;
        if (string.IsNullOrWhiteSpace(url))
        {
            error = "URL is verplicht.";
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            error = "Gebruik een geldige http(s)-link.";
            return false;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            error = "Deze link is niet toegestaan.";
            return false;
        }

        normalized = uri;
        return true;
    }

    public static bool LooksLikeLoginWall(string visibleText, string html)
    {
        if (string.IsNullOrWhiteSpace(visibleText) && html.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var lower = html.ToLowerInvariant();
        return lower.Contains("type=\"password\"", StringComparison.Ordinal)
               || lower.Contains("name=\"password\"", StringComparison.Ordinal)
               || lower.Contains("wachtwoord", StringComparison.OrdinalIgnoreCase)
               && lower.Contains("inlog", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string> FilterSharedFactKeys(
        IReadOnlyList<string>? requestedKeys,
        IReadOnlyDictionary<string, string> structuredFacts)
    {
        if (requestedKeys is null || requestedKeys.Count == 0)
        {
            return structuredFacts.Keys.Take(6).ToList();
        }

        var set = new HashSet<string>(requestedKeys, StringComparer.OrdinalIgnoreCase);
        return structuredFacts.Keys.Where(k => set.Contains(k)).ToList();
    }

    public static ExternalVacancyMatchInsightsDto SanitizeMatchInsights(
        IReadOnlyList<string>? strengths,
        IReadOnlyList<string>? challenges,
        CandidateFactSheet sheet)
    {
        const string unknown = "Dat weet ik niet";
        var cleanStrengths = SanitizeLines(strengths, sheet, unknown);
        var cleanChallenges = SanitizeLines(challenges, sheet, unknown);
        return new ExternalVacancyMatchInsightsDto(cleanStrengths, cleanChallenges);
    }

    private static List<string> SanitizeLines(
        IReadOnlyList<string>? lines,
        CandidateFactSheet sheet,
        string fallback)
    {
        var result = new List<string>();
        foreach (var line in lines ?? [])
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var trimmed = line.Trim();
            if (CandidateFactGuard.RejectionReason(trimmed, sheet) is not null)
            {
                result.Add(fallback);
            }
            else
            {
                result.Add(trimmed);
            }
        }

        if (result.Count == 0)
        {
            result.Add(fallback);
        }

        return result;
    }
}
