using Jobsy.Core.Entities.Scholen;

namespace Jobsy.Core.Scholen.PupilPassport;

/// <summary>Top-3 competence strengths for the passport (rule-based, no scores shown).</summary>
public static class PupilPassportStrengths
{
    public static IReadOnlyList<PupilPassportStrength> For(PupilResult result)
    {
        var keys = PupilStoryTemplates.TryParse(result.StoryKeysJson);
        var competenceKey = keys?.TileCompetenceKey ?? "LeerlingStory.Tile.Competence.Samenwerken";
        var suffix = competenceKey.Split('.').LastOrDefault() ?? "Samenwerken";
        if (suffix is "Balanced")
        {
            suffix = "Samenwerken";
        }

        var primary = MapPrimary(suffix);
        var secondary = MapSecondary(result.HollandCode);
        var tertiary = MapTertiary(result.TopValue);
        return [primary, secondary, tertiary];
    }

    private static PupilPassportStrength MapPrimary(string suffix) => suffix switch
    {
        "Resultaat" => new("Resultaatgericht", PupilPassportCopy.Get("LeerlingPassport.Strength.Resultaat.Sub"), "target"),
        "Stress" => new("Rustig onder druk", PupilPassportCopy.Get("LeerlingPassport.Strength.Stress.Sub"), "bulb"),
        "Innovatie" => new("Nieuwe ideeën", PupilPassportCopy.Get("LeerlingPassport.Strength.Innovatie.Sub"), "spark"),
        "Extraversie" => new("In contact komen", PupilPassportCopy.Get("LeerlingPassport.Strength.Extraversie.Sub"), "users"),
        _ => new("Samenwerken", PupilPassportCopy.Get("LeerlingPassport.Strength.Samenwerken.Sub"), "hands")
    };

    private static PupilPassportStrength MapSecondary(string? holland) => holland switch
    {
        null or "" => new("Zorgvuldig werken", PupilPassportCopy.Get("LeerlingPassport.Strength.Zorgvuldig.Sub"), "pencil"),
        _ when holland.Contains('S', StringComparison.Ordinal) =>
            new("Anderen helpen", PupilPassportCopy.Get("LeerlingPassport.Strength.Helpen.Sub"), "heart"),
        _ when holland.Contains('I', StringComparison.Ordinal) =>
            new("Uitzoeken", PupilPassportCopy.Get("LeerlingPassport.Strength.Onderzoek.Sub"), "search"),
        _ when holland.Contains('R', StringComparison.Ordinal) =>
            new("Praktisch aanpakken", PupilPassportCopy.Get("LeerlingPassport.Strength.Praktisch.Sub"), "wrench"),
        _ => new("Zorgvuldig werken", PupilPassportCopy.Get("LeerlingPassport.Strength.Zorgvuldig.Sub"), "pencil")
    };

    private static PupilPassportStrength MapTertiary(string? topValue) => topValue switch
    {
        "Connection" => new("Samen iets doen", PupilPassportCopy.Get("LeerlingPassport.Strength.Verbinding.Sub"), "users"),
        "Achievement" => new("Doelen halen", PupilPassportCopy.Get("LeerlingPassport.Strength.Doelen.Sub"), "flag"),
        "Impact" => new("Iets betekenen", PupilPassportCopy.Get("LeerlingPassport.Strength.Impact.Sub"), "world-coral"),
        _ => new("Betrouwbaar", PupilPassportCopy.Get("LeerlingPassport.Strength.Betrouwbaar.Sub"), "shield")
    };
}
