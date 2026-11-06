using System.Text;

namespace Jobsy.Core.Rules;

/// <summary>
/// Shared 6-character code alphabet (no I, L, O, 0, 1), displayed <c>XXX-XXX</c>.
/// School pupil codes and passport-partner codes both use this format.
/// </summary>
public static class ShortCodeFormat
{
    public const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public const int Length = 6;

    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Code is verplicht.", nameof(input));
        }

        var sb = new StringBuilder(Length);
        foreach (var ch in input)
        {
            if (ch is ' ' or '-' or '\t')
            {
                continue;
            }

            var upper = char.ToUpperInvariant(ch);
            if (Alphabet.IndexOf(upper) < 0)
            {
                throw new ArgumentException("Code bevat ongeldige tekens.", nameof(input));
            }

            sb.Append(upper);
        }

        if (sb.Length != Length)
        {
            throw new ArgumentException($"Code moet {Length} tekens zijn.", nameof(input));
        }

        return sb.ToString();
    }

    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;
        try
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            normalized = Normalize(input);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static bool IsWellFormed(string? input) => TryNormalize(input, out _);

    public static string Display(string code)
    {
        var n = Normalize(code);
        return $"{n[..3]}-{n[3..]}";
    }
}
