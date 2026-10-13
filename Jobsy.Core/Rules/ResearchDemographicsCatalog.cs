namespace Jobsy.Core.Rules;

public static class ResearchDemographicsCatalog
{
    public const string PreferNotToSay = "prefer_not_to_say";

    public static readonly string[] AgeBands =
    [
        "18-24",
        "25-34",
        "35-44",
        "45-54",
        "55+",
        PreferNotToSay
    ];

    public static readonly string[] Genders =
    [
        "female",
        "male",
        "non_binary",
        "other",
        PreferNotToSay
    ];

    public static readonly string[] EducationLevels =
    [
        "primary",
        "vmo",
        "havo_vwo",
        "mbo",
        "hbo",
        "wo",
        PreferNotToSay
    ];

    public static readonly string[] Languages =
    [
        "nl",
        "en",
        "pl",
        "ro",
        "ar",
        PreferNotToSay
    ];

    public static string? NormalizeBand(string? value, IReadOnlyList<string> allowed)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim().ToLowerInvariant();
        return allowed.Contains(trimmed, StringComparer.Ordinal) ? trimmed : null;
    }
}
