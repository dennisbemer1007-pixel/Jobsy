using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class KandidatenkaartInviteDelivery
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public KandidatenkaartInviteCampaign Campaign { get; set; } = null!;
    public Guid CandidateUserId { get; set; }
    public User CandidateUser { get; set; } = null!;

    public int MatchPercent { get; set; }
    public KandidatenkaartInviteDeliveryStatus Status { get; set; } = KandidatenkaartInviteDeliveryStatus.Queued;
    public int RoundNumber { get; set; }

    public DateTime? SentAtUtc { get; set; }
    public DateTime? ReminderSentAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
