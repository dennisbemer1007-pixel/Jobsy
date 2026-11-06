using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Freemium gate for employer candidate insights.
/// Full access = an active paid unlock (tokens). Viewing never spends tokens; unlocking does.
/// </summary>
public static class CandidateInsightsAccess
{
    public const int RenewWindowDays = 14;
    public const int DefaultUnlockDays = 90;
    public const int MinUnlockDays = 7;
    public const int MaxUnlockDays = 365;
    public const decimal DefaultUnlockCostTokens = 12m;

    public static Guid ResolveWalletCompanyId(Company company)
        => company.TokensManagedByEnterprise && company.ParentCompanyId is Guid parent
            ? parent
            : company.Id;

    /// <summary>
    /// Resolves coverage for the selected scope companies against active unlock rows.
    /// Company-scope unlocks always cover every vestiging of that wallet.
    /// Branch unlocks stay valid when admin switches between company-wide and per-vestiging.
    /// </summary>
    public static InsightsCoverage GetCoverage(
        IReadOnlyList<Company> scopeCompanies,
        IReadOnlyList<CandidateInsightsUnlock> activeUnlocks,
        DateTime utcNow)
    {
        if (scopeCompanies.Count == 0)
        {
            return new InsightsCoverage(false, 0, 0, null, false);
        }

        var covering = new List<CandidateInsightsUnlock>();
        var coveredCount = 0;
        foreach (var company in scopeCompanies)
        {
            var walletId = ResolveWalletCompanyId(company);
            var companyWide = activeUnlocks.FirstOrDefault(u =>
                u.WalletCompanyId == walletId
                && u.ScopeKind == CandidateInsightsUnlockScopeKind.Company
                && u.ExpiresAtUtc > utcNow);
            var branch = activeUnlocks.FirstOrDefault(u =>
                u.WalletCompanyId == walletId
                && u.ScopeKind == CandidateInsightsUnlockScopeKind.Branch
                && u.ScopeCompanyId == company.Id
                && u.ExpiresAtUtc > utcNow);

            var match = companyWide ?? branch;
            if (match is not null)
            {
                coveredCount++;
                covering.Add(match);
            }
        }

        var isFull = coveredCount == scopeCompanies.Count && coveredCount > 0;
        DateTime? expires = covering.Count == 0
            ? null
            : covering.Min(u => u.ExpiresAtUtc);
        var canRenew = expires is DateTime exp
                       && exp > utcNow
                       && exp <= utcNow.AddDays(RenewWindowDays);

        return new InsightsCoverage(isFull, coveredCount, scopeCompanies.Count, expires, canRenew);
    }

    public static int ClampUnlockDays(int days)
        => Math.Clamp(days, MinUnlockDays, MaxUnlockDays);
}

/// <param name="IsFull">True when every company in the current scope is covered.</param>
/// <param name="CoveredCount">How many selected vestigingen have an active unlock.</param>
/// <param name="TotalCount">Selected vestigingen count.</param>
/// <param name="ExpiresAtUtc">Earliest expiry among covering unlocks.</param>
/// <param name="CanRenew">True within 14 days before expiry.</param>
public sealed record InsightsCoverage(
    bool IsFull,
    int CoveredCount,
    int TotalCount,
    DateTime? ExpiresAtUtc,
    bool CanRenew);
