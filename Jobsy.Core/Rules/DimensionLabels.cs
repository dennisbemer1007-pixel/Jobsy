namespace Jobsy.Core.Rules;

/// <summary>
/// Single Dutch display-label source for assessment dimensions (DNA, result pages, PDF, matching copy).
/// Prefer this over ad-hoc strings in UI or catalogs.
/// </summary>
public static class DimensionLabels
{
    public static string For(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return "Onderwerp";
        }

        return domain switch
        {
            DeepAnalysisCompetenceItems.Openheid => "Nieuwe dingen proberen",
            DeepAnalysisCompetenceItems.Consciëntieusheid => "Afmaken & netjes werken",
            DeepAnalysisCompetenceItems.Extraversie => "Energie van mensen",
            DeepAnalysisCompetenceItems.Vriendelijkheid => "Samen & aardig",
            DeepAnalysisCompetenceItems.EmotioneleStabiliteit => "Kalm blijven",
            CompetencyTestCatalog.Samenwerken => "Samen & aardig",
            CompetencyTestCatalog.Resultaatgerichtheid => "Afmaken & netjes werken",
            CompetencyTestCatalog.Stressbestendigheid => "Kalm blijven",
            CompetencyTestCatalog.Innovatie => "Nieuwe dingen proberen",
            // Extraversie shares the string with DeepAnalysisCompetenceItems.Extraversie above.
            CareerTestCatalog.Realistic => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Realistic),
            CareerTestCatalog.Investigative => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Investigative),
            CareerTestCatalog.Artistic => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Artistic),
            CareerTestCatalog.Social => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Social),
            CareerTestCatalog.Enterprising => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Enterprising),
            CareerTestCatalog.Conventional => CareerCompassBuilder.TypeLabel(CareerTestCatalog.Conventional),
            SchwartzValuesCatalog.Autonomy => "Eigen regie & uitdaging",
            SchwartzValuesCatalog.Connection => "Verbinding & zorg",
            SchwartzValuesCatalog.Achievement => "Prestatie & groei",
            SchwartzValuesCatalog.Stability => "Zekerheid & traditie",
            SchwartzValuesCatalog.Impact => "Impact & rechtvaardigheid",
            CulturePersonalityCatalog.Informal => "Informele sfeer",
            CulturePersonalityCatalog.Collaboration => "Samenwerken",
            CulturePersonalityCatalog.Flexibility => "Flexibel meebewegen",
            CulturePersonalityCatalog.Innovation => "Nieuwe dingen proberen",
            CulturePersonalityCatalog.PeopleFirst => "Mensen voorop",
            CulturePersonalityCatalog.Openness => "Openstaan voor nieuw",
            CulturePersonalityCatalog.Conscientiousness => "Netjes en betrouwbaar",
            CulturePersonalityCatalog.Extraversion => "Energie van mensen",
            CulturePersonalityCatalog.Agreeableness => "Prettig samen optrekken",
            CulturePersonalityCatalog.EmotionalStability => "Kalm onder druk",
            CulturePersonalityCatalog.Autonomy => "Eigen regie & uitdaging",
            _ => domain
        };
    }

    /// <summary>
    /// Maps legacy stored / EverydayLabel phrases onto the shared display label.
    /// Unknown values pass through unchanged.
    /// </summary>
    public static string MapStored(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value ?? string.Empty;
        }

        var key = value.Trim();
        return key.ToLowerInvariant() switch
        {
            "zekerheid en voorspelbaarheid" => For(SchwartzValuesCatalog.Stability),
            "eigen regie en nieuwe uitdagingen" => For(SchwartzValuesCatalog.Autonomy),
            "warme band met collega's en klanten" or "warme band met collega’s en klanten" => For(SchwartzValuesCatalog.Connection),
            "resultaat en groei" => For(SchwartzValuesCatalog.Achievement),
            "bijdrage aan mens en omgeving" => For(SchwartzValuesCatalog.Impact),
            "stressbestendigheid" => For(CompetencyTestCatalog.Stressbestendigheid),
            "innovatie" => For(CompetencyTestCatalog.Innovatie),
            "samenwerken" => For(CompetencyTestCatalog.Samenwerken),
            "resultaatgerichtheid" => For(CompetencyTestCatalog.Resultaatgerichtheid),
            _ => key
        };
    }
}
