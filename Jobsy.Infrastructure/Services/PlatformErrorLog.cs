using System.Text.RegularExpressions;
using Jobsy.Core.Diagnostics;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;

namespace Jobsy.Infrastructure.Services;

public sealed class PlatformErrorLog(JobsyDbContext db) : IPlatformErrorLog
{
    private static readonly Regex Email = new(
        @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task WriteAsync(
        string category,
        string message,
        string? supportCode,
        string? detail,
        CancellationToken cancellationToken = default)
    {
        var code = string.IsNullOrWhiteSpace(supportCode) ? SupportCodeGenerator.Create() : supportCode.Trim();
        var text = Sanitize(message);
        if (!text.Contains(code, StringComparison.Ordinal))
        {
            text = code + " " + text;
        }

        if (text.Length > 2000)
        {
            text = text[..2000];
        }

        var cat = string.IsNullOrWhiteSpace(category) ? "Interactive" : category.Trim();
        if (cat.Length > 80)
        {
            cat = cat[..80];
        }

        var details = Sanitize(detail);
        if (details.Length > 2000)
        {
            details = details[..2000];
        }

        db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Error,
            Category = cat,
            Message = text,
            DetailsJson = string.IsNullOrWhiteSpace(details) ? null : details,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // A log failure must not hide the original error.
        }
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        return Email.Replace(value, "[redacted]");
    }
}
