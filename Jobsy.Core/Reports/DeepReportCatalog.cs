using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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

        ["strength.Realistic"] = (
            "Je pakt werk aan met je handen en maakt het af.",
            "You get things done with your hands."),
        ["strength.Investigative"] = (
            "Je zoekt eerst uit hoe iets zit. Daarna begin je.",
            "You find out how something works. Then you start."),
        ["strength.Artistic"] = (
            "Je maakt iets dat er mooi of nieuw uitziet.",
            "You make something that looks new or beautiful."),
        ["strength.Social"] = (
            "Je helpt mensen en houdt het team bij elkaar.",
            "You help people and keep the team together."),
        ["strength.Enterprising"] = (
            "Je brengt anderen in beweging en pakt kansen.",
            "You get others moving and you take chances."),
        ["strength.Conventional"] = (
            "Je houdt lijsten, planning en afspraken netjes.",
            "You keep lists, plans and agreements tidy."),
        ["pitfall.Realistic"] = (
            "Je kunt te snel doen en te weinig overleggen.",
            "You can act too fast and skip talking it through."),
        ["pitfall.Investigative"] = (
            "Je kunt te lang zoeken en te laat starten.",
            "You can search too long and start too late."),
        ["pitfall.Artistic"] = (
            "Je kunt te veel willen veranderen als het al goed is.",
            "You can want to change things that already work."),
        ["pitfall.Social"] = (
            "Je kunt te vaak ja zeggen om anderen te helpen.",
            "You can say yes too often just to help."),
        ["pitfall.Enterprising"] = (
            "Je kunt te hard duwen en anderen voorbijlopen.",
            "You can push too hard and leave others behind."),
        ["pitfall.Conventional"] = (
            "Je kunt vasthouden aan de oude manier als iets nieuws beter is.",
            "You can stick to the old way when a new way is better."),

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
        ["holland.title"] = ("Jouw beroepsletters", "Your job letters"),
        ["holland.body"] = (
            "Je letters {0} betekenen dat je het sterkst scoort op {1}. Typische omgevingen: {2}. De letters zijn een startpunt. Ze zijn geen cijfer en geen oordeel over jou.",
            "Your letters {0} mean you score highest on {1}. Typical environments: {2}. The letters are a starting point. They are not a grade and not a judgement of you."),

        // Values ranking
        ["values.rank.title"] = ("Jouw waarden op volgorde", "Your values ranked"),
        ["values.rank.lead"] = (
            "Wat het zwaarst weegt – en wat minder. Geen gedwongen trade-offs, wel een eerlijke rangorde van je scores.",
            "What weighs heaviest – and what weighs less. Not forced trade-offs, just an honest ranking of your scores."),
        ["values.choose"] = (
            "Bij het kiezen van werk: let op organisaties waar dit zichtbaar is in het dagelijks werk.",
            "When choosing work: look for organisations where this shows up in day-to-day work."),
        ["values.choose.Autonomy"] = (
            "Kies werk waar jij zelf mag bepalen hoe je het doet.",
            "Choose work where you decide how to do it."),
        ["values.choose.Connection"] = (
            "Kies werk waar je met mensen samenwerkt en je gezien voelt.",
            "Choose work where you work with people and feel seen."),
        ["values.choose.Achievement"] = (
            "Kies werk waar je resultaat ziet en mag groeien.",
            "Choose work where you see results and can grow."),
        ["values.choose.Stability"] = (
            "Kies werk met duidelijke afspraken en een vast ritme.",
            "Choose work with clear agreements and a steady rhythm."),
        ["values.choose.Impact"] = (
            "Kies werk waar jouw inzet iets betekent voor anderen.",
            "Choose work where your effort matters to other people."),
        ["v.strength.Autonomy"] = (
            "Je pakt werk zelf op en zoekt je eigen weg.",
            "You pick up work yourself and find your own way."),
        ["v.strength.Connection"] = (
            "Je bouwt vertrouwen en houdt het team bij elkaar.",
            "You build trust and keep the team together."),
        ["v.strength.Achievement"] = (
            "Je wilt resultaat zien en maakt dingen af.",
            "You want to see results and you finish things."),
        ["v.strength.Stability"] = (
            "Je houdt het rustig en betrouwbaar, ook als het druk is.",
            "You keep things calm and reliable, even when it is busy."),
        ["v.strength.Impact"] = (
            "Je wilt dat je werk ertoe doet voor anderen.",
            "You want your work to matter for other people."),
        ["v.pitfall.Autonomy"] = (
            "Je kunt te snel alleen doorwerken en hulp overslaan.",
            "You can rush ahead alone and skip asking for help."),
        ["v.pitfall.Connection"] = (
            "Je kunt te veel meebuigen om de sfeer goed te houden.",
            "You can bend too much just to keep the mood good."),
        ["v.pitfall.Achievement"] = (
            "Je kunt te hard doorgaan en rust overslaan.",
            "You can push too hard and skip rest."),
        ["v.pitfall.Stability"] = (
            "Je kunt verandering uitstellen tot het te laat voelt.",
            "You can put off change until it feels too late."),
        ["v.pitfall.Impact"] = (
            "Je kunt jezelf vergeten omdat het werk voor anderen gaat.",
            "You can forget yourself because the work is for other people."),

        // Action / strengths templates
        ["action.lead"] = (
            "Drie concrete stappen die bij jouw profiel passen.",
            "Three concrete steps that fit your profile."),
        ["strength.lead"] = (
            "Sterke kanten om uit te spelen, en valkuilen om in de gaten te houden.",
            "Strengths to lean on, and pitfalls to watch."),
    };

    /// <summary>Optional host logger. Missing keys are warned once and never returned raw.</summary>
    public static ILogger Logger { get; set; } = NullLogger.Instance;

    private static readonly ConcurrentDictionary<string, byte> MissingKeysLogged = new(StringComparer.Ordinal);

    public static string Get(string key, string? lang)
    {
        if (TryGet(key, lang, out var value))
        {
            return value;
        }

        if (MissingKeysLogged.TryAdd(key, 0))
        {
            Logger.LogWarning(
                "Deep report catalog has no text for key {CatalogKey}. Showing a plain label instead.",
                key);
        }

        return Humanize(key);
    }

    public static bool TryGet(string key, string? lang, out string value)
    {
        if (TryResolve(key, out var pair))
        {
            value = ReportLanguage.IsEnglish(lang) ? pair.En : pair.Nl;
            return true;
        }

        value = "";
        return false;
    }

    private static bool TryResolve(string key, out (string Nl, string En) pair)
    {
        if (Map.TryGetValue(key, out pair))
        {
            return true;
        }

        var alias = Alias(key);
        if (alias is not null && Map.TryGetValue(alias, out pair))
        {
            return true;
        }

        pair = default;
        return false;
    }

    private static string? Alias(string key)
    {
        const string prefix = "riasec.";
        if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var letter = ToRiasecLetter(key[prefix.Length..]);
        return letter is null ? null : prefix + letter;
    }

    /// <summary>Last segment, without dots, so a missing key never paints as <c>riasec.REALISTIC</c>.</summary>
    public static string Humanize(string key)
    {
        var last = key;
        var dot = key.LastIndexOf('.');
        if (dot >= 0 && dot < key.Length - 1)
        {
            last = key[(dot + 1)..];
        }

        if (string.IsNullOrWhiteSpace(last))
        {
            return "—";
        }

        var spaced = Regex.Replace(last.Replace('_', ' '), "([a-z])([A-Z])", "$1 $2");
        if (spaced.Length == 1)
        {
            return spaced.ToUpperInvariant();
        }

        var lower = spaced.ToLowerInvariant();
        return char.ToUpperInvariant(lower[0]) + lower[1..];
    }

    public static string Format(string key, string? lang, params object[] args)
        => string.Format(System.Globalization.CultureInfo.InvariantCulture, Get(key, lang), args);

    public static string LevelKey(int scorePercent)
        => scorePercent < 40 ? "level.low" : scorePercent < 70 ? "level.mid" : "level.high";

    public static string RiasecLabel(string code, string? lang)
    {
        var letter = ToRiasecLetter(code);
        return letter is null
            ? Get($"riasec.{code.Trim()}", lang)
            : Get($"riasec.{letter}", lang);
    }

    /// <summary>Accepts R–C and the full Holland names (Realistic, REALISTIC, …).</summary>
    public static string? ToRiasecLetter(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var trimmed = code.Trim();
        if (trimmed.Length == 1)
        {
            var letter = char.ToUpperInvariant(trimmed[0]);
            return "RIASEC".Contains(letter) ? letter.ToString() : null;
        }

        return trimmed.ToUpperInvariant() switch
        {
            "REALISTIC" => "R",
            "INVESTIGATIVE" => "I",
            "ARTISTIC" => "A",
            "SOCIAL" => "S",
            "ENTERPRISING" => "E",
            "CONVENTIONAL" => "C",
            _ => null
        };
    }

    public static string ValueLabel(string code, string? lang)
        => Get($"value.{code}", lang);

    public static string CultureLabel(string code, string? lang)
    {
        if (string.Equals(code, CulturePersonalityCatalog.Extraversion, StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "Extraversie", StringComparison.OrdinalIgnoreCase))
        {
            return DimensionLabels.For(CulturePersonalityCatalog.Extraversion, lang);
        }

        return Get($"culture.{code}", lang);
    }

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
