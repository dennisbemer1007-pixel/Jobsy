using Jobsy.Core.Entities.Scholen;

namespace Jobsy.Core.Scholen.PupilPassport;

/// <summary>Top-3 competence strengths for the passport (rule-based, no scores shown).</summary>
public static class PupilPassportStrengths
{
    public static IReadOnlyList<PupilPassportStrength> For(PupilResult result, string language = "nl")
    {
        var keys = PupilStoryTemplates.TryParse(result.StoryKeysJson);
        var competenceKey = keys?.TileCompetenceKey ?? "LeerlingStory.Tile.Competence.Samenwerken";
        var suffix = competenceKey.Split('.').LastOrDefault() ?? "Samenwerken";
        if (suffix is "Balanced")
        {
            suffix = "Samenwerken";
        }

        var primary = MapPrimary(suffix, language);
        var secondary = MapSecondary(result.HollandCode, language);
        var tertiary = MapTertiary(result.TopValue, language);
        return [primary, secondary, tertiary];
    }

    private static PupilPassportStrength MapPrimary(string suffix, string language) => suffix switch
    {
        "Resultaat" => new(
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Resultaat.Title", language),
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Resultaat.Sub", language),
            "target"),
        "Stress" => new(
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Stress.Title", language),
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Stress.Sub", language),
            "bulb"),
        "Innovatie" => new(
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Innovatie.Title", language),
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Innovatie.Sub", language),
            "spark"),
        "Extraversie" => new(
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Extraversie.Title", language),
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Extraversie.Sub", language),
            "users"),
        _ => new(
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Samenwerken.Title", language),
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Samenwerken.Sub", language),
            "hands")
    };

    private static PupilPassportStrength MapSecondary(string? holland, string language) => holland switch
    {
        null or "" => new(
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Zorgvuldig.Title", language),
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Zorgvuldig.Sub", language),
            "pencil"),
        _ when holland.Contains('S', StringComparison.Ordinal) =>
            new(
                PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Helpen.Title", language),
                PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Helpen.Sub", language),
                "heart"),
        _ when holland.Contains('I', StringComparison.Ordinal) =>
            new(
                PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Onderzoek.Title", language),
                PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Onderzoek.Sub", language),
                "search"),
        _ when holland.Contains('R', StringComparison.Ordinal) =>
            new(
                PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Praktisch.Title", language),
                PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Praktisch.Sub", language),
                "wrench"),
        _ => new(
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Zorgvuldig.Title", language),
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Zorgvuldig.Sub", language),
            "pencil")
    };

    private static PupilPassportStrength MapTertiary(string? topValue, string language) => topValue switch
    {
        "Connection" => new(
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Verbinding.Title", language),
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Verbinding.Sub", language),
            "users"),
        "Achievement" => new(
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Doelen.Title", language),
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Doelen.Sub", language),
            "flag"),
        "Impact" => new(
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Impact.Title", language),
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Impact.Sub", language),
            "world-coral"),
        _ => new(
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Betrouwbaar.Title", language),
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Strength.Betrouwbaar.Sub", language),
            "shield")
    };
}
