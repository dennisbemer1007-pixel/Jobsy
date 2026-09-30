using Jobsy.Web.Components.Candidate.Journey;
using Jobsy.Web.Models;

namespace Jobsy.Web.Components.Candidate.Career;

/// <summary>
/// Everything <c>/carriere</c> renders, derived from the plan API (01 §9). Pure data:
/// no localization, no feature lookups, no markup. Localization keys are returned as keys.
/// </summary>
public sealed record CareerPlanViewModel
{
    public static CareerPlanViewModel Empty { get; } = new();

    public bool HasPlan { get; init; }

    public bool GoalReached { get; init; }

    public string DreamTitle { get; init; } = "";

    /// <summary>MBO/HBO level from the catalog; empty for free text.</summary>
    public string DreamLevel { get; init; } = "";

    /// <summary><c>CareerFit.*</c> key for the dream fit band; null when Unknown.</summary>
    public string? DreamBandLabelKey { get; init; }

    public bool FromAi { get; init; }

    public string PlanLanguage { get; init; } = "nl";

    /// <summary>True when the plan text language differs from the UI language (D13).</summary>
    public bool LanguageDiffers { get; init; }

    public int CarriedOverCount { get; init; }

    public IReadOnlyList<CareerPlanStepView> Steps { get; init; } = [];

    public CareerPlanStepView? CurrentStep { get; init; }

    public int TotalSteps => Steps.Count;

    public int CompletedSteps { get; init; }

    /// <summary>1-based number of the step that is growing now; 0 when the goal is reached.</summary>
    public int CurrentStepNumber { get; init; }

    /// <summary>Stones for the climb scene, rail and stepper: Nu · steps · dream.</summary>
    public IReadOnlyList<CareerStoneModel> Stones { get; init; } = [];

    public int CurrentStoneIndex { get; init; }

    /// <summary>Career plate count (D7): <c>min(10, 4 + 2 × completed)</c>.</summary>
    public int PlatesShed { get; init; } = 4;

    public int LobsterSizeDesktop { get; init; } = 118;

    public int LobsterSizeMobile { get; init; } = 62;

    /// <summary>Up to 3 things from the paspoort this plan already counts.</summary>
    public IReadOnlyList<string> AlreadyHave { get; init; } = [];

    /// <summary>Course/diploma proofs that stay in the paspoort after a dream change.</summary>
    public int ProofCourseCount { get; init; }

    public bool ProofHasExperience { get; init; }
}

public sealed record CareerPlanStepView(
    string Id,
    int Order,
    string Title,
    string ShortTitle,
    CareerStepStatus Status,
    int GapCount,
    string? BandLabelKey,
    int CourseCount,
    string Summary);

/// <summary>One gap or requirement line on the step detail (03 §2).</summary>
public sealed record CareerStepGapLine(string Text, bool Met);

/// <summary>
/// One step, fully explained (03 §1–§4). Pure data; the card localizes and gates.
/// </summary>
public sealed record CareerStepDetailView
{
    public string Id { get; init; } = "";

    public int Order { get; init; }

    public int TotalSteps { get; init; }

    /// <summary>Step title without the level in brackets; <see cref="Level"/> carries that.</summary>
    public string Title { get; init; } = "";

    public string ShortTitle { get; init; } = "";

    /// <summary>MBO/HBO level the catalog gave separately; empty when the title had none.</summary>
    public string Level { get; init; } = "";

    /// <summary>The plan summary, cut at a sentence end after at most two sentences.</summary>
    public string Lead { get; init; } = "";

    public CareerStepStatus Status { get; init; } = CareerStepStatus.Open;

    /// <summary>1-based number of the step that is growing now; 0 when the goal is reached.</summary>
    public int CurrentStepNumber { get; init; }

    /// <summary>Only the last completed step may be undone (D9).</summary>
    public bool CanUndo { get; init; }

    public IReadOnlyList<CareerStepGapLine> Missing { get; init; } = [];

    public IReadOnlyList<CareerStepGapLine> Present { get; init; } = [];

    /// <summary>Years of experience the step asks for; 0 means the line is never rendered (B8).</summary>
    public int Years { get; init; }

    /// <summary><c>CareerFit.*</c> key; null for Unknown (no pill, D4).</summary>
    public string? BandLabelKey { get; init; }

    /// <summary>Good / Fair / NotYet / Unknown, for the band sentence.</summary>
    public string Band { get; init; } = "Unknown";

    /// <summary>The first missing claw, used in the Fair sentence.</summary>
    public string FirstGap { get; init; } = "";

    /// <summary>AI course names; never links (D5).</summary>
    public IReadOnlyList<string> CourseNames { get; init; } = [];

    /// <summary>Course/gap/title keys for the course match input (D5).</summary>
    public IReadOnlyList<string> CourseSearchKeys { get; init; } = [];

    /// <summary>Name prefilled in the Bewijzen certificate form (Dependency F).</summary>
    public string ProofName { get; init; } = "";

    public string VacanciesHref { get; init; } = "/";

    public CareerPlanStepView? NextStep { get; init; }
}

/// <summary>What the growth moment may claim after a completion (03 §5). Only true items.</summary>
public sealed record CareerStepDoneView
{
    public string ProofName { get; init; } = "";

    public IReadOnlyList<string> GrownClaws { get; init; } = [];
}

/// <summary>One stone; <see cref="StateTextKey"/> and <see cref="Number"/> are rendered by the view.</summary>
public sealed record CareerStoneModel(
    string Label,
    ClimbStoneState State,
    string? StateTextKey,
    int? Number);
