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

/// <summary>One stone; <see cref="StateTextKey"/> and <see cref="Number"/> are rendered by the view.</summary>
public sealed record CareerStoneModel(
    string Label,
    ClimbStoneState State,
    string? StateTextKey,
    int? Number);
