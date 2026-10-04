using Jobsy.Core.Localization;

namespace Jobsy.Core.Rules;

/// <summary>
/// Single Dutch display-label source for assessment dimensions (DNA, result pages, PDF, matching copy).
/// Prefer this over ad-hoc strings in UI or catalogs.
/// </summary>
public static class DimensionLabels
{
    public static string For(string? domain, string? lang)
    {
        var code = JobsyLanguages.Normalize(lang);
        if (code is "" or "nl")
        {
            return For(domain);
        }

        if (string.IsNullOrWhiteSpace(domain))
        {
            return "";
        }

        var translated = code switch
        {
            "en" => English(domain),
            "pl" => Polish(domain),
            "ro" => Romanian(domain),
            "ar" => Arabic(domain),
            _ => null
        };
        return string.IsNullOrWhiteSpace(translated) ? For(domain) : translated;
    }

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
            SchwartzValuesCatalog.Autonomy => "Zelf kiezen en uitdaging",
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
            // Autonomy shares the string with SchwartzValuesCatalog.Autonomy above.
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

    private static string? English(string domain) => domain switch
    {
        CompetencyTestCatalog.Samenwerken => "Working together",
        CompetencyTestCatalog.Resultaatgerichtheid => "Finishing the job",
        CompetencyTestCatalog.Stressbestendigheid => "Staying calm",
        CompetencyTestCatalog.Innovatie => "Trying new things",
        CompetencyTestCatalog.Extraversie => "Energy from people",
        CareerTestCatalog.Realistic => "Hands-on work",
        CareerTestCatalog.Investigative => "Figuring things out",
        CareerTestCatalog.Artistic => "Making something new",
        CareerTestCatalog.Social => "Helping people",
        CareerTestCatalog.Enterprising => "Getting things moving",
        CareerTestCatalog.Conventional => "Keeping things tidy",
        CulturePersonalityCatalog.Informal => "An informal atmosphere",
        CulturePersonalityCatalog.Collaboration => "Working together",
        CulturePersonalityCatalog.Flexibility => "Moving with change",
        CulturePersonalityCatalog.Innovation => "Trying new things",
        CulturePersonalityCatalog.PeopleFirst => "People first",
        SchwartzValuesCatalog.Autonomy => "Choosing for yourself",
        SchwartzValuesCatalog.Connection => "Connection and care",
        SchwartzValuesCatalog.Achievement => "Achievement and growth",
        SchwartzValuesCatalog.Stability => "Security and tradition",
        SchwartzValuesCatalog.Impact => "Making a difference",
        _ => null
    };

    private static string? Polish(string domain) => domain switch
    {
        CompetencyTestCatalog.Samenwerken => "Wspólna praca",
        CompetencyTestCatalog.Resultaatgerichtheid => "Dokańczanie pracy",
        CompetencyTestCatalog.Stressbestendigheid => "Spokój",
        CompetencyTestCatalog.Innovatie => "Próbowanie nowych rzeczy",
        CompetencyTestCatalog.Extraversie => "Energia od ludzi",
        CareerTestCatalog.Realistic => "Praca rękami",
        CareerTestCatalog.Investigative => "Rozumienie jak to działa",
        CareerTestCatalog.Artistic => "Tworzenie czegoś nowego",
        CareerTestCatalog.Social => "Pomaganie ludziom",
        CareerTestCatalog.Enterprising => "Ruszanie spraw do przodu",
        CareerTestCatalog.Conventional => "Porządek",
        CulturePersonalityCatalog.Informal => "Swobodna atmosfera",
        CulturePersonalityCatalog.Collaboration => "Wspólna praca",
        CulturePersonalityCatalog.Flexibility => "Dopasowanie się",
        CulturePersonalityCatalog.Innovation => "Próbowanie nowych rzeczy",
        CulturePersonalityCatalog.PeopleFirst => "Ludzie na pierwszym miejscu",
        SchwartzValuesCatalog.Autonomy => "Własny wybór",
        SchwartzValuesCatalog.Connection => "Bliskość i troska",
        SchwartzValuesCatalog.Achievement => "Wyniki i rozwój",
        SchwartzValuesCatalog.Stability => "Pewność i tradycja",
        SchwartzValuesCatalog.Impact => "Wpływ na innych",
        _ => null
    };

    private static string? Romanian(string domain) => domain switch
    {
        CompetencyTestCatalog.Samenwerken => "Lucrul împreună",
        CompetencyTestCatalog.Resultaatgerichtheid => "Ducerea trebei la capăt",
        CompetencyTestCatalog.Stressbestendigheid => "Calm",
        CompetencyTestCatalog.Innovatie => "Încercarea de lucruri noi",
        CompetencyTestCatalog.Extraversie => "Energie de la oameni",
        CareerTestCatalog.Realistic => "Lucrul cu mâinile",
        CareerTestCatalog.Investigative => "A înțelege cum stau lucrurile",
        CareerTestCatalog.Artistic => "A face ceva nou",
        CareerTestCatalog.Social => "Ajutor pentru oameni",
        CareerTestCatalog.Enterprising => "Punerea lucrurilor în mișcare",
        CareerTestCatalog.Conventional => "Ordine",
        CulturePersonalityCatalog.Informal => "O atmosferă lejeră",
        CulturePersonalityCatalog.Collaboration => "Lucrul împreună",
        CulturePersonalityCatalog.Flexibility => "Adaptare",
        CulturePersonalityCatalog.Innovation => "Încercarea de lucruri noi",
        CulturePersonalityCatalog.PeopleFirst => "Oamenii pe primul loc",
        SchwartzValuesCatalog.Autonomy => "Alegere proprie",
        SchwartzValuesCatalog.Connection => "Legătură și grijă",
        SchwartzValuesCatalog.Achievement => "Rezultate și creștere",
        SchwartzValuesCatalog.Stability => "Siguranță și tradiție",
        SchwartzValuesCatalog.Impact => "Efect asupra altora",
        _ => null
    };

    private static string? Arabic(string domain) => domain switch
    {
        CompetencyTestCatalog.Samenwerken => "العمل مع الآخرين",
        CompetencyTestCatalog.Resultaatgerichtheid => "إنهاء العمل",
        CompetencyTestCatalog.Stressbestendigheid => "الهدوء",
        CompetencyTestCatalog.Innovatie => "تجربة أشياء جديدة",
        CompetencyTestCatalog.Extraversie => "طاقة من الناس",
        CareerTestCatalog.Realistic => "العمل باليدين",
        CareerTestCatalog.Investigative => "فهم كيف تسير الأمور",
        CareerTestCatalog.Artistic => "صنع شيء جديد",
        CareerTestCatalog.Social => "مساعدة الناس",
        CareerTestCatalog.Enterprising => "تحريك الأمور",
        CareerTestCatalog.Conventional => "الترتيب",
        CulturePersonalityCatalog.Informal => "جو بسيط",
        CulturePersonalityCatalog.Collaboration => "العمل مع الآخرين",
        CulturePersonalityCatalog.Flexibility => "المرونة",
        CulturePersonalityCatalog.Innovation => "تجربة أشياء جديدة",
        CulturePersonalityCatalog.PeopleFirst => "الناس أولاً",
        SchwartzValuesCatalog.Autonomy => "الاختيار بنفسك",
        SchwartzValuesCatalog.Connection => "القرب والاهتمام",
        SchwartzValuesCatalog.Achievement => "النتيجة والنمو",
        SchwartzValuesCatalog.Stability => "الأمان والعادات",
        SchwartzValuesCatalog.Impact => "أثر على الآخرين",
        _ => null
    };
}
