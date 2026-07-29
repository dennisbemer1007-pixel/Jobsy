using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Sends mail via SMTP (MailKit) when Mail integration credentials are complete.
/// Gmail: enable 2FA and use an App Password as ClientSecret — normal account passwords are rejected.
/// Falls back to <see cref="EmailServiceStub"/> (PlatformLog only) when SMTP settings are incomplete.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly IIntegrationCredentialService _credentials;
    private readonly EmailServiceStub _stub;
    private readonly JobsyDbContext _db;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IIntegrationCredentialService credentials,
        EmailServiceStub stub,
        JobsyDbContext db,
        ILogger<SmtpEmailService> logger)
    {
        _credentials = credentials;
        _stub = stub;
        _db = db;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var secrets = await _credentials.GetSecretsAsync(IntegrationKey.Mail, cancellationToken);
        if (!TryResolveSmtp(secrets, out var settings))
        {
            await _stub.SendAsync(message, cancellationToken);
            return;
        }

        var redactedTo = EmailServiceStub.RedactEmail(message.To);
        try
        {
            var mime = new MimeMessage();
            mime.From.Add(MailboxAddress.Parse(settings.FromAddress));
            mime.To.Add(MailboxAddress.Parse(message.To));
            mime.Subject = message.Subject;
            mime.Body = new TextPart("html") { Text = message.BodyHtml ?? string.Empty };

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

    internal static bool TryResolveSmtp(
        IntegrationCredentialSecrets? secrets,
        out SmtpSettings settings)
    {
        settings = default!;
        if (secrets is null
            || string.IsNullOrWhiteSpace(secrets.BaseUrl)
            || string.IsNullOrWhiteSpace(secrets.ClientId)
            || string.IsNullOrWhiteSpace(secrets.ClientSecret)
            || string.IsNullOrWhiteSpace(secrets.FromAddress))
        {
            return false;
        }

        if (!TryParseHostPort(secrets.BaseUrl, out var host, out var port))
        {
            return false;
        }

        // Gmail app passwords are often shown as "xxxx xxxx xxxx xxxx" — spaces are ignored.
        var password = secrets.ClientSecret.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        settings = new SmtpSettings(
            host,
            port,
            secrets.ClientId.Trim(),
            password,
            secrets.FromAddress.Trim());
        return true;
    }

    /// <summary>
    /// Parses host or host:port. Accepts optional smtp:// / smtps:// scheme.
    /// Default port 587 (Gmail STARTTLS).
    /// </summary>
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

        // Strip path if someone pasted a URL.
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
        var looksLikeAuth =
            raw.Contains("5.7.0", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("Authentication", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("authenticate", StringComparison.OrdinalIgnoreCase)
            || ex is AuthenticationException;

        if (isGmail && looksLikeAuth)
        {
            return
                "Gmail weigert de login (Authentication Required). " +
                "Gebruik géén gewoon Gmail-wachtwoord: zet 2-stapsverificatie aan en maak een " +
                "App-wachtwoord (16 tekens). Vul dat in bij ‘App-wachtwoord’, je volledige Gmail-adres " +
                "bij ‘SMTP-gebruiker’, en hetzelfde adres bij ‘Afzender’. " +
                $"Technisch: {Truncate(raw, 180)}";
        }

        if (looksLikeAuth)
        {
            return
                "SMTP-authenticatie mislukt. Controleer gebruiker/wachtwoord (voor Gmail: App-wachtwoord). " +
                $"Technisch: {Truncate(raw, 180)}";
        }

        return Truncate(raw, 280);
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

    internal sealed record SmtpSettings(
        string Host,
        int Port,
        string Username,
        string Password,
        string FromAddress);
}
