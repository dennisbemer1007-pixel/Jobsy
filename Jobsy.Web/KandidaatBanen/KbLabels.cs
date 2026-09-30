using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Localization;

namespace Jobsy.Web.KandidaatBanen;

/// <summary>
/// Candidate-facing labels for enums shown on job surfaces.
/// Employer-side wording stays in <see cref="UiLabels"/>.
/// </summary>
public static class KbLabels
{
    public static string Status(CultureState culture, ApplicationStatus status) => status switch
    {
        ApplicationStatus.Pending => culture["Kb.Status.Pending"],
        ApplicationStatus.Accepted => culture["Kb.Status.Accepted"],
        ApplicationStatus.EmployerContacting => culture["Kb.Status.EmployerContacting"],
        ApplicationStatus.Hired => culture["Kb.Status.Hired"],
        ApplicationStatus.Rejected => culture["Kb.Status.Rejected"],
        ApplicationStatus.FilledElsewhere => culture["Kb.Status.FilledElsewhere"],
        ApplicationStatus.Withdrawn => culture["Kb.Status.Withdrawn"],
        _ => status.ToString()
    };

    public static string Status(CultureState culture, string status)
    {
        if (Enum.TryParse<ApplicationStatus>(status, ignoreCase: true, out var parsed))
        {
            return Status(culture, parsed);
        }

        return status;
    }

    /// <summary>CSS modifier for status pills (file 07 migrates Applications.razor here).</summary>
    public static string StatusModifier(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Pending => "pending",
        ApplicationStatus.Accepted => "review",
        ApplicationStatus.EmployerContacting => "contact",
        ApplicationStatus.Hired => "hired",
        _ => "closed"
    };

    public static string StatusModifier(string status)
        => Enum.TryParse<ApplicationStatus>(status, ignoreCase: true, out var parsed)
            ? StatusModifier(parsed)
            : "closed";

    public static string FitBand(CultureState culture, KbFitBand band) => band switch
    {
        KbFitBand.Strong => culture["Kb.Fit.Strong"],
        KbFitBand.Good => culture["Kb.Fit.Good"],
        KbFitBand.Some => culture["Kb.Fit.Some"],
        _ => band.ToString()
    };

    public static string FitBandCss(KbFitBand band) => band switch
    {
        KbFitBand.Strong => "strong",
        KbFitBand.Good => "good",
        KbFitBand.Some => "some",
        _ => "some"
    };

    public static string Transport(CultureState culture, string transport)
        => UiLabels.Transport(culture, transport);

    public static string TransportVerb(CultureState culture, string transport)
        => UiLabels.TransportVerb(culture, transport);

    /// <summary>Timeline step keys (file 07 fills the history kinds).</summary>
    public static string TimelineStep(CultureState culture, string stepKey) => stepKey switch
    {
        "Created" or "Sent" => culture["Kb.Timeline.Sent"],
        "EmployerViewed" => culture["Kb.Timeline.Seen"],
        "Interview" or "Gesprek" => culture["Kb.Timeline.Interview"],
        "Outcome" or "Uitslag" => culture["Kb.Timeline.Outcome"],
        _ => culture["Kb.Timeline.StatusChanged"]
    };

    /// <summary>Saved-job state labels (file 07).</summary>
    public static string SavedState(CultureState culture, string stateKey) => stateKey switch
    {
        "Saved" => culture["Kb.Saved.State"],
        "Applied" => culture["Kb.Saved.Applied"],
        "Closed" => culture["Kb.Saved.Closed"],
        _ => stateKey
    };

    /// <summary>All enum values that must have a non-empty label in every UI language.</summary>
    public static IReadOnlyList<ApplicationStatus> AllApplicationStatuses { get; } =
        Enum.GetValues<ApplicationStatus>();

    public static IReadOnlyList<KbFitBand> AllFitBands { get; } =
        Enum.GetValues<KbFitBand>();

    public static IReadOnlyList<string> AllTransportModes { get; } =
        [TransportLabels.Bike, TransportLabels.Car, TransportLabels.Walking, TransportLabels.PublicTransport];

    public static IReadOnlyList<string> AllTimelineSteps { get; } =
        ["Created", "EmployerViewed", "Interview", "Outcome", "StatusChanged"];

    public static IReadOnlyList<string> AllSavedStates { get; } =
        ["Saved", "Applied", "Closed"];
}
