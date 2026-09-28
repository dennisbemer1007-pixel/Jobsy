using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;

namespace Jobsy.Core.Rules;

/// <summary>
/// Single freemium gate for employer candidate insights.
/// Today: positive token balance on the wallet company (no Pro/subscription flag exists).
/// Viewing insights never spends tokens. Swap the body later if a subscription flag is added.
/// </summary>
public static class CandidateInsightsAccess
{
    public static async Task<bool> IsFullAccessAsync(
        ITokenLedgerService tokens,
        IReadOnlyList<Company> scopeCompanies,
        CancellationToken cancellationToken = default)
    {
        if (scopeCompanies.Count == 0)
        {
            return false;
        }

        var walletIds = new HashSet<Guid>();
        foreach (var company in scopeCompanies)
        {
            walletIds.Add(ResolveWalletCompanyId(company));
        }

        foreach (var walletId in walletIds)
        {
            var balance = await tokens.GetBalanceAsync(walletId, cancellationToken);
            if (balance > 0)
            {
                return true;
            }
        }

        return false;
    }

    public static Guid ResolveWalletCompanyId(Company company)
        => company.TokensManagedByEnterprise && company.ParentCompanyId is Guid parent
            ? parent
            : company.Id;
}
