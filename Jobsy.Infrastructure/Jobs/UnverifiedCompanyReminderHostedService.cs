using System.Globalization;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Daily (Europe/Amsterdam ~09:00) day-7 and day-21 reminders for unverified companies.
/// </summary>
public sealed class UnverifiedCompanyReminderHostedService : BackgroundService
{
    private static readonly TimeZoneInfo Dutch = ResolveDutch();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UnverifiedCompanyReminderHostedService> _logger;
    private readonly TimeProvider _clock;

    public UnverifiedCompanyReminderHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<UnverifiedCompanyReminderHostedService> logger,
        TimeProvider clock)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _clock = clock;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unverified company reminder job failed.");
            }

            await DelayUntilNextAmsterdamNineAsync(stoppingToken);
        }
    }

    internal async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var features = scope.ServiceProvider.GetRequiredService<IPlatformFeatureService>();
        var snap = await features.GetAsync(cancellationToken);
        var baseUrl = snap.PublicWebBaseUrl;
        var now = _clock.GetUtcNow().UtcDateTime;

        var registrations = await db.CompanyRegistrations
            .Include(r => r.CreatedUser)
            .Include(r => r.CreatedOrganizationCompany)
            .Include(r => r.CreatedBranchCompany)
            .Where(r => r.Status == CompanyRegistrationStatus.Activated
                        && r.ActivatedAt != null
                        && r.CreatedUserId != null)
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var registration in registrations)
        {
            var root = registration.CreatedOrganizationCompany
                       ?? registration.CreatedBranchCompany;
            if (root is null)
            {
                continue;
            }

            if (root.ParentCompanyId is Guid parentId)
            {
                root = await db.Companies.FirstOrDefaultAsync(c => c.Id == parentId, cancellationToken)
                       ?? root;
            }

            if (root.VerificationStatus is not (CompanyVerificationStatus.Unverified
                or CompanyVerificationStatus.Rejected))
            {
                continue;
            }

            var activated = registration.ActivatedAt!.Value;
            var ageDays = (now - activated).TotalDays;
            var contactName = registration.ContactName;
            var contactEmail = registration.ContactEmail;
            var companyName = root.Name;

            if (ageDays >= 7 && registration.ReminderSentDay7AtUtc is null)
            {
                var mail = TransactionalEmails.CompanyVerificationReminder(
                    baseUrl, contactName, companyName, day: 7, deletionDateLabel: null);
                await email.SendAsync(
                    new EmailMessage(contactEmail, mail.Subject, mail.Html, mail.Category),
                    cancellationToken);
                registration.ReminderSentDay7AtUtc = now;
                sent++;
            }

            if (ageDays >= 21 && registration.ReminderSentDay21AtUtc is null)
            {
                var deletionLocal = TimeZoneInfo.ConvertTimeFromUtc(
                    activated.AddDays(60), Dutch);
                var deletionLabel = deletionLocal.ToString("d MMMM yyyy", new CultureInfo("nl-NL"));
                var mail = TransactionalEmails.CompanyVerificationReminder(
                    baseUrl, contactName, companyName, day: 21, deletionDateLabel: deletionLabel);
                await email.SendAsync(
                    new EmailMessage(contactEmail, mail.Subject, mail.Html, mail.Category),
                    cancellationToken);
                registration.ReminderSentDay21AtUtc = now;
                sent++;
            }
        }

        if (sent > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Sent {Count} unverified-company reminder(s).", sent);
        }
    }

    private async Task DelayUntilNextAmsterdamNineAsync(CancellationToken stoppingToken)
    {
        var utcNow = _clock.GetUtcNow().UtcDateTime;
        var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, Dutch);
        var nextLocal = new DateTime(local.Year, local.Month, local.Day, 9, 0, 0, DateTimeKind.Unspecified);
        if (local >= nextLocal)
        {
            nextLocal = nextLocal.AddDays(1);
        }

        var nextUtc = TimeZoneInfo.ConvertTimeToUtc(nextLocal, Dutch);
        var delay = nextUtc - utcNow;
        if (delay < TimeSpan.FromMinutes(30))
        {
            delay = TimeSpan.FromHours(12);
        }

        await Task.Delay(delay, stoppingToken);
    }

    private static TimeZoneInfo ResolveDutch()
    {
        foreach (var id in new[] { "Europe/Amsterdam", "W. Europe Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
