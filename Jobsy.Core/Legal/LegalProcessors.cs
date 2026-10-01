namespace Jobsy.Core.Legal;

public enum ProcessorStatus
{
    /// <summary>Live today.</summary>
    Active,

    /// <summary>Contracted or planned, not processing personal data yet.</summary>
    Planned
}

/// <summary>
/// One processor / third party row of the privacy statement (03.3 fills the list).
/// <paramref name="TransferBasisKey"/> is only set for transfers outside the EEA (DPF / SCC).
/// </summary>
public sealed record LegalProcessor(
    string Id,
    string Name,
    string Region,
    string PurposeKey,
    string DataKey,
    ProcessorStatus Status,
    string? TransferBasisKey);

/// <summary>
/// The processor catalog behind the privacy statement table. Rows live in code, never in copy,
/// so the page and reality cannot drift apart. 03 fills this list.
/// </summary>
public static class LegalProcessors
{
    public static readonly IReadOnlyList<LegalProcessor> All = [];

    public static IReadOnlyList<LegalProcessor> Active
        => All.Where(p => p.Status == ProcessorStatus.Active).ToList();

    public static IReadOnlyList<LegalProcessor> Planned
        => All.Where(p => p.Status == ProcessorStatus.Planned).ToList();
}
