using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class OneTimeLinkServiceTests
{
    [Fact]
    public async Task Create_stores_hash_only_and_invalidates_older()
    {
        await using var db = CreateDb();
        var sut = CreateSut(db);
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "u@jobsy.local",
            FullName = "User",
            Role = UserRole.Candidate,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var first = await sut.CreateAsync(
            OneTimeLinkPurpose.SetPassword, userId, null, "u@jobsy.local", OneTimeLinkRules.SetPasswordLifetime);
        var second = await sut.CreateAsync(
            OneTimeLinkPurpose.SetPassword, userId, null, "u@jobsy.local", OneTimeLinkRules.SetPasswordLifetime);

        var rows = await db.OneTimeLinks.OrderBy(l => l.CreatedAtUtc).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.DoesNotContain(first.Token, rows.Select(r => r.TokenHash));
        Assert.DoesNotContain(second.Token, rows.Select(r => r.TokenHash));
        Assert.Equal(VerificationCodes.Hash(first.Token), rows[0].TokenHash);
        Assert.NotNull(rows[0].UsedAtUtc);
        Assert.Null(rows[1].UsedAtUtc);
    }

    [Fact]
    public async Task Consume_is_single_use_under_concurrency()
    {
        await using var db = CreateDb();
        var sut = CreateSut(db);
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "c@jobsy.local",
            FullName = "User",
            Role = UserRole.Candidate,
            IsActive = true
        });
        await db.SaveChangesAsync();
        var created = await sut.CreateAsync(
            OneTimeLinkPurpose.SetPassword, userId, null, "c@jobsy.local", OneTimeLinkRules.SetPasswordLifetime);

        var tasks = Enumerable.Range(0, 8)
            .Select(_ => sut.ConsumeAsync(OneTimeLinkPurpose.SetPassword, created.Token))
            .ToArray();
        var results = await Task.WhenAll(tasks);
        Assert.Equal(1, results.Count(r => r is not null));
    }

    [Fact]
    public async Task Peek_and_consume_respect_expiry()
    {
        await using var db = CreateDb();
        var sut = CreateSut(db);
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "e@jobsy.local",
            FullName = "User",
            Role = UserRole.Candidate,
            IsActive = true
        });
        await db.SaveChangesAsync();
        var created = await sut.CreateAsync(
            OneTimeLinkPurpose.SetPassword, userId, null, "e@jobsy.local", TimeSpan.FromMilliseconds(1));
        await Task.Delay(20);

        var peek = await sut.PeekAsync(OneTimeLinkPurpose.SetPassword, created.Token);
        Assert.False(peek.Valid);
        Assert.Null(await sut.ConsumeAsync(OneTimeLinkPurpose.SetPassword, created.Token));
    }

    private static OneTimeLinkService CreateSut(JobsyDbContext db)
        => new(db, NullLogger<OneTimeLinkService>.Instance);

    private static JobsyDbContext CreateDb()
        => new(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
