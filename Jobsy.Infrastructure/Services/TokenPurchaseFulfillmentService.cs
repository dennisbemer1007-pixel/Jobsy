using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class TokenPurchaseFulfillmentService : ITokenPurchaseFulfillmentService
{
    private readonly JobsyDbContext _db;
    private readonly ITokenLedgerService _tokenLedger;
    private readonly IPaymentService _payments;
    private readonly ITokenPurchaseInvoiceService _invoices;
    private readonly IVatBufferTransferService _vatBuffer;
    private readonly IRevenueShareService _revenueShare;
    private readonly ICommissionLedgerService _commissions;
    private readonly IPendingTokenActionService _pendingActions;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<TokenPurchaseFulfillmentService> _logger;

    public TokenPurchaseFulfillmentService(
        JobsyDbContext db,
        ITokenLedgerService tokenLedger,
        IPaymentService payments,
        ITokenPurchaseInvoiceService invoices,
        IVatBufferTransferService vatBuffer,
        IRevenueShareService revenueShare,
        ICommissionLedgerService commissions,
        IPendingTokenActionService pendingActions,
        IHostEnvironment environment,
        ILogger<TokenPurchaseFulfillmentService> logger)
    {
        _db = db;
        _tokenLedger = tokenLedger;
        _payments = payments;
        _invoices = invoices;
        _vatBuffer = vatBuffer;
        _revenueShare = revenueShare;
        _commissions = commissions;
        _pendingActions = pendingActions;
        _environment = environment;
        _logger = logger;
    }

    public async Task<TokenPurchaseFulfillmentResult?> TryFulfillPaidCheckoutAsync(
        Guid checkoutId,
        Guid? actorUserId = null,
        bool allowDevStubMarkPaid = false,
        CancellationToken cancellationToken = default)
    {
        var session = await _db.TokenPurchaseCheckouts
            .Include(c => c.Company)
            .FirstOrDefaultAsync(c => c.Id == checkoutId, cancellationToken);

        if (session is null)
        {
            return null;
        }

        if (session.Status == TokenPurchaseCheckoutStatus.Cancelled)
        {
            return null;
        }

        // Already fully fulfilled — still retry commission settlement (idempotent) so a
        // transient failure after invoice never leaves salesmanagers without credit.
        if (session.TokenPurchaseInvoiceId is Guid existingInvoiceId)
        {
            if (session.TokenTransactionId is Guid creditedTxId)
            {
                await TryApplyRevenueShareAsync(session, creditedTxId, cancellationToken);
            }

            var already = await BuildResultAsync(session, existingInvoiceId, alreadyFulfilled: true, cancellationToken);
            var pendingAlready = await TryRunPendingActionAsync(session.Id, cancellationToken);
            return already with { PendingAction = pendingAlready };
        }

        // Credited / partial — repair forward without double-crediting.
        if (session.Status == TokenPurchaseCheckoutStatus.Credited
            || session.TokenTransactionId is not null)
        {
            var repaired = await RepairIncompleteFulfillmentAsync(session, actorUserId, cancellationToken);
            var pendingRepaired = await TryRunPendingActionAsync(session.Id, cancellationToken);
            return repaired with { PendingAction = pendingRepaired };
        }

        if (allowDevStubMarkPaid
            && _environment.IsDevelopment()
            && session.PaymentId.StartsWith("stub_pay_", StringComparison.Ordinal)
            && session.Status == TokenPurchaseCheckoutStatus.Pending)
        {
            session.Status = TokenPurchaseCheckoutStatus.Paid;
            await _db.SaveChangesAsync(cancellationToken);
        }

        var status = await _payments.GetPaymentStatusAsync(session.PaymentId, cancellationToken);
        if (!status.IsPaid && session.Status != TokenPurchaseCheckoutStatus.Paid
            && session.Status != TokenPurchaseCheckoutStatus.Credited)
        {
            return null;
        }

        EnsureMoneyFields(session);

        var creditedAt = DateTime.UtcNow;
        var claimed = await TryClaimAsync(session, creditedAt, cancellationToken);
        if (claimed == 0)
        {
            await _db.Entry(session).ReloadAsync(cancellationToken);
            if (session.TokenPurchaseInvoiceId is Guid invId)
            {
                return await BuildResultAsync(session, invId, alreadyFulfilled: true, cancellationToken);
            }

            if (session.Status == TokenPurchaseCheckoutStatus.Credited
                || session.TokenTransactionId is not null)
            {
                return await RepairIncompleteFulfillmentAsync(session, actorUserId, cancellationToken);
            }

            // Another worker is mid-flight; treat as not ready yet (caller can retry).
            return null;
        }

        try
        {
            return await CompleteAfterClaimAsync(session, actorUserId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token purchase fulfillment failed for checkout {CheckoutId}", session.Id);

            // Never roll back to Paid after a ledger credit — repair on next attempt instead.
            var existingTx = await _db.TokenTransactions.AsNoTracking()
                .AnyAsync(t => t.TokenPurchaseCheckoutId == session.Id, cancellationToken);
            if (!existingTx && session.TokenTransactionId is null)
            {
                await ReleaseClaimAsync(session, cancellationToken);
            }

            throw;
        }
    }

    private async Task<TokenPurchaseFulfillmentResult> RepairIncompleteFulfillmentAsync(
        TokenPurchaseCheckout session,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        EnsureMoneyFields(session);

        var entry = await _db.TokenTransactions
            .FirstOrDefaultAsync(t => t.TokenPurchaseCheckoutId == session.Id
                                      && t.Kind == TokenTransactionKind.Purchase, cancellationToken);

        if (entry is null && session.TokenTransactionId is Guid txId)
        {
            entry = await _db.TokenTransactions.FirstOrDefaultAsync(t => t.Id == txId, cancellationToken);
        }

        if (entry is null)
        {
            // Credited without ledger row — complete as if we just claimed.
            return await CompleteAfterClaimAsync(session, actorUserId, cancellationToken);
        }

        var invoice = await _db.TokenPurchaseInvoices
            .FirstOrDefaultAsync(i => i.TokenPurchaseCheckoutId == session.Id, cancellationToken)
            ?? await _invoices.CreateForCheckoutAsync(session, entry, cancellationToken);

        await LinkCheckoutAndTransactionAsync(session, entry, invoice, cancellationToken);
        await _vatBuffer.QueueForInvoiceAsync(invoice, cancellationToken);
        await TryApplyRevenueShareAsync(session, entry.Id, cancellationToken);

        return await BuildResultAsync(session, invoice.Id, alreadyFulfilled: true, cancellationToken);
    }

    private async Task<TokenPurchaseFulfillmentResult> CompleteAfterClaimAsync(
        TokenPurchaseCheckout session,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        // Idempotent: reuse existing purchase ledger row for this checkout.
        var entry = await _db.TokenTransactions
            .FirstOrDefaultAsync(t => t.TokenPurchaseCheckoutId == session.Id
                                      && t.Kind == TokenTransactionKind.Purchase, cancellationToken);

        if (entry is null)
        {
            var notePrefix = session.PaymentId.StartsWith("stub_pay_", StringComparison.Ordinal)
                ? "Mollie stub"
                : "Mollie";

            try
            {
                entry = await _tokenLedger.RecordPurchaseAsync(
                    session.CompanyId,
                    session.PackSize,
                    session.AmountExVatCents,
                    session.VatAmountCents,
                    session.TotalAmountCents,
                    session.Id,
                    invoiceId: null,
                    actorUserId,
                    $"{notePrefix} {session.PaymentId}",
                    cancellationToken);
            }
            catch (DbUpdateException) when (_db.Database.IsRelational())
            {
                // Unique purchase-per-checkout race with concurrent webhook/redirect.
                entry = await _db.TokenTransactions
                    .FirstOrDefaultAsync(t => t.TokenPurchaseCheckoutId == session.Id
                                              && t.Kind == TokenTransactionKind.Purchase, cancellationToken);
                if (entry is null)
                {
                    throw;
                }
            }
        }

        TokenPurchaseInvoice invoice;
        try
        {
            invoice = await _invoices.CreateForCheckoutAsync(session, entry, cancellationToken);
        }
        catch (DbUpdateException) when (_db.Database.IsRelational())
        {
            // Unique InvoiceNumber race — reload existing for this checkout if present.
            invoice = await _db.TokenPurchaseInvoices
                .FirstOrDefaultAsync(i => i.TokenPurchaseCheckoutId == session.Id, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Factuur aanmaken mislukt en geen bestaande factuur voor checkout {session.Id}.");
        }

        await LinkCheckoutAndTransactionAsync(session, entry, invoice, cancellationToken);
        await _vatBuffer.QueueForInvoiceAsync(invoice, cancellationToken);
        await TryApplyRevenueShareAsync(session, entry.Id, cancellationToken);

        _logger.LogInformation(
            "Token purchase fulfilled: checkout {CheckoutId}, invoice {InvoiceNumber}, VAT {VatCents}c",
            session.Id, invoice.InvoiceNumber, invoice.VatAmountCents);

        var pending = await TryRunPendingActionAsync(session.Id, cancellationToken);

        return new TokenPurchaseFulfillmentResult(
            session.Id,
            session.CompanyId,
            session.Company.Name,
            entry.NewBalance,
            entry.Id,
            invoice.Id,
            invoice.InvoiceNumber,
            AlreadyFulfilled: false,
            PendingAction: pending);
    }

    private async Task<PendingTokenActionExecutionResult?> TryRunPendingActionAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _pendingActions.TryExecuteForCheckoutAsync(checkoutId, cancellationToken);
        }
        catch (Exception ex)
        {
            // Tokens are already credited — do not fail the purchase fulfillment.
            _logger.LogWarning(
                ex,
                "Pending vacancy action after checkout {CheckoutId} failed; tokens remain credited",
                checkoutId);
            return null;
        }
    }

    private async Task LinkCheckoutAndTransactionAsync(
        TokenPurchaseCheckout session,
        TokenTransaction entry,
        TokenPurchaseInvoice invoice,
        CancellationToken cancellationToken)
    {
        if (_db.Database.IsRelational())
        {
            await _db.TokenPurchaseCheckouts
                .Where(c => c.Id == session.Id)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(c => c.Status, TokenPurchaseCheckoutStatus.Credited)
                        .SetProperty(c => c.TokenTransactionId, entry.Id)
                        .SetProperty(c => c.TokenPurchaseInvoiceId, invoice.Id),
                    cancellationToken);

            await _db.TokenTransactions
                .Where(t => t.Id == entry.Id)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(t => t.TokenPurchaseInvoiceId, invoice.Id),
                    cancellationToken);
        }
        else
        {
            session.Status = TokenPurchaseCheckoutStatus.Credited;
            session.TokenTransactionId = entry.Id;
            session.TokenPurchaseInvoiceId = invoice.Id;
            var trackedTx = await _db.TokenTransactions.FirstAsync(t => t.Id == entry.Id, cancellationToken);
            trackedTx.TokenPurchaseInvoiceId = invoice.Id;
            await _db.SaveChangesAsync(cancellationToken);
        }

        session.TokenTransactionId = entry.Id;
        session.TokenPurchaseInvoiceId = invoice.Id;
    }

    /// <summary>
    /// Real-time commission settlement triggered from Mollie webhook / checkout complete.
    /// Uses ex-BTW purchase base; direct + upline credits land on the commission ledger immediately.
    /// Failures are logged (not fatal) — retry on subsequent webhook/complete via already-fulfilled path.
    /// </summary>
    private async Task TryApplyRevenueShareAsync(
        TokenPurchaseCheckout session,
        Guid purchaseTokenTransactionId,
        CancellationToken cancellationToken)
    {
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == session.CompanyId, cancellationToken);
        if (company is null)
        {
            return;
        }

        EnsureMoneyFields(session);
        var purchaseExVatEuro = TokenVatPricing.FromCents(session.AmountExVatCents);
        if (purchaseExVatEuro <= 0 && session.AmountEuro > 0)
        {
            // Legacy sessions without cents: derive ex-BTW from incl. total.
            purchaseExVatEuro = TokenVatPricing.FromCents(
                TokenVatPricing.SplitInclVatEuros(session.AmountEuro).ExVatCents);
        }

        try
        {
            await _revenueShare.ApplyTokenPurchaseShareAsync(
                session.Id,
                session.CompanyId,
                purchaseTokenTransactionId,
                session.PackSize,
                purchaseExVatEuro,
                company.ReferredBySalesManagerUserId,
                company.FirstYearStartedAt,
                cancellationToken);

            if (company.ReferredByAmbassadeurUserId is Guid ambassadeurId
                && company.CommissionAmbassadeurRateSnapshot is decimal amRate
                && amRate > 0)
            {
                await _commissions.TryCreditAmbassadeurTokenCommissionAsync(
                    ambassadeurId,
                    session.CompanyId,
                    session.Id,
                    purchaseExVatEuro,
                    company.FirstYearStartedAt,
                    amRate,
                    company.CommissionDurationDaysSnapshot,
                    cancellationToken);
            }

            if (company.ReferredBySalesManagerUserId is Guid smId)
            {
                _logger.LogInformation(
                    "Commission settlement applied for checkout {CheckoutId}: company {CompanyId}, SM {SalesManagerId}, exVat €{ExVat}",
                    session.Id, session.CompanyId, smId, purchaseExVatEuro);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Commission settlement failed for checkout {CheckoutId} (tokens/invoice remain; will retry on next webhook/complete)",
                session.Id);
        }
    }

    private async Task<TokenPurchaseFulfillmentResult> BuildResultAsync(
        TokenPurchaseCheckout session,
        Guid invoiceId,
        bool alreadyFulfilled,
        CancellationToken cancellationToken)
    {
        var existingInvoice = await _invoices.GetAsync(invoiceId, cancellationToken);
        var bal = await _tokenLedger.GetBalanceAsync(session.CompanyId, cancellationToken);
        return new TokenPurchaseFulfillmentResult(
            session.Id,
            session.CompanyId,
            session.Company.Name,
            bal,
            session.TokenTransactionId ?? Guid.Empty,
            invoiceId,
            existingInvoice?.InvoiceNumber ?? "",
            alreadyFulfilled);
    }

    private async Task<int> TryClaimAsync(
        TokenPurchaseCheckout session,
        DateTime creditedAt,
        CancellationToken cancellationToken)
    {
        if (_db.Database.IsRelational())
        {
            return await _db.TokenPurchaseCheckouts
                .Where(c => c.Id == session.Id
                            && (c.Status == TokenPurchaseCheckoutStatus.Pending
                                || c.Status == TokenPurchaseCheckoutStatus.Paid))
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(c => c.Status, TokenPurchaseCheckoutStatus.Credited)
                        .SetProperty(c => c.CreditedAt, creditedAt)
                        .SetProperty(c => c.AmountExVatCents, session.AmountExVatCents)
                        .SetProperty(c => c.VatAmountCents, session.VatAmountCents)
                        .SetProperty(c => c.TotalAmountCents, session.TotalAmountCents),
                    cancellationToken);
        }

        if (session.Status is TokenPurchaseCheckoutStatus.Pending or TokenPurchaseCheckoutStatus.Paid)
        {
            session.Status = TokenPurchaseCheckoutStatus.Credited;
            session.CreditedAt = creditedAt;
            await _db.SaveChangesAsync(cancellationToken);
            return 1;
        }

        return 0;
    }

    private async Task ReleaseClaimAsync(TokenPurchaseCheckout session, CancellationToken cancellationToken)
    {
        if (_db.Database.IsRelational())
        {
            await _db.TokenPurchaseCheckouts
                .Where(c => c.Id == session.Id
                            && c.Status == TokenPurchaseCheckoutStatus.Credited
                            && c.TokenTransactionId == null
                            && c.TokenPurchaseInvoiceId == null)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(c => c.Status, TokenPurchaseCheckoutStatus.Paid)
                        .SetProperty(c => c.CreditedAt, (DateTime?)null),
                    cancellationToken);
            return;
        }

        if (session.TokenTransactionId is null && session.TokenPurchaseInvoiceId is null)
        {
            session.Status = TokenPurchaseCheckoutStatus.Paid;
            session.CreditedAt = null;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private static void EnsureMoneyFields(TokenPurchaseCheckout session)
    {
        if (session.TotalAmountCents > 0)
        {
            return;
        }

        var (ex, vat, total) = TokenVatPricing.SplitInclVatEuros(session.AmountEuro);
        session.AmountExVatCents = ex;
        session.VatAmountCents = vat;
        session.TotalAmountCents = total;
    }
}
