using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Sales;

public sealed class SalesBeneficiaryService : ISalesBeneficiaryService
{
    private readonly JobsyDbContext _db;
    private readonly IPlatformFeatureService _features;

    public SalesBeneficiaryService(JobsyDbContext db, IPlatformFeatureService features)
    {
        _db = db;
        _features = features;
    }

    public async Task<SalesBeneficiary> GetOrThrowAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? user.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Geen geldige gebruiker.");
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.SalesManager))
        {
            var profile = await _db.SalesManagerProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
            return new SalesBeneficiary(
                userId,
                SalesBeneficiaryKind.SalesManager,
                profile?.TrackingCode,
                profile?.IsOnboardingComplete == true,
                profile?.CanRecruitSalesManagers == true);
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Ambassadeur))
        {
            var snap = await _features.GetAsync(cancellationToken);
            if (!snap.AmbassadorsEnabled)
            {
                throw new UnauthorizedAccessException("Het ambassadeursprogramma is gepauzeerd.");
            }

            var profile = await _db.AmbassadeurProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
            return new SalesBeneficiary(
                userId,
                SalesBeneficiaryKind.Ambassadeur,
                profile?.TrackingCode,
                profile?.IsOnboardingComplete == true,
                CanRecruit: false);
        }

        throw new UnauthorizedAccessException("Geen sales-begunstigde.");
    }

    public async Task<bool> CanSeeCompanyAsync(
        Guid beneficiaryUserId,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => new
            {
                c.Id,
                c.ParentCompanyId,
                c.ReferredBySalesManagerUserId,
                c.ReferredByAmbassadeurUserId,
                c.CommissionIndirectSalesManagerUserId
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (company is null)
        {
            return false;
        }

        var rootId = company.ParentCompanyId ?? company.Id;
        if (company.Id != rootId)
        {
            var root = await _db.Companies.AsNoTracking()
                .Where(c => c.Id == rootId)
                .Select(c => new
                {
                    c.ReferredBySalesManagerUserId,
                    c.ReferredByAmbassadeurUserId,
                    c.CommissionIndirectSalesManagerUserId
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (root is null)
            {
                return false;
            }

            return root.ReferredBySalesManagerUserId == beneficiaryUserId
                   || root.ReferredByAmbassadeurUserId == beneficiaryUserId
                   || root.CommissionIndirectSalesManagerUserId == beneficiaryUserId;
        }

        return company.ReferredBySalesManagerUserId == beneficiaryUserId
               || company.ReferredByAmbassadeurUserId == beneficiaryUserId
               || company.CommissionIndirectSalesManagerUserId == beneficiaryUserId;
    }

    public Task<bool> CanSeeInvoiceAsync(
        Guid beneficiaryUserId,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
        => _db.SelfBillingInvoices.AsNoTracking()
            .AnyAsync(
                i => i.Id == invoiceId && i.SalesManagerUserId == beneficiaryUserId,
                cancellationToken);

    public Task<bool> CanSeePayoutRequestAsync(
        Guid beneficiaryUserId,
        Guid payoutRequestId,
        CancellationToken cancellationToken = default)
        => _db.SalesPayoutRequests.AsNoTracking()
            .AnyAsync(
                r => r.Id == payoutRequestId && r.BeneficiaryUserId == beneficiaryUserId,
                cancellationToken);
}

public sealed class SalesParkedBalanceService : ISalesParkedBalanceService
{
    private readonly JobsyDbContext _db;

    public SalesParkedBalanceService(JobsyDbContext db) => _db = db;

    public async Task<IReadOnlyList<SalesParkedBalanceItem>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var ambassadeurIds = await _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Ambassadeur)
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(cancellationToken);
        if (ambassadeurIds.Count == 0)
        {
            return [];
        }

        var idSet = ambassadeurIds.Select(a => a.Id).ToHashSet();
        var lines = await _db.CommissionLedgerEntries.AsNoTracking()
            .Where(e => idSet.Contains(e.SalesManagerUserId))
            .GroupBy(e => e.SalesManagerUserId)
            .Select(g => new
            {
                UserId = g.Key,
                Balance = g.Sum(x => x.AmountExVat),
                Last = g.Max(x => (DateTime?)x.CreatedAt)
            })
            .ToListAsync(cancellationToken);

        var result = new List<SalesParkedBalanceItem>();
        foreach (var line in lines.Where(l => l.Balance != 0m))
        {
            var name = ambassadeurIds.First(a => a.Id == line.UserId).FullName ?? "";
            result.Add(new SalesParkedBalanceItem(
                line.UserId,
                MaskDisplayName(name),
                line.Balance,
                line.Last));
        }

        return result;
    }

    private static string MaskDisplayName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "***";
        }

        if (parts.Length == 1)
        {
            return parts[0].Length <= 1 ? "*" : parts[0][0] + "***";
        }

        return $"{parts[0][0]}*** {parts[^1]}";
    }
}
