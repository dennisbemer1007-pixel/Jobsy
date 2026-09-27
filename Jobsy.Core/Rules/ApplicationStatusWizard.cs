namespace Jobsy.Core.Rules;

/// <summary>
/// Candidate "Mijn sollicitaties" stepper: track A (open pipeline) vs track B (rejected).
/// </summary>
public static class ApplicationStatusWizard
{
    public static readonly string[] TrackASteps =
    [
        "Gesolliciteerd",
        "In behandeling",
        "Contact",
        "Gematched"
    ];

    public static readonly string[] TrackBSteps =
    [
        "Gesolliciteerd",
        "Afgewezen"
    ];

    public static bool IsRejectedTrack(string? status)
        => status is "Rejected" or "FilledElsewhere" or "Withdrawn";

    public static string[] StepsFor(string? status)
        => IsRejectedTrack(status) ? TrackBSteps : TrackASteps;

    /// <summary>Zero-based index of the current step (inclusive of completed steps before it).</summary>
    public static int CurrentStepIndex(string? status) => status switch
    {
        "Pending" => 0,
        "Accepted" => 1,
        "EmployerContacting" => 2,
        "Hired" => 3,
        "Rejected" or "FilledElsewhere" or "Withdrawn" => 1,
        _ => 0
    };

    public static string CurrentStepLabel(string? status)
    {
        var steps = StepsFor(status);
        var index = Math.Clamp(CurrentStepIndex(status), 0, steps.Length - 1);
        return steps[index];
    }

    /// <summary>
    /// Candidate applications filter group: open / running / rejected (null = unknown).
    /// </summary>
    public static string? FilterGroup(string? status) => status switch
    {
        "Pending" => "open",
        "Accepted" or "EmployerContacting" or "Hired" => "running",
        "Rejected" or "FilledElsewhere" or "Withdrawn" => "rejected",
        _ => null
    };

    public static bool IsOpen(string? status) => FilterGroup(status) == "open";

    public static bool IsRunning(string? status) => FilterGroup(status) == "running";

    public static bool IsRejectedFilter(string? status) => FilterGroup(status) == "rejected";
}
