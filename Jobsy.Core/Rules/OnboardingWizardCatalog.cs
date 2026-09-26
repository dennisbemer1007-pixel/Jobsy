namespace Jobsy.Core.Rules;

/// <summary>
/// Mini-test question IDs, education chips, dream-job suggestions, and step metadata
/// for the candidate first-login wizard (<c>/candidate/start</c>).
/// </summary>
public static class OnboardingWizardCatalog
{
    public const int StepCount = 10;
    public const int TotalMinutesEstimate = 6;

    /// <summary>Minutes remaining estimate by current step (1-based), inclusive of current step.</summary>
    public static readonly int[] RemainingMinutesByStep =
    [
        0, // unused (1-based)
        6, 5, 4, 4, 3, 3, 2, 2, 1, 1
    ];

    public static int RemainingMinutes(int step)
        => step is >= 1 and <= StepCount ? RemainingMinutesByStep[step] : 1;

    /// <summary>
    /// Competentie mini: one item per Big Five workplace dimension
    /// (Q01 Samenwerken, Q06 Resultaat, Q11 Stress, Q16 Innovatie, Q21 Extraversie).
    /// </summary>
    public static readonly int[] CompetencyQuestionIds = [1, 6, 11, 16, 21];

    /// <summary>
    /// Beroepen mini: one item per RIASEC direction
    /// (Q01 R, Q06 I, Q10 A, Q14 S, Q18 E, Q22 C).
    /// </summary>
    public static readonly int[] CareerQuestionIds = [1, 6, 10, 14, 18, 22];

    /// <summary>
    /// Cultuur &amp; persoonlijkheid mini: five most discriminating culture dimensions
    /// (Autonomy Q01, Informal Q03, Collaboration Q05, Flexibility Q07, PeopleFirst Q11).
    /// Innovation omitted to keep the mini-test to five items.
    /// </summary>
    public static readonly int[] CultureQuestionIds = [1, 3, 5, 7, 11];

    public static readonly string[] CultureDimensionCodes =
    [
        CulturePersonalityCatalog.Autonomy,
        CulturePersonalityCatalog.Informal,
        CulturePersonalityCatalog.Collaboration,
        CulturePersonalityCatalog.Flexibility,
        CulturePersonalityCatalog.PeopleFirst
    ];

    /// <summary>
    /// Waarden mini: one item per Schwartz workplace driver
    /// (Q01 Autonomy, Q06 Connection, Q11 Achievement, Q16 Stability, Q21 Impact).
    /// </summary>
    public static readonly int[] ValuesQuestionIds = [1, 6, 11, 16, 21];

    /// <summary>Education chips shown in the wizard (order = UI order).</summary>
    public static readonly string[] EducationChips =
    [
        EducationLevelLabels.None,
        "Basisschool",
        "VMBO",
        "HAVO",
        "VWO",
        "MBO 1",
        "MBO 2",
        "MBO 3",
        "MBO 4",
        "HBO",
        "WO"
    ];

    /// <summary>Popular dream-job chips before the Beroepentest is answered.</summary>
    public static readonly string[] PopularDreamJobChips =
    [
        "Verkoper",
        "Magazijnmedewerker",
        "Zorghulp",
        "Horecamedewerker",
        "Administratief medewerker",
        "Chauffeur",
        "Productiemedewerker",
        "Klantenservice"
    ];

    public static readonly string[] DreamJobChipsByRiasecR =
    [
        "Monteur", "Magazijnmedewerker", "Chauffeur", "Productiemedewerker"
    ];

    public static readonly string[] DreamJobChipsByRiasecI =
    [
        "Laborant", "IT-support", "Kwaliteitscontroleur", "Analist"
    ];

    public static readonly string[] DreamJobChipsByRiasecA =
    [
        "Vormgever", "Contentmaker", "Winkelstylist", "Fotograaf"
    ];

    public static readonly string[] DreamJobChipsByRiasecS =
    [
        "Zorghulp", "Docent-assistent", "Klantenservice", "Recreatiemedewerker"
    ];

    public static readonly string[] DreamJobChipsByRiasecE =
    [
        "Verkoper", "Teamleider", "Accountmanager", "Ondernemer"
    ];

    public static readonly string[] DreamJobChipsByRiasecC =
    [
        "Administratief medewerker", "Boekhoudkundig medewerker", "Planner", "Receptionist"
    ];

    public static IReadOnlyList<string> DreamChipsForRiasec(IEnumerable<string>? topCodes)
    {
        var chips = new List<string>();
        foreach (var code in topCodes ?? [])
        {
            var set = code switch
            {
                CareerTestCatalog.Realistic => DreamJobChipsByRiasecR,
                CareerTestCatalog.Investigative => DreamJobChipsByRiasecI,
                CareerTestCatalog.Artistic => DreamJobChipsByRiasecA,
                CareerTestCatalog.Social => DreamJobChipsByRiasecS,
                CareerTestCatalog.Enterprising => DreamJobChipsByRiasecE,
                CareerTestCatalog.Conventional => DreamJobChipsByRiasecC,
                _ => []
            };
            foreach (var c in set)
            {
                if (!chips.Contains(c, StringComparer.OrdinalIgnoreCase))
                {
                    chips.Add(c);
                }
            }
        }

        if (chips.Count == 0)
        {
            return PopularDreamJobChips;
        }

        return chips.Take(8).ToList();
    }

    public static string FormatEducationLine(string level, string? direction)
    {
        var lvl = (level ?? "").Trim();
        var dir = (direction ?? "").Trim();
        if (string.IsNullOrWhiteSpace(lvl))
        {
            return "";
        }

        if (string.IsNullOrWhiteSpace(dir)
            || string.Equals(lvl, EducationLevelLabels.None, StringComparison.OrdinalIgnoreCase))
        {
            return lvl;
        }

        return $"{lvl} – {dir}";
    }
}
