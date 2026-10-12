using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Jobsy.Core.Dev;

/// <summary>
/// Local E2E only: last numeric sign-up/sign-in code per normalized e-mail when
/// <c>JOBSY_E2E_CAPTURE_EMAIL_CODES=1</c>. Never used in production hosts.
/// </summary>
public static class DevEmailCodeCapture
{
    private static readonly Regex SixDigits = new(@"\b(\d{6})\b", RegexOptions.Compiled);

    private static readonly ConcurrentDictionary<string, string> ByEmail = new(StringComparer.OrdinalIgnoreCase);

    public static bool Enabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_E2E_CAPTURE_EMAIL_CODES"),
            "1",
            StringComparison.Ordinal);

    public static void TryCapture(string? to, string? htmlOrText)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(htmlOrText))
        {
            return;
        }

        var match = SixDigits.Match(htmlOrText);
        if (!match.Success)
        {
            return;
        }

        var email = to.Trim().ToLowerInvariant();
        ByEmail[email] = match.Groups[1].Value;
    }

    public static bool TryGet(string email, out string code)
    {
        code = "";
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return ByEmail.TryGetValue(email.Trim().ToLowerInvariant(), out code!);
    }

    public static void Clear() => ByEmail.Clear();
}
