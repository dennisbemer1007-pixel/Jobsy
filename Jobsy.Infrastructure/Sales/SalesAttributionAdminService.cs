using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Sales;

public sealed class SalesAttributionAdminService : ISalesAttributionAdminService
{
    private readonly JobsyDbContext _db;

    public SalesAttributionAdminService(JobsyDbContext db) => _db = db;

    public async Task<SalesAttributionChange> ReassignAsync(
        Guid companyId,
        Guid? toBeneficiaryUserId,
        string reason,
        Guid changedByUserId,
        CancellationToken cancellationToken = default)
    {
        var trimmed = (reason ?? string.Empty).Trim();
        if (trimmed.Length is < 5 or > 500)
        {
            throw new ArgumentException("Reden moet tussen 5 en 500 tekens zijn.");
        }

        if (toBeneficiaryUserId is Guid toId)
        {
            var active = await _db.SalesManagerProfiles.AsNoTracking()
                .AnyAsync(
                    p => p.UserId == toId
                         && p.OnboardingCompletedAt != null
                         && p.AgreementSignedAt != null
                         && p.TrackingCode != null,
                    cancellationToken);
            if (!active)
            {
                throw new ArgumentException("Doel-salesmanager is onbekend of niet actief.");
            }
        }

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Bedrijf niet gevonden.");

        var rootId = SalesCommercialUnit.RootIdOf(company);
        var root = rootId == company.Id
            ? company
            : await _db.Companies.FirstOrDefaultAsync(c => c.Id == rootId, cancellationToken)
              ?? throw new KeyNotFoundException("Organisatie niet gevonden.");

        var units = await _db.Companies
            .Where(c => c.Id == rootId || c.ParentCompanyId == rootId)
            .ToListAsync(cancellationToken);

        var fromUserId = root.ReferredBySalesManagerUserId;
        Guid? newIndirect = null;
        if (toBeneficiaryUserId is Guid newSm)
        {
            newIndirect = await _db.SalesManagerProfiles.AsNoTracking()
                .Where(p => p.UserId == newSm)
                .Select(p => p.ReferredBySalesManagerUserId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var now = DateTime.UtcNow;
        var activated = root.CommissionStartsAtUtc is not null;

        foreach (var unit in units)
        {
            unit.ReferredBySalesManagerUserId = toBeneficiaryUserId;
            unit.SalesAttributionSource = SalesAttributionSource.Admin;
            unit.SalesAttributedAtUtc ??= now;

            // Future purchases only: when already activated, keep window/rates but recompute indirect beneficiary.
            if (activated && toBeneficiaryUserId is not null)
            {
                unit.CommissionIndirectSalesManagerUserId = newIndirect;
            }
            else if (!activated)
            {
                // Not yet activated — clear stale indirect snapshot so activation uses the new SM's upline.
                unit.CommissionIndirectSalesManagerUserId = null;
                unit.CommissionIndirectRateSnapshot = null;
                unit.CommissionDirectRateSnapshot = null;
                unit.CommissionTermsSnapshottedAtUtc = null;
            }
            else if (toBeneficiaryUserId is null)
            {
                unit.CommissionIndirectSalesManagerUserId = null;
            }
        }

        var change = new SalesAttributionChange
        {
            Id = Guid.NewGuid(),
            CompanyId = rootId,
            FromUserId = fromUserId,
            ToUserId = toBeneficiaryUserId,
            Source = SalesAttributionSource.Admin,
            Reason = trimmed,
            ChangedByUserId = changedByUserId,
            ChangedAtUtc = now
        };
        _db.SalesAttributionChanges.Add(change);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "sales.attribution.reassign",
            Message = "Admin reassigned sales attribution",
            DetailsJson = JsonSerializer.Serialize(new
            {
                companyId = rootId,
                fromUserId,
                toUserId = toBeneficiaryUserId,
                changedByUserId,
                activated
            }),
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);
        return change;
    }

    public async Task<IReadOnlyList<SalesAttributionHistoryItem>> GetHistoryAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => new { c.Id, c.ParentCompanyId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Bedrijf niet gevonden.");

        var rootId = SalesCommercialUnit.RootIdOf(company.Id, company.ParentCompanyId);
        return await _db.SalesAttributionChanges.AsNoTracking()
            .Where(c => c.CompanyId == rootId)
            .OrderByDescending(c => c.ChangedAtUtc)
            .Select(c => new SalesAttributionHistoryItem(
                c.Id,
                c.CompanyId,
                c.FromUserId,
                c.ToUserId,
                c.Source.ToString(),
                c.Reason,
                c.ChangedByUserId,
                c.ChangedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
