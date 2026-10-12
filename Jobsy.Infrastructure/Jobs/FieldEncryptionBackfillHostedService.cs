using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Idempotent backfill: encrypts plaintext columns and fills e-mail blind indexes.
/// Safe to run on every deploy; skips rows already encrypted.
/// </summary>
public sealed class FieldEncryptionBackfillHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FieldEncryptionBackfillHostedService> _logger;

    public FieldEncryptionBackfillHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<FieldEncryptionBackfillHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let migrations / seed finish first.
        await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);

        try
        {
            await RunAsync(stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Field encryption backfill failed.");
        }
    }

    internal async Task RunAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var crypto = scope.ServiceProvider.GetRequiredService<IFieldEncryptionService>();

        if (!crypto.IsEnabled)
        {
            _logger.LogInformation("Field encryption passthrough — backfill limited to e-mail lookup hashes.");
        }

        var usersUpdated = 0;
        await foreach (var user in db.Users.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var changed = false;
            if (string.IsNullOrEmpty(user.EmailLookupHash) && !string.IsNullOrEmpty(user.Email))
            {
                user.EmailLookupHash = EmailLookupHash.Compute(user.Email);
                changed = true;
            }

            if (crypto.IsEnabled)
            {
                changed |= ReencryptUserFields(user, crypto);
            }

            if (changed)
            {
                usersUpdated++;
            }
        }

        var credUpdated = 0;
        await foreach (var cred in db.LocalAuthCredentials.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var changed = false;
            if (string.IsNullOrEmpty(cred.EmailLookupHash) && !string.IsNullOrEmpty(cred.Email))
            {
                cred.EmailLookupHash = EmailLookupHash.Compute(cred.Email);
                changed = true;
            }

            if (crypto.IsEnabled && !crypto.IsEncrypted(cred.Email))
            {
                cred.Email = cred.Email;
                changed = true;
            }

            if (changed)
            {
                credUpdated++;
            }
        }

        var appsUpdated = 0;
        await foreach (var app in db.Applications.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var changed = false;
            if (string.IsNullOrEmpty(app.CandidateEmailLookupHash)
                && !string.IsNullOrEmpty(app.CandidateEmail))
            {
                app.CandidateEmailLookupHash = EmailLookupHash.Compute(app.CandidateEmail);
                changed = true;
            }

            if (crypto.IsEnabled)
            {
                changed |= ReencryptApplicationFields(app, crypto);
            }

            if (changed)
            {
                appsUpdated++;
            }
        }

        if (usersUpdated + credUpdated + appsUpdated > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Field encryption backfill updated {Users} users, {Creds} credentials, {Apps} applications.",
                usersUpdated,
                credUpdated,
                appsUpdated);
        }
    }

    private static bool ReencryptUserFields(Core.Entities.User user, IFieldEncryptionService crypto)
    {
        var changed = false;
        changed |= TouchString(user.Email, v => user.Email = v!, crypto);
        changed |= TouchNullable(user.PhoneNumber, v => user.PhoneNumber = v, crypto);
        changed |= TouchNullable(user.PhoneVerifiedE164, v => user.PhoneVerifiedE164 = v, crypto);
        changed |= TouchNullable(user.ParentalConsentEmail, v => user.ParentalConsentEmail = v, crypto);
        changed |= TouchNullable(user.PreferencesJson, v => user.PreferencesJson = v, crypto);
        changed |= TouchNullable(user.UnsubscribeReasonOther, v => user.UnsubscribeReasonOther = v, crypto);
        if (crypto.IsEnabled && user.DateOfBirth is { } dob)
        {
            user.DateOfBirth = dob;
            changed = true;
        }

        return changed;
    }

    private static bool ReencryptApplicationFields(Core.Entities.Application app, IFieldEncryptionService crypto)
    {
        var changed = false;
        changed |= TouchString(app.CandidateEmail, v => app.CandidateEmail = v!, crypto);
        changed |= TouchNullable(app.CandidateAddress, v => app.CandidateAddress = v, crypto);
        changed |= TouchNullable(app.SnapshotPhoneNumber, v => app.SnapshotPhoneNumber = v, crypto);
        changed |= TouchNullable(app.SchoolEmail, v => app.SchoolEmail = v, crypto);
        changed |= TouchNullable(app.StudentNumber, v => app.StudentNumber = v, crypto);
        changed |= TouchNullable(app.Motivation, v => app.Motivation = v, crypto);
        if (app.SnapshotDateOfBirth is { } dob)
        {
            app.SnapshotDateOfBirth = dob;
            changed = true;
        }

        return changed;
    }

    private static bool TouchString(string value, Action<string> set, IFieldEncryptionService crypto)
    {
        if (string.IsNullOrEmpty(value) || crypto.IsEncrypted(value))
        {
            return false;
        }

        set(value);
        return true;
    }

    private static bool TouchNullable(string? value, Action<string?> set, IFieldEncryptionService crypto)
    {
        if (string.IsNullOrEmpty(value) || crypto.IsEncrypted(value))
        {
            return false;
        }

        set(value);
        return true;
    }
}
