using System.Globalization;
using System.Text.Json;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Localization;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class DeepTestPaymentService : IDeepTestPaymentService
{
    public const string PaymentsUnavailableCode = "payments_unavailable";
    public const string WaiverRequiredCode = "waiver_required";
    public const string AlreadyUnlockedCode = "already_unlocked";
    public const string ConsentRequiredCode = "consent_required";

    private readonly JobsyDbContext _db;
    private readonly IFlexCommercialService _commercial;
    private readonly IDeepAnalysisService _deep;
    private readonly IMollieApiClient _mollie;
    private readonly IPlatformFeatureService _features;
    private readonly IConsumerInvoiceService _invoices;
    private readonly IVatBufferTransferService _vatBuffer;
    private readonly ITransactionalMailer _mailer;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DeepTestPaymentService> _logger;

    public DeepTestPaymentService(
        JobsyDbContext db,
        IFlexCommercialService commercial,
        IDeepAnalysisService deep,
        IMollieApiClient mollie,
        IPlatformFeatureService features,
        IConsumerInvoiceService invoices,
        IVatBufferTransferService vatBuffer,
        ITransactionalMailer mailer,
        IHostEnvironment environment,
        IConfiguration configuration,
        ILogger<DeepTestPaymentService> logger)
    {
        _db = db;
        _commercial = commercial;
        _deep = deep;
        _mollie = mollie;
        _features = features;
        _invoices = invoices;
        _vatBuffer = vatBuffer;
        _mailer = mailer;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetPaymentModeAsync(CancellationToken cancellationToken = default)
    {
        if (AllowStubPayments())
        {
            return "stub";
        }

        if (await _mollie.TryGetApiKeyAsync(cancellationToken))
        {
            return "mollie";
        }

        return "unavailable";
    }

    public async Task<DeepTestCheckoutCreateResult> CreateCheckoutAsync(
        Guid userId,
        AssessmentKind kind,
        bool waiverAccepted,
        string? locale,
        CancellationToken cancellationToken = default)
    {
        if (!DeepAnalysisCatalog.SupportsDeepAnalysis(kind))
        {
            throw new InvalidOperationException("not_found");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("not_found");

        if (!CandidateConsentRules.CanUseCandidateFeatures(user)
            || !CandidateConsentRules.HasCurrentTestAiConsent(user))
        {
            throw new InvalidOperationException(ConsentRequiredCode);
        }

        if (!waiverAccepted && !user.IsTestAccount)
        {
            throw new InvalidOperationException(WaiverRequiredCode);
        }

        var existing = await _db.CandidateDeepAnalyses
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Kind == kind, cancellationToken);
        if (existing is not null && CandidateDeepAnalysisStatuses.IsUnlocked(existing.Status))
        {
            throw new InvalidOperationException(AlreadyUnlockedCode);
        }

        await CancelOpenCheckoutsAsync(userId, kind, cancellationToken);

        if (user.IsTestAccount)
        {
            return await CreateTestUnlockAsync(userId, kind, locale, cancellationToken);
        }

        var mode = await GetPaymentModeAsync(cancellationToken);
        if (mode == "unavailable")
        {
            _logger.LogWarning("Deep-test checkout unavailable: no Mollie key and stubs disabled.");
            throw new InvalidOperationException(PaymentsUnavailableCode);
        }

        var commercial = await _commercial.GetAsync(cancellationToken);
        var price = DeepAnalysisPricing.For(commercial, kind);
        var money = TokenVatPricing.SplitInclVatEuros(price);
        var checkoutId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var uiLocale = NormalizeLocale(locale);
        var isStub = mode == "stub";

        string paymentId;
        string checkoutUrl;
        if (isStub)
        {
            paymentId = $"stub_deep_{Guid.NewGuid():N}";
            checkoutUrl = $"/candidate/deep-analysis/checkout?checkoutId={checkoutId:D}";
        }
        else
        {
            var features = await _features.GetAsync(cancellationToken);
            var webBase = features.PublicWebBaseUrl.TrimEnd('/');
            var redirectUrl = $"{webBase}/candidate/deep-analysis/checkout?checkoutId={checkoutId:D}";
            var webhookUrl = _mollie.ResolveWebhookUrl();
            var amountValue = price.ToString("0.00", CultureInfo.InvariantCulture);
            var metadata = new Dictionary<string, string>
            {
                ["purpose"] = "deep_test",
                ["checkoutId"] = checkoutId.ToString("D"),
                ["kind"] = AssessmentKindLabels.ToSlug(kind)
            };

            MolliePaymentSnapshot payment;
            try
            {
                payment = await _mollie.CreatePaymentAsync(
                    new MollieCreatePaymentRequest(
                        amountValue,
                        DeepAnalysisPricing.DescriptionNl(kind)[..Math.Min(255, DeepAnalysisPricing.DescriptionNl(kind).Length)],
                        redirectUrl,
                        webhookUrl,
                        MolliePaymentMethods.PrimaryMethods.ToArray(),
                        MapMollieLocale(uiLocale),
                        metadata),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mollie deep-test create failed for user {UserId}", userId);
                throw new InvalidOperationException(PaymentsUnavailableCode, ex);
            }

            if (string.IsNullOrWhiteSpace(payment.Id) || string.IsNullOrWhiteSpace(payment.CheckoutUrl))
            {
                throw new InvalidOperationException(PaymentsUnavailableCode);
            }

            paymentId = payment.Id;
            checkoutUrl = payment.CheckoutUrl;
        }

        var checkout = new DeepAnalysisCheckout
        {
            Id = checkoutId,
            UserId = userId,
            Kind = kind,
            PaymentId = paymentId,
            AmountEuro = price,
            AmountExVatCents = money.ExVatCents,
            VatAmountCents = money.VatCents,
            TotalAmountCents = money.TotalCents,
            IsStub = isStub,
            WaiverAcceptedAtUtc = now,
            WaiverTextVersion = DeepAnalysisPricing.WaiverTextVersion,
            Locale = uiLocale,
            ExpiresAtUtc = now.AddHours(48),
            Status = DeepAnalysisCheckoutStatus.Pending,
            CreatedAtUtc = now
        };
        _db.DeepAnalysisCheckouts.Add(checkout);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deep-test checkout {CheckoutId} kind={Kind} stub={Stub} totalCents={Total} user={UserId}",
            checkoutId, kind, isStub, money.TotalCents, userId);

        return new DeepTestCheckoutCreateResult(
            checkoutId,
            checkoutUrl,
            money.TotalCents,
            isStub,
            kind,
            paymentId);
    }

    public async Task<DeepTestCheckoutStatusDto> GetStatusAsync(
        Guid userId,
        Guid checkoutId,
        CancellationToken cancellationToken = default)
    {
        var checkout = await _db.DeepAnalysisCheckouts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == checkoutId, cancellationToken);
        if (checkout is null || checkout.UserId != userId)
        {
            throw new KeyNotFoundException("not_found");
        }

        if (checkout.Status == DeepAnalysisCheckoutStatus.Pending && !checkout.IsStub)
        {
            await TryFulfillAsync(checkoutId, DeepTestFulfillSource.Return, cancellationToken);
            checkout = await _db.DeepAnalysisCheckouts.AsNoTracking()
                .FirstAsync(c => c.Id == checkoutId, cancellationToken);
        }

        string? invoiceNumber = null;
        if (checkout.InvoiceId is Guid invId)
        {
            invoiceNumber = await _db.ConsumerPurchaseInvoices.AsNoTracking()
                .Where(i => i.Id == invId)
                .Select(i => i.InvoiceNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new DeepTestCheckoutStatusDto(
            checkout.Id,
            MapStatus(checkout.Status),
            checkout.Kind,
            AssessmentKindLabels.ToSlug(checkout.Kind),
            checkout.TotalAmountCents > 0
                ? checkout.TotalAmountCents
                : TokenVatPricing.ToCents(checkout.AmountEuro),
            checkout.PaidAtUtc,
            invoiceNumber,
            checkout.InvoiceId,
            checkout.IsStub,
            checkout.PaymentMethod);
    }

    public async Task<DeepTestFulfillResult> TryFulfillAsync(
        Guid checkoutId,
        DeepTestFulfillSource source,
        CancellationToken cancellationToken = default)
    {
        // Relational providers: transaction + optional FOR UPDATE. In-memory tests skip both.
        IDbContextTransaction? tx = null;
        if (_db.Database.IsRelational())
        {
            tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            DeepAnalysisCheckout? checkout = null;
            if (_db.Database.IsRelational())
            {
                try
                {
                    checkout = await _db.DeepAnalysisCheckouts
                        .FromSqlInterpolated(
                            $"SELECT * FROM \"DeepAnalysisCheckouts\" WHERE \"Id\" = {checkoutId} FOR UPDATE")
                        .FirstOrDefaultAsync(cancellationToken);
                }
                catch
                {
                    checkout = null;
                }
            }

            checkout ??= await _db.DeepAnalysisCheckouts
                .FirstOrDefaultAsync(c => c.Id == checkoutId, cancellationToken);
            if (checkout is null)
            {
                if (tx is not null)
                {
                    await tx.RollbackAsync(cancellationToken);
                }

                return new DeepTestFulfillResult(false, "not_found", checkoutId, false, null);
            }

            if (checkout.Status == DeepAnalysisCheckoutStatus.Paid)
            {
                var ensured = await EnsurePaidSideEffectsAsync(checkout, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                if (tx is not null)
                {
                    await tx.CommitAsync(cancellationToken);
                }

                return ensured;
            }

            if (checkout.Status is not DeepAnalysisCheckoutStatus.Pending)
            {
                if (tx is not null)
                {
                    await tx.CommitAsync(cancellationToken);
                }

                return new DeepTestFulfillResult(false, MapStatus(checkout.Status), checkout.Id, false, null);
            }

            if (checkout.IsStub)
            {
                if (!AllowStubPayments() || source != DeepTestFulfillSource.Stub)
                {
                    if (tx is not null)
                    {
                        await tx.CommitAsync(cancellationToken);
                    }

                    return new DeepTestFulfillResult(false, "pending", checkout.Id, false, null);
                }

                checkout.Status = DeepAnalysisCheckoutStatus.Paid;
                checkout.PaidAtUtc = DateTime.UtcNow;
                checkout.ProviderStatus = "paid";
                checkout.PaymentMethod ??= "stub";
                var stubResult = await EnsurePaidSideEffectsAsync(checkout, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                if (tx is not null)
                {
                    await tx.CommitAsync(cancellationToken);
                }

                return stubResult;
            }

            MolliePaymentSnapshot payment;
            try
            {
                payment = await _mollie.FetchPaymentAsync(checkout.PaymentId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Deep-test Mollie fetch failed for checkout {CheckoutId}", checkoutId);
                if (tx is not null)
                {
                    await tx.CommitAsync(cancellationToken);
                }

                return new DeepTestFulfillResult(false, "pending", checkout.Id, false, null);
            }

            checkout.ProviderStatus = payment.Status;
            var method = MolliePaymentMethods.NormalizeOrNull(payment.Method);
            if (method is not null)
            {
                checkout.PaymentMethod = method;
            }

            if (payment.Status is "paid")
            {
                checkout.Status = DeepAnalysisCheckoutStatus.Paid;
                checkout.PaidAtUtc = DateTime.UtcNow;
                var paidResult = await EnsurePaidSideEffectsAsync(checkout, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                if (tx is not null)
                {
                    await tx.CommitAsync(cancellationToken);
                }

                return paidResult;
            }

            if (payment.Status is "failed")
            {
                checkout.Status = DeepAnalysisCheckoutStatus.Failed;
                checkout.FailedAtUtc = DateTime.UtcNow;
            }
            else if (payment.Status is "canceled" or "cancelled")
            {
                checkout.Status = DeepAnalysisCheckoutStatus.Cancelled;
                checkout.FailedAtUtc = DateTime.UtcNow;
            }
            else if (payment.Status is "expired")
            {
                checkout.Status = DeepAnalysisCheckoutStatus.Expired;
                checkout.FailedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
            if (tx is not null)
            {
                await tx.CommitAsync(cancellationToken);
            }

            return new DeepTestFulfillResult(false, MapStatus(checkout.Status), checkout.Id, false, null);
        }
        finally
        {
            if (tx is not null)
            {
                await tx.DisposeAsync();
            }
        }
    }

    private async Task<DeepTestCheckoutCreateResult> CreateTestUnlockAsync(
        Guid userId,
        AssessmentKind kind,
        string? locale,
        CancellationToken cancellationToken)
    {
        var checkoutId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var checkout = new DeepAnalysisCheckout
        {
            Id = checkoutId,
            UserId = userId,
            Kind = kind,
            PaymentId = $"test_deep_{Guid.NewGuid():N}",
            AmountEuro = 0,
            AmountExVatCents = 0,
            VatAmountCents = 0,
            TotalAmountCents = 0,
            PaymentMethod = DeepTestFinanceRules.TestUnlockMethod,
            ProviderStatus = "test",
            IsStub = true,
            WaiverAcceptedAtUtc = now,
            WaiverTextVersion = DeepAnalysisPricing.WaiverTextVersion,
            Locale = NormalizeLocale(locale),
            ExpiresAtUtc = now.AddHours(48),
            Status = DeepAnalysisCheckoutStatus.Paid,
            PaidAtUtc = now,
            CreatedAtUtc = now
        };
        _db.DeepAnalysisCheckouts.Add(checkout);
        await _deep.UnlockForUserAsync(userId, kind, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deep-test test unlock {CheckoutId} kind={Kind} user={UserId}",
            checkoutId, kind, userId);

        return new DeepTestCheckoutCreateResult(
            checkoutId,
            $"/candidate/deep-analysis/checkout?checkoutId={checkoutId:D}",
            0,
            true,
            kind,
            checkout.PaymentId);
    }

    private async Task CancelOpenCheckoutsAsync(
        Guid userId,
        AssessmentKind kind,
        CancellationToken cancellationToken)
    {
        var open = await _db.DeepAnalysisCheckouts
            .Where(c => c.UserId == userId && c.Kind == kind && c.Status == DeepAnalysisCheckoutStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var prior in open)
        {
            prior.Status = DeepAnalysisCheckoutStatus.Cancelled;
            prior.FailedAtUtc = DateTime.UtcNow;
            if (!prior.IsStub
                && !DeepTestFinanceRules.IsTestUnlock(prior.PaymentMethod)
                && !string.IsNullOrWhiteSpace(prior.PaymentId))
            {
                await _mollie.TryCancelPaymentAsync(prior.PaymentId, cancellationToken);
            }
        }
    }

    private async Task<DeepTestFulfillResult> EnsurePaidSideEffectsAsync(
        DeepAnalysisCheckout checkout,
        CancellationToken cancellationToken)
    {
        if (DeepTestFinanceRules.IsTestUnlock(checkout.PaymentMethod))
        {
            await _deep.UnlockForUserAsync(checkout.UserId, checkout.Kind, cancellationToken);
            return new DeepTestFulfillResult(false, "paid", checkout.Id, true, null);
        }

        var alreadyUnlocked = await _db.CandidateDeepAnalyses.AsNoTracking()
            .AnyAsync(
                d => d.UserId == checkout.UserId
                     && d.Kind == checkout.Kind
                     && (d.Status == CandidateDeepAnalysisStatuses.Draft
                         || d.Status == CandidateDeepAnalysisStatuses.Completed),
                cancellationToken);

        await _deep.UnlockForUserAsync(checkout.UserId, checkout.Kind, cancellationToken);

        if (alreadyUnlocked)
        {
            _db.PlatformLogs.Add(new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Warning,
                Category = "DeepTestPayment",
                Message = "manual refund",
                DetailsJson = JsonSerializer.Serialize(new
                {
                    checkoutId = checkout.Id,
                    kind = checkout.Kind.ToString(),
                    paymentId = checkout.PaymentId
                }),
                CreatedAt = DateTime.UtcNow
            });
        }

        var invoice = await _invoices.CreateForDeepCheckoutAsync(checkout.Id, cancellationToken);
        checkout.InvoiceId = invoice.Id;
        try
        {
            await _vatBuffer.QueueForInvoiceAsync(invoice, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "VAT buffer queue failed for consumer invoice {InvoiceId}", invoice.Id);
        }

        await TrySendReceiptAsync(checkout, invoice, cancellationToken);

        return new DeepTestFulfillResult(
            true,
            "paid",
            checkout.Id,
            true,
            invoice.InvoiceNumber);
    }

    private async Task TrySendReceiptAsync(
        DeepAnalysisCheckout checkout,
        ConsumerPurchaseInvoice invoice,
        CancellationToken cancellationToken)
    {
        if (checkout.ReceiptSentAtUtc is not null)
        {
            return;
        }

        if (checkout.ReceiptSendAttempts >= 5)
        {
            return;
        }

        checkout.ReceiptSendAttempts++;
        try
        {
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == checkout.UserId, cancellationToken);
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
            {
                return;
            }

            var features = await _features.GetAsync(cancellationToken);
            var culture = EmailCulture.ForLanguage(checkout.Locale);
            var pdf = await _invoices.RenderPdfAsync(invoice.Id, culture.Language, cancellationToken);
            var composed = TransactionalEmails.DeepTestReceipt(
                features.PublicWebBaseUrl,
                user.FullName,
                DeepAnalysisPricing.TestNameNl(checkout.Kind),
                TokenVatPricing.FromCents(invoice.TotalAmountCents),
                invoice.IssuedAt,
                invoice.InvoiceNumber,
                AssessmentKindLabels.ToSlug(checkout.Kind),
                culture,
                checkout.WaiverTextVersion);

            var outcome = await _mailer.SendAsync(
                composed,
                user.Email,
                new EmailSendOptions(
                    BypassSuppression: true,
                    IdempotencyKey: $"deep_test_receipt:{checkout.Id:D}",
                    Culture: culture,
                    Attachments:
                    [
                        new EmailAttachment(
                            $"{invoice.InvoiceNumber}.pdf",
                            "application/pdf",
                            pdf)
                    ]),
                cancellationToken);

            if (outcome.Sent || outcome.Suppressed)
            {
                checkout.ReceiptSentAtUtc = DateTime.UtcNow;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Deep-test receipt mail failed for checkout {CheckoutId}", checkout.Id);
            if (checkout.ReceiptSendAttempts >= 5)
            {
                _db.PlatformLogs.Add(new PlatformLog
                {
                    Id = Guid.NewGuid(),
                    Level = PlatformLogLevel.Error,
                    Category = "DeepTestReceipt",
                    Message = $"Receipt mail gave up after 5 tries for checkout {checkout.Id}",
                    DetailsJson = JsonSerializer.Serialize(new { checkout.Id, error = ex.GetType().Name }),
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }


    private bool AllowStubPayments() =>
        _environment.IsDevelopment()
        || _configuration.GetValue("JobsyAuth:AllowStubPayments", false);

    private static string MapStatus(DeepAnalysisCheckoutStatus status) => status switch
    {
        DeepAnalysisCheckoutStatus.Paid => "paid",
        DeepAnalysisCheckoutStatus.Failed => "failed",
        DeepAnalysisCheckoutStatus.Cancelled => "cancelled",
        DeepAnalysisCheckoutStatus.Expired => "expired",
        _ => "pending"
    };

    private static string NormalizeLocale(string? locale)
    {
        var lang = (locale ?? "nl").Trim().ToLowerInvariant();
        if (lang.StartsWith("nl", StringComparison.Ordinal)) return "nl";
        if (lang.StartsWith("en", StringComparison.Ordinal)) return "en";
        if (lang.StartsWith("pl", StringComparison.Ordinal)) return "pl";
        if (lang.StartsWith("ro", StringComparison.Ordinal)) return "ro";
        if (lang.StartsWith("ar", StringComparison.Ordinal)) return "ar";
        return "en";
    }

    private static string MapMollieLocale(string uiLocale) => uiLocale switch
    {
        "nl" => "nl_NL",
        "en" => "en_US",
        "pl" => "pl_PL",
        _ => "en_US"
    };
}
