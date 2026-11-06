using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Entities;

public class KandidatenkaartInviteCampaign
{
    public Guid Id { get; set; }
    public Guid VacancyId { get; set; }
    public Vacancy Vacancy { get; set; } = null!;
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string AreaKey { get; set; } = string.Empty;
    public string AreaLabel { get; set; } = string.Empty;

    public int MinMatchPercent { get; set; } = KandidatenkaartRules.MinMatchPercentForInvite;
    public int MaxTravelKm { get; set; } = 25;

    public KandidatenkaartCampaignStatus Status { get; set; } = KandidatenkaartCampaignStatus.Draft;
    public int CurrentRound { get; set; }
    public DateTime? NextRoundAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<KandidatenkaartInviteDelivery> Deliveries { get; set; } = new List<KandidatenkaartInviteDelivery>();
}
