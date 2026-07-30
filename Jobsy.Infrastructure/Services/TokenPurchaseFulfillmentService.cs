using Jobsy.Core.Entities;
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
    private readonly ICommissionLedgerService _commissions;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<TokenPurchaseFulfillmentService> _logger;

    public TokenPurchaseFulfillmentService(
        JobsyDbContext db,
        ITokenLedgerService tokenLedger,
        IPaymentService payments,
        ITokenPurchaseInvoiceService invoices,
        IVatBufferTransferService vatBuffer,
        ICommissionLedgerService commissions,
        IHostEnvironment environment,
        ILogger<TokenPurchaseFulfillmentService> logger)
    {
        _db = db;
        _tokenLedger = tokenLedger;
        _payments = payments;
        _invoices = invoices;
        _vatBuffer = vatBuffer;
        _commissions = commissions;
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

        if (session.Status == TokenPurchaseCheckoutStatus.Credited
            && session.TokenPurchaseInvoiceId is Guid existingInvoiceId)
        {
            var existingInvoice = await _invoices.GetAsync(existingInvoiceId, cancellationToken);
            var bal = await _tokenLedger.GetBalanceAsync(session.CompanyId, cancellationToken);
            return new TokenPurchaseFulfillmentResult(
                session.Id,
                session.CompanyId,
                session.Company.Name,
                bal,
                session.TokenTransactionId ?? Guid.Empty,
                existingInvoiceId,
                existingInvoice?.InvoiceNumber ?? "",
                AlreadyFulfilled: true);
        }

        if (session.Status == TokenPurchaseCheckoutStatus.Cancelled)
        {
            return null;
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

        // Atomic claim: only one concurrent fulfill can transition Pending/Paid → Credited.
        var creditedAt = DateTime.UtcNow;
        var claimed = 0;
        if (_db.Database.IsRelational())
        {
            claimed = await _db.TokenPurchaseCheckouts
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
        else
        {
            // InMemory / tests: claim via tracked entity.
            if (session.Status is TokenPurchaseCheckoutStatus.Pending or TokenPurchaseCheckoutStatus.Paid)
            {
                session.Status = TokenPurchaseCheckoutStatus.Credited;
                session.CreditedAt = creditedAt;
                session.AmountExVatCents = session.AmountExVatCents;
                session.VatAmountCents = session.VatAmountCents;
                session.TotalAmountCents = session.TotalAmountCents;
                await _db.SaveChangesAsync(cancellationToken);
                claimed = 1;
            }
        }

        if (claimed == 0)
        {
            // Another worker won; reload and return existing fulfillment if present.
            await _db.Entry(session).ReloadAsync(cancellationToken);
            if (session.TokenPurchaseInvoiceId is Guid invId)
            {
                var inv = await _invoices.GetAsync(invId, cancellationToken);
                var bal = await _tokenLedger.GetBalanceAsync(session.CompanyId, cancellationToken);
                return new TokenPurchaseFulfillmentResult(
                    session.Id,
                    session.CompanyId,
                    session.Company.Name,
                    bal,
                    session.TokenTransactionId ?? Guid.Empty,
                    invId,
                    inv?.InvoiceNumber ?? "",
                    AlreadyFulfilled: true);
            }

            return null;
        }

        try
        {
            var notePrefix = session.PaymentId.StartsWith("stub_pay_", StringComparison.Ordinal)
                ? "Mollie stub"
                : "Mollie";

            var entry = await _tokenLedger.RecordPurchaseAsync(
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

            var invoice = await _invoices.CreateForCheckoutAsync(session, entry, cancellationToken);

            if (_db.Database.IsRelational())
            {
                await _db.TokenPurchaseCheckouts
                    .Where(c => c.Id == session.Id)
                    .ExecuteUpdateAsync(
                        s => s
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
                session.TokenTransactionId = entry.Id;
                session.TokenPurchaseInvoiceId = invoice.Id;
                var trackedTx = await _db.TokenTransactions.FirstAsync(t => t.Id == entry.Id, cancellationToken);
                trackedTx.TokenPurchaseInvoiceId = invoice.Id;
                await _db.SaveChangesAsync(cancellationToken);
            }

            await _vatBuffer.QueueForInvoiceAsync(invoice, cancellationToken);

            // Accrue salesmanager token commission when the supplier was referred.
            var company = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == session.CompanyId, cancellationToken);
            if (company?.ReferredBySalesManagerUserId is Guid smId)
            {
                await _commissions.TryCreditTokenCommissionAsync(
                    smId,
                    session.CompanyId,
                    session.Id,
                    session.AmountEuro,
                    company.FirstYearStartedAt,
                    cancellationToken);
            }

            _logger.LogInformation(
                "Token purchase fulfilled: checkout {CheckoutId}, invoice {InvoiceNumber}, VAT {VatCents}c",
                session.Id, invoice.InvoiceNumber, invoice.VatAmountCents);

            return new TokenPurchaseFulfillmentResult(
                session.Id,
                session.CompanyId,
                session.Company.Name,
                entry.NewBalance,
                entry.Id,
                invoice.Id,
                invoice.InvoiceNumber,
                AlreadyFulfilled: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token purchase fulfillment failed for checkout {CheckoutId}", session.Id);
            if (_db.Database.IsRelational())
            {
                await _db.TokenPurchaseCheckouts
                    .Where(c => c.Id == session.Id && c.Status == TokenPurchaseCheckoutStatus.Credited)
                    .ExecuteUpdateAsync(
                        s => s
                            .SetProperty(c => c.Status, TokenPurchaseCheckoutStatus.Paid)
                            .SetProperty(c => c.CreditedAt, (DateTime?)null),
                        cancellationToken);
            }
            else
            {
                session.Status = TokenPurchaseCheckoutStatus.Paid;
                session.CreditedAt = null;
                await _db.SaveChangesAsync(cancellationToken);
            }

            throw;
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
