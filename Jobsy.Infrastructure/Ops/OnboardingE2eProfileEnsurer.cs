using Jobsy.Core.Entities;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Ops;

/// <summary>
/// Acceptatie/dev: keep <c>onboarding.e2e@jobsy.local</c> login aligned with Haaglanden home + travel prefs,
/// including when <see cref="LocalAuthCredential"/> points at a legacy user id.
/// </summary>
public static class OnboardingE2eProfileEnsurer
{
    public const string LoginEmail = "onboarding.e2e@jobsy.local";
    public const string DisplayFullName = "Onboarding E2E";
    public const string HomeAddress = "Kasweg 12, 2671 AB Naaldwijk";

    public static readonly GeoPoint HomeLocation = new(51.9930, 4.2150);

    public static readonly string PreferencesJson =
        """{"roles":["horeca","retail","logistiek"],"maxTravelMinutes":20,"preferredTransport":"Fiets","homeAddress":"Kasweg 12, 2671 AB Naaldwijk"}""";

    public static async Task EnsureAsync(
        JobsyDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var user = await ResolveLoginUserAsync(db, cancellationToken);
        if (user is null)
        {
            return;
        }

        var changed = ApplyProfile(user);
        await EnsureCredentialAsync(db, user.Id, cancellationToken);

        if (changed)
        {
            logger.LogInformation(
                "Onboarding E2E profile normalized (user {UserId}, Haaglanden home).",
                user.Id);
        }
    }

    private static async Task<User?> ResolveLoginUserAsync(JobsyDbContext db, CancellationToken cancellationToken)
    {
        var email = LoginEmail.ToLowerInvariant();
        var credential = await db.LocalAuthCredentials
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
        if (credential is not null)
        {
            var byCredential = await db.Users.FirstOrDefaultAsync(u => u.Id == credential.UserId, cancellationToken);
            if (byCredential is not null)
            {
                return byCredential;
            }
        }

        var byEmail = await db.Users.FirstOrDefaultAsync(
            u => u.Email.ToLower() == email,
            cancellationToken);
        if (byEmail is not null)
        {
            return byEmail;
        }

        return await db.Users.FirstOrDefaultAsync(
            u => u.Id == DemoCandidateIds.OnboardingE2eUserId,
            cancellationToken);
    }

    private static bool ApplyProfile(User user)
    {
        var changed = false;
        if (!string.Equals(user.Email, LoginEmail, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = LoginEmail;
            changed = true;
        }

        if (!string.Equals(user.FullName, DisplayFullName, StringComparison.Ordinal))
        {
            user.FullName = DisplayFullName;
            changed = true;
        }

        if (!user.OpenForWork)
        {
            user.OpenForWork = true;
            changed = true;
        }

        if (user.HomeLocation is null
            || Math.Abs(user.HomeLocation.Latitude - HomeLocation.Latitude) > 0.0001
            || Math.Abs(user.HomeLocation.Longitude - HomeLocation.Longitude) > 0.0001)
        {
            user.HomeLocation = HomeLocation;
            changed = true;
        }

        if (!string.Equals(user.PreferencesJson, PreferencesJson, StringComparison.Ordinal))
        {
            user.PreferencesJson = PreferencesJson;
            changed = true;
        }

        return changed;
    }

    private static async Task EnsureCredentialAsync(
        JobsyDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var email = LoginEmail.ToLowerInvariant();
        var credential = await db.LocalAuthCredentials.FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
        if (credential is null)
        {
            db.LocalAuthCredentials.Add(new LocalAuthCredential
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Email = email,
                PasswordHash = JobsyPasswordHasher.Hash(DemoUsersSeeder.DemoPassword)
            });
            return;
        }

        if (!JobsyPasswordHasher.Verify(DemoUsersSeeder.DemoPassword, credential.PasswordHash)
            || JobsyPasswordHasher.NeedsRehash(credential.PasswordHash))
        {
            credential.PasswordHash = JobsyPasswordHasher.Hash(DemoUsersSeeder.DemoPassword);
        }

        credential.FailedLoginCount = 0;
        credential.LockoutUntil = null;
        credential.LockoutCount = 0;
    }
}
