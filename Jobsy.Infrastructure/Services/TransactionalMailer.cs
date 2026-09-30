using System.Net;
using System.Text.RegularExpressions;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Jobsy.Infrastructure.Services;

public sealed class TransactionalMailer : ITransactionalMailer
{
    private static readonly Regex UnsubHrefHtml = new(
        @"href=""[^""]*""(\s+data-lobsy-unsub=""1"")|(data-lobsy-unsub=""1""\s+)href=""[^""]*""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex UnsubTextLine = new(
        @"^(?<label>[^\r\n:]+):\s*\S*mail/afmelden\S*$",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private readonly IEmailService _email;
    private readonly IFeatureFlags _featureFlags;
    private readonly IPlatformFeatureService _platformFeatures;
    private readonly IEmailPreferenceService _preferences;
    private readonly IMailUnsubscribeTokenService _unsubscribe;
    private readonly IOptions<MailOptions> _mailOptions;
    private readonly JobsyDbContext _db;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<TransactionalMailer> _logger;

    public TransactionalMailer(
        IEmailService email,
        IFeatureFlags featureFlags,
        IPlatformFeatureService platformFeatures,
        IEmailPreferenceService preferences,
        IMailUnsubscribeTokenService unsubscribe,
        IOptions<MailOptions> mailOptions,
        JobsyDbContext db,
        IHostEnvironment environment,
        ILogger<TransactionalMailer> logger)
    {
        _email = email;
        _featureFlags = featureFlags;
        _platformFeatures = platformFeatures;
        _preferences = preferences;
        _unsubscribe = unsubscribe;
        _mailOptions = mailOptions;
        _db = db;
        _environment = environment;
        _logger = logger;
    }

    public async Task<EmailSendOutcome> SendAsync(
        ComposedEmail mail,
        string to,
        EmailSendOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mail);
        options ??= new EmailSendOptions();

        if (!EmailTemplateRegistry.TryGet(mail.Key, out var def))
        {
            def = new EmailTemplateDefinition(
                mail.Key,
                mail.Category,
                "Onbekend",
                mail.Kind,
                "AdminNotice",
                false,
                mail.Key,
                mail.Key);
        }

        if (!options.BypassSuppression)
        {
            if (def.RequiresEmployers
                && !await _featureFlags.IsEnabledAsync(PlatformFeature.Employers, cancellationToken))
            {
                await LogSuppressedAsync(mail, to, "employers disabled", cancellationToken);
                return new EmailSendOutcome(false, true, "employers disabled");
            }

            if (string.Equals(def.Key, "AmbassadeurInvite", StringComparison.OrdinalIgnoreCase))
            {
                var snap = await _platformFeatures.GetAsync(cancellationToken);
                if (!snap.AmbassadorsEnabled)
                {
                    await LogSuppressedAsync(mail, to, "ambassadors disabled", cancellationToken);
                    return new EmailSendOutcome(false, true, "ambassadors disabled");
                }
            }
            else if (def.Parked)
            {
                await LogSuppressedAsync(mail, to, "parked", cancellationToken);
                return new EmailSendOutcome(false, true, "parked");
            }

            if (def.Kind == EmailKind.Optional
                && await _preferences.IsOptedOutAsync(to, def.Key, cancellationToken))
            {
                await LogSuppressedAsync(mail, to, "opted-out", cancellationToken);
                return new EmailSendOutcome(false, true, "opted-out");
            }
        }

        GuardBareHtml(mail);

        var features = await _platformFeatures.GetAsync(cancellationToken);
        var html = mail.Html ?? string.Empty;
        var text = mail.Text ?? string.Empty;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Entity-Ref-ID"] = Guid.NewGuid().ToString("D"),
            ["Auto-Submitted"] = "auto-generated"
        };

        if (def.Kind == EmailKind.Optional)
        {
            var unsubUrl = _unsubscribe.BuildUnsubscribeUrl(features.PublicWebBaseUrl, to, def.Key);
            html = ApplyUnsubscribeUrlHtml(html, unsubUrl);
            text = ApplyUnsubscribeUrlText(text, unsubUrl);
            headers["List-Unsubscribe"] = $"<{unsubUrl}>";
            headers["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click";
        }

        var replyTo = string.IsNullOrWhiteSpace(_mailOptions.Value.ReplyTo)
            ? MailOptions.DefaultSupportAddress
            : _mailOptions.Value.ReplyTo!.Trim();

        var tags = new List<(string Name, string Value)>
        {
            ("category", SanitizeTag(mail.Category ?? def.Category)),
            ("lang", SanitizeTag(mail.Language)),
            ("kind", SanitizeTag(KindTag(def.Kind)))
        };

        var delivery = await _email.SendAsync(
            new EmailMessage(to, mail.Subject, html, mail.Category)
            {
                BodyText = text,
                ReplyTo = replyTo,
                Headers = headers,
                Tags = tags,
                IdempotencyKey = options.IdempotencyKey
            },
            cancellationToken);

        return new EmailSendOutcome(true, false, null, delivery.Kind);
    }

    internal static string ApplyUnsubscribeUrlHtml(string html, string unsubscribeUrl)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html;
        }

        var encoded = WebUtility.HtmlEncode(unsubscribeUrl);
        if (html.Contains("data-lobsy-unsub=\"1\"", StringComparison.Ordinal))
        {
            return UnsubHrefHtml.Replace(html, m =>
                m.Groups[1].Success
                    ? $"href=\"{encoded}\"{m.Groups[1].Value}"
                    : $"{m.Groups[2].Value}href=\"{encoded}\"");
        }

        return html;
    }

    internal static string ApplyUnsubscribeUrlText(string text, string unsubscribeUrl)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (UnsubTextLine.IsMatch(text))
        {
            return UnsubTextLine.Replace(text, m => $"{m.Groups["label"].Value}: {unsubscribeUrl}");
        }

        return text;
    }

    private static string KindTag(EmailKind kind) => kind switch
    {
        EmailKind.Optional => "O",
        EmailKind.Security => "S",
        _ => "E"
    };

    private static string SanitizeTag(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "na";
        }

        var sb = new System.Text.StringBuilder(value.Length);
        foreach (var ch in value.Trim())
        {
            if (char.IsAsciiLetterOrDigit(ch) || ch is '_' or '-')
            {
                sb.Append(ch);
            }
            else if (ch is ' ' or '.')
            {
                sb.Append('_');
            }
        }

        return sb.Length == 0 ? "na" : sb.ToString();
    }

    private void GuardBareHtml(ComposedEmail mail)
    {
        var hasLayout = mail.Html?.Contains("data-lobsy-layout=\"2\"", StringComparison.Ordinal) == true;
        if (hasLayout)
        {
            return;
        }

        var isTesting = string.Equals(_environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase)
                        || _environment.IsDevelopment();
        if (isTesting)
        {
            throw new InvalidOperationException(
                $"Bare HTML mail blocked for template '{mail.Key}': missing data-lobsy-layout=\"2\".");
        }

        _logger.LogError(
            "Bare HTML mail for template {Key} (category {Category}) — missing data-lobsy-layout=2.",
            mail.Key,
            mail.Category);
    }

    private async Task LogSuppressedAsync(
        ComposedEmail mail,
        string to,
        string reason,
        CancellationToken cancellationToken)
    {
        var redacted = EmailServiceStub.RedactEmail(to);
        _logger.LogInformation(
            "email.suppressed key={Key} reason={Reason} to={To}",
            mail.Key,
            reason,
            redacted);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "email.suppressed",
            Message = $"email.suppressed key={mail.Key} reason={reason} to={redacted}",
            DetailsJson = JsonSerializer.Serialize(new
            {
                mail.Key,
                mail.Category,
                Reason = reason,
                To = redacted
            }),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
