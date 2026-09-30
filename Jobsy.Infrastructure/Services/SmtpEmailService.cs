using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Sends mail via Resend API (<c>POST https://api.resend.com/emails</c>) as the primary path.
/// SMTP (MailKit) is optional fallback. Gmail SMTP from datacenter IPs often fails with 5.7.9.
/// Falls back to <see cref="EmailServiceStub"/> only in Development/Testing when neither path is configured.
/// Open/click tracking stays off at the Resend domain level too (see docs/email-deliverability.md) — we never
/// send tracking options on the API request.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    public const string ResendHttpClientName = "ResendMail";
    public const string DefaultResendApiBase = "https://api.resend.com/";
    public const string DefaultFromAddress = MailOptions.DefaultFromAddress;

    private readonly IIntegrationCredentialService _credentials;
    private readonly EmailServiceStub _stub;
    private readonly JobsyDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHostEnvironment _environment;
    private readonly IFeatureFlags _featureFlags;
    private readonly IOptions<MailOptions> _mailOptions;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IIntegrationCredentialService credentials,
        EmailServiceStub stub,
        JobsyDbContext db,
        IHttpClientFactory httpClientFactory,
        IHostEnvironment environment,
        IFeatureFlags featureFlags,
        IOptions<MailOptions> mailOptions,
        ILogger<SmtpEmailService> logger)
    {
        _credentials = credentials;
        _stub = stub;
        _db = db;
        _httpClientFactory = httpClientFactory;
        _environment = environment;
        _featureFlags = featureFlags;
        _mailOptions = mailOptions;
        _logger = logger;
    }

    public async Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (await ShouldSuppressEmployersAsync(message, cancellationToken))
        {
            _logger.LogInformation(
                "Email suppressed: employers disabled (category={Category}).",
                message.Category ?? "(none)");
            return EmailDeliveryResult.Stub;
        }

        var secrets = await _credentials.GetSecretsAsync(IntegrationKey.Mail, cancellationToken);
        if (TryResolveResend(secrets, out var resend, _mailOptions.Value))
        {
            try
            {
                await SendViaResendAsync(message, resend, cancellationToken);
                return EmailDeliveryResult.Provider;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Resend failed; trying SMTP or stub fallback.");
            }
        }

        if (TryResolveSmtp(secrets, out var settings, _mailOptions.Value))
        {
            try
            {
                await SendViaSmtpAsync(message, settings, cancellationToken);
                return EmailDeliveryResult.Provider;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "SMTP failed; falling back to email stub.");
            }
        }

        if (_environment.IsDevelopment()
            || string.Equals(_environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            return await _stub.SendAsync(message, cancellationToken);
        }

        throw new InvalidOperationException(
            "E-mail is niet geconfigureerd. Stel Resend (API-key + From) of SMTP in onder Integraties, anders ontvangt niemand bevestigingsmails.");
    }

    private async Task SendViaResendAsync(
        EmailMessage message,
        ResendSettings settings,
        CancellationToken cancellationToken)
    {
        var redactedTo = EmailServiceStub.RedactEmail(message.To);
        try
        {
            var client = _httpClientFactory.CreateClient(ResendHttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            if (!string.IsNullOrWhiteSpace(message.IdempotencyKey))
            {
                request.Headers.TryAddWithoutValidation("Idempotency-Key", message.IdempotencyKey.Trim());
            }

            // No open/click tracking fields — keep tracking off at domain level (03.7 / D6).
            request.Content = JsonContent.Create(CreateResendRequest(message, settings.FromAddress));

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(FormatResendError((int)response.StatusCode, body));
            }

            _logger.LogInformation(
                "Resend mail sent → {To}: {Subject}",
                redactedTo, message.Subject);

            await WritePlatformLogAsync(
                PlatformLogLevel.Info,
                message.Category ?? "Email",
                $"Resend mail to {redactedTo}: {message.Subject}",
                new
                {
                    To = redactedTo,
                    message.Subject,
                    Category = message.Category,
                    BodyLength = message.BodyHtml?.Length ?? 0,
                    Provider = "Resend",
                    Sent = true
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var friendly = ex is InvalidOperationException ioe
                ? ioe.Message
                : $"Resend-fout: {Truncate(ex.Message, 220)}";
            _logger.LogError(ex, "Resend mail failed → {To}: {Subject}", redactedTo, message.Subject);
            await WritePlatformLogAsync(
                PlatformLogLevel.Error,
                message.Category ?? "Email",
                $"Resend mail failed to {redactedTo}: {message.Subject} — {friendly}",
                new
                {
                    To = redactedTo,
                    message.Subject,
                    Category = message.Category,
                    Provider = "Resend",
                    Sent = false,
                    Error = friendly
                },
                cancellationToken);
            throw new InvalidOperationException(friendly, ex);
        }
    }

    private async Task SendViaSmtpAsync(
        EmailMessage message,
        SmtpSettings settings,
        CancellationToken cancellationToken)
    {
        var redactedTo = EmailServiceStub.RedactEmail(message.To);
        try
        {
            var mime = new MimeMessage();
            mime.From.Add(EnsureLobsyDisplayName(MailboxAddress.Parse(settings.FromAddress)));
            mime.To.Add(MailboxAddress.Parse(message.To));
            mime.Subject = message.Subject;
            if (!string.IsNullOrWhiteSpace(message.ReplyTo))
            {
                mime.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));
            }

            if (message.Headers is not null)
            {
                foreach (var (name, value) in message.Headers)
                {
                    if (string.IsNullOrWhiteSpace(name) || value is null)
                    {
                        continue;
                    }

                    mime.Headers.Add(name, value);
                }
            }

            var builder = new BodyBuilder
            {
                HtmlBody = message.BodyHtml,
                TextBody = message.BodyText ?? string.Empty
            };
            mime.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            client.Timeout = 20_000;

            var secure = settings.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await client.ConnectAsync(settings.Host, settings.Port, secure, cancellationToken);
            await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);
            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "SMTP mail sent → {To}: {Subject} via {Host}:{Port}",
                redactedTo, message.Subject, settings.Host, settings.Port);

            await WritePlatformLogAsync(
                PlatformLogLevel.Info,
                message.Category ?? "Email",
                $"SMTP mail to {redactedTo}: {message.Subject}",
                new
                {
                    To = redactedTo,
                    message.Subject,
                    Category = message.Category,
                    BodyLength = message.BodyHtml?.Length ?? 0,
                    settings.Host,
                    settings.Port,
                    Sent = true
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var friendly = FormatSmtpError(ex, settings);
            _logger.LogError(
                ex,
                "SMTP mail failed → {To}: {Subject} via {Host}:{Port}",
                redactedTo, message.Subject, settings.Host, settings.Port);

            await WritePlatformLogAsync(
                PlatformLogLevel.Error,
                message.Category ?? "Email",
                $"SMTP mail failed to {redactedTo}: {message.Subject} — {friendly}",
                new
                {
                    To = redactedTo,
                    message.Subject,
                    Category = message.Category,
                    settings.Host,
                    settings.Port,
                    Sent = false,
                    Error = friendly
                },
                cancellationToken);

            throw new InvalidOperationException(friendly, ex);
        }
    }

    /// <summary>
    /// From resolution: DB integration FromAddress → MailOptions.FromAddress → default.
    /// Display name "Lobsy" is added when missing.
    /// </summary>
    public static string ResolveFromAddress(string? dbFrom, MailOptions? options = null)
        => MailAddressResolution.ResolveFromAddress(dbFrom, options);

    public static string FormatFromWithDisplayName(string fromAddress)
        => MailAddressResolution.EnsureLobsyDisplayName(fromAddress);

    public static MailboxAddress EnsureLobsyDisplayName(MailboxAddress address)
    {
        if (string.IsNullOrWhiteSpace(address.Name))
        {
            return new MailboxAddress("Lobsy", address.Address);
        }

        return address;
    }

    /// <summary>True when Production From domain is not mail.lobsy.nl.</summary>
    public static bool IsFromDomainMismatch(string fromAddress)
        => MailAddressResolution.IsFromDomainMismatch(fromAddress);

    public static string EffectiveReplyTo(MailOptions? options)
        => MailAddressResolution.EffectiveReplyTo(options);

    internal static bool TryResolveResend(
        IntegrationCredentialSecrets? secrets,
        out ResendSettings settings,
        MailOptions? mailOptions = null)
    {
        settings = default!;
        if (secrets is null || string.IsNullOrWhiteSpace(secrets.ApiKey))
        {
            return false;
        }

        // Api key alone is enough; From falls through DB → config → default (03.2).
        var from = ResolveFromAddress(secrets.FromAddress, mailOptions);
        settings = new ResendSettings(secrets.ApiKey.Trim(), from);
        return true;
    }

    /// <summary>Back-compat overload used by older call sites/tests.</summary>
    internal static bool TryResolveResend(
        IntegrationCredentialSecrets? secrets,
        out ResendSettings settings)
        => TryResolveResend(secrets, out settings, null);

    internal static bool TryResolveSmtp(
        IntegrationCredentialSecrets? secrets,
        out SmtpSettings settings,
        MailOptions? mailOptions = null)
    {
        settings = default!;
        if (secrets is null
            || string.IsNullOrWhiteSpace(secrets.BaseUrl)
            || string.IsNullOrWhiteSpace(secrets.ClientId)
            || string.IsNullOrWhiteSpace(secrets.ClientSecret))
        {
            return false;
        }

        if (!TryParseHostPort(secrets.BaseUrl, out var host, out var port))
        {
            return false;
        }

        var password = secrets.ClientSecret.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var from = ResolveFromAddress(secrets.FromAddress, mailOptions);
        settings = new SmtpSettings(
            host,
            port,
            secrets.ClientId.Trim(),
            password,
            from);
        return true;
    }

    internal static bool TryResolveSmtp(
        IntegrationCredentialSecrets? secrets,
        out SmtpSettings settings)
        => TryResolveSmtp(secrets, out settings, null);

    internal static bool TryParseHostPort(string baseUrl, out string host, out int port)
    {
        host = string.Empty;
        port = 587;

        var raw = baseUrl.Trim();
        if (raw.StartsWith("smtp://", StringComparison.OrdinalIgnoreCase)
            || raw.StartsWith("smtps://", StringComparison.OrdinalIgnoreCase))
        {
            var schemeEnd = raw.IndexOf("://", StringComparison.Ordinal);
            raw = raw[(schemeEnd + 3)..];
        }

        var slash = raw.IndexOf('/');
        if (slash >= 0)
        {
            raw = raw[..slash];
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var colon = raw.LastIndexOf(':');
        if (colon > 0 && colon < raw.Length - 1
            && int.TryParse(raw[(colon + 1)..], out var parsedPort)
            && parsedPort is > 0 and <= 65535)
        {
            host = raw[..colon].Trim();
            port = parsedPort;
            return !string.IsNullOrWhiteSpace(host);
        }

        host = raw.Trim();
        return !string.IsNullOrWhiteSpace(host);
    }

    internal static string FormatSmtpError(Exception ex, SmtpSettings settings)
    {
        var raw = ex.Message;
        var isGmail = settings.Host.Contains("gmail", StringComparison.OrdinalIgnoreCase)
            || settings.Host.Contains("google", StringComparison.OrdinalIgnoreCase);
        var webLoginRequired =
            raw.Contains("5.7.9", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("WebLoginRequired", StringComparison.OrdinalIgnoreCase);
        var looksLikeAuth =
            webLoginRequired
            || raw.Contains("5.7.0", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("Authentication", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("authenticate", StringComparison.OrdinalIgnoreCase)
            || ex is AuthenticationException;

        if (isGmail && webLoginRequired)
        {
            return
                "Gmail blokkeert SMTP vanaf deze server (5.7.9 WebLoginRequired). " +
                "Dat gebeurt vaak op cloud-hosts, ook met een correct App-wachtwoord. " +
                "Oplossing: gebruik Resend (API-key + From) i.p.v. Gmail-SMTP, of probeer " +
                "https://accounts.google.com/DisplayUnlockCaptcha terwijl je bent ingelogd. " +
                $"Technisch: {Truncate(raw, 160)}";
        }

        if (isGmail && looksLikeAuth)
        {
            return
                "Gmail weigert de login (Authentication Required). " +
                "Gebruik géén gewoon Gmail-wachtwoord: zet 2-stapsverificatie aan en maak een " +
                "App-wachtwoord (16 tekens). Op cloud-hosts faalt Gmail-SMTP vaak alsnog — " +
                "gebruik dan Resend (API-key). " +
                $"Technisch: {Truncate(raw, 160)}";
        }

        if (looksLikeAuth)
        {
            return
                "SMTP-authenticatie mislukt. Controleer gebruiker/wachtwoord. " +
                $"Technisch: {Truncate(raw, 180)}";
        }

        return Truncate(raw, 280);
    }

    internal static ResendSendRequest CreateResendRequest(EmailMessage message, string fromAddress)
    {
        Dictionary<string, string>? headers = null;
        if (message.Headers is { Count: > 0 })
        {
            headers = new Dictionary<string, string>(message.Headers, StringComparer.OrdinalIgnoreCase);
        }

        List<ResendTag>? tags = null;
        if (message.Tags is { Count: > 0 })
        {
            tags = message.Tags
                .Where(t => !string.IsNullOrWhiteSpace(t.Name) && t.Value is not null)
                .Select(t => new ResendTag { Name = t.Name, Value = t.Value })
                .ToList();
        }

        return new ResendSendRequest
        {
            From = fromAddress,
            To = [message.To],
            Subject = message.Subject,
            Html = message.BodyHtml ?? string.Empty,
            Text = message.BodyText,
            ReplyTo = string.IsNullOrWhiteSpace(message.ReplyTo) ? null : message.ReplyTo.Trim(),
            Headers = headers,
            Tags = tags
            // Intentionally no tracking / click options (D6).
        };
    }

    internal static string FormatResendError(int statusCode, string body)
    {
        var detail = Truncate(body.Replace('\n', ' ').Trim(), 180);
        if (statusCode is 401 or 403)
        {
            return
                $"Resend weigert de aanvraag ({statusCode}). Controleer API-key en of het From-domein " +
                $"geverifieerd is (of gebruik onboarding@resend.dev naar je eigen inbox). {detail}";
        }

        if (statusCode == 422)
        {
            return
                $"Resend wijst het bericht af (422). Vaak: From niet geverifieerd of ongeldig adres. {detail}";
        }

        return $"Resend gaf {statusCode}. {detail}";
    }


    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "…";

    private async Task WritePlatformLogAsync(
        PlatformLogLevel level,
        string category,
        string message,
        object details,
        CancellationToken cancellationToken)
    {
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = level,
            Category = category,
            Message = message,
            DetailsJson = JsonSerializer.Serialize(details),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async ValueTask<bool> ShouldSuppressEmployersAsync(
        EmailMessage message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Category))
        {
            return false;
        }

        var cat = message.Category.Trim();
        var requires = TransactionalEmails.Templates.Any(t =>
            t.RequiresEmployers
            && (string.Equals(t.Category, cat, StringComparison.OrdinalIgnoreCase)
                || string.Equals(t.Key, cat, StringComparison.OrdinalIgnoreCase)));
        if (!requires)
        {
            return false;
        }

        return !await _featureFlags.IsEnabledAsync(PlatformFeature.Employers, cancellationToken);
    }

    internal sealed record ResendSettings(string ApiKey, string FromAddress);

    internal sealed record SmtpSettings(
        string Host,
        int Port,
        string Username,
        string Password,
        string FromAddress);

    internal sealed class ResendSendRequest
    {
        [JsonPropertyName("from")]
        public required string From { get; init; }

        [JsonPropertyName("to")]
        public required string[] To { get; init; }

        [JsonPropertyName("subject")]
        public required string Subject { get; init; }

        [JsonPropertyName("html")]
        public required string Html { get; init; }

        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; init; }

        [JsonPropertyName("reply_to")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ReplyTo { get; init; }

        [JsonPropertyName("headers")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? Headers { get; init; }

        [JsonPropertyName("tags")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ResendTag>? Tags { get; init; }
    }

    internal sealed class ResendTag
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("value")]
        public required string Value { get; init; }
    }
}
