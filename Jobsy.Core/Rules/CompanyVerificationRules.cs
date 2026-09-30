using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Gates for unverified companies (D4). Separate from <see cref="KvkVerificationRules"/>
/// (KvK record existence). Always evaluate the <em>root organisation</em> status.
/// </summary>
public static class CompanyVerificationRules
{
    public const string UnverifiedErrorCode = "company_unverified";

    /// <summary>Dutch API message (UI uses <c>WaBanner.Blocked.*</c>).</summary>
    public const string BlockedMessageNl =
        "Je bedrijf is nog niet geverifieerd. Publiceren, tokens kopen en kandidatengegevens volgen na verificatie.";

    public static bool IsVerified(CompanyVerificationStatus status)
        => status == CompanyVerificationStatus.Verified;

    public static bool CanPublish(CompanyVerificationStatus rootStatus)
        => IsVerified(rootStatus);

    public static bool CanBuyTokens(CompanyVerificationStatus rootStatus)
        => IsVerified(rootStatus);

    public static bool CanSeeCandidates(CompanyVerificationStatus rootStatus)
        => IsVerified(rootStatus);

    public static bool CanUseWelcomeToken(CompanyVerificationStatus rootStatus)
        => IsVerified(rootStatus);

    public static bool CanUseFreePublishPromo(CompanyVerificationStatus rootStatus)
        => IsVerified(rootStatus);

    public static bool CanPublish(Company? company)
        => company is not null && CanPublish(company.VerificationStatus);

    public static bool CanBuyTokens(Company? company)
        => company is not null && CanBuyTokens(company.VerificationStatus);

    public static bool CanSeeCandidates(Company? company)
        => company is not null && CanSeeCandidates(company.VerificationStatus);

    public static bool CanUseWelcomeToken(Company? company)
        => company is not null && CanUseWelcomeToken(company.VerificationStatus);

    public static bool CanUseFreePublishPromo(Company? company)
        => company is not null && CanUseFreePublishPromo(company.VerificationStatus);

    /// <summary>Walk ParentCompanyId to the root (organisation) id.</summary>
    public static Guid ResolveRootCompanyId(Company company)
    {
        var current = company;
        // Safety: avoid cycles in corrupted data.
        for (var i = 0; i < 32 && current.ParentCompanyId is Guid parentId; i++)
        {
            if (current.ParentCompany is { } parent && parent.Id == parentId)
            {
                current = parent;
                continue;
            }

            return parentId;
        }

        return current.Id;
    }
}
