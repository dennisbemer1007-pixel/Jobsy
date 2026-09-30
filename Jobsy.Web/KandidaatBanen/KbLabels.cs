using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;
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

    /// <summary>CSS modifier for status pills.</summary>
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

    public static string TimelineStep(CultureState culture, string stepKey) => stepKey switch
    {
        "Created" or "Sent" => culture["Kb.Timeline.Sent"],
        "EmployerViewed" or "Seen" => culture["Kb.Timeline.Seen"],
        "Interview" or "Gesprek" => culture["Kb.Timeline.Interview"],
        "Outcome" or "Uitslag" => culture["Kb.Timeline.Outcome"],
        _ => culture["Kb.Timeline.StatusChanged"]
    };

    public static string TimelineStep(CultureState culture, ApplicationTimelineStepKey key) => key switch
    {
        ApplicationTimelineStepKey.Sent => culture["Kb.Timeline.Sent"],
        ApplicationTimelineStepKey.Seen => culture["Kb.Timeline.Seen"],
        ApplicationTimelineStepKey.Interview => culture["Kb.Timeline.Interview"],
        ApplicationTimelineStepKey.Outcome => culture["Kb.Timeline.Outcome"],
        _ => culture["Kb.Timeline.StatusChanged"]
    };

    public static string SavedState(CultureState culture, string stateKey) => stateKey switch
    {
        "Open" or "Saved" => culture["Kb.Saved.Open"],
        "ClosingSoon" => culture["Kb.Saved.ClosingSoon"],
        "Fulfilled" => culture["Kb.Saved.Fulfilled"],
        "Applied" => culture["Kb.Saved.Applied"],
        "Closed" or "Hidden" => culture["Kb.Saved.Closed"],
        _ => stateKey
    };

    public static string SavedStateLabel(CultureState culture, string? kind, int? daysUntilEnd)
    {
        if (string.Equals(kind, nameof(KbSavedJobStateKind.ClosingSoon), StringComparison.OrdinalIgnoreCase)
            && daysUntilEnd is int n)
        {
            return string.Format(culture["Kb.Saved.ClosingSoon"], n);
        }

        return kind switch
        {
            nameof(KbSavedJobStateKind.Open) => culture["Kb.Saved.Open"],
            nameof(KbSavedJobStateKind.Fulfilled) => culture["Kb.Saved.Fulfilled"],
            nameof(KbSavedJobStateKind.Closed) => culture["Kb.Saved.Closed"],
            nameof(KbSavedJobStateKind.Hidden) => culture["Kb.Saved.Closed"],
            _ => SavedState(culture, kind ?? "Closed")
        };
    }

    public static string SavedStateCss(string? kind) => kind switch
    {
        nameof(KbSavedJobStateKind.Open) => "open",
        nameof(KbSavedJobStateKind.ClosingSoon) => "soon",
        nameof(KbSavedJobStateKind.Fulfilled) => "fulfilled",
        _ => "closed"
    };

    public static IReadOnlyList<ApplicationStatus> AllApplicationStatuses { get; } =
        Enum.GetValues<ApplicationStatus>();

    public static IReadOnlyList<KbFitBand> AllFitBands { get; } =
        Enum.GetValues<KbFitBand>();

    public static IReadOnlyList<string> AllTransportModes { get; } =
        [TransportLabels.Bike, TransportLabels.Car, TransportLabels.Walking, TransportLabels.PublicTransport];

    public static IReadOnlyList<string> AllTimelineSteps { get; } =
        ["Created", "Sent", "EmployerViewed", "Seen", "Interview", "Outcome", "StatusChanged"];

    public static IReadOnlyList<string> AllSavedStates { get; } =
        ["Saved", "Applied", "Closed", "Open", "ClosingSoon", "Fulfilled", "Hidden"];

    public static IReadOnlyList<ApplicationStatusEventKind> AllStatusEventKinds { get; } =
        Enum.GetValues<ApplicationStatusEventKind>();
}
