namespace Jobsy.Core.Reports;

/// <summary>
/// Shared nl/en strings for paid deep reports and PDFs. Builders store domain keys + levels;
/// views/PDF resolve labels through this catalog (no Dutch literals in renderers).
/// </summary>
public static class DeepReportCatalog
{
    private static readonly Dictionary<string, (string Nl, string En)> Map = new(StringComparer.Ordinal)
    {
        // Levels
        ["level.low"] = ("Laag", "Low"),
        ["level.mid"] = ("Gemiddeld", "Average"),
        ["level.high"] = ("Hoog", "High"),

        // PDF / cover
        ["pdf.confidential"] = ("Vertrouwelijk", "Confidential"),
        ["pdf.page"] = ("Pagina", "Page"),
        ["pdf.watermark"] = ("VOORBEELD", "SAMPLE"),
        ["pdf.sampleCover"] = ("Voorbeeld – niet jouw resultaat", "Sample – not your result"),
        ["pdf.disclaimer"] = (
            "Dit rapport is geen medische of klinische diagnose. Ruwe antwoorden blijven in jouw account.",
            "This report is not a medical or clinical diagnosis. Your raw answers stay in your account."),
        ["pdf.scores"] = ("Jouw scores", "Your scores"),
        ["pdf.overview"] = ("Overzicht", "Overview"),
        ["pdf.actionPlan"] = ("Jouw actieplan", "Your action plan"),
        ["pdf.strengths"] = ("Sterke punten & valkuilen", "Strengths & pitfalls"),
        ["pdf.comparison"] = ("Vergelijking met anderen", "How you compare"),
        ["pdf.radar"] = ("Jouw profiel", "Your profile"),
        ["pdf.sources"] = ("Bronnen & toelichting", "Sources & notes"),

        // Titles
        ["title.competence"] = ("Jouw competentierapport", "Your competency report"),
        ["title.career"] = ("Jouw loopbaanrapport", "Your career report"),
        ["title.culture"] = ("Jouw cultuur- & persoonlijkheidsrapport", "Your culture & personality report"),
        ["title.values"] = ("Jouw waardenrapport", "Your work values report"),

        // RIASEC everyday labels (EN glossary)
        ["riasec.R"] = ("Aanpakken met je handen", "Hands-on work"),
        ["riasec.I"] = ("Uitzoeken hoe het zit", "Figuring things out"),
        ["riasec.A"] = ("Iets moois of nieuws maken", "Making something new or beautiful"),
        ["riasec.S"] = ("Mensen helpen", "Helping people"),
        ["riasec.E"] = ("Aanjagen en verkopen", "Driving and selling ideas"),
        ["riasec.C"] = ("Netjes organiseren", "Organising and order"),

        // Values domains
        ["value.Autonomy"] = ("Zelf richting geven", "Setting your own direction"),
        ["value.Connection"] = ("Samenwerken en erbij horen", "Belonging and connection"),
        ["value.Achievement"] = ("Presteren en groeien", "Achievement and growth"),
        ["value.Stability"] = ("Rust en zekerheid", "Stability and security"),
        ["value.Impact"] = ("Bijdragen aan iets groters", "Making a wider impact"),

        // Culture domains (6 culture + 5 personality)
        ["culture.Autonomy"] = ("Zelfstandigheid", "Autonomy"),
        ["culture.Informal"] = ("Informele sfeer", "Informal atmosphere"),
        ["culture.Collaboration"] = ("Samenwerken", "Collaboration"),
        ["culture.Flexibility"] = ("Flexibiliteit", "Flexibility"),
        ["culture.Innovation"] = ("Vernieuwing", "Innovation"),
        ["culture.PeopleFirst"] = ("Mens eerst", "People first"),
        ["culture.Openness"] = ("Openheid", "Openness"),
        ["culture.Conscientiousness"] = ("Zorgvuldigheid", "Conscientiousness"),
        ["culture.Extraversion"] = ("Extraversie", "Extraversion"),
        ["culture.Agreeableness"] = ("Vriendelijkheid", "Agreeableness"),
        ["culture.EmotionalStability"] = ("Kalm onder druk", "Calm under pressure"),

        // Org types
        ["org.startup"] = ("Start-up", "Start-up"),
        ["org.family"] = ("Familiebedrijf", "Family business"),
        ["org.project"] = ("Projectorganisatie", "Project-based organisation"),
        ["org.corporate"] = ("Corporate", "Corporate"),
        ["org.government"] = ("Overheid", "Government"),
        ["org.care"] = ("Zorginstelling", "Care organisation"),

        ["org.startup.why"] = (
            "Past bij hoge autonomie, innovatie en informele sfeer.",
            "Fits high autonomy, innovation and an informal atmosphere."),
        ["org.family.why"] = (
            "Past bij mensen-eerst, samenwerking en wat meer stabiliteit.",
            "Fits people-first cultures, collaboration and a bit more stability."),
        ["org.project.why"] = (
            "Past bij flexibiliteit, samenwerking en resultaatgericht werken.",
            "Fits flexibility, collaboration and results-focused work."),
        ["org.corporate.why"] = (
            "Past bij structuur, zorgvuldigheid en duidelijke rollen.",
            "Fits structure, conscientiousness and clear roles."),
        ["org.government.why"] = (
            "Past bij impact, stabiliteit en zorgvuldig samenwerken.",
            "Fits impact, stability and careful collaboration."),
        ["org.care.why"] = (
            "Past bij mensen-eerst, samenwerking en kalm onder druk.",
            "Fits people-first cultures, collaboration and calm under pressure."),

        // Norm disclaimer (Lobsy internal)
        ["norm.lobsy"] = (
            "Vergeleken met {0} Lobsy-kandidaten ({1}). Dit is geen wetenschappelijk gevalideerde normgroep.",
            "Compared with {0} Lobsy candidates ({1}). This is not a scientifically validated comparison group."),

        // Holland
        ["holland.title"] = ("Jouw Holland-code uitgelegd", "Your Holland code explained"),
        ["holland.body"] = (
            "Je code {0} betekent dat je het sterkst scoort op {1}. Typische omgevingen: {2}.",
            "Your code {0} means you score highest on {1}. Typical environments: {2}."),

        // Values ranking
        ["values.rank.title"] = ("Jouw waarden op volgorde", "Your values ranked"),
        ["values.rank.lead"] = (
            "Wat het zwaarst weegt – en wat minder. Geen gedwongen trade-offs, wel een eerlijke rangorde van je scores.",
            "What weighs heaviest – and what weighs less. Not forced trade-offs, just an honest ranking of your scores."),
        ["values.choose"] = (
            "Bij het kiezen van werk: let op organisaties waar dit zichtbaar is in het dagelijks werk.",
            "When choosing work: look for organisations where this shows up in day-to-day work."),

        // Action / strengths templates
        ["action.lead"] = (
            "Drie concrete stappen die bij jouw profiel passen.",
            "Three concrete steps that fit your profile."),
        ["strength.lead"] = (
            "Sterke kanten om uit te spelen, en valkuilen om in de gaten te houden.",
            "Strengths to lean on, and pitfalls to watch."),
    };

    public static string Get(string key, string? lang)
    {
        if (!Map.TryGetValue(key, out var pair))
        {
            return key;
        }

        return ReportLanguage.IsEnglish(lang) ? pair.En : pair.Nl;
    }

    public static string Format(string key, string? lang, params object[] args)
        => string.Format(System.Globalization.CultureInfo.InvariantCulture, Get(key, lang), args);

    public static string LevelKey(int scorePercent)
        => scorePercent < 40 ? "level.low" : scorePercent < 70 ? "level.mid" : "level.high";

    public static string RiasecLabel(string code, string? lang)
        => Get($"riasec.{code.Trim().ToUpperInvariant()}", lang);

    public static string ValueLabel(string code, string? lang)
        => Get($"value.{code}", lang);

    public static string CultureLabel(string code, string? lang)
        => Get($"culture.{code}", lang);

    public static string FileName(AssessmentKindSlug kind, string? lang, DateTime utcNow)
    {
        var slug = kind.Slug;
        var date = utcNow.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        return ReportLanguage.IsEnglish(lang)
            ? $"Lobsy-{slug}-report-{date}.pdf"
            : $"Lobsy-{slug}-rapport-{date}.pdf";
    }
}

public readonly record struct AssessmentKindSlug(string Slug);
