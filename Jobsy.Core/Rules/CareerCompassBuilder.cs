namespace Jobsy.Core.Rules;

/// <summary>
/// Candidate-facing career compass: occupation matches and plain-language advice.
/// Internal type codes stay RIASEC; all output strings are Jip-en-Janneke Dutch.
/// </summary>
public static class CareerCompassBuilder
{
    public const int SuperMatchMin = 95;
    public const int StrongMatchMin = 85;
    public const int BroadenMin = 75;

    public const string BandSuper = "super";
    public const string BandStrong = "strong";
    public const string BandBroaden = "broaden";

    public static CareerCompassSnapshot Build(RiasecScores? scores, bool fromDeepAnalysis = false)
    {
        if (scores is not { IsComplete: true })
        {
            return CareerCompassSnapshot.Empty(fromDeepAnalysis);
        }

        var ranked = Occupations
            .Select(job => Score(job, scores))
            .Where(m => m.Percent >= BroadenMin)
            .OrderByDescending(m => m.Percent)
            .ThenBy(m => m.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var strengths = CareerTestCatalog.RiasecCodes
            .Select(code => (Code: code, Percent: scores.Get(code), Label: TypeLabel(code)))
            .OrderByDescending(x => x.Percent)
            .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Where(x => x.Percent >= 55)
            .Select(x => x.Label)
            .ToList();

        return CareerCompassHierarchy.FromOccupations(
            strengths,
            ranked,
            PracticalNotes(scores, strengths, fromDeepAnalysis),
            fromDeepAnalysis,
            fromOpenAi: false);
    }

    public static string TypeLabel(string code) => code switch
    {
        CareerTestCatalog.Realistic => "Aanpakken met je handen",
        CareerTestCatalog.Investigative => "Uitzoeken hoe het zit",
        CareerTestCatalog.Artistic => "Iets moois of nieuws maken",
        CareerTestCatalog.Social => "Mensen helpen",
        CareerTestCatalog.Enterprising => "Aanjagen en verkopen",
        CareerTestCatalog.Conventional => "Netjes organiseren",
        _ => "Werk dat bij je past"
    };

    public static string BandLabel(string band) => band switch
    {
        BandSuper => "Super-match — meer dan 95% (de kernfit)",
        BandStrong => "Sterke keus — meer dan 85% (uitstekende alternatieven)",
        BandBroaden => "Handige verbreding — meer dan 75% (doorgroeirichtingen)",
        _ => "Richting om te bekijken"
    };

    public static string Band(int percent)
    {
        if (percent >= SuperMatchMin)
        {
            return BandSuper;
        }

        if (percent >= StrongMatchMin)
        {
            return BandStrong;
        }

        if (percent >= BroadenMin)
        {
            return BandBroaden;
        }

        return "";
    }

    public static IReadOnlyList<string> ForbiddenJargon { get; } =
    [
        "RIASEC",
        "OCEAN",
        "Holland-code",
        "Holland code",
        "Realistic",
        "Investigative",
        "Enterprising",
        "Conventional",
        "Big Five",
        "extraversie",
        "extraversion",
        "neuroticisme",
        "neuroticism",
        "consciëntieusheid"
    ];

    public static bool ContainsForbiddenJargon(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        foreach (var term in ForbiddenJargon)
        {
            if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Whole-word only: "sociale" is Dutch, "Social" as type name is jargon.
        return System.Text.RegularExpressions.Regex.IsMatch(
            text,
            @"\b(Social|Artistic|DISC)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    }

    internal static CareerOccupationMatch Score(CareerOccupation job, RiasecScores scores)
    {
        var weightSum = 0;
        var weighted = 0.0;
        foreach (var (code, weight) in job.Weights)
        {
            if (weight <= 0)
            {
                continue;
            }

            weightSum += weight;
            weighted += scores.Get(code) / 100.0 * weight;
        }

        var percent = weightSum <= 0
            ? 0
            : (int)Math.Clamp(Math.Round(100 * weighted / weightSum, MidpointRounding.AwayFromZero), 0, 100);
        var top = job.Weights
            .OrderByDescending(w => w.Weight)
            .Select(w => TypeLabel(w.Code).ToLowerInvariant())
            .First();
        return new CareerOccupationMatch(
            job.Title,
            percent,
            Band(percent),
            $"Dit werk vraagt vooral {top} — en dat sluit aan bij hoe jij scoort.",
            CareerOccupationKeys.FromTitle(job.Title));
    }

    private static IReadOnlyList<string> PracticalNotes(
        RiasecScores scores,
        IReadOnlyList<string> strengths,
        bool fromDeepAnalysis)
    {
        var top = CareerTestCatalog.RiasecCodes
            .Select(code => (Code: code, Percent: scores.Get(code)))
            .OrderByDescending(x => x.Percent)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .First();

        var workplace = top.Code switch
        {
            CareerTestCatalog.Realistic =>
                "Jij floreert op een werkplek waar je iets kunt aanpakken: kas, magazijn, keuken, werkplaats of buiten. Duidelijke klussen met een zichtbaar einde van de dag.",
            CareerTestCatalog.Investigative =>
                "Jij floreert waar je mag uitzoeken: kwaliteit, metingen, teelttechniek of verbetering op de vestiging. Rust om na te denken hoort erbij.",
            CareerTestCatalog.Artistic =>
                "Jij floreert waar iets mag opvallen: presentatie in de winkel, seizoensconcepten, styling of een eigen draai aan hoe het eruitziet.",
            CareerTestCatalog.Social =>
                "Jij floreert tussen mensen: zorg, horeca, winkelvloer of begeleiden van collega’s. Een warme sfeer telt zwaarder dan een stille backoffice.",
            CareerTestCatalog.Enterprising =>
                "Jij floreert waar je mag trekken: verkoop, een shift trekken, kansen zien en anderen meekrijgen. Wachten tot iemand anders begint past minder.",
            _ =>
                "Jij floreert waar de stappen duidelijk zijn: planning, kassa, administratie, lijsten en systemen. Rommelige ad-hoc dagen kosten je energie."
        };

        var culture = top.Code switch
        {
            CareerTestCatalog.Realistic =>
                "Cultuur: nuchter, doen, samen de klus afmaken. Minder vergaderen, meer resultaat dat je kunt aanwijzen.",
            CareerTestCatalog.Investigative =>
                "Cultuur: nieuwsgierig en precies. Collega’s die ‘waarom?’ ook een goede vraag vinden.",
            CareerTestCatalog.Artistic =>
                "Cultuur: ruimte voor een eigen inbreng. Vastgeroeste ‘zo doen we het altijd’ past minder.",
            CareerTestCatalog.Social =>
                "Cultuur: aandacht voor de ander. Een team dat groet, helpt en samen de piek doorstaat.",
            CareerTestCatalog.Enterprising =>
                "Cultuur: tempo en eigenaarschap. Doelen, omzet, een ploeg die in beweging is.",
            _ =>
                "Cultuur: voorspelbaar en netjes. Afspraken die kloppen, spullen op hun plek, geen chaos op de vloer."
        };

        var platform = fromDeepAnalysis
            ? "Zo zet je dit in op Lobsy: open de banenkaart. Vacatures die bij jouw richting horen scoren hoger. Filter op hoge match en bewaar wat voelt als ‘dit is het’. Werkgevers zien geen ruwe antwoorden — alleen dat je past."
            : "Zo zet je dit in op Lobsy: vul daarna de uitgebreide beroepentest (200 vragen) in voor een scherper rapport. Tot die tijd weegt de banenkaart al mee wat je hier hebt aangegeven.";

        var lines = new List<string>
        {
            strengths.Count == 0
                ? "Jouw mix is nog breed. Dat is oké: kijk welke taken je energie geven en zet die als voorkeur op je profiel."
                : $"Kort samengevat ben jij het sterkst in {JoinNl(strengths.Select(s => s.ToLowerInvariant()).ToList())}.",
            workplace,
            culture,
            "Taken die vaak passen: afwisseling tussen doen en overleg, met een duidelijke rol. Twijfel je? Kies de vacature waarvan de dagelijkse klus het meest klinkt als jouw top-richting hierboven.",
            TrainingCopy.GapAdvice,
            platform
        };
        return lines;
    }

    private static string JoinNl(IReadOnlyList<string> items) => items.Count switch
    {
        0 => "",
        1 => items[0],
        2 => $"{items[0]} en {items[1]}",
        _ => string.Join(", ", items.Take(items.Count - 1)) + " en " + items[^1]
    };

    /// <summary>
    /// Fallback labour-market catalog (general Dutch occupations). Not live Lobsy vacancies.
    /// OpenAI is the primary source after the paid 150-item test.
    /// </summary>
    public static readonly IReadOnlyList<CareerOccupation> Occupations =
    [
        new("Medewerker tuinbouw / kas", W(CareerTestCatalog.Realistic, 100)),
        new("Magazijnmedewerker / orderpicker", W(CareerTestCatalog.Realistic, 80), W(CareerTestCatalog.Conventional, 40)),
        new("Productiemedewerker", W(CareerTestCatalog.Realistic, 90), W(CareerTestCatalog.Conventional, 30)),
        new("Onderhoudsmonteur", W(CareerTestCatalog.Realistic, 90), W(CareerTestCatalog.Investigative, 30)),
        new("Medewerker bouw / afbouw", W(CareerTestCatalog.Realistic, 100)),
        new("Medewerker schoonmaak", W(CareerTestCatalog.Realistic, 70), W(CareerTestCatalog.Conventional, 40)),
        new("Kwaliteitscontroleur", W(CareerTestCatalog.Investigative, 90), W(CareerTestCatalog.Conventional, 40)),
        new("Teelttechnisch medewerker", W(CareerTestCatalog.Investigative, 70), W(CareerTestCatalog.Realistic, 50)),
        new("Lab- of meetassistent", W(CareerTestCatalog.Investigative, 100)),
        new("Winkelstylist / visuele presentatie", W(CareerTestCatalog.Artistic, 90), W(CareerTestCatalog.Enterprising, 30)),
        new("Bloemist / groenpresentatie", W(CareerTestCatalog.Artistic, 80), W(CareerTestCatalog.Realistic, 40)),
        new("Content- of seizoensmaker", W(CareerTestCatalog.Artistic, 80), W(CareerTestCatalog.Enterprising, 30)),
        new("Helpende zorg", W(CareerTestCatalog.Social, 100)),
        new("Activiteitenbegeleider", W(CareerTestCatalog.Social, 90), W(CareerTestCatalog.Artistic, 30)),
        new("Medewerker horeca / bediening", W(CareerTestCatalog.Social, 70), W(CareerTestCatalog.Enterprising, 40)),
        new("Gastvrouw / gastheer", W(CareerTestCatalog.Social, 80), W(CareerTestCatalog.Enterprising, 40)),
        new("Begeleider nieuwe collega’s", W(CareerTestCatalog.Social, 80), W(CareerTestCatalog.Enterprising, 30)),
        new("Verkoopmedewerker winkel", W(CareerTestCatalog.Enterprising, 80), W(CareerTestCatalog.Social, 50)),
        new("Teamleider winkel of horeca", W(CareerTestCatalog.Enterprising, 90), W(CareerTestCatalog.Social, 40)),
        new("Medewerker verkoop binnendienst", W(CareerTestCatalog.Enterprising, 70), W(CareerTestCatalog.Conventional, 40)),
        new("Administratief medewerker", W(CareerTestCatalog.Conventional, 100)),
        new("Planningsmedewerker", W(CareerTestCatalog.Conventional, 80), W(CareerTestCatalog.Enterprising, 30)),
        new("Kassamedewerker", W(CareerTestCatalog.Conventional, 70), W(CareerTestCatalog.Social, 40)),
        new("Orderadministrator", W(CareerTestCatalog.Conventional, 90), W(CareerTestCatalog.Realistic, 20)),
        new("Verpleegkundige / zorgmedewerker", W(CareerTestCatalog.Social, 100)),
        new("Docent / leraar", W(CareerTestCatalog.Social, 70), W(CareerTestCatalog.Artistic, 40)),
        new("ICT-beheerder", W(CareerTestCatalog.Investigative, 70), W(CareerTestCatalog.Conventional, 50)),
        new("Softwareontwikkelaar", W(CareerTestCatalog.Investigative, 80), W(CareerTestCatalog.Conventional, 40)),
        new("Chauffeur", W(CareerTestCatalog.Realistic, 80), W(CareerTestCatalog.Conventional, 30)),
        new("Elektricien", W(CareerTestCatalog.Realistic, 80), W(CareerTestCatalog.Investigative, 40)),
        new("Kok", W(CareerTestCatalog.Realistic, 60), W(CareerTestCatalog.Artistic, 50)),
        new("Receptionist", W(CareerTestCatalog.Conventional, 60), W(CareerTestCatalog.Social, 50)),
        new("HR-medewerker", W(CareerTestCatalog.Social, 60), W(CareerTestCatalog.Conventional, 50)),
        new("Marketingmedewerker", W(CareerTestCatalog.Enterprising, 70), W(CareerTestCatalog.Artistic, 50)),
        new("Boekhouder / administrateur", W(CareerTestCatalog.Conventional, 100)),
        new("Dierenverzorger", W(CareerTestCatalog.Realistic, 50), W(CareerTestCatalog.Social, 50))
    ];

    private static (string Code, int Weight) W(string code, int weight) => (code, weight);
}

public sealed record CareerOccupation(string Title, params (string Code, int Weight)[] Weights);

public sealed record CareerOccupationMatch(
    string Title,
    int Percent,
    string Band,
    string Why,
    IReadOnlyList<string>? Keys = null)
{
    public IReadOnlyList<string> SearchKeys =>
        Keys is { Count: > 0 } keys ? keys : CareerOccupationKeys.FromTitle(Title);
}

public sealed record CareerCompassSnapshot(
    IReadOnlyList<string> Strengths,
    IReadOnlyList<CareerOccupationMatch> SuperMatches,
    IReadOnlyList<CareerOccupationMatch> StrongChoices,
    IReadOnlyList<CareerOccupationMatch> Broadening,
    IReadOnlyList<string> PracticalNotes,
    bool FromDeepAnalysis,
    bool FromOpenAi = false)
{
    public static CareerCompassSnapshot Empty(bool fromDeepAnalysis = false)
        => new([], [], [], [],
        [
            "Rond de beroepentest af. Daarna laten we in gewone taal zien welk werk bij je past, en hoe je dat op de banenkaart gebruikt."
        ], fromDeepAnalysis);

    public bool HasOccupations =>
        SuperMatches.Count > 0 || StrongChoices.Count > 0 || Broadening.Count > 0;

    public IEnumerable<CareerOccupationMatch> AllOccupations =>
        SuperMatches.Concat(StrongChoices).Concat(Broadening);
}
