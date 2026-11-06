using System.Numerics;
using System.Text;
using Jobsy.Core.Privacy;

namespace Jobsy.Core.Sales;

/// <summary>SEPA IBAN normalize / mod-97 validate / mask helpers (D6).</summary>
public static class Iban
{
    /// <summary>ISO 13616 lengths for common SEPA countries we accept.</summary>
    private static readonly Dictionary<string, int> CountryLengths = new(StringComparer.Ordinal)
    {
        ["AD"] = 24,
        ["AT"] = 20,
        ["BE"] = 16,
        ["BG"] = 22,
        ["CH"] = 21,
        ["CY"] = 28,
        ["CZ"] = 24,
        ["DE"] = 22,
        ["DK"] = 18,
        ["EE"] = 20,
        ["ES"] = 24,
        ["FI"] = 18,
        ["FR"] = 27,
        ["GB"] = 22,
        ["GI"] = 23,
        ["GR"] = 27,
        ["HR"] = 21,
        ["HU"] = 28,
        ["IE"] = 22,
        ["IS"] = 26,
        ["IT"] = 27,
        ["LI"] = 21,
        ["LT"] = 20,
        ["LU"] = 20,
        ["LV"] = 21,
        ["MC"] = 27,
        ["MT"] = 31,
        ["NL"] = 18,
        ["NO"] = 15,
        ["PL"] = 28,
        ["PT"] = 25,
        ["RO"] = 24,
        ["SE"] = 24,
        ["SI"] = 19,
        ["SK"] = 24,
        ["SM"] = 27,
        ["VA"] = 22,
    };

    public static string Normalize(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(iban.Length);
        foreach (var c in iban)
        {
            if (char.IsWhiteSpace(c) || c == '-')
            {
                continue;
            }

            sb.Append(char.ToUpperInvariant(c));
        }

        return sb.ToString();
    }

    public static bool IsValid(string? iban)
    {
        var compact = Normalize(iban);
        if (compact.Length is < 15 or > 34)
        {
            return false;
        }

        if (!char.IsAsciiLetter(compact[0]) || !char.IsAsciiLetter(compact[1]))
        {
            return false;
        }

        if (!char.IsAsciiDigit(compact[2]) || !char.IsAsciiDigit(compact[3]))
        {
            return false;
        }

        for (var i = 4; i < compact.Length; i++)
        {
            if (!char.IsAsciiLetterOrDigit(compact[i]))
            {
                return false;
            }
        }

        var country = compact[..2];
        if (CountryLengths.TryGetValue(country, out var expected) && compact.Length != expected)
        {
            return false;
        }

        // Move first 4 chars to the end and convert letters A=10 … Z=35.
        var rearranged = compact[4..] + compact[..4];
        var numeric = new StringBuilder(rearranged.Length * 2);
        foreach (var c in rearranged)
        {
            if (char.IsAsciiDigit(c))
            {
                numeric.Append(c);
            }
            else
            {
                numeric.Append((c - 'A') + 10);
            }
        }

        return BigInteger.Parse(numeric.ToString()) % 97 == 1;
    }

    /// <summary>Display mask: NL•• •••• •••• 4821</summary>
    public static string Mask(string? iban) => PersonalDataMasker.MaskIban(Normalize(iban));

    public static string Last4(string? iban)
    {
        var compact = Normalize(iban);
        return compact.Length >= 4 ? compact[^4..] : compact;
    }
}
