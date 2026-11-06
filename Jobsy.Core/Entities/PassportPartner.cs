using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class PassportPartner
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public bool IsActive { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public byte[]? LogoPng { get; set; }
    public string? LogoContentType { get; set; }
    public DateTime? LogoUpdatedAtUtc { get; set; }
    public int MaxBranches { get; set; } = 1;
    public string? TermsVersion { get; set; }
    public DateTime? TermsAcceptedAtUtc { get; set; }
    public Guid? TermsAcceptedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public static PassportPartnerType TypeFromCompany(CompanyType companyType)
        => companyType == CompanyType.Intermediary
            ? PassportPartnerType.Uitzendbureau
            : PassportPartnerType.Werkgever;
}

public class PassportPartnerCode
{
    public Guid Id { get; set; }
    public Guid PassportPartnerId { get; set; }
    public PassportPartner? PassportPartner { get; set; }
    public Guid BranchCompanyId { get; set; }
    public Company? BranchCompany { get; set; }
    public string CodeLookupHash { get; set; } = string.Empty;
    public string CodeDisplay { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? DeactivatedAtUtc { get; set; }
}

public class PassportPartnerCandidateLink
{
    public Guid Id { get; set; }
    public Guid CandidateUserId { get; set; }
    public User? Candidate { get; set; }
    public Guid PassportPartnerId { get; set; }
    public PassportPartner? PassportPartner { get; set; }
    public Guid? PartnerCodeId { get; set; }
    public PassportPartnerCode? PartnerCode { get; set; }
    public PassportPartnerLinkSource Source { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? ConsentGivenAtUtc { get; set; }
    public string? ConsentVersion { get; set; }
    public DateTime? ContactConsentAtUtc { get; set; }
    public DateTime? ConsentPromptDismissedAtUtc { get; set; }
    public DateTime? ReconfirmDueAtUtc { get; set; }
    public DateTime? ReconfirmReminderSentAtUtc { get; set; }
    public DateTime? SuspendedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public PassportPartnerRevokeReason? RevokedReason { get; set; }
}

public class PassportAccessLog
{
    public Guid Id { get; set; }
    public Guid CandidateUserId { get; set; }
    public Guid? PassportPartnerId { get; set; }
    public Guid? ViewerUserId { get; set; }
    /// <summary>Filled in step 05. No FK yet.</summary>
    public Guid? ShareLinkId { get; set; }
    public PassportAccessKind Kind { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
