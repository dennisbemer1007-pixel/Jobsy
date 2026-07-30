using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly JobsyDbContext _db;
    private readonly IIntegrationCredentialService _credentials;
    private readonly IPlatformFeatureService _features;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly MolliePaymentStub _stub;
    private readonly ILogger<MolliePaymentService> _logger;

    public MolliePaymentService(
        JobsyDbContext db,
        IIntegrationCredentialService credentials,
        IPlatformFeatureService features,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IHostEnvironment environment,
        MolliePaymentStub stub,
        ILogger<MolliePaymentService> logger)
    {
        _db = db;
        _credentials = credentials;
        _features = features;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _environment = environment;
        _stub = stub;
        _logger = logger;
    }

    public async Task<PaymentCheckoutResult> CreateTokenPurchaseCheckoutAsync(
        Guid companyId,
        int packSize,
        CancellationToken cancellationToken = default)
    {
        if (!await TryGetApiKeyAsync(cancellationToken))
        {
            if (_environment.IsDevelopment())
            {
                return await _stub.CreateTokenPurchaseCheckoutAsync(companyId, packSize, cancellationToken);
            }

            throw new InvalidOperationException(
                "Betalingen zijn niet geconfigureerd. Sla een Mollie API-key op onder Admin → Integraties.");
        }

        if (packSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(packSize));
        }

        _ = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new InvalidOperationException("Company not found.");

        var price = await ResolvePackPriceAsync(packSize, cancellationToken);
        var checkoutId = Guid.NewGuid();
        var features = await _features.GetAsync(cancellationToken);
        var webBase = features.PublicWebBaseUrl.TrimEnd('/');
        var redirectUrl = $"{webBase}/tokens/checkout-return?checkoutId={checkoutId:D}";
        var webhookUrl = ResolveWebhookUrl();

        var amountValue = price.ToString("0.00", CultureInfo.InvariantCulture);
        var createBody = new Dictionary<string, object?>
        {
            ["amount"] = new Dictionary<string, string>
            {
                ["currency"] = "EUR",
                ["value"] = amountValue
            },
            ["description"] = $"Lobsy tokens ({packSize})",
            ["redirectUrl"] = redirectUrl,
            ["metadata"] = new Dictionary<string, string>
            {
                ["checkoutId"] = checkoutId.ToString("D"),
                ["companyId"] = companyId.ToString("D"),
                ["packSize"] = packSize.ToString(CultureInfo.InvariantCulture)
            }
        };
        if (!string.IsNullOrWhiteSpace(webhookUrl))
        {
            createBody["webhookUrl"] = webhookUrl;
        }

        MolliePaymentResponse payment;
        try
        {
            payment = await SendMollieAsync<MolliePaymentResponse>(
                HttpMethod.Post,
                "payments",
                createBody,
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

        var checkoutUrl = payment.Links?.Checkout?.Href;
        if (string.IsNullOrWhiteSpace(checkoutUrl))
        {
            throw new InvalidOperationException("Mollie gaf geen checkout-URL terug.");
        }

        _db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = checkoutId,
            PaymentId = payment.Id,
            CompanyId = companyId,
            PackSize = packSize,
            AmountEuro = price,
            Status = TokenPurchaseCheckoutStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Mollie checkout for company {CompanyId}: {Pack} tokens = €{Price} ({PaymentId})",
            companyId, packSize, price, payment.Id);

        return new PaymentCheckoutResult(
            payment.Id,
            checkoutUrl,
            packSize,
            price,
            IsStub: false);
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

        if (session.Status is TokenPurchaseCheckoutStatus.Paid or TokenPurchaseCheckoutStatus.Credited)
        {
            return new PaymentStatusResult(
                paymentId,
                session.Status.ToString().ToLowerInvariant(),
                IsPaid: true);
        }

        if (!await TryGetApiKeyAsync(cancellationToken))
        {
            return new PaymentStatusResult(
                paymentId,
                session.Status.ToString().ToLowerInvariant(),
                IsPaid: false);
        }

        MolliePaymentResponse payment;
        try
        {
            payment = await SendMollieAsync<MolliePaymentResponse>(
                HttpMethod.Get,
                $"payments/{Uri.EscapeDataString(paymentId)}",
                body: null,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mollie get payment failed for {PaymentId}", paymentId);
            return new PaymentStatusResult(paymentId, "provider_error", IsPaid: false);
        }

        var status = string.IsNullOrWhiteSpace(payment.Status) ? "unknown" : payment.Status.Trim().ToLowerInvariant();
        var isPaid = status is "paid";

        if (isPaid && session.Status == TokenPurchaseCheckoutStatus.Pending)
        {
            session.Status = TokenPurchaseCheckoutStatus.Paid;
            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (status is "canceled" or "cancelled" or "expired" or "failed"
                 && session.Status == TokenPurchaseCheckoutStatus.Pending)
        {
            session.Status = TokenPurchaseCheckoutStatus.Cancelled;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new PaymentStatusResult(paymentId, status, IsPaid: isPaid);
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

    private async Task<bool> TryGetApiKeyAsync(CancellationToken cancellationToken)
    {
        var secrets = await _credentials.GetSecretsAsync(IntegrationKey.Mollie, cancellationToken);
        return !string.IsNullOrWhiteSpace(secrets?.ApiKey);
    }

    private string? ResolveWebhookUrl()
    {
        var apiBase = FirstNonEmpty(
            _configuration["PublicApiBaseUrl"],
            _configuration["RENDER_EXTERNAL_URL"]);
        if (string.IsNullOrWhiteSpace(apiBase))
        {
            // Localhost webhooks are unreachable for Mollie; redirect-return still verifies status.
            return null;
        }

        var origin = JobsyPublicUrl.NormalizeOrigin(apiBase);
        if (string.IsNullOrWhiteSpace(origin))
        {
            return null;
        }

        if (origin.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || origin.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return origin.TrimEnd('/') + "/api/webhooks/mollie";
    }

    private async Task<T> SendMollieAsync<T>(
        HttpMethod method,
        string relativePath,
        object? body,
        CancellationToken cancellationToken)
    {
        var secrets = await _credentials.GetSecretsAsync(IntegrationKey.Mollie, cancellationToken)
            ?? throw new InvalidOperationException("Geen Mollie API-key geconfigureerd.");
        var apiKey = secrets.ApiKey?.Trim()
            ?? throw new InvalidOperationException("Geen Mollie API-key geconfigureerd.");

        var rawBase = string.IsNullOrWhiteSpace(secrets.BaseUrl) ? DefaultApiBaseUrl : secrets.BaseUrl;
        if (!IntegrationEndpointUrl.TryNormalizeBaseUrl(rawBase, out var baseUrl, out var error)
            || string.IsNullOrWhiteSpace(baseUrl))
        {
            // Allow default Mollie host even when admin BaseUrl empty; block private hosts otherwise.
            if (string.IsNullOrWhiteSpace(secrets.BaseUrl))
            {
                baseUrl = DefaultApiBaseUrl;
            }
            else
            {
                throw new InvalidOperationException(error ?? "Ongeldige Mollie Base URL.");
            }
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(method, new Uri(new Uri(baseUrl), relativePath));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        using var response = await client.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = raw.Length > 240 ? raw[..240] : raw;
            throw new InvalidOperationException(
                $"Mollie {(int)response.StatusCode}: {detail}");
        }

        var parsed = JsonSerializer.Deserialize<T>(raw, JsonOptions);
        return parsed ?? throw new InvalidOperationException("Lege Mollie-response.");
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private sealed class MolliePaymentResponse
    {
        public string? Id { get; set; }
        public string? Status { get; set; }

        [JsonPropertyName("_links")]
        public MollieLinks? Links { get; set; }
    }

    private sealed class MollieLinks
    {
        public MollieLink? Checkout { get; set; }
    }

    private sealed class MollieLink
    {
        public string? Href { get; set; }
    }
}
