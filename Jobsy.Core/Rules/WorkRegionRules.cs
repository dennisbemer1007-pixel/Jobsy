using System.Text;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>Work region is a city or area label. Postcodes and other digits never stick.</summary>
public static class WorkRegionRules
{
    public const int MaxLength = 60;

    public static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var buffer = new StringBuilder(value.Length);
        var pendingSpace = false;
        foreach (var ch in value.Trim())
        {
            if (char.IsDigit(ch))
            {
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                pendingSpace = buffer.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                buffer.Append(' ');
                pendingSpace = false;
            }

            buffer.Append(ch);
        }

        var text = buffer.ToString().Trim().Trim(',', '-', '/');
        if (text.Length == 0)
        {
            return null;
        }

        return text.Length <= MaxLength ? text : text[..MaxLength].TrimEnd();
    }

    /// <summary>Suggested label from a home address. Not stored until the candidate saves it.</summary>
    public static string? SuggestFromHomeAddress(string? homeAddress)
    {
        var city = LobsyCvModelFactory.ExtractCity(homeAddress);
        if (string.IsNullOrWhiteSpace(city))
        {
            return null;
        }

        var suggestion = Sanitize(city + " e.o.");
        return suggestion;
    }

    /// <summary>True when <paramref name="stored"/> is the raw home address (postcode and all).</summary>
    public static bool IsVerbatimHomeAddress(string? stored, string? homeAddress)
    {
        if (string.IsNullOrWhiteSpace(stored) || string.IsNullOrWhiteSpace(homeAddress))
        {
            return false;
        }

        return string.Equals(stored.Trim(), homeAddress.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
