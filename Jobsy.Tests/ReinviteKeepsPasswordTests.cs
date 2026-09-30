using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class ReinviteKeepsPasswordTests
{
    [Fact]
    public async Task Reinviting_existing_user_with_password_leaves_hash_unchanged()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "mgr@jobsy.local",
            FullName = "Manager",
            Role = UserRole.SalesManager,
            IsActive = true
        });
        var hash = JobsyPasswordHasher.Hash("BestaandWachtwoord12!");
        db.LocalAuthCredentials.Add(new LocalAuthCredential
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = "mgr@jobsy.local",
            PasswordHash = hash
        });
        await db.SaveChangesAsync();

        var links = new OneTimeLinkService(db, NullLogger<OneTimeLinkService>.Instance);
        var email = new NoopEmail();
        var invite = new SalesManagerInviteService(
            db, email, links, new AlwaysOnFeatures(), NullLogger<SalesManagerInviteService>.Instance);

        await invite.InviteAsync("mgr@jobsy.local", "Manager");

        var credential = await db.LocalAuthCredentials.SingleAsync(c => c.UserId == userId);
        Assert.Equal(hash, credential.PasswordHash);
        Assert.False(await db.OneTimeLinks.AnyAsync(l => l.UserId == userId && l.UsedAtUtc == null));
    }

    private static JobsyDbContext CreateDb()
        => new(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class NoopEmail : IEmailService
    {
        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(EmailDeliveryResult.Stub);
    }
}
