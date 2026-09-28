namespace Jobsy.Core.Rules;

/// <summary>
/// Mini-test question IDs, education chips, phases, and step metadata
/// for the candidate first-login wizard v2 (<c>/candidate/start</c>).
/// </summary>
public static class OnboardingWizardCatalog
{
    public const int StepCount = 10;
    public const int TotalMinutesEstimate = 6;
    public const int WizardVersionV2 = 2;

    /// <summary>Minutes remaining estimate by current step (1-based), inclusive of current step.</summary>
    public static readonly int[] RemainingMinutesByStep =
    [
        0, // unused (1-based)
        6, 5, 4, 4, 3, 3, 2, 2, 1, 1
    ];

    public static int RemainingMinutes(int step)
        => step is >= 1 and <= StepCount ? RemainingMinutesByStep[step] : 1;

    public sealed record PhaseInfo(string Code, string NameKey, int FirstStep, int LastStep)
    {
        public int StepCount => LastStep - FirstStep + 1;

        public bool Contains(int step) => step >= FirstStep && step <= LastStep;

        public int IndexInPhase(int step) => Contains(step) ? step - FirstStep : -1;
    }

    /// <summary>Progress-bar phases covering steps 1–10 without gaps.</summary>
    public static readonly PhaseInfo[] Phases =
    [
        new("over-jou", "Onboarding.Phase.OverJou", 1, 3),
        new("achtergrond", "Onboarding.Phase.Achtergrond", 4, 5),
        new("droom", "Onboarding.Phase.Droom", 6, 6),
        new("wie-ben-jij", "Onboarding.Phase.WieBenJij", 7, 10)
    ];

    public static PhaseInfo PhaseForStep(int step)
        => Phases.FirstOrDefault(p => p.Contains(step)) ?? Phases[0];

    /// <summary>
    /// Fill fraction (0–1) for a phase segment given the current counted step.
    /// Completed = 1, future = 0, current = (index in phase + 1) / steps in phase.
    /// </summary>
    public static double PhaseFill(PhaseInfo phase, int currentStep)
    {
        if (currentStep > phase.LastStep)
        {
            return 1;
        }

        if (currentStep < phase.FirstStep)
        {
            return 0;
        }

        var index = phase.IndexInPhase(currentStep);
        return (index + 1) / (double)phase.StepCount;
    }

    /// <summary>
    /// Map an in-progress v1 wizard step to v2 numbering.
    /// Old step 10 (result) maps to 10; callers should also open the finish screen
    /// via <see cref="MapV1ShowsFinish"/>.
    /// </summary>
    public static int MapV1Step(int v1Step) => v1Step switch
    {
        1 => 1,
        2 => 2,
        3 => 4,
        4 => 5,
        5 => 6,
        6 => 7,
        7 => 8,
        8 => 9,
        9 => 10,
        10 => 10,
        _ => Math.Clamp(v1Step, 1, StepCount)
    };

    /// <summary>True when a v1 in-progress row was on the old result step.</summary>
    public static bool MapV1ShowsFinish(int v1Step) => v1Step == 10;

    /// <summary>
    /// Competentie mini: one item per Big Five workplace dimension
    /// (Q01 Samenwerken, Q06 Resultaat, Q11 Stress, Q16 Innovatie, Q21 Extraversie).
    /// </summary>
    public static readonly int[] CompetencyQuestionIds = [1, 6, 11, 16, 21];

    /// <summary>
    /// Beroepen mini: five of six RIASEC directions (Q01 R, Q06 I, Q14 S, Q18 E, Q22 C).
    /// Artistic is measured in the full test.
    /// </summary>
    public static readonly int[] CareerQuestionIds = [1, 6, 14, 18, 22];

    public static int TotalMiniQuestionCount =>
        CompetencyQuestionIds.Length
        + CareerQuestionIds.Length
        + CultureQuestionIds.Length
        + ValuesQuestionIds.Length;

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

    /// <summary>Radio rows for education (MBO expands to niveau 1–4).</summary>
    public static readonly string[] EducationRadioLevels =
    [
        EducationLevelLabels.None,
        "Basisschool",
        "VMBO",
        "HAVO",
        "VWO",
        "MBO",
        "HBO",
        "WO"
    ];

    public static readonly string[] EducationDirectionHavoVwo =
    [
        "Natuur & Techniek",
        "Natuur & Gezondheid",
        "Economie & Maatschappij",
        "Cultuur & Maatschappij"
    ];

    public static readonly string[] EducationDirectionVmboMbo =
    [
        "Zorg & welzijn",
        "Techniek",
        "Economie & handel",
        "ICT",
        "Horeca & bakkerij",
        "Groen"
    ];

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
