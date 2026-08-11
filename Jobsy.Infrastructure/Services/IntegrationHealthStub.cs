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
    private readonly IEmailService _email;
    private readonly OpenAiOptions _openAiOptions;
    private readonly ILogger<IntegrationHealthStub> _logger;

    public IntegrationHealthStub(
        IIntegrationCredentialService credentials,
        IHttpClientFactory httpClientFactory,
        IEmailService email,
        IOptions<OpenAiOptions> openAiOptions,
        ILogger<IntegrationHealthStub> logger)
    {
        _credentials = credentials;
        _httpClientFactory = httpClientFactory;
        _email = email;
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

    public async Task<SendTestMailResult> SendTestMailAsync(
        string to,
        CancellationToken cancellationToken = default)
    {
        var trimmed = (to ?? string.Empty).Trim();
        if (!LooksLikeEmail(trimmed))
        {
            return new SendTestMailResult(
                false,
                false,
                "Vul een geldig e-mailadres in.");
        }

        var secrets = await _credentials.GetSecretsAsync(IntegrationKey.Mail, cancellationToken);
        var resendReady = SmtpEmailService.TryResolveResend(secrets, out _);
        var smtpReady = SmtpEmailService.TryResolveSmtp(secrets, out var smtp);
        var redacted = EmailServiceStub.RedactEmail(trimmed);
        var body =
            "<p>Dit is een testmail van Lobsy.</p>" +
            "<p>Als je dit bericht ziet, werkt de uitgaande mailconfiguratie.</p>";

        if (!resendReady && !smtpReady)
        {
            await _email.SendAsync(
                new EmailMessage(trimmed, "Lobsy testmail", body, "MailTest"),
                cancellationToken);
            var stubMessage =
                "Mail niet geconfigureerd. Vul Resend API-key + From in (aanbevolen op cloud), " +
                "of SMTP-host/gebruiker/app-wachtwoord/From. " +
                $"Testmail naar {redacted} is alleen in PlatformLog gelogd — niet echt verzonden.";
            await _credentials.SavePingResultAsync(IntegrationKey.Mail, false, stubMessage, cancellationToken);
            return new SendTestMailResult(false, false, stubMessage);
        }

        try
        {
            var delivery = await _email.SendAsync(
                new EmailMessage(trimmed, "Lobsy testmail", body, "MailTest"),
                cancellationToken);

            if (!delivery.DeliveredViaProvider)
            {
                var failMessage =
                    "Testmail niet afgeleverd via Resend/SMTP (providerfout — zie PlatformLogs). " +
                    "Bericht is alleen als stub gelogd.";
                await _credentials.SavePingResultAsync(IntegrationKey.Mail, false, failMessage, cancellationToken);
                return new SendTestMailResult(false, true, failMessage);
            }

            var via = resendReady
                ? "Resend API"
                : $"{smtp.Host}:{smtp.Port}";
            var okMessage = $"Testmail verzonden naar {redacted} via {via}.";
            await _credentials.SavePingResultAsync(IntegrationKey.Mail, true, okMessage, cancellationToken);
            return new SendTestMailResult(true, true, okMessage);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Testmail mislukt");
            var failMessage = ex is InvalidOperationException
                ? ex.Message
                : (smtpReady
                    ? SmtpEmailService.FormatSmtpError(ex, smtp)
                    : Truncate(ex.Message, 280));
            if (smtpReady
                && !resendReady
                && !failMessage.Contains(smtp.Host, StringComparison.OrdinalIgnoreCase))
            {
                failMessage = $"Testmail mislukt via {smtp.Host}:{smtp.Port}: {failMessage}";
            }
            else if (!failMessage.StartsWith("Testmail", StringComparison.OrdinalIgnoreCase)
                     && !failMessage.StartsWith("Gmail", StringComparison.OrdinalIgnoreCase)
                     && !failMessage.StartsWith("Resend", StringComparison.OrdinalIgnoreCase)
                     && !failMessage.StartsWith("SMTP", StringComparison.OrdinalIgnoreCase))
            {
                failMessage = $"Testmail mislukt: {failMessage}";
            }

            await _credentials.SavePingResultAsync(IntegrationKey.Mail, false, failMessage, cancellationToken);
            return new SendTestMailResult(false, true, failMessage);
        }

        static string Truncate(string value, int max)
            => value.Length <= max ? value : value[..max] + "…";
    }

    private static bool LooksLikeEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 254)
        {
            return false;
        }

        var at = value.IndexOf('@');
        return at > 0
            && at < value.Length - 1
            && value.IndexOf('@', at + 1) < 0
            && value.Contains('.', StringComparison.Ordinal);
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

        var apiKey = secrets.ApiKey.Trim();
        if (!apiKey.StartsWith("test_", StringComparison.Ordinal)
            && !apiKey.StartsWith("live_", StringComparison.Ordinal)
            && !apiKey.StartsWith("access_", StringComparison.Ordinal))
        {
            return (false, "Mollie API-key moet beginnen met test_, live_ of access_.");
        }

        var rawBase = string.IsNullOrWhiteSpace(secrets.BaseUrl)
            ? "https://api.mollie.com/v2/"
            : secrets.BaseUrl;
        if (!IntegrationEndpointUrl.TryNormalizeBaseUrl(rawBase, out var baseUrl, out var error)
            || string.IsNullOrWhiteSpace(baseUrl))
        {
            return (false, error ?? "Ongeldige Mollie Base URL.");
        }

        // /methods werkt met gewone API-keys (test_/live_). /permissions is alleen OAuth.
        var client = _httpClientFactory.CreateClient("IntegrationProbe");
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(baseUrl), "methods"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var mode = apiKey.StartsWith("live_", StringComparison.Ordinal) ? "live" : "test";
            return (true, $"Verbinding met Mollie OK ({mode}).");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var detail = body.Length > 160 ? body[..160] : body;
        return (false, $"Mollie gaf {(int)response.StatusCode} — controleer de API-key. {detail}".Trim());
    }

    private async Task<(bool Ok, string Message)> TestMailAsync(CancellationToken cancellationToken)
    {
        var secrets = await _credentials.GetSecretsAsync(IntegrationKey.Mail, cancellationToken);
        if (SmtpEmailService.TryResolveResend(secrets, out _))
        {
            return (true, "Resend API-key + From aanwezig. Gebruik ‘Stuur testmail’ om echt te versturen.");
        }

        if (SmtpEmailService.TryResolveSmtp(secrets, out var smtp))
        {
            // Lightweight TCP reachability check (no auth handshake).
            using var tcp = new System.Net.Sockets.TcpClient();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(TimeSpan.FromSeconds(8));
            await tcp.ConnectAsync(smtp.Host, smtp.Port, linked.Token);
            return (true, $"SMTP bereikbaar op {smtp.Host}:{smtp.Port}. Let op: Gmail blokkeert cloud-SMTP vaak (5.7.9) — Resend API is betrouwbaarder.");
        }

        return (false, "Configureer Resend (API-key + From) of SMTP (host, poort, gebruiker, app-wachtwoord, From).");
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
        return (true, $"{label}-credentials aanwezig. Login-knoppen gebruiken deze Integraties-gegevens.");
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
        // Resend needs API key + From; a partial env key alone must not look "configured".
        if (view.Key == IntegrationKey.Mail)
        {
            return view.HasApiKey && !string.IsNullOrWhiteSpace(view.FromAddress);
        }

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
