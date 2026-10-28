using Jobsy.Core.Entities;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Ops;

/// <summary>
/// Keeps the public demo login <c>kandidaat@jobsy.local</c> on a fictional display name (Sam de Wit),
/// including rows already renamed manually on Acceptatie.
/// </summary>
public static class KandidaatDemoProfileEnsurer
{
    public static readonly Guid PrimaryDemoCandidateId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");

    public const string DemoFullName = "Sam de Wit";
    public const string DemoFirstName = "Sam";
    public const string DemoLastName = "de Wit";
    public const string DemoLoginEmail = "kandidaat@jobsy.local";

    public static async Task EnsureAsync(JobsyDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        var user = await ResolveUserAsync(db, cancellationToken);
        if (user is null)
        {
            return;
        }

        var changed = false;
        if (!string.Equals(user.FullName, DemoFullName, StringComparison.Ordinal))
        {
            user.FullName = DemoFullName;
            changed = true;
        }

        if (!string.Equals(user.FirstName, DemoFirstName, StringComparison.Ordinal))
        {
            user.FirstName = DemoFirstName;
            changed = true;
        }

        if (!string.Equals(user.LastName, DemoLastName, StringComparison.Ordinal))
        {
            user.LastName = DemoLastName;
            changed = true;
        }

        if (changed)
        {
            logger.LogInformation(
                "Demo candidate profile normalized to {Name} (user {UserId}, login {Email}).",
                DemoFullName,
                user.Id,
                DemoLoginEmail);
        }

        await SyncDenormalizedCandidateNamesAsync(db, user.Id, cancellationToken);
    }

    private static async Task<User?> ResolveUserAsync(JobsyDbContext db, CancellationToken cancellationToken)
    {
        var byId = await db.Users.FirstOrDefaultAsync(u => u.Id == PrimaryDemoCandidateId, cancellationToken);
        if (byId is not null)
        {
            return byId;
        }

        var credential = await db.LocalAuthCredentials.FirstOrDefaultAsync(
            c => c.Email == DemoLoginEmail,
            cancellationToken);
        if (credential is null)
        {
            return null;
        }

        return await db.Users.FirstOrDefaultAsync(u => u.Id == credential.UserId, cancellationToken);
    }

    private static async Task SyncDenormalizedCandidateNamesAsync(
        JobsyDbContext db,
        Guid candidateId,
        CancellationToken cancellationToken)
    {
        var applications = await db.Applications
            .Where(a => a.CandidateUserId == candidateId && a.CandidateName != DemoFullName)
            .ToListAsync(cancellationToken);
        foreach (var app in applications)
        {
            app.CandidateName = DemoFullName;
        }

        var initiatives = await db.WorkInitiatives
            .Where(w => w.CandidateUserId == candidateId && w.CandidateDisplayName != DemoFullName)
            .ToListAsync(cancellationToken);
        foreach (var row in initiatives)
        {
            row.CandidateDisplayName = DemoFullName;
        }
    }
}
