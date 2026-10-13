namespace Jobsy.Core.Enums;

/// <summary>Derived from <see cref="CompanyType"/>. Not stored on the partner row.</summary>
public enum PassportPartnerType
{
    Werkgever = 0,
    Uitzendbureau = 1
}

public enum PassportPartnerLinkSource
{
    Code = 0,
    AddedCode = 1
}

public enum PassportPartnerRevokeReason
{
    Candidate = 0,
    PartnerEnded = 1,
    AccountDeleted = 2,
    Admin = 3
}

public enum PassportAccessKind
{
    PartnerPortalView = 0,
    PartnerPdfDownload = 1,
    ShareLinkView = 2,
    VerificationView = 3
}
