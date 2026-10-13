using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class SalesCorrectionService : ISalesCorrectionService
{
    private static readonly CommissionEntryKind[] CorrectableKinds =
    [
        CommissionEntryKind.TokenCommission,
        CommissionEntryKind.IndirectTokenCommission,
        CommissionEntryKind.FounderBonus
    ];

    private readonly JobsyDbContext _db;

    public SalesCorrectionService(JobsyDbContext db) => _db = db;

    public async Task ApplyPaymentReversalsAsync(
        Guid checkoutId,
        decimal amountPaidEuro,
        decimal amountRefundedEuro,
        decimal amountChargedBackEuro,
        CancellationToken cancellationToken = default)
    {
        if (amountPaidEuro <= 0)
        {
            return;
        }

        var refunded = Math.Clamp(amountRefundedEuro, 0m, amountPaidEuro);
        var chargedBack = Math.Clamp(amountChargedBackEuro, 0m, amountPaidEuro);
        if (refunded <= 0 && chargedBack <= 0)
        {
            return;
        }

        var originals = await _db.CommissionLedgerEntries
            .Where(e => e.SourceTokenCheckoutId == checkoutId
                        && CorrectableKinds.Contains(e.Kind)
                        && e.SourceRefundKey == null)
            .ToListAsync(cancellationToken);

        // Founder bonus may be keyed by SourcePaymentId on the onboarding checkout path.
        if (originals.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var anyBooked = false;
        foreach (var original in originals)
        {
            if (refunded > 0)
            {
                anyBooked |= await BookDeltaCorrectionAsync(
                    original,
                    CommissionEntryKind.RefundCorrection,
                    "refund",
                    refunded,
                    amountPaidEuro,
                    now,
                    cancellationToken);
            }

            if (chargedBack > 0)
            {
                anyBooked |= await BookDeltaCorrectionAsync(
                    original,
                    CommissionEntryKind.ChargebackCorrection,
                    "chargeback",
                    chargedBack,
                    amountPaidEuro,
                    now,
                    cancellationToken);
            }
        }

        if (anyBooked || refunded > 0 || chargedBack > 0)
        {
            _db.PlatformLogs.Add(new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Warning,
                Category = "sales.refund.tokens-not-reversed",
                Message = "Commission corrected; purchased tokens and bonus tokens were not reversed",
                DetailsJson = JsonSerializer.Serialize(new
                {
                    checkoutId,
                    amountPaidEuro,
                    amountRefundedEuro = refunded,
                    amountChargedBackEuro = chargedBack
                }),
                CreatedAt = now
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<CommissionLedgerEntry> BookManualCorrectionAsync(
        Guid beneficiaryUserId,
        Guid? companyId,
        decimal amountExVat,
        string reason,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (amountExVat == 0m)
        {
            throw new ArgumentException("Correctiebedrag mag niet 0 zijn.");
        }

        if (Math.Abs(amountExVat) > 10_000m)
        {
            throw new ArgumentException("Correctiebedrag mag maximaal € 10.000 zijn.");
        }

        var trimmed = (reason ?? string.Empty).Trim();
        if (trimmed.Length is < 5 or > 500)
        {
            throw new ArgumentException("Reden moet tussen 5 en 500 tekens zijn.");
        }

        var now = DateTime.UtcNow;
        var rounded = decimal.Round(amountExVat, 2, MidpointRounding.AwayFromZero);
        var entry = new CommissionLedgerEntry
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = beneficiaryUserId,
            Kind = CommissionEntryKind.Adjustment,
            AmountExVat = rounded,
            VatAmount = SalesCommissionRules.VatOn(Math.Abs(rounded)) * Math.Sign(rounded),
            VatRate = SalesCommissionRules.VatRate,
            Note = "Sales.Label.Kind.Adjustment",
            CompanyId = companyId,
            Reason = trimmed,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            AvailableFromUtc = now
        };
        _db.CommissionLedgerEntries.Add(entry);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "sales.ledger.correction",
            Message = "Admin booked a manual commission correction",
            DetailsJson = JsonSerializer.Serialize(new
            {
                entryId = entry.Id,
                beneficiaryUserId,
                companyId,
                amountExVat = rounded,
                createdByUserId
            }),
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    private async Task<bool> BookDeltaCorrectionAsync(
        CommissionLedgerEntry original,
        CommissionEntryKind correctionKind,
        string keyPrefix,
        decimal reversedEuro,
        decimal amountPaidEuro,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var target = -decimal.Round(
            original.AmountExVat * (reversedEuro / amountPaidEuro),
            2,
            MidpointRounding.AwayFromZero);
        if (target == 0m)
        {
            return false;
        }

        var already = await _db.CommissionLedgerEntries
            .Where(e => e.SourceTokenCheckoutId == original.SourceTokenCheckoutId
                        && e.SalesManagerUserId == original.SalesManagerUserId
                        && e.Kind == correctionKind
                        && e.CorrectsEntryId == original.Id)
            .SumAsync(e => (decimal?)e.AmountExVat, cancellationToken) ?? 0m;

        var delta = decimal.Round(target - already, 2, MidpointRounding.AwayFromZero);
        if (delta == 0m)
        {
            return false;
        }

        var cumulativeCents = (int)decimal.Round(Math.Abs(target) * 100m, 0, MidpointRounding.AwayFromZero);
        var refundKey = $"{keyPrefix}:{cumulativeCents}";
        if (refundKey.Length > 80)
        {
            refundKey = refundKey[..80];
        }

        var exists = await _db.CommissionLedgerEntries.AsNoTracking()
            .AnyAsync(
                e => e.SourceTokenCheckoutId == original.SourceTokenCheckoutId
                     && e.SalesManagerUserId == original.SalesManagerUserId
                     && e.Kind == correctionKind
                     && e.SourceRefundKey == refundKey,
                cancellationToken);
        if (exists)
        {
            return false;
        }

        var entry = new CommissionLedgerEntry
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = original.SalesManagerUserId,
            Kind = correctionKind,
            AmountExVat = delta,
            VatAmount = SalesCommissionRules.VatOn(Math.Abs(delta)) * Math.Sign(delta),
            VatRate = SalesCommissionRules.VatRate,
            Note = correctionKind == CommissionEntryKind.RefundCorrection
                ? "Sales.Label.Kind.RefundCorrection"
                : "Sales.Label.Kind.ChargebackCorrection",
            CompanyId = original.CompanyId,
            SourceTokenCheckoutId = original.SourceTokenCheckoutId,
            SourceRefundKey = refundKey,
            CorrectsEntryId = original.Id,
            CreatedAt = now,
            AvailableFromUtc = now
        };
        _db.CommissionLedgerEntries.Add(entry);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _db.Entry(entry).State = EntityState.Detached;
            return false;
        }
    }
}
