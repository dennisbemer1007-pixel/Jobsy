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
    public const int WizardVersionV3 = 3;

    /// <summary>Counted steps in ontdekkingsreis (v3).</summary>
    public const int V3StepCount = 10;

    /// <summary>Total minutes estimate for the journey (mockup: "10 stappen · ± 12 minuten").</summary>
    public const int V3TotalMinutesEstimate = 12;

    /// <summary>Minutes remaining estimate by current step (1-based), inclusive of current step.</summary>
    public static readonly int[] RemainingMinutesByStep =
    [
        0, // unused (1-based)
        6, 5, 4, 4, 3, 3, 2, 2, 1, 1
    ];

    /// <summary>V3 remaining minutes by counted step (1-based), ≈12 min total.</summary>
    public static readonly int[] V3RemainingMinutesByStep =
    [
        0, // unused
        12, 11, 9, 7, 5, 4, 3, 2, 1, 1
    ];

    public static int RemainingMinutes(int step)
        => step is >= 1 and <= StepCount ? RemainingMinutesByStep[step] : 1;

    public static int V3RemainingMinutes(int step)
        => step is >= 1 and <= V3StepCount ? V3RemainingMinutesByStep[step] : 1;

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
    /// Map an in-progress v2 wizard step to v3 (ontdekkingsreis) numbering.
    /// v2 transport (3) folds into v3 step 2; education (5) and dream (6) fold into v3 4.
    /// </summary>
    public static int MapV2ToV3(int v2Step) => v2Step switch
    {
        1 => 1,
        2 => 2,
        3 => 2,
        4 => 3,
        5 => 4,
        6 => 4,
        7 => 7,
        8 => 8,
        9 => 9,
        10 => 10,
        _ => Math.Clamp(v2Step, 1, V3StepCount)
    };

    /// <summary>
    /// Map an in-progress v3 journey step back to v2 numbering (flag OFF mid-journey).
    /// </summary>
    public static int MapV3ToV2(int v3Step) => v3Step switch
    {
        1 => 1,
        2 => 2,
        3 => 4,
        4 => 5,
        5 => 6,
        6 => 6,
        7 => 7,
        8 => 8,
        9 => 9,
        10 => 10,
        _ => Math.Clamp(v3Step, 1, StepCount)
    };

    /// <summary>Map any prior version step to v3 (v1 goes through <see cref="MapV1Step"/> first).</summary>
    public static int MapToV3(int step, int fromVersion) => fromVersion switch
    {
        >= WizardVersionV3 => Math.Clamp(step, 1, V3StepCount),
        WizardVersionV2 => MapV2ToV3(step),
        _ => MapV2ToV3(MapV1Step(step))
    };

    public const int MiniLevelCount = 5;
    public const int DeeperLevelCount = 10;

    public enum OnboardingTestKind
    {
        Competency,
        Career,
        Culture,
        Values
    }

    /// <summary>
    /// Competentie mini: one item per Big Five workplace dimension
    /// (Q01 Samenwerken, Q06 Resultaat, Q11 Stress, Q16 Innovatie, Q21 Extraversie).
    /// </summary>
    public static readonly int[] CompetencyQuestionIds = [1, 6, 11, 16, 21];

    /// <summary>
    /// Competentie deeper (5→10): next lowest id per Big Five dimension not in the mini set
    /// (Q02, Q07, Q12, Q17, Q22).
    /// </summary>
    public static readonly int[] CompetencyDeeperQuestionIds = [2, 7, 12, 17, 22];

    /// <summary>
    /// Beroepen mini: five of six RIASEC directions (Q01 R, Q06 I, Q14 S, Q18 E, Q22 C).
    /// Artistic is measured in the deeper / full test.
    /// </summary>
    public static readonly int[] CareerQuestionIds = [1, 6, 14, 18, 22];

    /// <summary>
    /// Beroepen deeper (5→10): next RIASEC item per mini dimension, with Artistic (Q10) included
    /// instead of Conventional's next (Q23) so the set stays five and covers Artistic.
    /// Ids: R2, I7, A10, S15, E19.
    /// </summary>
    public static readonly int[] CareerDeeperQuestionIds = [2, 7, 10, 15, 19];

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

    /// <summary>
    /// Cultuur deeper (5→10): next id per mini culture dimension
    /// (Q02 Autonomy, Q04 Informal, Q06 Collaboration, Q08 Flexibility, Q12 PeopleFirst).
    /// Full depth is all 18 catalog items.
    /// </summary>
    public static readonly int[] CultureDeeperQuestionIds = [2, 4, 6, 8, 12];

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

    /// <summary>
    /// Waarden deeper (5→10): next lowest id per Schwartz driver not in the mini set
    /// (Q02, Q07, Q12, Q17, Q22).
    /// </summary>
    public static readonly int[] ValuesDeeperQuestionIds = [2, 7, 12, 17, 22];

    public static int FullLevelCount(OnboardingTestKind kind)
        => TestDepthRules.FullCount(TestDepthRules.ToAssessmentKind(kind));

    public static int[] MiniIds(OnboardingTestKind kind) => kind switch
    {
        OnboardingTestKind.Competency => CompetencyQuestionIds,
        OnboardingTestKind.Career => CareerQuestionIds,
        OnboardingTestKind.Culture => CultureQuestionIds,
        OnboardingTestKind.Values => ValuesQuestionIds,
        _ => []
    };

    public static int[] DeeperIds(OnboardingTestKind kind) => kind switch
    {
        OnboardingTestKind.Competency => CompetencyDeeperQuestionIds,
        OnboardingTestKind.Career => CareerDeeperQuestionIds,
        OnboardingTestKind.Culture => CultureDeeperQuestionIds,
        OnboardingTestKind.Values => ValuesDeeperQuestionIds,
        _ => []
    };

    public static OnboardingTestKind? TestKindForStep(int step) => step switch
    {
        7 => OnboardingTestKind.Competency,
        8 => OnboardingTestKind.Career,
        9 => OnboardingTestKind.Culture,
        10 => OnboardingTestKind.Values,
        _ => null
    };

    /// <summary>
    /// Highest completed depth band from answered count: 0, 5, 10, or full (18/25).
    /// Delegates to <see cref="TestDepthRules"/> (single source for levels/counts).
    /// </summary>
    public static int ReachedLevel(int answeredCount, OnboardingTestKind kind)
    {
        var reached = TestDepthRules.Reached(TestDepthRules.ToAssessmentKind(kind), answeredCount);
        return reached switch
        {
            TestDepthLevel.Full => FullLevelCount(kind),
            TestDepthLevel.Deeper => DeeperLevelCount,
            TestDepthLevel.First => MiniLevelCount,
            _ => 0
        };
    }

    public static bool IsLevelReached(int answeredCount, int level)
        => answeredCount >= level;

    /// <summary>
    /// Ordered question ids up to <paramref name="targetCount"/> (5, 10, or full).
    /// Mini, then deeper, then remaining catalog ids — via <see cref="TestDepthRules"/>.
    /// </summary>
    public static int[] QuestionIdsUpTo(OnboardingTestKind kind, int targetCount)
        => TestDepthRules.QuestionIdsUpTo(TestDepthRules.ToAssessmentKind(kind), targetCount).ToArray();

    public static IReadOnlyList<int> AllCatalogIds(OnboardingTestKind kind) => kind switch
    {
        OnboardingTestKind.Competency => CompetencyTestCatalog.Questions.Select(q => q.Id).ToArray(),
        OnboardingTestKind.Career => CareerTestCatalog.Questions.Select(q => q.Id).ToArray(),
        OnboardingTestKind.Culture => CulturePersonalityCatalog.Questions.Select(q => q.Id).ToArray(),
        OnboardingTestKind.Values => SchwartzValuesCatalog.Questions.Select(q => q.Id).ToArray(),
        _ => []
    };

    public static string TestTitleKey(OnboardingTestKind kind) => kind switch
    {
        OnboardingTestKind.Competency => "Discovery.Test.Competency.Title",
        OnboardingTestKind.Career => "Discovery.Test.Career.Title",
        OnboardingTestKind.Culture => "Discovery.Test.Culture.Title",
        OnboardingTestKind.Values => "Discovery.Test.Values.Title",
        _ => "Discovery.Test.Competency.Title"
    };

    public static string TestNameKey(OnboardingTestKind kind) => kind switch
    {
        OnboardingTestKind.Competency => "Onboarding.Test.Competency",
        OnboardingTestKind.Career => "Onboarding.Test.Career",
        OnboardingTestKind.Culture => "Onboarding.Test.Culture",
        OnboardingTestKind.Values => "Onboarding.Test.Values",
        _ => "Onboarding.Test.Competency"
    };

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
