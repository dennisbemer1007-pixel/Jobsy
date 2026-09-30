using System.Text.Json;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>Dev fallback: logs outbound mail to PlatformLog without sending.</summary>
public sealed class EmailServiceStub : IEmailService, ITransactionalMailer
{
    private readonly JobsyDbContext _db;
    private readonly IFeatureFlags? _featureFlags;
    private readonly ILogger<EmailServiceStub> _logger;

    public EmailServiceStub(JobsyDbContext db, ILogger<EmailServiceStub> logger, IFeatureFlags? featureFlags = null)
    {
        _db = db;
        _logger = logger;
        _featureFlags = featureFlags;
    }

    public async Task<EmailSendOutcome> SendAsync(
        ComposedEmail mail,
        string to,
        EmailSendOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mail);
        var delivery = await SendCoreAsync(
            to,
            mail.Subject,
            mail.Html,
            mail.Category,
            cancellationToken);
        return new EmailSendOutcome(true, false, null, delivery.Kind);
    }

    public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        => SendCoreAsync(
            message.To,
            message.Subject,
            message.BodyHtml,
            message.Category,
            cancellationToken);

    private async Task<EmailDeliveryResult> SendCoreAsync(
        string to,
        string subject,
        string? bodyHtml,
        string? category,
        CancellationToken cancellationToken)
    {
        if (_featureFlags is not null
            && !string.IsNullOrWhiteSpace(category)
            && await ShouldSuppressAsync(category, cancellationToken))
        {
            _logger.LogInformation(
                "Email suppressed: employers disabled (category={Category}).",
                category);
            return EmailDeliveryResult.Stub;
        }

        var redactedTo = RedactEmail(to);
        _logger.LogInformation(
            "Email stub → {To}: {Subject}",
            redactedTo, subject);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = category ?? "Email",
            Message = $"Mail to {redactedTo}: {subject}",
            DetailsJson = JsonSerializer.Serialize(new
            {
                To = redactedTo,
                Subject = subject,
                Category = category,
                BodyLength = bodyHtml?.Length ?? 0,
                Provider = "Stub",
                Sent = false
            }),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return EmailDeliveryResult.Stub;
    }

    private async ValueTask<bool> ShouldSuppressAsync(string category, CancellationToken cancellationToken)
    {
        var cat = category.Trim();
        var requires = TransactionalEmails.Templates.Any(t =>
            t.RequiresEmployers
            && (string.Equals(t.Category, cat, StringComparison.OrdinalIgnoreCase)
                || string.Equals(t.Key, cat, StringComparison.OrdinalIgnoreCase)));
        if (!requires || _featureFlags is null)
        {
            return false;
        }

        return !await _featureFlags.IsEnabledAsync(PlatformFeature.Employers, cancellationToken);
    }

    public static string RedactEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "(empty)";
        }

        var at = email.IndexOf('@');
        if (at <= 1)
        {
            return "***";
        }

        return email[0] + "***" + email[at..];
    }
}
