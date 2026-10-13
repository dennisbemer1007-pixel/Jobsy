namespace Jobsy.Api.Models;

/// <summary>
/// Read-only per-stone done state for the candidate how-to guide (05 §2).
/// Purely derived from data the candidate already produced — the endpoint never writes.
/// </summary>
public sealed record CandidateJourneySummaryDto(
    bool DiscoveryDone,
    bool PassportDone,
    bool CareerDone,
    bool JobMapDone,
    bool ApplicationsDone);
