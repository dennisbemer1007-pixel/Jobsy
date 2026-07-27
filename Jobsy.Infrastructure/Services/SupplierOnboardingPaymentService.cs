using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class SupplierOnboardingPaymentService : ISupplierOnboardingPaymentService
{
    private readonly JobsyDbContext _db;
    private readonly ICommissionLedgerService _commissions;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SupplierOnboardingPaymentService> _logger;

    public SupplierOnboardingPaymentService(
        JobsyDbContext db,
        ICommissionLedgerService commissions,
        IHostEnvironment environment,
        ILogger<SupplierOnboardingPaymentService> logger)
    {
        _db = db;
        _commissions = commissions;
        _environment = environment;
        _logger = logger;
    }

    public async Task<SupplierOnboardingCheckoutResult> CreateCheckoutAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        _ = await _db.Companies
                .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Bedrijf niet gevonden.");

        var alreadyCredited = await _db.SupplierOnboardingCheckouts.AnyAsync(
            c => c.CompanyId == companyId && c.Status == SupplierOnboardingCheckoutStatus.Credited,
            cancellationToken);
        if (alreadyCredited)
        {
            throw new InvalidOperationException("First-year onboarding is al betaald voor dit bedrijf.");
        }

        // Only one open checkout per company — cancel stale Pending/Paid sessions.
        var open = await _db.SupplierOnboardingCheckouts
            .Where(c => c.CompanyId == companyId
                        && (c.Status == SupplierOnboardingCheckoutStatus.Pending
                            || c.Status == SupplierOnboardingCheckoutStatus.Paid))
            .ToListAsync(cancellationToken);
        foreach (var prior in open)
        {
            prior.Status = SupplierOnboardingCheckoutStatus.Cancelled;
        }

        var paymentId = $"stub_onboard_{Guid.NewGuid():N}";
        _db.SupplierOnboardingCheckouts.Add(new SupplierOnboardingCheckout
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            CompanyId = companyId,
            AmountEuro = SalesCommissionRules.FirstYearOnboardingEuro,
            Status = SupplierOnboardingCheckoutStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Onboarding checkout for company {CompanyId}: €{Amount} ({PaymentId})",
            companyId, SalesCommissionRules.FirstYearOnboardingEuro, paymentId);

        return new SupplierOnboardingCheckoutResult(
            paymentId,
            $"https://localhost:5201/employer/onboarding-checkout?paymentId={Uri.EscapeDataString(paymentId)}&companyId={companyId}",
            SalesCommissionRules.FirstYearOnboardingEuro,
            IsStub: true);
    }

    public async Task<SupplierOnboardingCompleteResult> CompleteCheckoutAsync(
        string paymentId,
        Guid? actorUserId,
        Guid? expectedCompanyId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            throw new ArgumentException("Ongeldige checkout.");
        }

        var session = await _db.SupplierOnboardingCheckouts
            .Include(c => c.Company)
            .FirstOrDefaultAsync(c => c.PaymentId == paymentId, cancellationToken)
            ?? throw new KeyNotFoundException("Checkout-sessie niet gevonden.");

        // Authz: reject IDOR before any status mutation or commission credit.
        if (expectedCompanyId is Guid expected && session.CompanyId != expected)
        {
            throw new UnauthorizedAccessException("Checkout hoort niet bij dit bedrijf.");
        }

        if (session.Status == SupplierOnboardingCheckoutStatus.Credited)
        {
            var alreadyCreditedCommission = await _db.CommissionLedgerEntries.AnyAsync(
                e => e.CompanyId == session.CompanyId && e.Kind == CommissionEntryKind.FounderBonus,
                cancellationToken);
            return new SupplierOnboardingCompleteResult(
                session.CompanyId,
                session.Status.ToString(),
                CommissionCredited: alreadyCreditedCommission,
                session.Company.FirstYearSupplierSlot);
        }

        if (session.Status == SupplierOnboardingCheckoutStatus.Cancelled)
        {
            throw new InvalidOperationException("Checkout is geannuleerd.");
        }

        if (_environment.IsDevelopment()
            && session.PaymentId.StartsWith("stub_onboard_", StringComparison.Ordinal)
            && session.Status == SupplierOnboardingCheckoutStatus.Pending)
        {
            session.Status = SupplierOnboardingCheckoutStatus.Paid;
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Hard payment gate: only Paid/Credited rows count (set by stub webhook or real provider).
        if (session.Status is not (SupplierOnboardingCheckoutStatus.Paid
            or SupplierOnboardingCheckoutStatus.Credited))
        {
            throw new InvalidOperationException("Betaling is nog niet afgerond.");
        }

        if (!await TryClaimCreditedAsync(session.Id, cancellationToken))
        {
            var refreshed = await _db.SupplierOnboardingCheckouts
                .Include(c => c.Company)
                .FirstAsync(c => c.Id == session.Id, cancellationToken);
            var credited = await _db.CommissionLedgerEntries.AnyAsync(
                e => e.CompanyId == refreshed.CompanyId && e.Kind == CommissionEntryKind.FounderBonus,
                cancellationToken);
            return new SupplierOnboardingCompleteResult(
                refreshed.CompanyId,
                refreshed.Status.ToString(),
                CommissionCredited: credited,
                refreshed.Company.FirstYearSupplierSlot);
        }

        try
        {
            var company = await _db.Companies.FirstAsync(c => c.Id == session.CompanyId, cancellationToken);
            company.FirstYearStartedAt ??= DateTime.UtcNow;

            if (company.FirstYearSupplierSlot is null && company.ReferredBySalesManagerUserId is not null)
            {
                await AssignFounderSlotAsync(company, cancellationToken);
            }

            var commissionCredited = false;
            if (company.ReferredBySalesManagerUserId is Guid smId
                && SalesCommissionRules.IsEligibleFounderSlot(company.FirstYearSupplierSlot))
            {
                var credit = await _commissions.TryCreditFounderBonusAsync(
                    smId,
                    company.Id,
                    session.PaymentId,
                    company.FirstYearSupplierSlot,
                    cancellationToken);
                commissionCredited = credit is not null;
            }

            _ = actorUserId;
            return new SupplierOnboardingCompleteResult(
                company.Id,
                nameof(SupplierOnboardingCheckoutStatus.Credited),
                commissionCredited,
                company.FirstYearSupplierSlot);
        }
        catch
        {
            await TryRevertClaimAsync(session.Id, cancellationToken);
            throw;
        }
    }

    private async Task<bool> TryClaimCreditedAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var claimed = await _db.SupplierOnboardingCheckouts
                .Where(c => c.Id == sessionId
                            && (c.Status == SupplierOnboardingCheckoutStatus.Pending
                                || c.Status == SupplierOnboardingCheckoutStatus.Paid))
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(c => c.Status, SupplierOnboardingCheckoutStatus.Credited)
                        .SetProperty(c => c.CreditedAt, DateTime.UtcNow),
                    cancellationToken);
            return claimed > 0;
        }
        catch (InvalidOperationException)
        {
            // InMemory provider has no ExecuteUpdate — fall back to tracked update.
            var session = await _db.SupplierOnboardingCheckouts
                .FirstAsync(c => c.Id == sessionId, cancellationToken);
            if (session.Status is not (SupplierOnboardingCheckoutStatus.Pending
                or SupplierOnboardingCheckoutStatus.Paid))
            {
                return false;
            }

            session.Status = SupplierOnboardingCheckoutStatus.Credited;
            session.CreditedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    private async Task TryRevertClaimAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            await _db.SupplierOnboardingCheckouts
                .Where(c => c.Id == sessionId && c.Status == SupplierOnboardingCheckoutStatus.Credited)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(c => c.Status, SupplierOnboardingCheckoutStatus.Paid)
                        .SetProperty(c => c.CreditedAt, (DateTime?)null),
                    cancellationToken);
        }
        catch (InvalidOperationException)
        {
            var session = await _db.SupplierOnboardingCheckouts
                .FirstOrDefaultAsync(c => c.Id == sessionId, cancellationToken);
            if (session is { Status: SupplierOnboardingCheckoutStatus.Credited })
            {
                session.Status = SupplierOnboardingCheckoutStatus.Paid;
                session.CreditedAt = null;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task AssignFounderSlotAsync(Company company, CancellationToken cancellationToken)
    {
        // Prefer unused slots; unique filtered index enforces uniqueness under concurrency.
        for (var attempt = 0; attempt < SalesCommissionRules.MaxFounderSlots; attempt++)
        {
            var usedSlots = await _db.Companies
                .Where(c => c.FirstYearSupplierSlot != null)
                .Select(c => c.FirstYearSupplierSlot!.Value)
                .ToListAsync(cancellationToken);

            var next = Enumerable.Range(1, SalesCommissionRules.MaxFounderSlots)
                .FirstOrDefault(s => !usedSlots.Contains(s));
            if (next <= 0)
            {
                return;
            }

            company.FirstYearSupplierSlot = next;
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException)
            {
                company.FirstYearSupplierSlot = null;
                _db.Entry(company).Property(c => c.FirstYearSupplierSlot).IsModified = false;
                await _db.Entry(company).ReloadAsync(cancellationToken);
                if (company.FirstYearSupplierSlot is not null)
                {
                    return;
                }
            }
        }
    }
}
