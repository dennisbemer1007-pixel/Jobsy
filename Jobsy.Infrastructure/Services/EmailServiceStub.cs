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
public sealed class EmailServiceStub : IEmailService
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

    public async Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (_featureFlags is not null
            && !string.IsNullOrWhiteSpace(message.Category)
            && await ShouldSuppressAsync(message.Category, cancellationToken))
        {
            _logger.LogInformation(
                "Email suppressed: employers disabled (category={Category}).",
                message.Category);
            return EmailDeliveryResult.Stub;
        }

        var redactedTo = RedactEmail(message.To);
        _logger.LogInformation(
            "Email stub → {To}: {Subject}",
            redactedTo, message.Subject);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = message.Category ?? "Email",
            Message = $"Mail to {redactedTo}: {message.Subject}",
            DetailsJson = JsonSerializer.Serialize(new
            {
                To = redactedTo,
                message.Subject,
                Category = message.Category,
                BodyLength = message.BodyHtml?.Length ?? 0,
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
