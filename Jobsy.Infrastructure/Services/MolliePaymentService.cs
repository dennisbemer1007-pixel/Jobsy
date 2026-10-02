using System.Globalization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Live Mollie Payments for token packs. Falls back to <see cref="MolliePaymentStub"/> in Development
/// when no API key is configured.
/// </summary>
public sealed class MolliePaymentService : IPaymentService
{
    public const string HttpClientName = "Mollie";
    public const string DefaultApiBaseUrl = "https://api.mollie.com/v2/";

    private readonly JobsyDbContext _db;
    private readonly IPlatformFeatureService _features;
    private readonly IMollieApiClient _mollie;
    private readonly IHostEnvironment _environment;
    private readonly MolliePaymentStub _stub;
    private readonly ILogger<MolliePaymentService> _logger;

    public MolliePaymentService(
        JobsyDbContext db,
        IPlatformFeatureService features,
        IMollieApiClient mollie,
        IHostEnvironment environment,
        MolliePaymentStub stub,
        ILogger<MolliePaymentService> logger)
    {
        _db = db;
        _features = features;
        _mollie = mollie;
        _environment = environment;
        _stub = stub;
        _logger = logger;
    }

    public async Task<PaymentCheckoutResult> CreateTokenPurchaseCheckoutAsync(
        Guid companyId,
        int packSize,
        string? paymentMethod = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _mollie.TryGetApiKeyAsync(cancellationToken))
        {
            if (_environment.IsDevelopment())
            {
                return await _stub.CreateTokenPurchaseCheckoutAsync(
                    companyId, packSize, paymentMethod, cancellationToken);
            }

            throw new InvalidOperationException(
                "Betalingen zijn niet geconfigureerd. Sla een Mollie API-key op onder Admin → Integraties.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(packSize);

        var company = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new InvalidOperationException("Company not found.");

        var resolvedMethod = ResolvePaymentMethod(paymentMethod, company.PreferredPaymentMethod);
        var price = await ResolvePackPriceAsync(packSize, cancellationToken);
        var money = TokenVatPricing.SplitInclVatEuros(price);
        var checkoutId = Guid.NewGuid();
        var features = await _features.GetAsync(cancellationToken);
        var webBase = features.PublicWebBaseUrl.TrimEnd('/');
        var redirectUrl = $"{webBase}/tokens/checkout-return?checkoutId={checkoutId:D}";
        var webhookUrl = _mollie.ResolveWebhookUrl();

        var amountValue = price.ToString("0.00", CultureInfo.InvariantCulture);
        var metadata = new Dictionary<string, string>
        {
            ["checkoutId"] = checkoutId.ToString("D"),
            ["companyId"] = companyId.ToString("D"),
            ["packSize"] = packSize.ToString(CultureInfo.InvariantCulture)
        };
        if (resolvedMethod is not null)
        {
            metadata["paymentMethod"] = resolvedMethod;
        }

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogWarning(
                "Mollie webhook URL unavailable for company {CompanyId}; fulfillment relies on redirect return.",
                companyId);
        }

        MolliePaymentSnapshot payment;
        try
        {
            payment = await _mollie.CreatePaymentAsync(
                new MollieCreatePaymentRequest(
                    amountValue,
                    $"Lobsy tokens ({packSize})",
                    redirectUrl,
                    webhookUrl,
                    resolvedMethod is not null
                        ? [resolvedMethod]
                        : MolliePaymentMethods.PrimaryMethods.ToArray(),
                    Locale: null,
                    metadata),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mollie create payment failed for company {CompanyId}", companyId);
            throw new InvalidOperationException(
                "Mollie-betaling starten mislukt. Controleer de API-key en probeer opnieuw.", ex);
        }

        if (string.IsNullOrWhiteSpace(payment.Id))
        {
            throw new InvalidOperationException("Mollie gaf geen payment-id terug.");
        }

        if (string.IsNullOrWhiteSpace(payment.CheckoutUrl))
        {
            throw new InvalidOperationException("Mollie gaf geen checkout-URL terug.");
        }

        var storedMethod = MolliePaymentMethods.NormalizeOrNull(payment.Method) ?? resolvedMethod;
        _db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = checkoutId,
            PaymentId = payment.Id,
            CompanyId = companyId,
            PackSize = packSize,
            AmountEuro = price,
            AmountExVatCents = money.ExVatCents,
            VatAmountCents = money.VatCents,
            TotalAmountCents = money.TotalCents,
            PaymentMethod = storedMethod,
            Status = TokenPurchaseCheckoutStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Mollie checkout for company {CompanyId}: {Pack} tokens = €{Price} method={Method} webhook={HasWebhook} ({PaymentId})",
            companyId, packSize, price, storedMethod, !string.IsNullOrWhiteSpace(webhookUrl), payment.Id);

        return new PaymentCheckoutResult(
            payment.Id,
            payment.CheckoutUrl,
            packSize,
            price,
            IsStub: false,
            CheckoutId: checkoutId,
            PaymentMethod: storedMethod);
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            return new PaymentStatusResult(paymentId ?? "", "unknown", IsPaid: false);
        }

        if (paymentId.StartsWith("stub_pay_", StringComparison.Ordinal))
        {
            return await _stub.GetPaymentStatusAsync(paymentId, cancellationToken);
        }

        var session = await _db.TokenPurchaseCheckouts
            .FirstOrDefaultAsync(c => c.PaymentId == paymentId, cancellationToken);
        if (session is null)
        {
            return new PaymentStatusResult(paymentId, "not_found", IsPaid: false);
        }

        var sessionAmount = session.AmountEuro > 0
            ? session.AmountEuro
            : TokenVatPricing.FromCents(session.TotalAmountCents);

        if (!await _mollie.TryGetApiKeyAsync(cancellationToken))
        {
            var localPaid = session.Status is TokenPurchaseCheckoutStatus.Paid
                or TokenPurchaseCheckoutStatus.Credited;
            return new PaymentStatusResult(
                paymentId,
                session.Status.ToString().ToLowerInvariant(),
                IsPaid: localPaid,
                Method: session.PaymentMethod,
                AmountEuro: sessionAmount);
        }

        MolliePaymentSnapshot payment;
        try
        {
            payment = await _mollie.FetchPaymentAsync(paymentId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mollie get payment failed for {PaymentId}", paymentId);
            return new PaymentStatusResult(paymentId, "provider_error", IsPaid: false, AmountEuro: sessionAmount);
        }

        var status = string.IsNullOrWhiteSpace(payment.Status) ? "unknown" : payment.Status.Trim().ToLowerInvariant();
        var isPaid = status is "paid"
                     || session.Status is TokenPurchaseCheckoutStatus.Paid or TokenPurchaseCheckoutStatus.Credited;
        var method = MolliePaymentMethods.NormalizeOrNull(payment.Method);
        var amountEuro = ParseMollieAmount(payment.AmountValue) is decimal liveAmount && liveAmount > 0
            ? liveAmount
            : sessionAmount;
        var amountRefunded = ParseMollieAmount(payment.AmountRefundedValue) ?? 0m;
        var amountChargedBack = ParseMollieAmount(payment.AmountChargedBackValue) ?? 0m;
        var dirty = false;

        if (method is not null && !string.Equals(session.PaymentMethod, method, StringComparison.Ordinal))
        {
            session.PaymentMethod = method;
            dirty = true;
        }

        if (status is "paid" && session.Status == TokenPurchaseCheckoutStatus.Pending)
        {
            session.Status = TokenPurchaseCheckoutStatus.Paid;
            dirty = true;
        }
        else if (status is "canceled" or "cancelled" or "expired" or "failed"
                 && session.Status == TokenPurchaseCheckoutStatus.Pending)
        {
            session.Status = TokenPurchaseCheckoutStatus.Cancelled;
            dirty = true;
        }

        if (dirty)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new PaymentStatusResult(
            paymentId,
            status,
            IsPaid: isPaid,
            Method: method ?? session.PaymentMethod,
            AmountEuro: amountEuro,
            AmountRefundedEuro: amountRefunded,
            AmountChargedBackEuro: amountChargedBack);
    }

    private static decimal? ParseMollieAmount(string? value)
    {
        if (value is null)
        {
            return null;
        }

        return decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;
    }

    private static string? ResolvePaymentMethod(string? requested, string? companyPreferred)
    {
        var fromRequest = MolliePaymentMethods.NormalizeOrNull(requested);
        if (fromRequest is not null)
        {
            return fromRequest;
        }

        return MolliePaymentMethods.NormalizeOrNull(companyPreferred);
    }

    private async Task<decimal> ResolvePackPriceAsync(int packSize, CancellationToken cancellationToken)
    {
        var priced = await _db.TokenPricings.AsNoTracking()
            .Where(p => p.IsActive && p.PackSize == packSize)
            .Select(p => (decimal?)p.PriceEuro)
            .FirstOrDefaultAsync(cancellationToken);

        return priced ?? packSize switch
        {
            1 => 5.00m,
            5 => 22.50m,
            10 => 40.00m,
            50 => 175.00m,
            100 => 300.00m,
            _ => packSize * 5.00m
        };
    }
}
