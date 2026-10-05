using Jobsy.Core.Careers;

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

        var ranked = Listed(scores);

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
            fromOpenAi: false,
            ScoresKey(scores));
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
        BandSuper => "Past het best",
        BandStrong => "Past goed",
        BandBroaden => "Ook de moeite",
        _ => "Richting om te bekijken"
    };

    /// <summary>
    /// Drops lines that point at vacancies or the job map. The RIASEC list itself stays unchanged.
    /// </summary>
    public static IReadOnlyList<string> NotesForCandidate(IReadOnlyList<string> notes, bool employersOn)
    {
        if (employersOn)
        {
            return notes;
        }

        return notes.Where(note => !MentionsEmployerSurface(note)).ToList();
    }

    public static bool MentionsEmployerSurface(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return false;
        }

        return note.Contains("vacature", StringComparison.OrdinalIgnoreCase)
               || note.Contains("banenkaart", StringComparison.OrdinalIgnoreCase)
               || note.Contains("job map", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Every listable occupation (high or medium confidence, with a sourced profile), scored and ordered.</summary>
    public static IReadOnlyList<CareerOccupationMatch> Ranked(RiasecScores scores)
        => OccupationCatalog.Shared.Listable
            .Select(job => Score(job, scores))
            .OrderByDescending(m => m.Percent)
            .ThenBy(m => m.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// The one job list for the report, the coach, the action plan and /carriere.
    /// Top 8–12 by profile match. Tiers are by rank, not by a fixed percent.
    /// Leadership stays out when Enterprising is not a top-3 direction.
    /// At most two jobs per ISCO unit group. Only high and medium confidence.
    /// </summary>
    public static IReadOnlyList<CareerOccupationMatch> Listed(RiasecScores scores, string? education = null)
    {
        var catalog = OccupationCatalog.Shared;
        var gate = CareerEducationGate.MaxIscoLevel(education);
        var allowLead = EnterprisingInTop3(scores);
        var jobs = new List<CareerOccupationMatch>();
        foreach (var job in Ranked(scores))
        {
            var occupation = catalog.Get(job.EscoId);
            if (occupation is null)
            {
                continue;
            }

            if (!allowLead && OccupationCatalog.IsLeadership(occupation))
            {
                continue;
            }

            if (!CareerEducationGate.Passes(occupation.IscoLevel, gate))
            {
                continue;
            }

            jobs.Add(job);
        }

        ApplyLevelSwaps(jobs, scores, gate);
        jobs = TakeDiverse(jobs, perIsco: 2, take: CareerCompassSanitize.MaxCatalogueJobs);
        jobs = CareerCompassSanitize.OrderForEducation(jobs, education);
        return CareerCompassSanitize.AssignRankBands(jobs);
    }

    private static void ApplyLevelSwaps(List<CareerOccupationMatch> jobs, RiasecScores scores, CareerEducationGate.Result gate)
    {
        if (!gate.SubstituteLowerOffice)
        {
            return;
        }

        var catalog = OccupationCatalog.Shared;
        foreach (var swap in catalog.LevelSwaps)
        {
            var from = jobs.FindIndex(job => string.Equals(job.EscoId, swap.FromId, StringComparison.OrdinalIgnoreCase));
            if (from < 0)
            {
                continue;
            }

            jobs.RemoveAt(from);
            if (jobs.Any(job => string.Equals(job.EscoId, swap.ToId, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var target = catalog.Get(swap.ToId);
            if (target is null || !target.IsListable || !CareerEducationGate.Passes(target.IscoLevel, gate))
            {
                continue;
            }

            if (!EnterprisingInTop3(scores) && OccupationCatalog.IsLeadership(target))
            {
                continue;
            }

            jobs.Insert(Math.Min(from, jobs.Count), Score(target, scores));
        }
    }

    private static List<CareerOccupationMatch> TakeDiverse(List<CareerOccupationMatch> jobs, int perIsco, int take)
    {
        var catalog = OccupationCatalog.Shared;
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var picked = new List<CareerOccupationMatch>(take);
        foreach (var job in jobs)
        {
            var isco = catalog.Get(job.EscoId)?.Isco ?? "";
            counts.TryGetValue(isco, out var seen);
            if (isco.Length > 0 && seen >= perIsco)
            {
                continue;
            }

            counts[isco] = seen + 1;
            picked.Add(job);
            if (picked.Count == take)
            {
                break;
            }
        }

        return picked;
    }

    /// <summary>Teamleider, voorman and ploegleider need Enterprising in the top 3.</summary>
    public static bool IsLeadershipTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var fold = title.ToLowerInvariant();
        return fold.Contains("teamleider", StringComparison.Ordinal)
               || fold.Contains("leidinggevende", StringComparison.Ordinal)
               || fold.Contains("ploegleider", StringComparison.Ordinal)
               || fold.Contains("voorman", StringComparison.Ordinal);
    }

    public static bool EnterprisingInTop3(RiasecScores scores)
    {
        var top = CareerTestCatalog.RiasecCodes
            .Select(code => (Code: code, Score: scores.Get(code)))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .Take(3);
        return top.Any(x => string.Equals(x.Code, CareerTestCatalog.Enterprising, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Profile-match percent for a title that resolves to an occupation with a sourced profile.
    /// Null when the title does not resolve, or when that occupation has no sourced profile.
    /// <paramref name="education"/> does not change the number: the education gate only decides what is listed.
    /// </summary>
    public static int? CatalogueFit(string title, RiasecScores scores, string? education = null)
    {
        _ = education;
        var occupation = OccupationCatalog.Shared.Resolve(title);
        // Low confidence and occupations without a profile have no percent. Callers show "Geen score".
        return occupation is { IsListable: true, Oi: { Count: 6 } oi } ? ProfileMatch(oi, scores) : null;
    }

    /// <summary>The job's strongest sourced letters, or empty when the title has no profile.</summary>
    public static IReadOnlyList<(string Code, int Weight)> WeightsFor(string title)
    {
        var occupation = OccupationCatalog.Shared.Resolve(title);
        if (occupation?.Oi is not { Count: 6 } oi)
        {
            return [];
        }

        var letters = new List<(string Code, int Weight)>(6);
        for (var i = 0; i < CareerTestCatalog.RiasecCodes.Length && i < oi.Count; i++)
        {
            var weight = (int)Math.Round((oi[i] - 1d) * 100d, MidpointRounding.AwayFromZero);
            if (weight > 0)
            {
                letters.Add((CareerTestCatalog.RiasecCodes[i], weight));
            }
        }

        return letters
            .OrderByDescending(weight => weight.Weight)
            .ThenBy(weight => weight.Code, StringComparer.Ordinal)
            .Take(3)
            .ToList();
    }

    /// <summary>Primary direction of a resolved catalogue title. Empty when the title has no profile.</summary>
    public static string PrimaryCode(string title)
        => TopCode(OccupationCatalog.Shared.Resolve(title)?.Oi);

    private static string TopCode(IReadOnlyList<double>? oi)
    {
        if (oi is not { Count: >= 6 })
        {
            return "";
        }

        var best = 0;
        for (var i = 1; i < 6; i++)
        {
            if (oi[i] > oi[best])
            {
                best = i;
            }
        }

        return oi[best] <= 1d ? "" : CareerTestCatalog.RiasecCodes[best];
    }

    /// <summary>Holland letters for role-fit helpers. Null when the title has no sourced profile.</summary>
    internal static CareerOccupation? WeightedOccupation(string title)
    {
        var weights = WeightsFor(title);
        if (weights.Count == 0)
        {
            return null;
        }

        var occupation = OccupationCatalog.Shared.Resolve(title);
        return new CareerOccupation(occupation?.Nl ?? title.Trim(), weights.ToArray());
    }

    /// <summary>Short workplace phrases for the top directions, used when fewer than three jobs survived.</summary>
    public static string TypicalEnvironments(IEnumerable<string> domainCodes, string? lang)
    {
        var en = string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase);
        var phrases = new List<string>();
        foreach (var code in domainCodes)
        {
            if (phrases.Count == 3)
            {
                break;
            }

            var phrase = EnvironmentPhrase(code, en);
            if (phrase.Length == 0 || phrases.Contains(phrase, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            phrases.Add(phrase);
        }

        return string.Join(", ", phrases);
    }

    private static string EnvironmentPhrase(string code, bool en)
    {
        var key = code.Trim();
        if (key.Length == 1)
        {
            key = key.ToUpperInvariant() switch
            {
                "R" => CareerTestCatalog.Realistic,
                "I" => CareerTestCatalog.Investigative,
                "A" => CareerTestCatalog.Artistic,
                "S" => CareerTestCatalog.Social,
                "E" => CareerTestCatalog.Enterprising,
                "C" => CareerTestCatalog.Conventional,
                _ => key
            };
        }

        return key.ToUpperInvariant() switch
        {
            "REALISTIC" => en ? "a workplace, warehouse, kitchen or outdoors" : "werkplaats, magazijn, keuken of buiten",
            "INVESTIGATIVE" => en ? "measuring, quality checks or figuring something out" : "meten, kwaliteit of uitzoeken",
            "ARTISTIC" => en ? "a shop floor, styling or presentation" : "winkel, styling of presentatie",
            "SOCIAL" => en ? "care, hospitality or a shop floor with people" : "zorg, horeca of een winkelvloer",
            "ENTERPRISING" => en ? "sales or a team that is on the move" : "verkoop of een ploeg in beweging",
            "CONVENTIONAL" => en ? "planning, a till or administration" : "planning, kassa of administratie",
            _ => ""
        };
    }

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

    internal static CareerOccupationMatch Score(Occupation job, RiasecScores scores)
    {
        var topCode = TopCode(job.Oi);
        var top = string.IsNullOrEmpty(topCode)
            ? "werk dat bij je past"
            : TypeLabel(topCode).ToLowerInvariant();
        var percent = job.Oi is { Count: 6 } oi ? ProfileMatch(oi, scores) : 0;
        var noScore = job.NoScore;
        return new CareerOccupationMatch(
            job.Nl,
            percent,
            "",
            noScore
                ? OccupationCopy.NoScoreSentence
                : $"Dit werk vraagt vooral {top} — en dat sluit aan bij hoe jij scoort.",
            CareerOccupationKeys.FromTitle(job.Nl),
            job.Id,
            job.Confidence,
            job.Onet,
            noScore);
    }

    /// <summary>Six letter scores joined, so a stored compass can be checked against the profile.</summary>
    public static string ScoresKey(RiasecScores scores)
        => string.Join('|',
            scores.Realistic,
            scores.Investigative,
            scores.Artistic,
            scores.Social,
            scores.Enterprising,
            scores.Conventional);

    /// <summary>
    /// Job-fit percent (0–100). It is a weighted mean of the candidate's own letter scores,
    /// so it never exceeds their highest letter and never drops below their lowest.
    ///
    /// Steps:
    /// 1. Weight each letter with w = OI − 1 (0..6), straight from the sourced profile.
    /// 2. percent = round(Σ w·score / Σ w), midpoint away from zero, clamped to 0..100.
    /// 3. Letters with w = 0 do not pull the result.
    ///
    /// Worked example. Profile R66 I38 A37 S64 E43 C62.
    /// Tuinbouwmedewerker OI [7, 1.97, 1.59, 1.66, 2.09, 3.35].
    /// Weights [6, 0.97, 0.59, 0.66, 1.09, 2.35], sum of weights 11.66.
    /// Σ w·score = 6×66 + 0.97×38 + 0.59×37 + 0.66×64 + 1.09×43 + 2.35×62 = 689.5.
    /// 689.5 / 11.66 = 59.13 → 59.
    /// </summary>
    public static int ProfileMatch(IReadOnlyList<double> oi, RiasecScores scores)
    {
        if (oi.Count < 6)
        {
            return 0;
        }

        double used = 0;
        double sum = 0;
        for (var i = 0; i < 6; i++)
        {
            var weight = oi[i] - 1d;
            if (weight <= 0)
            {
                continue;
            }

            used += weight;
            sum += weight * scores.Get(CareerTestCatalog.RiasecCodes[i]);
        }

        return used <= 0
            ? 0
            : (int)Math.Clamp(Math.Round(sum / used, MidpointRounding.AwayFromZero), 0, 100);
    }

    private static IReadOnlyList<string> PracticalNotes(
        RiasecScores scores,
        IReadOnlyList<string> strengths,
        bool fromDeepAnalysis)
    {
        var top = RiasecRanking.Rank(
                scores.Realistic, scores.Investigative, scores.Artistic,
                scores.Social, scores.Enterprising, scores.Conventional)
            .Select(x => (Code: x.Code, Percent: x.Value))
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
            ? "Zo zet je dit in op Lobsy: open de banenkaart. Vacatures die bij jouw richting horen scoren hoger. Filter op hoge match en bewaar wat voelt als ‘dit is het’. Werkgevers zien geen ruwe antwoorden, alleen dat je past."
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

}

public sealed record CareerOccupation(string Title, params (string Code, int Weight)[] Weights);

public sealed record CareerOccupationMatch(
    string Title,
    int Percent,
    string Band,
    string Why,
    IReadOnlyList<string>? Keys = null,
    string? EscoId = null,
    string Confidence = "",
    IReadOnlyList<string>? OnetCodes = null,
    bool NoScore = false)
{
    public IReadOnlyList<string> SearchKeys =>
        Keys is { Count: > 0 } keys ? keys : CareerOccupationKeys.FromTitle(Title);

    public IReadOnlyList<string> Onet => OnetCodes ?? [];
}

public sealed record CareerCompassSnapshot(
    IReadOnlyList<string> Strengths,
    IReadOnlyList<CareerOccupationMatch> SuperMatches,
    IReadOnlyList<CareerOccupationMatch> StrongChoices,
    IReadOnlyList<CareerOccupationMatch> Broadening,
    IReadOnlyList<string> PracticalNotes,
    bool FromDeepAnalysis,
    bool FromOpenAi = false,
    string ScoresFingerprint = "")
{
    /// <summary>UI key for the incomplete-test prompt. The sentence lives in localization.</summary>
    public const string EmptyNoteKey = "Tests.CareerCompass.Empty";

    public static CareerCompassSnapshot Empty(bool fromDeepAnalysis = false)
        => new([], [], [], [], [], fromDeepAnalysis);

    public bool HasOccupations =>
        SuperMatches.Count > 0 || StrongChoices.Count > 0 || Broadening.Count > 0;

    public IEnumerable<CareerOccupationMatch> AllOccupations =>
        SuperMatches.Concat(StrongChoices).Concat(Broadening);
}
