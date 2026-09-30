using System.Security.Claims;
using Jobsy.Api.Controllers;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class EmailPreferencesApiTests
{
    [Fact]
    public async Task Role_based_categories_put_round_trip_and_anonymous_401()
    {
        await using var db = CreateDb();
        var candidate = new User
        {
            Id = Guid.NewGuid(),
            Email = "cand@example.com",
            FullName = "Cand",
            Role = UserRole.Candidate,
            IsActive = true
        };
        var employer = new User
        {
            Id = Guid.NewGuid(),
            Email = "emp@example.com",
            FullName = "Emp",
            Role = UserRole.BranchManager,
            IsActive = true
        };
        db.Users.AddRange(candidate, employer);
        await db.SaveChangesAsync();

        var prefs = new EmailPreferenceService(db);
        var users = new Lookup(db);
        var sut = new MeEmailPreferencesController(users, prefs);

        sut.ControllerContext = Authenticated(candidate.Id, candidate.Email);
        var getCand = await sut.Get(CancellationToken.None);
        var candBody = Assert.IsType<OkObjectResult>(getCand.Result).Value as MeEmailPreferencesController.PreferencesResponse;
        Assert.Single(candBody!.Optional);
        Assert.Equal("PushBom", candBody.Optional[0].Key);
        Assert.True(candBody.Optional[0].Enabled);

        var put = await sut.Put(
            new MeEmailPreferencesController.UpdatePreferencesRequest(
            [
                new MeEmailPreferencesController.PreferenceDto("PushBom", "x", false)
            ]),
            CancellationToken.None);
        var putBody = Assert.IsType<OkObjectResult>(put.Result).Value as MeEmailPreferencesController.PreferencesResponse;
        Assert.False(putBody!.Optional[0].Enabled);
        Assert.True(await prefs.IsOptedOutAsync(candidate.Email, "PushBom"));

        sut.ControllerContext = Authenticated(employer.Id, employer.Email);
        var getEmp = await sut.Get(CancellationToken.None);
        var empBody = Assert.IsType<OkObjectResult>(getEmp.Result).Value as MeEmailPreferencesController.PreferencesResponse;
        Assert.Equal(2, empBody!.Optional.Count);
        Assert.Contains(empBody.Optional, i => i.Key == "VacancyEngagementReminder");
        Assert.Contains(empBody.Optional, i => i.Key == "CompanyReEngagement");

        sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var anon = await sut.Get(CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(anon.Result);
    }

    private static ControllerContext Authenticated(Guid userId, string email)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, email)
        ], "test");
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private static JobsyDbContext CreateDb()
        => new(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class Lookup(JobsyDbContext db) : IUserLookupService
    {
        public async Task<User?> FindByPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            var id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(id, out var userId))
            {
                return null;
            }

            return await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }
    }
}
