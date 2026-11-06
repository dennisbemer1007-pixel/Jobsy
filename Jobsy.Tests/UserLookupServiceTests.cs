using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public sealed class UserLookupServiceTests
{
    [Fact]
    public async Task FindByPrincipalAsync_resolves_active_user_by_subject_guid()
    {
        var userId = Guid.Parse("c1000000-0000-0000-0000-000000000020");
        await using var db = CreateDb();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "jwt-only@jobsy.local",
            FullName = "JWT User",
            Role = UserRole.Candidate,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString("D"))
        ], "JobsyJwt"));

        var sut = new UserLookupService(db);
        var found = await sut.FindByPrincipalAsync(principal);
        Assert.NotNull(found);
        Assert.Equal(userId, found!.Id);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("user-lookup-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }
}
