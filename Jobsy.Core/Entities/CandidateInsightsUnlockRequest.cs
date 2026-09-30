using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Vestigingsmanager request for the bedrijfsmanager to unlock Kandidaatinzichten
/// (when unlock is company-wide, or VM cannot pay from own wallet).
/// At most one <see cref="CandidateInsightsUnlockRequestStatus.Open"/> per vestiging.
/// </summary>
public class CandidateInsightsUnlockRequest
{
    public Guid Id { get; set; }

    public Guid WalletCompanyId { get; set; }
    public Company WalletCompany { get; set; } = null!;

    public Guid BranchCompanyId { get; set; }
    public Company BranchCompany { get; set; } = null!;

    public Guid RequestedByUserId { get; set; }
    public User RequestedByUser { get; set; } = null!;

    public CandidateInsightsUnlockRequestStatus Status { get; set; } =
        CandidateInsightsUnlockRequestStatus.Open;

    public Guid? HandledByUserId { get; set; }
    public User? HandledByUser { get; set; }

    public DateTime? HandledAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
