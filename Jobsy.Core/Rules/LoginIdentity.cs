namespace Jobsy.Core.Rules;

/// <summary>
/// Normalizes the login field so short demo names (e.g. <c>Twalieb</c>) resolve to
/// <c>twalieb@jobsy.local</c>. Full e-mail addresses pass through unchanged (lowercased).
/// </summary>
public static class LoginIdentity
{
    public const string DemoEmailDomain = "jobsy.local";

    public static string Normalize(string? login)
    {
        var value = (login ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            return string.Empty;
        }

        if (value.Contains('@', StringComparison.Ordinal))
        {
            return value.ToLowerInvariant();
        }

        // Bare username → demo local-login mailbox (Acc / Development seed accounts).
        return $"{value.ToLowerInvariant()}@{DemoEmailDomain}";
    }
}
