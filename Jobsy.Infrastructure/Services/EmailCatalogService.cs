using System.Collections.Concurrent;
using System.Text.Json;
using Jobsy.Core.Admin;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Localization;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Options;
using Jobsy.Core.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class EmailCatalogService : IEmailCatalogService
{
    private static readonly ConcurrentDictionary<Guid, EmailCatalogSendAllStatus> SendAllRuns = new();
    private static readonly ConcurrentDictionary<string, (DateTime WindowStartUtc, int Count)> DailyCaps = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<Guid, DateTime> SendAllCooldown = new();

    private readonly ITransactionalMailer _mailer;
    private readonly IPlatformFeatureService _features;
    private readonly IAdminAuditLog _audit;
    private readonly IServiceScopeFactory _scopes;
    private readonly MailOptions _mail;
    private readonly ILogger<EmailCatalogService> _logger;

    public EmailCatalogService(
        ITransactionalMailer mailer,
        IPlatformFeatureService features,
        IAdminAuditLog audit,
        IServiceScopeFactory scopes,
        IOptions<MailOptions> mail,
        ILogger<EmailCatalogService> logger)
    {
        _mailer = mailer;
        _features = features;
        _audit = audit;
        _scopes = scopes;
        _mail = mail.Value;
        _logger = logger;
    }

    /// <summary>Clears in-memory send-all / daily-cap state between unit tests.</summary>
    internal static void ResetForTests()
    {
        SendAllRuns.Clear();
        DailyCaps.Clear();
        SendAllCooldown.Clear();
    }

    public IReadOnlyList<EmailTemplateListItem> ListTemplates(bool ambassadorsEnabled)
    {
        var langs = EmailStrings.Languages.ToList();
        return EmailTemplateRegistry.All.Select(d => new EmailTemplateListItem(
            d.Key,
            d.Title,
            d.Audience,
            d.Description,
            d.Category,
            d.Kind switch
            {
                EmailKind.Optional => "O",
                EmailKind.Security => "S",
                _ => "E"
            },
            d.ReasonKey,
            d.GoodNews,
            Parked: string.Equals(d.Key, "AmbassadeurInvite", StringComparison.OrdinalIgnoreCase) && !ambassadorsEnabled,
            Languages: langs,
            d.RequiresEmployers)).ToList();
    }

    public EmailCatalogTestOptions GetTestOptions()
        => new(
            _mail.TestRecipientAllowList ?? [],
            _mail.TestDailyCap <= 0 ? 100 : _mail.TestDailyCap);

    public EmailTemplatePreview Preview(string key, string language, string theme, string publicWebBaseUrl)
    {
        if (!TransactionalEmails.TryGet(key, out var info))
        {
            throw new KeyNotFoundException(key);
        }

        var lang = (language ?? string.Empty).Trim().ToLowerInvariant();
        if (!EmailStrings.Languages.Contains(lang))
        {
            throw new ArgumentException("Onbekende taal.");
        }

        var themeNorm = (theme ?? "light").Trim().ToLowerInvariant();
        if (themeNorm is not ("light" or "dark"))
        {
            throw new ArgumentException("Onbekend thema.");
        }

        var culture = EmailCulture.ForLanguage(lang);
        var ctx = EmailSampleContext.ForPreview(publicWebBaseUrl);
        var dark = themeNorm == "dark";
        using var _ = dark ? TransactionalEmails.UseRenderMode(EmailRenderMode.PreviewDark) : null;
        var composed = TransactionalEmails.Compose(info.Key, ctx, culture);
        var def = EmailTemplateRegistry.GetRequired(info.Key);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["From"] = string.IsNullOrWhiteSpace(_mail.FromAddress) ? MailOptions.DefaultFromAddress : _mail.FromAddress!,
            ["Reply-To"] = string.IsNullOrWhiteSpace(_mail.ReplyTo) ? MailOptions.DefaultSupportAddress : _mail.ReplyTo!
        };
        if (def.Kind == EmailKind.Optional)
        {
            headers["List-Unsubscribe"] = "yes (optional mails only; omitted on test sends)";
        }

        return new EmailTemplatePreview(
            info.Key,
            composed.Subject,
            composed.Preheader,
            composed.Html,
            composed.Text,
            def.Kind.ToString(),
            culture.Language,
            culture.IsRightToLeft ? "rtl" : "ltr",
            headers);
    }

    public async Task<EmailCatalogSendResult> SendAsync(
        string key,
        string language,
        string adminEmail,
        string? requestedTo,
        CancellationToken cancellationToken = default)
    {
        var to = ResolveRecipient(adminEmail, requestedTo, out var recipientError);
        if (recipientError is not null)
        {
            return Fail(key, recipientError);
        }

        if (!TransactionalEmails.TryGet(key, out var info))
        {
            return Fail(key, "Onbekend mailtype.");
        }

        if (!TryConsumeDailyCap(adminEmail, out var capError))
        {
            return Fail(key, capError!);
        }

        var features = await _features.GetAsync(cancellationToken);
        var lang = JobsyLanguages.Normalize(language);
        var culture = EmailCulture.ForLanguage(lang);
        var ctx = EmailSampleContext.ForPreview(features.PublicWebBaseUrl, to);
        var composed = TransactionalEmails.Compose(info.Key, ctx, culture);
        var testSubject = composed.Subject.StartsWith("[Test] ", StringComparison.Ordinal)
            ? composed.Subject
            : "[Test] " + composed.Subject;
        var delivery = await _mailer.SendAsync(
            composed with { Subject = testSubject },
            to!,
            new EmailSendOptions(BypassSuppression: true, Culture: culture, IsTest: true),
            cancellationToken);

        await WriteAuditAsync(info.Key, culture.Language, to!, cancellationToken);

        var redacted = EmailServiceStub.RedactEmail(to!);
        var via = delivery.DeliveredViaProvider ? "Resend/SMTP" : "PlatformLog (stub)";
        return new EmailCatalogSendResult(
            info.Key,
            info.Title,
            info.Category,
            testSubject,
            true,
            delivery.DeliveredViaProvider,
            delivery.DeliveredViaProvider
                ? $"Verzonden naar {redacted} via {via}."
                : $"Alleen gelogd naar {redacted} via stub — configureer Mail in Integraties voor echte aflevering.");
    }

    public async Task<EmailCatalogSendAllAccepted> StartSendAllAsync(
        string language,
        string adminEmail,
        string? requestedTo,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        _ = ResolveRecipient(adminEmail, requestedTo, out var recipientError);
        if (recipientError is not null)
        {
            throw new InvalidOperationException(recipientError);
        }

        if (SendAllCooldown.TryGetValue(adminUserId, out var last)
            && DateTime.UtcNow - last < TimeSpan.FromMinutes(15))
        {
            throw new InvalidOperationException("Send-all mag hoogstens eens per 15 minuten.");
        }

        var features = await _features.GetAsync(cancellationToken);
        var items = ListTemplates(features.AmbassadorsEnabled).Where(t => !t.Parked).ToList();
        var runId = Guid.NewGuid();
        SendAllRuns[runId] = new EmailCatalogSendAllStatus(runId, items.Count, 0, 0, false, null);
        SendAllCooldown[adminUserId] = DateTime.UtcNow;

        var lang = language;
        var admin = adminEmail;
        var to = requestedTo;
        _ = Task.Run(async () =>
        {
            try
            {
                foreach (var item in items)
                {
                    try
                    {
                        using var scope = _scopes.CreateScope();
                        var catalog = scope.ServiceProvider.GetRequiredService<IEmailCatalogService>();
                        var result = await catalog.SendAsync(item.Key, lang, admin, to, CancellationToken.None);
                        UpdateRun(runId, ok: result.Ok);
                    }
                    catch
                    {
                        UpdateRun(runId, ok: false);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                FinishRun(runId, null);
            }
            catch (Exception ex)
            {
                FinishRun(runId, ex.Message);
            }
        }, CancellationToken.None);

        return new EmailCatalogSendAllAccepted(runId, items.Count);
    }

    public EmailCatalogSendAllStatus? GetSendAllStatus(Guid runId)
        => SendAllRuns.TryGetValue(runId, out var status) ? status : null;

    private string? ResolveRecipient(string adminEmail, string? requestedTo, out string? error)
    {
        error = null;
        var admin = (adminEmail ?? string.Empty).Trim();
        if (!LooksLikeEmail(admin))
        {
            error = "Admin e-mail ontbreekt.";
            return null;
        }

        var requested = (requestedTo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(requested) || string.Equals(requested, admin, StringComparison.OrdinalIgnoreCase))
        {
            return admin;
        }

        var allow = _mail.TestRecipientAllowList ?? [];
        if (allow.Any(a => string.Equals(a?.Trim(), requested, StringComparison.OrdinalIgnoreCase)))
        {
            return requested;
        }

        error = "Alleen naar je eigen adres of de allow-list.";
        return null;
    }

    private bool TryConsumeDailyCap(string adminEmail, out string? error)
    {
        error = null;
        var cap = _mail.TestDailyCap <= 0 ? 100 : _mail.TestDailyCap;
        var day = AmsterdamTime.ToLocal(DateTime.UtcNow).Date;
        var key = $"{adminEmail.Trim().ToLowerInvariant()}|{day:yyyy-MM-dd}";
        var entry = DailyCaps.AddOrUpdate(
            key,
            _ => (DateTime.UtcNow, 1),
            (_, existing) => (existing.WindowStartUtc, existing.Count + 1));
        if (entry.Count > cap)
        {
            error = $"Daglimiet van {cap} testmails bereikt. Probeer morgen opnieuw.";
            return false;
        }

        return true;
    }

    private async Task WriteAuditAsync(string key, string language, string to, CancellationToken cancellationToken)
    {
        var redacted = EmailServiceStub.RedactEmail(to);
        await _audit.WriteAsync(
            new AdminAuditEntry(
                Action: AdminAuditKeys.EmailTestSend,
                TargetType: AdminAuditKeys.TargetTypes.EmailTemplate,
                TargetId: key,
                TargetLabel: key,
                DetailsJson: JsonSerializer.Serialize(new
                {
                    language,
                    to = redacted
                }),
                Result: AdminAuditKeys.Results.Success),
            cancellationToken);
        _logger.LogInformation("Admin catalog mail {Key} to {To}", key, redacted);
    }

    private static void UpdateRun(Guid runId, bool ok)
    {
        SendAllRuns.AddOrUpdate(
            runId,
            _ => new EmailCatalogSendAllStatus(runId, 0, ok ? 1 : 0, ok ? 0 : 1, false, null),
            (_, cur) => cur with
            {
                Sent = cur.Sent + (ok ? 1 : 0),
                Failed = cur.Failed + (ok ? 0 : 1)
            });
    }

    private static void FinishRun(Guid runId, string? error)
    {
        SendAllRuns.AddOrUpdate(
            runId,
            _ => new EmailCatalogSendAllStatus(runId, 0, 0, 0, true, error),
            (_, cur) => cur with { Done = true, Error = error });
    }

    private static EmailCatalogSendResult Fail(string key, string message)
        => new(key, key, "", "", false, false, message);

    internal static bool LooksLikeEmail(string value)
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
}
