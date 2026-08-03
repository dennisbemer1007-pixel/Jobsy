using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class AmbassadeurDashboardService : IAmbassadeurDashboardService
{
    private readonly JobsyDbContext _db;
    private readonly ICommissionLedgerService _ledger;
    private readonly IAmbassadeurSettingsService _settings;

    public AmbassadeurDashboardService(
        JobsyDbContext db,
        ICommissionLedgerService ledger,
        IAmbassadeurSettingsService settings)
    {
        _db = db;
        _ledger = ledger;
        _settings = settings;
    }

    public async Task<AmbassadeurDashboardDto?> GetDashboardAsync(
        Guid ambassadeurUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == ambassadeurUserId && u.Role == UserRole.Ambassadeur, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var profile = await _db.AmbassadeurProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == ambassadeurUserId, cancellationToken);
        var settings = await _settings.GetAsync(cancellationToken);

        var candidateIds = await _db.Users.AsNoTracking()
            .Where(u => u.ReferredByAmbassadeurUserId == ambassadeurUserId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        var registeredCandidates = candidateIds.Count;

        var applicationCount = registeredCandidates == 0
            ? 0
            : await _db.Applications.AsNoTracking()
                .CountAsync(
                    a => a.CandidateUserId != null && candidateIds.Contains(a.CandidateUserId.Value),
                    cancellationToken);

        var current = AmbassadeurCommissionRules.ResolveCurrentPercentage(
            registeredCandidates,
            profile?.BaseCommissionPercentage ?? AmbassadeurCommissionRules.DefaultBaseCommissionPercentage,
            settings.CandidateThreshold,
            settings.PercentPerThreshold,
            settings.MaxCommissionPercentage,
            profile?.CommissionPercentageOverride);

        var threshold = Math.Max(1, settings.CandidateThreshold);
        var nextTierAt = ((registeredCandidates / threshold) + 1) * threshold;
        var untilNext = Math.Max(0, nextTierAt - registeredCandidates);
        if (current >= settings.MaxCommissionPercentage
            && profile?.CommissionPercentageOverride is null)
        {
            untilNext = 0;
        }

        var balance = await _ledger.GetBalanceExVatAsync(ambassadeurUserId, cancellationToken);
        var uninvoiced = await _ledger.GetUninvoicedBalanceExVatAsync(ambassadeurUserId, cancellationToken);

        var outstandingIssued = await _db.SelfBillingInvoices.AsNoTracking()
            .Where(i => i.SalesManagerUserId == ambassadeurUserId
                        && i.Status == SelfBillingInvoiceStatus.Issued)
            .SumAsync(i => (decimal?)i.SubtotalExVat, cancellationToken) ?? 0m;

        var recentCandidates = await _db.Users.AsNoTracking()
            .Where(u => u.ReferredByAmbassadeurUserId == ambassadeurUserId)
            .OrderByDescending(u => u.Id)
            .Take(12)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                ApplicationCount = _db.Applications.Count(a => a.CandidateUserId == u.Id)
            })
            .ToListAsync(cancellationToken);

        var suppliers = await _db.Companies.AsNoTracking()
            .Where(c => c.ReferredByAmbassadeurUserId == ambassadeurUserId)
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.KvkNumber,
                c.FirstYearSupplierSlot,
                c.FirstYearStartedAt,
                HasPaid = _db.SupplierOnboardingCheckouts.Any(o =>
                    o.CompanyId == c.Id && o.Status == SupplierOnboardingCheckoutStatus.Credited)
            })
            .ToListAsync(cancellationToken);

        var ledger = await _ledger.ListEntriesAsync(ambassadeurUserId, cancellationToken);
        var invoices = await _db.SelfBillingInvoices.AsNoTracking()
            .Where(i => i.SalesManagerUserId == ambassadeurUserId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        return new AmbassadeurDashboardDto(
            user.Id,
            user.Email,
            user.FullName,
            profile?.TrackingCode,
            profile?.IsOnboardingComplete ?? false,
            registeredCandidates,
            applicationCount,
            profile?.BaseCommissionPercentage ?? AmbassadeurCommissionRules.DefaultBaseCommissionPercentage,
            current,
            settings.MaxCommissionPercentage,
            profile?.CommissionPercentageOverride,
            settings.CandidateThreshold,
            settings.PercentPerThreshold,
            untilNext,
            balance,
            SalesCommissionRules.InclVat(balance),
            uninvoiced,
            outstandingIssued,
            recentCandidates.Select(c => new ReferredCandidateDto(
                c.Id, c.FullName, null, c.ApplicationCount)).ToList(),
            suppliers.Select(s => new ReferredSupplierDto(
                s.Id, s.Name, s.KvkNumber, s.FirstYearSupplierSlot, s.FirstYearStartedAt, s.HasPaid)).ToList(),
            ledger.Take(50).Select(e => new CommissionEntryDto(
                e.Id,
                e.Kind.ToString(),
                e.AmountExVat,
                e.VatAmount,
                e.Note,
                e.CompanyId,
                e.Company?.Name,
                e.CreatedAt,
                e.SelfBillingInvoiceId)).ToList(),
            invoices.Select(i => new SelfBillingInvoiceDto(
                i.Id,
                i.InvoiceNumber,
                i.SubtotalExVat,
                i.VatAmount,
                i.TotalInclVat,
                i.Status.ToString(),
                i.CreatedAt,
                i.IssuedAt,
                i.PaidAt)).ToList());
    }

    public async Task<IReadOnlyList<AmbassadeurListItemDto>> ListAmbassadeursAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Ambassadeur)
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, u.Email, u.FullName })
            .ToListAsync(cancellationToken);

        var ids = users.Select(u => u.Id).ToList();
        var profiles = await _db.AmbassadeurProfiles.AsNoTracking()
            .Where(p => ids.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, cancellationToken);
        var settings = await _settings.GetAsync(cancellationToken);

        var candidateCounts = await _db.Users.AsNoTracking()
            .Where(u => u.ReferredByAmbassadeurUserId != null && ids.Contains(u.ReferredByAmbassadeurUserId.Value))
            .GroupBy(u => u.ReferredByAmbassadeurUserId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var balances = await _db.CommissionLedgerEntries.AsNoTracking()
            .Where(e => ids.Contains(e.SalesManagerUserId))
            .GroupBy(e => e.SalesManagerUserId)
            .Select(g => new { g.Key, Sum = g.Sum(x => x.AmountExVat) })
            .ToDictionaryAsync(x => x.Key, x => x.Sum, cancellationToken);

        return users.Select(user =>
        {
            profiles.TryGetValue(user.Id, out var profile);
            var count = candidateCounts.GetValueOrDefault(user.Id);
            var current = AmbassadeurCommissionRules.ResolveCurrentPercentage(
                count,
                profile?.BaseCommissionPercentage ?? AmbassadeurCommissionRules.DefaultBaseCommissionPercentage,
                settings.CandidateThreshold,
                settings.PercentPerThreshold,
                settings.MaxCommissionPercentage,
                profile?.CommissionPercentageOverride);
            return new AmbassadeurListItemDto(
                user.Id,
                user.Email,
                user.FullName,
                profile?.TrackingCode,
                profile?.IsOnboardingComplete ?? false,
                count,
                current,
                balances.GetValueOrDefault(user.Id));
        }).ToList();
    }
}
