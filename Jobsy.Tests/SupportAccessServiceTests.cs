using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using UserRole = Jobsy.Core.Enums.UserRole;

namespace Jobsy.Tests;

public class SupportAccessServiceTests
{
    [Fact]
    public async Task Grant_active_until_expiry_scoped_and_revocable()
    {
        await using var db = CreateDb();
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
        var sut = new SupportAccessService(
            db, clock, NullLogger<SupportAccessService>.Instance, new StubFeatures());

        var adminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var subjectA = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var subjectB = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        db.Users.AddRange(
            new User { Id = adminId, Email = "admin@test.local", FullName = "Admin", Role = UserRole.Admin, IsActive = true },
            new User { Id = subjectA, Email = "a@test.local", FullName = "Alice", Role = UserRole.Candidate, IsActive = true },
            new User { Id = subjectB, Email = "b@test.local", FullName = "Bob", Role = UserRole.Candidate, IsActive = true });
        await db.SaveChangesAsync();

        await sut.RequestAsync(
            adminId,
            new SupportAccessRequest(subjectA, null, SupportAccessScope.Contact, "Ticket FB-1: cannot log in", "FB-1", 60),
            mfaVerifiedInSession: true);

        Assert.True(await sut.HasActiveGrantAsync(adminId, subjectA, null, SupportAccessScope.Contact));
        Assert.False(await sut.HasActiveGrantAsync(adminId, subjectB, null, SupportAccessScope.Contact));
        Assert.False(await sut.HasActiveGrantAsync(adminId, subjectA, null, SupportAccessScope.Iban));

        clock.Advance(TimeSpan.FromMinutes(61));
        Assert.False(await sut.HasActiveGrantAsync(adminId, subjectA, null, SupportAccessScope.Contact));

        var grant2 = await sut.RequestAsync(
            adminId,
            new SupportAccessRequest(
                subjectA,
                null,
                SupportAccessScope.Contact | SupportAccessScope.Applications,
                "Payout issue needs contact details",
                null,
                15),
            mfaVerifiedInSession: false);

        Assert.True(await sut.HasActiveGrantAsync(adminId, subjectA, null, SupportAccessScope.Applications));
        await sut.RevokeAsync(grant2.Id, adminId);
        Assert.False(await sut.HasActiveGrantAsync(adminId, subjectA, null, SupportAccessScope.Contact));
    }

    [Fact]
    public async Task Request_rejects_short_reason_and_missing_mfa_when_enrolled()
    {
        await using var db = CreateDb();
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var sut = new SupportAccessService(
            db, clock, NullLogger<SupportAccessService>.Instance, new StubFeatures());

        var adminId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        db.Users.AddRange(
            new User
            {
                Id = adminId,
                Email = "admin@test.local",
                FullName = "Admin",
                Role = UserRole.Admin,
                IsActive = true,
                AuthenticatorEnabled = true
            },
            new User
            {
                Id = subjectId,
                Email = "a@test.local",
                FullName = "Alice",
                Role = UserRole.Candidate,
                IsActive = true
            });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RequestAsync(
            adminId,
            new SupportAccessRequest(subjectId, null, SupportAccessScope.Contact, "too short", null, 60),
            mfaVerifiedInSession: true));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RequestAsync(
            adminId,
            new SupportAccessRequest(subjectId, null, SupportAccessScope.Contact,
                "Long enough reason for support", null, 60),
            mfaVerifiedInSession: false));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("SupportAccess-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;
        public FakeTimeProvider(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }

    private sealed class StubFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(true, true, false, "http://localhost", null));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }
}
