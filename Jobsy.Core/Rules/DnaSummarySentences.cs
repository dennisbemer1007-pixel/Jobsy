namespace Jobsy.Core.Rules;

/// <summary>
/// Fixed B1 Dutch template sentences for Mijn DNA test summary cards.
/// One sentence per test × dominant result/level — no AI.
/// Reviewable list: see PR description / DnaSummarySentences.AllEntries.
/// </summary>
public static class DnaSummarySentences
{
    public const int HighMin = 70;
    public const int MidMin = 45;

    public static string Level(int percent)
        => percent >= HighMin ? "high" : percent >= MidMin ? "mid" : "low";

    public static string Competence(string categoryCode, int percent)
    {
        var level = Level(percent);
        return (categoryCode, level) switch
        {
            (CompetencyTestCatalog.Samenwerken, "high") =>
                "Samenwerken is een sterke kant van jou: je trekt anderen mee en houdt rekening met het team.",
            (CompetencyTestCatalog.Samenwerken, "mid") =>
                "Je kunt samenwerken, en groeit nog in overleg en luisteren naar anderen.",
            (CompetencyTestCatalog.Samenwerken, _) =>
                "Samenwerken kost je nog energie; korte oefening in overleg maakt teamwerk makkelijker.",

            (CompetencyTestCatalog.Resultaatgerichtheid, "high") =>
                "Je maakt af wat je belooft. Collega’s kunnen op jouw planning en afronding rekenen.",
            (CompetencyTestCatalog.Resultaatgerichtheid, "mid") =>
                "Je wilt resultaat, en kunt nog scherper plannen zodat deadlines rustig blijven.",
            (CompetencyTestCatalog.Resultaatgerichtheid, _) =>
                "Afronden en plannen is nog geen automatisme; kleine stappen helpen klussen op tijd rond te krijgen.",

            (CompetencyTestCatalog.Stressbestendigheid, "high") =>
                "Je blijft kalm als het druk of onverwacht is — waardevol in zorg, logistiek en klantcontact.",
            (CompetencyTestCatalog.Stressbestendigheid, "mid") =>
                "Je houdt je meestal staande, en kunt nog groeien in herstellen na piekmomenten.",
            (CompetencyTestCatalog.Stressbestendigheid, _) =>
                "Drukte of onverwachte wendingen raken je snel; rust en overzicht maken werkdagen lichter.",

            (CompetencyTestCatalog.Innovatie, "high") =>
                "Je zoekt vanzelf een nieuwe weg als iets vastloopt. Dat past bij leren, techniek en verbeterwerk.",
            (CompetencyTestCatalog.Innovatie, "mid") =>
                "Je staat open voor nieuwe manieren, en kunt dat nog vaker inzetten als iets stokt.",
            (CompetencyTestCatalog.Innovatie, _) =>
                "Nieuwe werkwijzen voelen nog onwennig; stapsgewijs oefenen helpt.",

            (CompetencyTestCatalog.Extraversie, "high") =>
                "Je haalt energie uit mensen. Klantcontact, overleg en presenteren liggen je goed.",
            (CompetencyTestCatalog.Extraversie, "mid") =>
                "Je kunt met mensen werken, en kiest soms nog liever een rustigere setting.",
            (CompetencyTestCatalog.Extraversie, _) =>
                "Veel contact op een dag is vermoeiend; kleine stappen in gesprekken helpen.",

            _ => "Je sterke kanten uit de competentietest geven richting aan passend werk."
        };
    }

    public static string Career(string riasecCode)
        => riasecCode switch
        {
            CareerTestCatalog.Realistic =>
                "Je voelt je thuis bij werk met je handen: maken, repareren of iets concreets afleveren.",
            CareerTestCatalog.Investigative =>
                "Je wilt snappen hoe iets werkt en zoekt graag uit wat erachter zit.",
            CareerTestCatalog.Artistic =>
                "Je wilt iets moois of nieuws maken en zoekt ruimte voor eigen ideeën.",
            CareerTestCatalog.Social =>
                "Je wilt mensen helpen: uitleggen, begeleiden of zorgen dat anderen verder kunnen.",
            CareerTestCatalog.Enterprising =>
                "Je wilt aanjagen en verkopen: initiatief nemen, overtuigen en resultaat boeken.",
            CareerTestCatalog.Conventional =>
                "Je wilt netjes organiseren: lijsten, systemen en vaste stappen geven je rust.",
            _ => "Jouw beroepsrichting wijst naar werk dat past bij hoe jij graag bezig bent."
        };

    public static string Culture(string dimensionCode, int percent)
    {
        var high = percent >= 60;
        return (dimensionCode, high) switch
        {
            (CulturePersonalityCatalog.Autonomy, true) =>
                "Je werkt het liefst zelfstandig, zonder dat iemand elke stap voorzegt.",
            (CulturePersonalityCatalog.Autonomy, false) =>
                "Je voelt je prettiger met duidelijke kaders en sturing van een leidinggevende.",

            (CulturePersonalityCatalog.Informal, true) =>
                "Een informele sfeer past bij jou: kort lijntje, tutoyeren, weinig poespas.",
            (CulturePersonalityCatalog.Informal, false) =>
                "Je houdt van een formalere toon: duidelijke rollen en nette afspraken.",

            (CulturePersonalityCatalog.Collaboration, true) =>
                "Je wilt samen optrekken en overleggen in plaats van alles alleen te doen.",
            (CulturePersonalityCatalog.Collaboration, false) =>
                "Je werkt graag zelfstandig en deelt resultaat liever dan elk detail.",

            (CulturePersonalityCatalog.Flexibility, true) =>
                "Je beweegt soepel mee als de planning of de klus verandert.",
            (CulturePersonalityCatalog.Flexibility, false) =>
                "Je presteert beter met een vaste structuur en voorspelbare dagen.",

            (CulturePersonalityCatalog.Innovation, true) =>
                "Je wilt nieuwe dingen proberen en verbeteringen uitproberen op de werkvloer.",
            (CulturePersonalityCatalog.Innovation, false) =>
                "Je houdt van bekende werkwijzen die bewezen goed werken.",

            (CulturePersonalityCatalog.PeopleFirst, true) =>
                "Mensen gaan bij jou voor: sfeer en aandacht wegen zwaar in je keuze.",
            (CulturePersonalityCatalog.PeopleFirst, false) =>
                "Je let vooral op resultaat en output; mensencontact is middel, geen doel.",

            _ => "Jouw cultuurvoorkeur laat zien in wat voor team jij tot rust komt."
        };
    }

    public static string CultureCombined(string firstCode, int firstPercent, string secondCode, int secondPercent)
    {
        var a = Culture(firstCode, firstPercent);
        var b = Culture(secondCode, secondPercent);
        if (string.Equals(a, b, StringComparison.Ordinal))
        {
            return a;
        }

        // Shorten second into a trailing clause when possible.
        return $"{TrimSentence(a)} Daarnaast: {LowerFirst(TrimSentence(b))}.";
    }

    public static string Values(string valueCode)
        => valueCode switch
        {
            SchwartzValuesCatalog.Autonomy =>
                "Op werk wil je vooral eigen regie en ruimte voor nieuwe uitdagingen.",
            SchwartzValuesCatalog.Connection =>
                "Op werk wil je vooral een warme band met collega’s en klanten.",
            SchwartzValuesCatalog.Achievement =>
                "Op werk wil je vooral resultaat zien en groeien in wat je kunt.",
            SchwartzValuesCatalog.Stability =>
                "Op werk wil je vooral zekerheid, vaste afspraken en voorspelbaarheid.",
            SchwartzValuesCatalog.Impact =>
                "Op werk wil je vooral iets betekenen voor mens en omgeving.",
            _ => "Jouw topwaarden laten zien wat je écht belangrijk vindt op werk."
        };

    /// <summary>All fixed NL sentences for proofreading (key → sentence).</summary>
    public static IReadOnlyList<(string Key, string Sentence)> AllEntries()
    {
        var list = new List<(string, string)>();
        foreach (var cat in CompetencyTestCatalog.QuickScanCategories)
        {
            foreach (var level in new[] { "high", "mid", "low" })
            {
                var pct = level switch { "high" => 80, "mid" => 55, _ => 30 };
                list.Add(($"competence.{cat}.{level}", Competence(cat, pct)));
            }
        }

        foreach (var code in CareerTestCatalog.RiasecCodes)
        {
            list.Add(($"career.{code}", Career(code)));
        }

        foreach (var code in CulturePersonalityCatalog.CultureDimensionCodes)
        {
            list.Add(($"culture.{code}.high", Culture(code, 75)));
            list.Add(($"culture.{code}.low", Culture(code, 40)));
        }

        foreach (var code in SchwartzValuesCatalog.CategoryCodes)
        {
            list.Add(($"values.{code}", Values(code)));
        }

        return list;
    }

    public static (string LowPoleKey, string HighPoleKey) CulturePoles(string dimensionCode)
        => dimensionCode switch
        {
            CulturePersonalityCatalog.Autonomy => ("Dna.CulturePole.Autonomy.Low", "Dna.CulturePole.Autonomy.High"),
            CulturePersonalityCatalog.Informal => ("Dna.CulturePole.Informal.Low", "Dna.CulturePole.Informal.High"),
            CulturePersonalityCatalog.Collaboration => ("Dna.CulturePole.Collaboration.Low", "Dna.CulturePole.Collaboration.High"),
            CulturePersonalityCatalog.Flexibility => ("Dna.CulturePole.Flexibility.Low", "Dna.CulturePole.Flexibility.High"),
            CulturePersonalityCatalog.Innovation => ("Dna.CulturePole.Innovation.Low", "Dna.CulturePole.Innovation.High"),
            CulturePersonalityCatalog.PeopleFirst => ("Dna.CulturePole.PeopleFirst.Low", "Dna.CulturePole.PeopleFirst.High"),
            _ => ("Dna.CulturePole.Generic.Low", "Dna.CulturePole.Generic.High")
        };

    private static string TrimSentence(string s)
        => s.Trim().TrimEnd('.', '!', '?');

    private static string LowerFirst(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];
}
