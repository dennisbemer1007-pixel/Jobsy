using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class CandidateKandidatenkaartSettings
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public KandidatenkaartInvitePreference InvitePreference { get; set; } = KandidatenkaartInvitePreference.Normal;

    /// <summary>When set, show the re-availability popup on next login.</summary>
    public DateTime? ReAvailabilityPromptAtUtc { get; set; }

    public DateTime? InactivityCheckSentAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
