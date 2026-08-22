using System.Text.RegularExpressions;

namespace Jobsy.Api.Security;

public static partial class AnonymousKeyRules
{
    public static bool IsValid(string? anonymousKey)
    {
        if (string.IsNullOrWhiteSpace(anonymousKey) || anonymousKey.Length > 128)
        {
            return false;
        }

        if (AnonymousGuidKeyRegex().IsMatch(anonymousKey))
        {
            return true;
        }

        return anonymousKey.Length >= 10
               && anonymousKey.StartsWith("anon-", StringComparison.Ordinal);
    }

    [GeneratedRegex("^anon-[0-9a-fA-F-]{36}$", RegexOptions.CultureInvariant)]
    private static partial Regex AnonymousGuidKeyRegex();
}
