namespace Jobsy.Core.Entities;

/// <summary>Per-candidate UWV toolkit results (ondernemerstest, drijfveren).</summary>
public sealed class CandidateUwvToolkitState
{
    public Guid UserId { get; set; }

    public string? OndernemerAnswersJson { get; set; }

    public string? OndernemerOutcomeKey { get; set; }

    public DateTime? OndernemerCompletedAtUtc { get; set; }

    /// <summary>JSON array of driver keys (see <see cref="Uwv.UwvDriverKeys"/>).</summary>
    public string? DrijfverenDriverKeysJson { get; set; }

    public DateTime? DrijfverenCompletedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
