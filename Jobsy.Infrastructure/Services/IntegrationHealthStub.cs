using System.Net.Http.Headers;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class IntegrationHealthStub : IIntegrationHealthService
{
    private readonly IIntegrationCredentialService _credentials;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiOptions _openAiOptions;
    private readonly ILogger<IntegrationHealthStub> _logger;

    public IntegrationHealthStub(
        IIntegrationCredentialService credentials,
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiOptions> openAiOptions,
        ILogger<IntegrationHealthStub> logger)
    {
        _credentials = credentials;
        _httpClientFactory = httpClientFactory;
        _openAiOptions = openAiOptions.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<IntegrationHealthResult>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var views = await _credentials.GetConfigurableAsync(cancellationToken);
        var now = DateTime.UtcNow;
        return views.Select(v => ToHealth(v, now)).ToList();
    }

    public async Task<IntegrationHealthResult> PingAsync(
        IntegrationKey key,
        CancellationToken cancellationToken = default)
    {
        var view = await _credentials.GetAsync(key, cancellationToken)
            ?? throw new InvalidOperationException($"Onbekende integratie '{key}'.");
        return ToHealth(view, DateTime.UtcNow);
    }

    public async Task<IntegrationHealthResult> TestConnectionAsync(
        IntegrationKey key,
        CancellationToken cancellationToken = default)
    {
        var (ok, message) = await RunLiveTestAsync(key, cancellationToken);
        await _credentials.SavePingResultAsync(key, ok, message, cancellationToken);
        var view = await _credentials.GetAsync(key, cancellationToken);
        return new IntegrationHealthResult(
            key,
            IntegrationCredentialService.DisplayName(key),
            ok,
            message,
            DateTime.UtcNow,
            ok);
    }

    private async Task<(bool Ok, string Message)> RunLiveTestAsync(
        IntegrationKey key,
        CancellationToken cancellationToken)
    {
        try
        {
            return key switch
            {
                IntegrationKey.OpenAI => await TestOpenAiAsync(cancellationToken),
                IntegrationKey.Mollie => await TestMollieAsync(cancellationToken),
                IntegrationKey.Kvk => await TestConfiguredAsync(key, "KvK API-key", cancellationToken),
                IntegrationKey.Mail => await TestMailAsync(cancellationToken),
                IntegrationKey.PostcodeCheck => await TestConfiguredAsync(key, "Postcode API-key", cancellationToken),
                IntegrationKey.MicrosoftEntra => await TestOAuthAsync(key, requireTenant: true, cancellationToken),
                IntegrationKey.GoogleEntra => await TestOAuthAsync(key, requireTenant: false, cancellationToken),
                _ => (false, "Geen test beschikbaar voor deze integratie.")
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Integratietest {Key} mislukt", key);
            return (false, $"Geen verbinding: {ex.Message}");
        }
    }

    private async Task<(bool Ok, string Message)> TestOpenAiAsync(CancellationToken cancellationToken)
    {
        var secrets = await _credentials.GetSecretsAsync(IntegrationKey.OpenAI, cancellationToken);
        var apiKey = secrets?.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = string.IsNullOrWhiteSpace(_openAiOptions.ApiKey) ? null : _openAiOptions.ApiKey.Trim();
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return (false, "Geen API-key geconfigureerd.");
        }

        var baseUrl = secrets?.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = string.IsNullOrWhiteSpace(_openAiOptions.BaseUrl)
                ? "https://api.openai.com/v1/"
                : _openAiOptions.BaseUrl;
        }

        if (!IntegrationEndpointUrl.TryNormalizeBaseUrl(baseUrl, out var normalized, out var error)
            || string.IsNullOrWhiteSpace(normalized))
        {
            return (false, error ?? "Ongeldige OpenAI Base URL.");
        }

        var client = _httpClientFactory.CreateClient("IntegrationProbe");
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(normalized), "models"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return (true, "Verbinding met OpenAI OK.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return (false, $"OpenAI gaf {(int)response.StatusCode}: {(body.Length > 180 ? body[..180] : body)}");
    }

    private async Task<(bool Ok, string Message)> TestMollieAsync(CancellationToken cancellationToken)
    {
        var secrets = await _credentials.GetSecretsAsync(IntegrationKey.Mollie, cancellationToken);
        if (string.IsNullOrWhiteSpace(secrets?.ApiKey))
        {
            return (false, "Geen Mollie API-key geconfigureerd.");
        }

        var rawBase = string.IsNullOrWhiteSpace(secrets.BaseUrl)
            ? "https://api.mollie.com/v2/"
            : secrets.BaseUrl;
        if (!IntegrationEndpointUrl.TryNormalizeBaseUrl(rawBase, out var baseUrl, out var error)
            || string.IsNullOrWhiteSpace(baseUrl))
        {
            return (false, error ?? "Ongeldige Mollie Base URL.");
        }

        var client = _httpClientFactory.CreateClient("IntegrationProbe");
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(baseUrl), "permissions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secrets.ApiKey);
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return (true, "Verbinding met Mollie OK.");
        }

        // Test keys / stubs may 401 on live API — still report clearly.
        return (false, $"Mollie gaf {(int)response.StatusCode} — controleer de API-key.");
    }

    private async Task<(bool Ok, string Message)> TestMailAsync(CancellationToken cancellationToken)
    {
        var secrets = await _credentials.GetSecretsAsync(IntegrationKey.Mail, cancellationToken);
        var hasTransport = !string.IsNullOrWhiteSpace(secrets?.ApiKey)
            || (!string.IsNullOrWhiteSpace(secrets?.BaseUrl)
                && !string.IsNullOrWhiteSpace(secrets.ClientId)
                && !string.IsNullOrWhiteSpace(secrets.ClientSecret));
        if (!hasTransport)
        {
            return (false, "Configureer API-key of SMTP (host + gebruiker + wachtwoord).");
        }

        if (string.IsNullOrWhiteSpace(secrets?.FromAddress))
        {
            return (false, "Vul een afzenderadres (From) in.");
        }

        return (true, "Mailconfig compleet (stub verstuurt naar PlatformLog tot SMTP live is).");
    }

    private async Task<(bool Ok, string Message)> TestOAuthAsync(
        IntegrationKey key,
        bool requireTenant,
        CancellationToken cancellationToken)
    {
        var secrets = await _credentials.GetSecretsAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(secrets?.ClientId) || string.IsNullOrWhiteSpace(secrets.ClientSecret))
        {
            return (false, "Client ID en Client Secret zijn verplicht.");
        }

        if (requireTenant && string.IsNullOrWhiteSpace(secrets.TenantId))
        {
            return (false, "Tenant ID is verplicht voor Microsoft Entra.");
        }

        var label = IntegrationCredentialService.DisplayName(key);
        return (true, $"{label}-credentials aanwezig. Herstart de Web-app om login te activeren.");
    }

    private async Task<(bool Ok, string Message)> TestConfiguredAsync(
        IntegrationKey key,
        string label,
        CancellationToken cancellationToken)
    {
        var secrets = await _credentials.GetSecretsAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(secrets?.ApiKey))
        {
            return (false, $"Geen {label} geconfigureerd.");
        }

        return (true, $"{label} opgeslagen (stub — live ping volgt bij echte API-koppeling).");
    }

    private static IntegrationHealthResult ToHealth(IntegrationCredentialView view, DateTime now)
    {
        if (view.LastPingOk is bool ok)
        {
            return new IntegrationHealthResult(
                view.Key,
                view.DisplayName,
                ok,
                view.LastPingMessage ?? (ok ? "Laatste test OK." : "Laatste test mislukt."),
                view.LastPingAtUtc ?? now,
                ok);
        }

        var configured = IsConfigured(view);
        return new IntegrationHealthResult(
            view.Key,
            view.DisplayName,
            false,
            configured
                ? (view.LastPingMessage ?? "Geconfigureerd — nog niet getest.")
                : "Niet geconfigureerd.",
            view.LastPingAtUtc ?? now,
            null);
    }

    private static bool IsConfigured(IntegrationCredentialView view)
    {
        if (view.SupportsApiKey && view.HasApiKey)
        {
            return true;
        }

        if (view.SupportsOAuth && !string.IsNullOrWhiteSpace(view.ClientId) && view.HasClientSecret)
        {
            return true;
        }

        return false;
    }
}
