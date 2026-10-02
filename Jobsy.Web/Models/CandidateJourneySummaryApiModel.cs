namespace Jobsy.Web.Models;

/// <summary>
/// Read-only done state per Lobsy stone for <c>/candidate/hoe-werkt-lobsy</c> (05 §2).
/// Derived from data the candidate already produced; <c>GET api/me/journey-summary</c> never writes.
/// </summary>
public sealed class CandidateJourneySummaryApiModel
{
    /// <summary>Onboarding wizard finished or its finish screen reached.</summary>
    public bool DiscoveryDone { get; set; }

    /// <summary>At least one proof on the paspoort (employer, education, certificate, reference or own CV).</summary>
    public bool PassportDone { get; set; }

    /// <summary>An active career plan with at least one completed step.</summary>
    public bool CareerDone { get; set; }

    /// <summary>At least one liked or shared vacancy.</summary>
    public bool JobMapDone { get; set; }

    /// <summary>At least one application.</summary>
    public bool ApplicationsDone { get; set; }
}
