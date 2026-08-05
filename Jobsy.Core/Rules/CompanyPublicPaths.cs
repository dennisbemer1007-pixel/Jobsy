using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>
/// Public SEO-style URLs for employers: /{kvkNumber}/{vestigingsnummer}.
/// </summary>
public static partial class CompanyPublicPaths
{
    public const int KvkNumberLength = 8;

    /// <summary>
    /// Builds <c>/{kvk}/{vestigingsnummer}</c> when both identifiers are valid; otherwise null.
    /// </summary>
    public static string? TryBuildPath(string? kvkNumber, string? kvkEstablishmentId)
    {
        var kvk = NormalizeKvkNumber(kvkNumber);
        var vestiging = TryParseVestigingsnummer(kvkEstablishmentId, kvk);
        if (kvk is null || vestiging is null)
        {
            return null;
        }

        return $"/{kvk}/{vestiging}";
    }

    /// <summary>
    /// Builds the ondernemer (KvK-wide) path <c>/{kvk}</c>.
    /// </summary>
    public static string? TryBuildKvkPath(string? kvkNumber)
    {
        var kvk = NormalizeKvkNumber(kvkNumber);
        return kvk is null ? null : $"/{kvk}";
    }

    public static string? NormalizeKvkNumber(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var digits = NonDigitRegex().Replace(raw.Trim(), string.Empty);
        return digits.Length == KvkNumberLength ? digits : null;
    }

    /// <summary>
    /// Extracts the vestigingsnummer from <c>{kvk}_{vestigingsnummer}</c> (or a bare number).
    /// </summary>
    public static string? TryParseVestigingsnummer(string? kvkEstablishmentId, string? normalizedKvkNumber = null)
    {
        if (string.IsNullOrWhiteSpace(kvkEstablishmentId))
        {
            return null;
        }

        var raw = kvkEstablishmentId.Trim();
        var underscore = raw.IndexOf('_');
        string candidate;
        if (underscore >= 0 && underscore < raw.Length - 1)
        {
            var prefix = raw[..underscore];
            candidate = raw[(underscore + 1)..];
            if (normalizedKvkNumber is not null
                && !string.Equals(NormalizeKvkNumber(prefix) ?? prefix, normalizedKvkNumber, StringComparison.Ordinal))
            {
                // Prefer the suffix even when prefix casing/format differs; only reject empty suffix.
            }
        }
        else
        {
            candidate = raw;
        }

        var digits = NonDigitRegex().Replace(candidate, string.Empty);
        if (digits.Length is < 1 or > 12)
        {
            return null;
        }

        // Keep leading zeros from the establishment id when present.
        var trimmed = candidate.Trim();
        if (VestigingNumberRegex().IsMatch(trimmed))
        {
            return trimmed;
        }

        return digits;
    }

    public static string BuildEstablishmentId(string kvkNumber, string vestigingsnummer)
        => $"{kvkNumber}_{vestigingsnummer}";

    public static bool IsValidKvkRouteSegment(string? value)
        => NormalizeKvkNumber(value) is not null;

    public static bool IsValidVestigingRouteSegment(string? value)
        => !string.IsNullOrWhiteSpace(value) && VestigingNumberRegex().IsMatch(value.Trim());

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitRegex();

    [GeneratedRegex(@"^\d{1,12}$")]
    private static partial Regex VestigingNumberRegex();
}
