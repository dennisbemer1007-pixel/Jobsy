using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>Dev fallback: logs outbound mail to PlatformLog without sending.</summary>
public sealed class EmailServiceStub : IEmailService
{
    private readonly JobsyDbContext _db;
    private readonly ILogger<EmailServiceStub> _logger;

    public EmailServiceStub(JobsyDbContext db, ILogger<EmailServiceStub> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
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
                BodyLength = message.BodyHtml?.Length ?? 0
            }),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
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
