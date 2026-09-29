using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Vestigingsmanager request for tokens from the organisatiepot (bedrijfsmanager approves).
/// </summary>
public class TokenRequest
{
    public const int MaxOpenPerBranch = 3;
    public const int MinAmount = 1;
    public const int MaxAmount = 500;
    public const int MaxNoteLength = 280;

    public Guid Id { get; set; }

    /// <summary>Wallet owner (organisatiepot), resolved like CandidateInsightsAccess.ResolveWalletCompanyId.</summary>
    public Guid OrganisationCompanyId { get; set; }
    public Company OrganisationCompany { get; set; } = null!;

    public Guid BranchCompanyId { get; set; }
    public Company BranchCompany { get; set; } = null!;

    public Guid RequestedByUserId { get; set; }
    public User RequestedByUser { get; set; } = null!;

    public int Amount { get; set; }

    public TokenRequestReason Reason { get; set; }

    public string? Note { get; set; }

    public TokenRequestStatus Status { get; set; } = TokenRequestStatus.Open;

    public Guid? HandledByUserId { get; set; }
    public User? HandledByUser { get; set; }

    public DateTime? HandledAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
