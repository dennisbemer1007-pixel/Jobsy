using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>
/// 8-character letter codes: Crockford base32 without 0/O/1/I/L (D3).
/// Displayed as <c>XXXX-XXXX</c>; input accepts lowercase, spaces and a missing dash.
/// </summary>
public static partial class LetterVerificationCodes
{
    /// <summary>
    /// Crockford alphabet minus 0 and 1 (I/L/O already absent). Length 30.
    /// </summary>
    public const string Alphabet = "23456789ABCDEFGHJKMNPQRSTVWXYZ";

    public const int CodeLength = 8;

    public static string Create()
    {
        Span<char> chars = stackalloc char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }

    public static string Format(string code)
    {
        var normalized = Normalize(code);
        if (normalized.Length != CodeLength)
        {
            return normalized;
        }

        return $"{normalized[..4]}-{normalized[4..]}";
    }

    /// <summary>Uppercase A–Z/2–9 only; strips spaces and dashes; maps ambiguous glyphs.</summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(CodeLength);
        foreach (var ch in input.Trim().ToUpperInvariant())
        {
            if (ch is ' ' or '-' or '_')
            {
                continue;
            }

            var mapped = ch switch
            {
                '0' => 'O', // reject later — not in alphabet
                '1' => 'I',
                'I' => 'I',
                'L' => 'L',
                'O' => 'O',
                _ => ch
            };

            // Spec alphabet excludes 0/O/1/I/L — drop invalid rather than map into alphabet.
            if (Alphabet.Contains(mapped))
            {
                sb.Append(mapped);
            }
        }

        return sb.ToString();
    }

    public static bool IsWellFormed(string? input)
    {
        var n = Normalize(input);
        return n.Length == CodeLength && n.All(c => Alphabet.Contains(c));
    }

    public static bool LooksLikeDisplay(string? input)
        => DisplayRegex().IsMatch(input ?? string.Empty);

    [GeneratedRegex(@"^[2-9A-HJKMNP-TV-Z]{4}-[2-9A-HJKMNP-TV-Z]{4}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DisplayRegex();
}
