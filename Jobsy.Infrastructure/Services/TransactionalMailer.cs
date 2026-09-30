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
    private readonly IEmailService _email;
    private readonly IFeatureFlags _featureFlags;
    private readonly IPlatformFeatureService _platformFeatures;
    private readonly JobsyDbContext _db;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<TransactionalMailer> _logger;

    public TransactionalMailer(
        IEmailService email,
        IFeatureFlags featureFlags,
        IPlatformFeatureService platformFeatures,
        JobsyDbContext db,
        IHostEnvironment environment,
        ILogger<TransactionalMailer> logger)
    {
        _email = email;
        _featureFlags = featureFlags;
        _platformFeatures = platformFeatures;
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
            // Allow send for unknown keys only when category is present (sales/other ad-hoc).
            // Prefer registered templates.
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
        }

        GuardBareHtml(mail);

        var delivery = await _email.SendAsync(
            new EmailMessage(to, mail.Subject, mail.Html, mail.Category),
            cancellationToken);

        return new EmailSendOutcome(true, false, null, delivery.Kind);
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
