using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services.Letters;

/// <summary>
/// Pingen API v2 letter sender (OAuth2 client credentials). PDFs are not retained after send.
/// Only used when <see cref="CompanyVerificationSettings.LetterProvider"/> is Pingen
/// and credentials are configured — never the Dev/CI/Acc default.
/// </summary>
public sealed class PingenLetterService : ILetterService
{
    public const string HttpClientName = "PingenLetters";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<CompanyVerificationSettings> _options;
    private readonly ILogger<PingenLetterService> _logger;
    private readonly SemaphoreSlim _tokenGate = new(1, 1);
    private string? _accessToken;
    private DateTime _tokenExpiresAtUtc = DateTime.MinValue;

    public PingenLetterService(
        IHttpClientFactory httpClientFactory,
        IOptions<CompanyVerificationSettings> options,
        ILogger<PingenLetterService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public string ProviderName => "Pingen";

    public async Task<LetterSendResult> SendAsync(LetterRequest request, CancellationToken cancellationToken = default)
    {
        var settings = _options.Value;
        if (string.IsNullOrWhiteSpace(settings.PingenClientId)
            || string.IsNullOrWhiteSpace(settings.PingenClientSecret)
            || string.IsNullOrWhiteSpace(settings.PingenOrganisationId))
        {
            return new LetterSendResult(false, null, "pingen_not_configured", "Pingen credentials missing.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            client.BaseAddress ??= new Uri(ResolveApiBase(settings.PingenEnvironment));
            await EnsureTokenAsync(client, settings, cancellationToken);

            var fileId = await UploadPdfAsync(client, settings.PingenOrganisationId!, request.Pdf, request.Reference, cancellationToken);
            if (fileId is null)
            {
                return new LetterSendResult(false, null, "pingen_upload_failed", "PDF upload failed.");
            }

            var letterId = await CreateLetterAsync(
                client,
                settings,
                fileId,
                request,
                cancellationToken);
            if (letterId is null)
            {
                return new LetterSendResult(false, null, "pingen_create_failed", "Letter create failed.");
            }

            _logger.LogInformation(
                "Pingen letter created id={LetterId} ref={Reference} (PDF discarded, code not logged)",
                letterId,
                request.Reference);
            return new LetterSendResult(true, letterId, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pingen send failed for ref={Reference}", request.Reference);
            return new LetterSendResult(false, null, "pingen_error", "Letter provider error.");
        }
    }

    public async Task<LetterStatus> GetStatusAsync(string providerLetterId, CancellationToken cancellationToken = default)
    {
        var settings = _options.Value;
        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.BaseAddress ??= new Uri(ResolveApiBase(settings.PingenEnvironment));
        await EnsureTokenAsync(client, settings, cancellationToken);

        var org = Uri.EscapeDataString(settings.PingenOrganisationId ?? "");
        var id = Uri.EscapeDataString(providerLetterId);
        using var response = await client.GetAsync($"organisations/{org}/letters/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new LetterStatus(providerLetterId, LetterDeliveryStatus.Unknown, DateTime.UtcNow, response.StatusCode.ToString());
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var statusRaw = doc.RootElement
            .GetProperty("data")
            .GetProperty("attributes")
            .GetProperty("status")
            .GetString() ?? "";

        var mapped = statusRaw.ToLowerInvariant() switch
        {
            "sent" or "printing" or "print" => LetterDeliveryStatus.Sent,
            "delivered" or "posted" => LetterDeliveryStatus.Delivered,
            "undeliverable" or "failed" or "canceled" or "cancelled" => LetterDeliveryStatus.Undeliverable,
            "action_required" or "validating" or "queued" => LetterDeliveryStatus.Queued,
            _ => LetterDeliveryStatus.Unknown
        };
        return new LetterStatus(providerLetterId, mapped, DateTime.UtcNow, statusRaw);
    }

    internal static string ResolveApiBase(string? environment)
        => string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase)
            ? "https://api.pingen.com/"
            : "https://api-staging.pingen.com/";

    private async Task EnsureTokenAsync(
        HttpClient client,
        CompanyVerificationSettings settings,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && DateTime.UtcNow < _tokenExpiresAtUtc.AddMinutes(-1))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            return;
        }

        await _tokenGate.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken) && DateTime.UtcNow < _tokenExpiresAtUtc.AddMinutes(-1))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                return;
            }

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = settings.PingenClientId!,
                ["client_secret"] = settings.PingenClientSecret!,
                ["scope"] = "letter"
            });
            using var response = await client.PostAsync("auth/access-tokens", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Empty Pingen token response.");
            _accessToken = payload.AccessToken;
            _tokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, payload.ExpiresIn));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }
        finally
        {
            _tokenGate.Release();
        }
    }

    private static async Task<string?> UploadPdfAsync(
        HttpClient client,
        string organisationId,
        byte[] pdf,
        string reference,
        CancellationToken cancellationToken)
    {
        var org = Uri.EscapeDataString(organisationId);
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pdf);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", $"{reference}.pdf");
        using var response = await client.PostAsync($"organisations/{org}/file-upload", form, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (doc.RootElement.TryGetProperty("data", out var data)
            && data.TryGetProperty("id", out var id))
        {
            return id.GetString();
        }

        if (doc.RootElement.TryGetProperty("url", out _)
            && doc.RootElement.TryGetProperty("file_url", out var fileUrl))
        {
            // Some Pingen versions return a signed upload URL; treat file_url/id interchangeably.
            return fileUrl.GetString();
        }

        return doc.RootElement.TryGetProperty("id", out var topId) ? topId.GetString() : null;
    }

    private static async Task<string?> CreateLetterAsync(
        HttpClient client,
        CompanyVerificationSettings settings,
        string fileId,
        LetterRequest request,
        CancellationToken cancellationToken)
    {
        var org = Uri.EscapeDataString(settings.PingenOrganisationId!);
        var deliveryProduct = settings.PingenPreferFastDelivery ? "fast" : "cheap";
        var body = new
        {
            data = new
            {
                type = "letters",
                attributes = new
                {
                    file_original_name = $"{request.Reference}.pdf",
                    address_position = "left",
                    auto_send = true,
                    delivery_product = deliveryProduct,
                    print_mode = "simplex",
                    print_spectrum = "grayscale",
                    meta_data = new Dictionary<string, string> { ["reference"] = request.Reference }
                },
                relationships = new
                {
                    organisation = new
                    {
                        data = new { type = "organisations", id = settings.PingenOrganisationId }
                    },
                    file = new
                    {
                        data = new { type = "files", id = fileId }
                    }
                }
            }
        };

        using var response = await client.PostAsJsonAsync(
            $"organisations/{org}/letters",
            body,
            JsonOptions,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString();
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = "";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 3600;
    }
}
