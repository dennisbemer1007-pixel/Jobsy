using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

public enum GeolocationPrefillMode
{
    Precise,
    Approximate,
}

/// <summary>Result of turning a browser geolocation fix + reverse-geocode label into onboarding address prefill.</summary>
public sealed record GeolocationAddressPrefillResult(
    GeolocationPrefillMode Mode,
    string? AddressQuery,
    string? Postcode,
    string? City,
    bool ApplyCoordinates,
    int AccuracyMeters);

/// <summary>
/// Accuracy-aware geolocation prefill for home address (onboarding / profile).
/// Low-accuracy fixes must not invent a house number.
/// </summary>
public static partial class GeolocationAddressPrefill
{
    /// <summary>Reverse-geocode full address is trusted at or below this accuracy (metres).</summary>
    public const int PreciseAccuracyThresholdMeters = 50;

    private static readonly Regex HouseNumberSuffix = new(
        @"\s+\d+[A-Za-z]?(?:\s*[-–/]\s*\d+[A-Za-z]?)?\s*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static GeolocationAddressPrefillResult? FromReverseGeocodeLabel(
        string? reverseGeocodeLabel,
        double? accuracyMeters)
    {
        if (string.IsNullOrWhiteSpace(reverseGeocodeLabel))
        {
            return null;
        }

        var label = reverseGeocodeLabel.Trim();
        var accuracy = accuracyMeters is > 0 and < 1_000_000
            ? (int)Math.Round(accuracyMeters.Value, MidpointRounding.AwayFromZero)
            : int.MaxValue;

        if (accuracy <= PreciseAccuracyThresholdMeters)
        {
            var (postcode, city) = SplitNlPostcodeAndCity(label);
            return new(
                GeolocationPrefillMode.Precise,
                label,
                postcode,
                city,
                ApplyCoordinates: true,
                AccuracyMeters: accuracy);
        }

        var approximate = StripHouseNumberFromLabel(label);
        var (approxPostcode, approxCity) = SplitNlPostcodeAndCity(approximate);
        return new(
            GeolocationPrefillMode.Approximate,
            approximate,
            approxPostcode,
            approxCity,
            ApplyCoordinates: false,
            AccuracyMeters: accuracy);
    }

    public static string StripHouseNumberFromLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return string.Empty;
        }

        var trimmed = label.Trim();
        var comma = trimmed.IndexOf(',', StringComparison.Ordinal);
        if (comma >= 0)
        {
            var streetPart = HouseNumberSuffix.Replace(trimmed[..comma].Trim(), string.Empty);
            var rest = trimmed[(comma + 1)..].Trim();
            return string.IsNullOrWhiteSpace(rest) ? streetPart : $"{streetPart}, {rest}";
        }

        return HouseNumberSuffix.Replace(trimmed, string.Empty);
    }

    private static (string Postcode, string City) SplitNlPostcodeAndCity(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return ("", "");
        }

        var match = NlPostcodeInText().Match(label);
        if (!match.Success)
        {
            return ("", "");
        }

        var digits = match.Groups["digits"].Value;
        var letters = match.Groups["letters"].Value.ToUpperInvariant();
        var postcode = $"{digits} {letters}";
        var after = label[(match.Index + match.Length)..].Trim();
        if (after.Length > 0 && after[0] == ',')
        {
            after = after[1..].Trim();
        }

        return (postcode, after);
    }

    [GeneratedRegex(@"(?<!\d)(?<digits>[1-9][0-9]{3})\s*(?<letters>[A-Za-z]{2})\b", RegexOptions.CultureInvariant)]
    private static partial Regex NlPostcodeInText();
}
