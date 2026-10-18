using Jobsy.Core.Enums;
using Jobsy.Core.Localization;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Reports.TestReflection;

/// <summary>
/// Deterministic NL/EN copy from score bands. AI may rewrite; this is always the fallback.
/// </summary>
public static class TestReflectionTemplateBuilder
{
    public static TestReflectionFull BuildFull(TestReflectionScoreSnapshot snapshot, string? testTitle, string language)
    {
        var lang = JobsyLanguages.Normalize(language);
        if (lang is "" or "nl")
        {
            lang = "nl";
        }

        var ranked = snapshot.Traits
            .OrderByDescending(t => t.Percent)
            .ThenBy(t => t.Code, StringComparer.Ordinal)
            .ToList();
        var themes = BuildThemes(ranked, lang).Take(3).ToList();
        while (themes.Count < 3 && ranked.Count > themes.Count)
        {
            var trait = ranked[themes.Count];
            themes.Add(new TestReflectionTheme(
                ThemeTitle(trait.Code, lang),
                SummaryLine(trait, lang)));
        }

        var practice = BuildPractice(ranked, lang);
        var watch = BuildWatch(ranked, lang);
        var (gives, drains) = BuildEnergy(ranked, lang);
        var (envTitle, envBody) = BuildEnvironment(ranked, snapshot.Kind, lang);
        var dream = BuildDreamCallout(snapshot.DreamTitle, envTitle, lang);

        var title = testTitle ?? DefaultTestTitle(snapshot.Kind, lang);
        return new TestReflectionFull(
            snapshot.Kind,
            TestDepthLevel.Full,
            lang,
            HeroTitle(title, lang),
            HeroSubtitle(lang),
            BadgeLabel(lang),
            themes,
            PracticeSectionTitle(lang),
            practice,
            WatchSectionTitle(lang),
            watch,
            gives,
            drains,
            envTitle,
            envBody,
            dream,
            FromOpenAi: false);
    }

    public static string ComputeSourceHash(TestReflectionScoreSnapshot snapshot)
    {
        var parts = snapshot.Traits
            .OrderBy(t => t.Code, StringComparer.Ordinal)
            .Select(t => $"{t.Code}:{t.Percent}");
        return $"{snapshot.Kind}|v{snapshot.QuickScanVersion}|n{snapshot.AnsweredCount}|{string.Join(",", parts)}|{snapshot.DreamTitle ?? ""}";
    }

    private static IEnumerable<TestReflectionTheme> BuildThemes(
        IReadOnlyList<TestReflectionTrait> ranked,
        string lang)
    {
        foreach (var trait in ranked.Where(t => t.Percent >= DnaSummarySentences.MidMin).Take(3))
        {
            yield return new TestReflectionTheme(
                ThemeTitle(trait.Code, lang),
                SummaryLine(trait, lang));
        }
    }

    private static string SummaryLine(TestReflectionTrait trait, string lang)
    {
        if (lang == "en")
        {
            return EnSummary(trait);
        }

        return DnaSummarySentences.Competence(trait.Code, trait.Percent) switch
        {
            { Length: > 0 } s when trait.Code is CompetencyTestCatalog.Samenwerken
                or CompetencyTestCatalog.Resultaatgerichtheid
                or CompetencyTestCatalog.Stressbestendigheid
                or CompetencyTestCatalog.Innovatie
                or CompetencyTestCatalog.Extraversie => Shorten(s),
            _ => NlSummary(trait)
        };
    }

    private static string Shorten(string sentence)
    {
        var idx = sentence.IndexOf('.');
        return idx > 0 ? sentence[..(idx + 1)] : sentence;
    }

    private static string NlSummary(TestReflectionTrait trait)
        => trait.Code switch
        {
            CompetencyTestCatalog.Samenwerken =>
                trait.Percent >= DnaSummarySentences.HighMin
                    ? "Je let op hoe het met anderen gaat, en dat merken mensen."
                    : "Je houdt rekening met anderen, ook als het druk is.",
            CompetencyTestCatalog.Resultaatgerichtheid =>
                trait.Percent >= DnaSummarySentences.HighMin
                    ? "Je werkt precies en levert iets af waar je achter staat."
                    : "Je wilt klussen netjes afronden voordat je verder gaat.",
            CompetencyTestCatalog.Stressbestendigheid =>
                trait.Percent >= DnaSummarySentences.HighMin
                    ? "Je blijft rustig bij gewone drukte en houdt vast aan wat werkt."
                    : "Je zoekt houvast als het druk wordt.",
            CompetencyTestCatalog.Innovatie =>
                trait.Percent >= DnaSummarySentences.HighMin
                    ? "Je zoekt vanzelf een andere route als iets vastloopt."
                    : "Je staat open voor verbeteringen als het echt nodig is.",
            CompetencyTestCatalog.Extraversie =>
                trait.Percent >= DnaSummarySentences.HighMin
                    ? "Contact met mensen geeft je energie op een werkdag."
                    : "Je kunt goed samenwerken, ook als je liever niet de hele dag praat.",
            _ => ValuesOrCultureSummary(trait, "nl")
        };

    private static string EnSummary(TestReflectionTrait trait)
        => trait.Code switch
        {
            CompetencyTestCatalog.Samenwerken =>
                trait.Percent >= DnaSummarySentences.HighMin
                    ? "You notice how others are doing, and people feel that."
                    : "You take others into account, even when it is busy.",
            CompetencyTestCatalog.Resultaatgerichtheid =>
                trait.Percent >= DnaSummarySentences.HighMin
                    ? "You work carefully and deliver something you stand behind."
                    : "You like to finish tasks neatly before moving on.",
            CompetencyTestCatalog.Stressbestendigheid =>
                trait.Percent >= DnaSummarySentences.HighMin
                    ? "You stay calm in everyday pressure and stick with what works."
                    : "You look for stability when things speed up.",
            CompetencyTestCatalog.Innovatie =>
                trait.Percent >= DnaSummarySentences.HighMin
                    ? "You naturally look for another route when something gets stuck."
                    : "You are open to improvements when they are really needed.",
            CompetencyTestCatalog.Extraversie =>
                trait.Percent >= DnaSummarySentences.HighMin
                    ? "Contact with people gives you energy on a workday."
                    : "You work well with others even if you prefer not to talk all day.",
            _ => ValuesOrCultureSummary(trait, "en")
        };

    private static string ValuesOrCultureSummary(TestReflectionTrait trait, string lang)
    {
        if (SchwartzValuesCatalog.CategoryCodes.Contains(trait.Code, StringComparer.OrdinalIgnoreCase))
        {
            var nl = DnaSummarySentences.Values(trait.Code);
            return lang == "en" ? EnValues(trait.Code) : nl;
        }

        if (CulturePersonalityCatalog.CultureDimensionCodes.Contains(trait.Code, StringComparer.OrdinalIgnoreCase))
        {
            var high = trait.Percent >= 60;
            return lang == "en"
                ? EnCulture(trait.Code, high)
                : DnaSummarySentences.Culture(trait.Code, trait.Percent);
        }

        if (CareerTestCatalog.RiasecCodes.Contains(trait.Code, StringComparer.OrdinalIgnoreCase))
        {
            return lang == "en" ? EnCareer(trait.Code) : DnaSummarySentences.Career(trait.Code);
        }

        return lang == "en"
            ? "This stands out in how you answered."
            : "Dit valt op in hoe je antwoordde.";
    }

    private static IReadOnlyList<string> BuildPractice(
        IReadOnlyList<TestReflectionTrait> ranked,
        string lang)
    {
        var high = ranked.Where(t => t.Percent >= DnaSummarySentences.HighMin).Take(2).ToList();
        if (high.Count == 0)
        {
            high = ranked.Take(2).ToList();
        }

        var lines = new List<string>();
        foreach (var trait in high)
        {
            lines.AddRange(PracticePool(trait.Code, lang).Take(2));
        }

        while (lines.Count < 3 && ranked.Count > 0)
        {
            var trait = ranked[lines.Count % ranked.Count];
            lines.AddRange(PracticePool(trait.Code, lang).Take(1));
        }

        return lines.Take(3).ToList();
    }

    private static IReadOnlyList<string> BuildWatch(
        IReadOnlyList<TestReflectionTrait> ranked,
        string lang)
    {
        var low = ranked.Where(t => t.Percent < DnaSummarySentences.HighMin).OrderBy(t => t.Percent).Take(2).ToList();
        if (low.Count == 0)
        {
            low = ranked.TakeLast(2).ToList();
        }

        var lines = new List<string>();
        foreach (var trait in low)
        {
            lines.AddRange(WatchPool(trait.Code, lang).Take(1));
        }

        if (lines.Count == 0)
        {
            lines.Add(lang == "en"
                ? "Big changes at once can take a moment to settle — asking for context upfront helps."
                : "Bij veel plotselinge veranderingen heb je even tijd nodig om je draai te vinden. Vraag gerust om uitleg vooraf.");
        }

        return lines.Take(3).ToList();
    }

    private static (IReadOnlyList<string> Gives, IReadOnlyList<string> Drains) BuildEnergy(
        IReadOnlyList<TestReflectionTrait> ranked,
        string lang)
    {
        var gives = ranked.Where(t => t.Percent >= DnaSummarySentences.MidMin).Take(3)
            .SelectMany(t => EnergyGivePool(t.Code, lang).Take(1))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
        if (gives.Count == 0)
        {
            gives.Add(lang == "en" ? "Clear agreements for the day" : "Duidelijke afspraken voor de dag");
        }

        var drains = ranked.Where(t => t.Percent < DnaSummarySentences.MidMin).Take(3)
            .SelectMany(t => EnergyDrainPool(t.Code, lang).Take(1))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
        if (drains.Count == 0)
        {
            drains.Add(lang == "en" ? "Constantly switching approach" : "Steeds van aanpak wisselen");
        }

        return (gives, drains);
    }

    private static (string Title, string Body) BuildEnvironment(
        IReadOnlyList<TestReflectionTrait> ranked,
        AssessmentKind kind,
        string lang)
    {
        if (ranked.Count == 0)
        {
            return lang == "en"
                ? ("A workplace that fits you", "Work with clear tasks and people you can rely on.")
                : ("Een werkplek die bij je past", "Werk met duidelijke taken en mensen waar je op kunt rekenen.");
        }

        var top = ranked[0];
        if (kind == AssessmentKind.Competence
            && top.Code is CompetencyTestCatalog.Samenwerken or CompetencyTestCatalog.Resultaatgerichtheid)
        {
            return lang == "en"
                ? ("A steady team, clear agreements, time to do it well",
                    "Think of care, logistics, or a small shop where customers know you.")
                : ("Een vaste ploeg, duidelijke afspraken, tijd om het goed te doen",
                    "Denk aan zorg, dieren of een kleine winkel waar je klanten kent.");
        }

        if (kind == AssessmentKind.Career && top.Code == CareerTestCatalog.Social)
        {
            return lang == "en"
                ? ("Helping people in a structured setting", "Guidance, care, or teaching where contact matters.")
                : ("Mensen helpen in een gestructureerde setting", "Begeleiding, zorg of lesgeven waar contact telt.");
        }

        return kind switch
        {
            AssessmentKind.Culture =>
                lang == "en"
                    ? ("A team atmosphere that matches your pace", "Look for the culture cues you scored highest on.")
                    : ("Een teamsfeer die bij je tempo past", "Zoek de cultuurkenmerken waar je het hoogst op scoorde."),
            AssessmentKind.Values =>
                lang == "en"
                    ? ("Work that matches what you find important", "Roles where your top values get room in daily choices.")
                    : ("Werk dat past bij wat jij belangrijk vindt", "Rollen waar je topwaarden ruimte krijgen in dagelijkse keuzes."),
            _ =>
                lang == "en"
                    ? ("An environment that uses your strengths", "Start with tasks that match your highest scores.")
                    : ("Een omgeving die je sterke kanten gebruikt", "Begin bij taken die passen bij je hoogste scores.")
        };
    }

    private static string? BuildDreamCallout(string? dreamTitle, string envTitle, string lang)
    {
        if (string.IsNullOrWhiteSpace(dreamTitle))
        {
            return null;
        }

        return lang == "en"
            ? $"For your dream role {dreamTitle.Trim()}, this comes together: {envTitle.ToLowerInvariant()}."
            : $"Bij je droombaan {dreamTitle.Trim()} komt dit samen: {envTitle.ToLowerInvariant()}.";
    }

    private static string ThemeTitle(string code, string lang)
    {
        if (lang == "en")
        {
            return EnThemeTitle(code);
        }

        return code switch
        {
            CompetencyTestCatalog.Samenwerken => "Zorgzaam",
            CompetencyTestCatalog.Resultaatgerichtheid => "Zorgvuldig",
            CompetencyTestCatalog.Stressbestendigheid => "Standvastig",
            CompetencyTestCatalog.Innovatie => "Vernieuwend",
            CompetencyTestCatalog.Extraversie => "Contactgericht",
            CareerTestCatalog.Social => "Helpend",
            CareerTestCatalog.Realistic => "Praktisch",
            CareerTestCatalog.Investigative => "Nieuwsgierig",
            CareerTestCatalog.Artistic => "Creatief",
            CareerTestCatalog.Enterprising => "Aanjagend",
            CareerTestCatalog.Conventional => "Gestructureerd",
            SchwartzValuesCatalog.Connection => "Verbonden",
            SchwartzValuesCatalog.Autonomy => "Zelfstandig",
            SchwartzValuesCatalog.Achievement => "Groeiend",
            SchwartzValuesCatalog.Stability => "Stabiel",
            SchwartzValuesCatalog.Impact => "Betrokken",
            CulturePersonalityCatalog.Collaboration => "Samen",
            CulturePersonalityCatalog.PeopleFirst => "Mensgericht",
            CulturePersonalityCatalog.Flexibility => "Soepel",
            CulturePersonalityCatalog.Innovation => "Vernieuwend",
            CulturePersonalityCatalog.Informal => "Informeel",
            _ => DimensionLabels.For(code, "nl")
        };
    }

    private static string EnThemeTitle(string code)
        => code switch
        {
            CompetencyTestCatalog.Samenwerken => "Caring",
            CompetencyTestCatalog.Resultaatgerichtheid => "Careful",
            CompetencyTestCatalog.Stressbestendigheid => "Steadfast",
            CompetencyTestCatalog.Innovatie => "Renewing",
            CompetencyTestCatalog.Extraversie => "People-oriented",
            CareerTestCatalog.Social => "Helping",
            CareerTestCatalog.Realistic => "Hands-on",
            CareerTestCatalog.Investigative => "Curious",
            CareerTestCatalog.Artistic => "Creative",
            CareerTestCatalog.Enterprising => "Driving",
            CareerTestCatalog.Conventional => "Structured",
            SchwartzValuesCatalog.Connection => "Connected",
            SchwartzValuesCatalog.Autonomy => "Independent",
            SchwartzValuesCatalog.Achievement => "Growing",
            SchwartzValuesCatalog.Stability => "Steady",
            SchwartzValuesCatalog.Impact => "Engaged",
            _ => DimensionLabels.For(code, "en")
        };

    private static string DefaultTestTitle(AssessmentKind kind, string lang)
        => kind switch
        {
            AssessmentKind.Career => lang == "en" ? "what you enjoy" : "wat je leuk vindt",
            AssessmentKind.Culture => lang == "en" ? "where you feel at home" : "waar je je thuis voelt",
            AssessmentKind.Values => lang == "en" ? "what matters to you" : "wat je belangrijk vindt",
            _ => lang == "en" ? "how you work" : "hoe je werkt"
        };

    private static string HeroTitle(string testTitle, string lang)
        => lang == "en"
            ? $"Your mirror: {testTitle}"
            : $"Jouw spiegel: {testTitle}";

    private static string HeroSubtitle(string lang)
        => lang == "en"
            ? "From all your answers in this test, and what you shared about your work and dream job."
            : "Uit al je antwoorden in deze test, en wat je vertelde over je werk en je droombaan.";

    private static string BadgeLabel(string lang)
        => lang == "en" ? "Complete · only you see this" : "Volledig · alleen jij ziet dit";

    private static string PracticeSectionTitle(string lang)
        => lang == "en" ? "How you see this on a normal workday" : "Zo zie je het terug op een gewone werkdag";

    private static string WatchSectionTitle(string lang)
        => lang == "en" ? "No weaknesses — just things to know" : "Geen zwaktes, wel dingen om te weten";

    private static IEnumerable<string> PracticePool(string code, string lang)
    {
        if (lang == "en")
        {
            return code switch
            {
                CompetencyTestCatalog.Samenwerken =>
                [
                    "Onboarding a new colleague: you take time and explain things calmly again.",
                    "Noticing someone feels off before they say it themselves."
                ],
                CompetencyTestCatalog.Resultaatgerichtheid =>
                [
                    "Closing a busy shift neatly, without leaving things for the next person.",
                    "Double-checking details before you hand work over."
                ],
                CompetencyTestCatalog.Stressbestendigheid =>
                [
                    "Keeping overview when the schedule shifts unexpectedly.",
                    "Staying polite when customers or colleagues are stressed."
                ],
                CompetencyTestCatalog.Innovatie =>
                [
                    "Suggesting a small improvement when a routine keeps failing.",
                    "Trying a new order of tasks when the old way slows you down."
                ],
                CompetencyTestCatalog.Extraversie =>
                [
                    "Starting a short check-in so the team knows who needs help.",
                    "Explaining something out loud so everyone stays aligned."
                ],
                CareerTestCatalog.Social =>
                [
                    "Explaining a task step by step to someone who is learning.",
                    "Checking in with people after a hectic moment."
                ],
                _ => ["Turning your highest scores into small habits on ordinary days."]
            };
        }

        return code switch
        {
            CompetencyTestCatalog.Samenwerken =>
            [
                "Een nieuwe collega inwerken: je neemt de tijd en legt het rustig nog een keer uit.",
                "Merken dat iemand zich niet goed voelt, nog voordat die het zelf zegt."
            ],
            CompetencyTestCatalog.Resultaatgerichtheid =>
            [
                "Een drukke dienst netjes afronden, zonder dat er dingen blijven liggen voor de volgende.",
                "Details nalopen voordat je werk overdraagt."
            ],
            CompetencyTestCatalog.Stressbestendigheid =>
            [
                "Overzicht houden als de planning onverwacht verschuift.",
                "Beleefd blijven als klanten of collega's gestrest zijn."
            ],
            CompetencyTestCatalog.Innovatie =>
            [
                "Een kleine verbetering voorstellen als een vaste routine blijft haperen.",
                "Een andere volgorde proberen als de oude manier je vertraagt."
            ],
            CompetencyTestCatalog.Extraversie =>
            [
                "Een korte check-in starten zodat het team weet wie hulp nodig heeft.",
                "Iets hardop uitleggen zodat iedereen dezelfde lijn volgt."
            ],
            CareerTestCatalog.Social =>
            [
                "Een taak stap voor stap uitleggen aan iemand die leert.",
                "Even bij iemand langsgaan na een hectisch moment."
            ],
            _ => ["Je hoogste scores zichtbaar maken in kleine gewoontes op gewone dagen."]
        };
    }

    private static IEnumerable<string> WatchPool(string code, string lang)
    {
        if (lang == "en")
        {
            return code switch
            {
                CompetencyTestCatalog.Samenwerken =>
                    ["You do not say no quickly. You may take on more than is good for you."],
                CompetencyTestCatalog.Resultaatgerichtheid =>
                    ["Perfect can take time — agree upfront what 'good enough' means on busy days."],
                CompetencyTestCatalog.Innovatie =>
                    ["Many changes at once need a moment — ask what stays the same."],
                CompetencyTestCatalog.Extraversie =>
                    ["Long days full of talk can drain you — plan quiet blocks."],
                _ => ["When the pace jumps, you need a moment to find your footing — ask for context upfront."]
            };
        }

        return code switch
        {
            CompetencyTestCatalog.Samenwerken =>
                ["Je zegt niet snel nee. Daardoor neem je soms meer op je dan goed voor je is."],
            CompetencyTestCatalog.Resultaatgerichtheid =>
                ["Perfectie kost tijd — spreek vooraf af wat op drukke dagen 'goed genoeg' is."],
            CompetencyTestCatalog.Innovatie =>
                ["Veel verandering tegelijk vraagt even landen — vraag wat wél hetzelfde blijft."],
            CompetencyTestCatalog.Extraversie =>
                ["Lange dagen vol praten kunnen vermoeien — plan stille blokken."],
            _ => ["Bij veel plotselinge veranderingen heb je even tijd nodig — vraag gerust om uitleg vooraf."]
        };
    }

    private static IEnumerable<string> EnergyGivePool(string code, string lang)
    {
        if (lang == "en")
        {
            return code switch
            {
                CompetencyTestCatalog.Samenwerken => ["A team you can count on", "Seeing that you really helped someone"],
                CompetencyTestCatalog.Resultaatgerichtheid => ["Work you can finish properly", "Clear handover at the end of a task"],
                CompetencyTestCatalog.Stressbestendigheid => ["Predictable routines on busy days", "Knowing what is expected of you"],
                CompetencyTestCatalog.Innovatie => ["Room to improve a stuck process", "Learning something that saves time later"],
                CompetencyTestCatalog.Extraversie => ["Short contact moments that align the team", "Explaining something to others"],
                _ => ["Tasks that match your strongest scores"]
            };
        }

        return code switch
        {
            CompetencyTestCatalog.Samenwerken => ["Een team waarin je op elkaar kunt rekenen", "Merken dat je iemand echt hielp"],
            CompetencyTestCatalog.Resultaatgerichtheid => ["Werk dat je goed kunt afronden", "Een duidelijke overdracht aan het eind"],
            CompetencyTestCatalog.Stressbestendigheid => ["Vaste routines op drukke dagen", "Weten wat er van je verwacht wordt"],
            CompetencyTestCatalog.Innovatie => ["Ruimte om een vastlopend proces te verbeteren", "Iets leren dat later tijd scheelt"],
            CompetencyTestCatalog.Extraversie => ["Korte contactmomenten die het team op één lijn zetten", "Iets uitleggen aan anderen"],
            _ => ["Taken die passen bij je sterkste scores"]
        };
    }

    private static IEnumerable<string> EnergyDrainPool(string code, string lang)
    {
        if (lang == "en")
        {
            return code switch
            {
                CompetencyTestCatalog.Samenwerken => ["Conflict that keeps hanging in the team"],
                CompetencyTestCatalog.Resultaatgerichtheid => ["Haste at the expense of accuracy"],
                CompetencyTestCatalog.Innovatie => ["Changing approach every hour without reason"],
                CompetencyTestCatalog.Extraversie => ["Days without any quiet moment"],
                CompetencyTestCatalog.Stressbestendigheid => ["Chaos without anyone owning the plan"],
                _ => ["Constantly switching approach"]
            };
        }

        return code switch
        {
            CompetencyTestCatalog.Samenwerken => ["Ruzie die blijft hangen in het team"],
            CompetencyTestCatalog.Resultaatgerichtheid => ["Haast ten koste van zorgvuldigheid"],
            CompetencyTestCatalog.Innovatie => ["Elk uur van aanpak wisselen zonder reden"],
            CompetencyTestCatalog.Extraversie => ["Dagen zonder enkel rustig moment"],
            CompetencyTestCatalog.Stressbestendigheid => ["Chaos zonder dat iemand het plan bewaakt"],
            _ => ["Steeds van aanpak wisselen"]
        };
    }

    private static string EnValues(string code)
        => code switch
        {
            SchwartzValuesCatalog.Autonomy => "At work you want room to choose and grow.",
            SchwartzValuesCatalog.Connection => "At work you want warm ties with colleagues and customers.",
            SchwartzValuesCatalog.Achievement => "At work you want to see results and grow in what you can do.",
            SchwartzValuesCatalog.Stability => "At work you want certainty and predictable agreements.",
            SchwartzValuesCatalog.Impact => "At work you want to mean something for people and the world around you.",
            _ => "This value shapes what you look for in work."
        };

    private static string EnCulture(string code, bool high)
        => (code, high) switch
        {
            (CulturePersonalityCatalog.Autonomy, true) => "You prefer working independently without someone dictating every step.",
            (CulturePersonalityCatalog.Autonomy, false) => "You feel better with clear frameworks and guidance from a lead.",
            (CulturePersonalityCatalog.Collaboration, true) => "You want to pull together and consult instead of doing everything alone.",
            (CulturePersonalityCatalog.Collaboration, false) => "You prefer working on your own and sharing results rather than every detail.",
            _ => high
                ? "This culture theme fits how you like to work."
                : "You may prefer the opposite pace or tone at work."
        };

    private static string EnCareer(string code)
        => code switch
        {
            CareerTestCatalog.Realistic => "You feel at home with hands-on work: making, fixing, or delivering something concrete.",
            CareerTestCatalog.Investigative => "You want to understand how something works and dig into what is behind it.",
            CareerTestCatalog.Artistic => "You want to make something new or beautiful and need room for your own ideas.",
            CareerTestCatalog.Social => "You want to help people: explain, guide, or make sure others can move forward.",
            CareerTestCatalog.Enterprising => "You want to drive things forward: take initiative, convince, and book results.",
            CareerTestCatalog.Conventional => "You like organizing neatly: lists, systems, and fixed steps give you calm.",
            _ => "Your career direction points to work that fits how you like to be busy."
        };
}
