using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Ops;

/// <summary>
/// Acceptatie: three Grokbot demo logins named Sam de Wit, password via <see cref="DemoUsersSeeder.DemoPassword"/>,
/// always reset to pre-onboarding state for screen recordings.
/// </summary>
public static class AcceptatieDemoRecordingCandidatesEnsurer
{
    public static readonly Guid Demo1UserId = Guid.Parse("aaaaaaaa-d001-4001-8001-000000000001");
    public static readonly Guid Demo2UserId = Guid.Parse("aaaaaaaa-d002-4002-8002-000000000002");
    public static readonly Guid Demo3UserId = Guid.Parse("aaaaaaaa-d003-4003-8003-000000000003");

    public const string Demo1Email = "lobsy-test+demo1@mail.grokbot.com";
    public const string Demo2Email = "lobsy-test+demo2@mail.grokbot.com";
    public const string Demo3Email = "lobsy-test+demo3@mail.grokbot.com";

    private static readonly DemoSlot[] Slots =
    [
        new(Demo1UserId, Demo1Email),
        new(Demo2UserId, Demo2Email),
        new(Demo3UserId, Demo3Email),
    ];

    public static IReadOnlyList<string> AllEmails { get; } =
        Slots.Select(s => s.Email).ToList();

    public static async Task EnsureAsync(
        JobsyDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        foreach (var slot in Slots)
        {
            await EnsureSlotAsync(db, slot, logger, cancellationToken);
        }
    }

    private static async Task EnsureSlotAsync(
        JobsyDbContext db,
        DemoSlot slot,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var email = slot.Email.ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Email != null && u.Email.ToLower() == email,
            cancellationToken);
        var created = false;
        if (user is null)
        {
            user = await db.Users.FirstOrDefaultAsync(u => u.Id == slot.UserId, cancellationToken);
        }

        if (user is null)
        {
            user = new User
            {
                Id = slot.UserId,
                Email = email,
                Role = UserRole.Candidate,
                IsActive = true,
                IsTestAccount = true,
            };
            db.Users.Add(user);
            created = true;
        }

        ApplyFreshSamProfile(user);
        ClearMfa(user);
        await EnsureCredentialAsync(db, user, cancellationToken);
        await ApplyPreOnboardingStateAsync(db, user, cancellationToken);

        if (created)
        {
            logger.LogInformation(
                "Demo recording candidate ensured (created) for {Email} as {Name}.",
                email,
                KandidaatDemoProfileEnsurer.DemoFullName);
        }
    }

    private static void ApplyFreshSamProfile(User user)
    {
        user.FullName = KandidaatDemoProfileEnsurer.DemoFullName;
        user.FirstName = KandidaatDemoProfileEnsurer.DemoFirstName;
        user.LastName = KandidaatDemoProfileEnsurer.DemoLastName;
        user.Role = UserRole.Candidate;
        user.IsActive = true;
        user.IsTestAccount = true;
    }

    private static async Task ApplyPreOnboardingStateAsync(
        JobsyDbContext db,
        User user,
        CancellationToken cancellationToken)
    {
        user.OpenForWork = false;
        user.HomeLocation = null;
        user.PreferencesJson = null;
        user.DateOfBirth = null;
        user.CandidateHowToCompletedAt = null;
        user.TalentPoolConsentAt = null;
        user.TalentPoolConsentVersion = null;
        user.TestAiConsentAt = null;
        user.TestAiConsentVersion = null;

        var onboarding = await db.CandidateOnboardings
            .FirstOrDefaultAsync(o => o.UserId == user.Id, cancellationToken);
        if (onboarding is not null)
        {
            db.CandidateOnboardings.Remove(onboarding);
        }
    }

    private static async Task EnsureCredentialAsync(
        JobsyDbContext db,
        User user,
        CancellationToken cancellationToken)
    {
        var email = user.Email.Trim().ToLowerInvariant();
        var credential = await db.LocalAuthCredentials.FirstOrDefaultAsync(c => c.Email == email, cancellationToken)
            ?? await db.LocalAuthCredentials.FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);

        if (credential is null)
        {
            db.LocalAuthCredentials.Add(new LocalAuthCredential
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Email = email,
                PasswordHash = JobsyPasswordHasher.Hash(DemoUsersSeeder.DemoPassword),
            });
            return;
        }

        credential.UserId = user.Id;
        credential.Email = email;
        if (!JobsyPasswordHasher.Verify(DemoUsersSeeder.DemoPassword, credential.PasswordHash)
            || JobsyPasswordHasher.NeedsRehash(credential.PasswordHash))
        {
            credential.PasswordHash = JobsyPasswordHasher.Hash(DemoUsersSeeder.DemoPassword);
        }

        credential.FailedLoginCount = 0;
        credential.LockoutUntil = null;
        credential.LockoutCount = 0;
    }

    private static void ClearMfa(User user)
    {
        user.AuthenticatorEnabled = false;
        user.AuthenticatorSecret = null;
        user.RecoveryCodesHash = null;
        user.AuthenticatorEnrolledAtUtc = null;
        user.MfaFailedCount = 0;
        user.MfaLockoutUntilUtc = null;
        user.LastTotpTimeStep = null;
        user.LastMfaLockoutMailAtUtc = null;
    }

    private readonly record struct DemoSlot(Guid UserId, string Email);
}
