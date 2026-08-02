using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class MolliePaymentStub : IPaymentService
{
    private readonly JobsyDbContext _db;
    private readonly IPlatformFeatureService _features;
    private readonly ILogger<MolliePaymentStub> _logger;

    public MolliePaymentStub(
        JobsyDbContext db,
        IPlatformFeatureService features,
        ILogger<MolliePaymentStub> logger)
    {
        _db = db;
        _features = features;
        _logger = logger;
    }

    public async Task<PaymentCheckoutResult> CreateTokenPurchaseCheckoutAsync(
        Guid companyId,
        int packSize,
        string? paymentMethod = null,
        CancellationToken cancellationToken = default)
    {
        if (packSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(packSize));
        }

        var company = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new InvalidOperationException("Company not found.");

        var resolvedMethod = MolliePaymentMethods.NormalizeOrNull(paymentMethod)
            ?? MolliePaymentMethods.NormalizeOrNull(company.PreferredPaymentMethod)
            ?? MolliePaymentMethods.Ideal;

        var priced = await _db.TokenPricings.AsNoTracking()
            .Where(p => p.IsActive && p.PackSize == packSize)
            .Select(p => (decimal?)p.PriceEuro)
            .FirstOrDefaultAsync(cancellationToken);

        var price = priced ?? packSize switch
        {
            1 => 5.00m,
            5 => 22.50m,
            10 => 40.00m,
            50 => 175.00m,
            100 => 300.00m,
            _ => packSize * 5.00m
        };

        var paymentId = $"stub_pay_{Guid.NewGuid():N}";
        var checkoutId = Guid.NewGuid();
        var (exVat, vat, total) = TokenVatPricing.SplitInclVatEuros(price);
        _db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = checkoutId,
            PaymentId = paymentId,
            CompanyId = companyId,
            PackSize = packSize,
            AmountEuro = price,
            AmountExVatCents = exVat,
            VatAmountCents = vat,
            TotalAmountCents = total,
            PaymentMethod = resolvedMethod,
            Status = TokenPurchaseCheckoutStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Mollie stub checkout for company {CompanyId}: {Pack} tokens = €{Price} method={Method} ({PaymentId})",
            companyId, packSize, price, resolvedMethod, paymentId);

        var features = await _features.GetAsync(cancellationToken);
        var webBase = features.PublicWebBaseUrl.TrimEnd('/');
        var checkoutUrl =
            $"{webBase}/tokens/checkout-stub?paymentId={Uri.EscapeDataString(paymentId)}&checkoutId={checkoutId:D}&method={Uri.EscapeDataString(resolvedMethod)}";
        return new PaymentCheckoutResult(
            paymentId,
            checkoutUrl,
            packSize,
            price,
            IsStub: true,
            CheckoutId: checkoutId,
            PaymentMethod: resolvedMethod);
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            return new PaymentStatusResult(paymentId ?? "", "unknown", IsPaid: false);
        }

        var session = await _db.TokenPurchaseCheckouts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.PaymentId == paymentId, cancellationToken);

        if (session is null)
        {
            return new PaymentStatusResult(paymentId, "not_found", IsPaid: false);
        }

        // Stub: only already-paid / credited sessions count as paid.
        // CompleteCheckout (Development) may mark Pending → Paid before calling this.
        var paid = session.Status is TokenPurchaseCheckoutStatus.Paid
            or TokenPurchaseCheckoutStatus.Credited;
        return new PaymentStatusResult(
            paymentId,
            session.Status.ToString().ToLowerInvariant(),
            IsPaid: paid);
    }
}
