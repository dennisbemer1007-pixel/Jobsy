using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Shared Mollie HTTP client used by token packs and deep-test checkouts.
/// </summary>
public sealed class MollieApiClient : IMollieApiClient
{
    public const string HttpClientName = MolliePaymentService.HttpClientName;
    public const string DefaultApiBaseUrl = MolliePaymentService.DefaultApiBaseUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IIntegrationCredentialService _credentials;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<MollieApiClient> _logger;

    public MollieApiClient(
        IIntegrationCredentialService credentials,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<MollieApiClient> logger)
    {
        _credentials = credentials;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task<bool> TryGetApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var secrets = await _credentials.GetSecretsAsync(IntegrationKey.Mollie, cancellationToken);
        var apiKey = secrets?.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        EnsureLiveKeyOutsideDevelopment(apiKey);
        return true;
    }

    public string? ResolveWebhookUrl()
    {
        var apiBase = FirstNonEmpty(
            _configuration["PublicApiBaseUrl"],
            _configuration["RENDER_EXTERNAL_URL"]);
        if (string.IsNullOrWhiteSpace(apiBase))
        {
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

    public string? ResolvePublicWebBaseUrl(string? configuredPublicWebBaseUrl)
    {
        var raw = FirstNonEmpty(configuredPublicWebBaseUrl, _configuration["PublicWebBaseUrl"]);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return JobsyPublicUrl.NormalizeOrigin(raw)?.TrimEnd('/');
    }

    public async Task<MolliePaymentSnapshot> FetchPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await SendMollieAsync<MolliePaymentResponse>(
            HttpMethod.Get,
            $"payments/{Uri.EscapeDataString(paymentId)}",
            body: null,
            cancellationToken);
        return ToSnapshot(payment);
    }

    public async Task<MolliePaymentSnapshot> CreatePaymentAsync(
        MollieCreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        // String = pin to that method; array = Mollie shows only those methods (token behaviour).
        object methodPayload = request.Methods.Count == 1
            ? request.Methods[0]
            : request.Methods.ToArray();
        var createBody = new Dictionary<string, object?>
        {
            ["amount"] = new Dictionary<string, string>
            {
                ["currency"] = "EUR",
                ["value"] = request.AmountValue
            },
            ["description"] = request.Description,
            ["redirectUrl"] = request.RedirectUrl,
            ["metadata"] = request.Metadata,
            ["method"] = methodPayload
        };
        if (!string.IsNullOrWhiteSpace(request.WebhookUrl))
        {
            createBody["webhookUrl"] = request.WebhookUrl;
        }

        if (!string.IsNullOrWhiteSpace(request.Locale))
        {
            createBody["locale"] = request.Locale;
        }

        var payment = await SendMollieAsync<MolliePaymentResponse>(
            HttpMethod.Post,
            "payments",
            createBody,
            cancellationToken);
        return ToSnapshot(payment);
    }

    public async Task TryCancelPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentId)
            || paymentId.StartsWith("stub_", StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            await SendMollieAsync<object>(
                HttpMethod.Delete,
                $"payments/{Uri.EscapeDataString(paymentId)}",
                body: null,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Mollie cancel payment best-effort failed for {PaymentId}", paymentId);
        }
    }

    public void EnsureLiveKeyOutsideDevelopment(string apiKey)
    {
        if (_environment.IsDevelopment())
        {
            return;
        }

        if (apiKey.StartsWith("test_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Mollie test-mode keys (test_…) zijn niet toegestaan buiten Development. Gebruik een live_ key in Production.");
        }
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
        EnsureLiveKeyOutsideDevelopment(apiKey);

        var rawBase = string.IsNullOrWhiteSpace(secrets.BaseUrl) ? DefaultApiBaseUrl : secrets.BaseUrl;
        if (!IntegrationEndpointUrl.TryNormalizeBaseUrl(rawBase, out var baseUrl, out var error)
            || string.IsNullOrWhiteSpace(baseUrl))
        {
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

        if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(raw))
        {
            return default!;
        }

        var parsed = JsonSerializer.Deserialize<T>(raw, JsonOptions);
        return parsed ?? throw new InvalidOperationException("Lege Mollie-response.");
    }

    private static MolliePaymentSnapshot ToSnapshot(MolliePaymentResponse payment)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (payment.Metadata is not null)
        {
            foreach (var (key, value) in payment.Metadata)
            {
                if (!string.IsNullOrWhiteSpace(key) && value is not null)
                {
                    metadata[key] = value.ToString() ?? "";
                }
            }
        }

        return new MolliePaymentSnapshot(
            payment.Id ?? "",
            string.IsNullOrWhiteSpace(payment.Status) ? "unknown" : payment.Status.Trim().ToLowerInvariant(),
            payment.Method,
            payment.Amount?.Value,
            payment.AmountRefunded?.Value,
            payment.AmountChargedBack?.Value,
            payment.Links?.Checkout?.Href,
            metadata);
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
        public string? Method { get; set; }
        public MollieAmountDto? Amount { get; set; }
        public MollieAmountDto? AmountRefunded { get; set; }
        public MollieAmountDto? AmountChargedBack { get; set; }
        public Dictionary<string, object?>? Metadata { get; set; }

        [JsonPropertyName("_links")]
        public MollieLinks? Links { get; set; }
    }

    private sealed class MollieAmountDto
    {
        public string? Currency { get; set; }
        public string? Value { get; set; }
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
