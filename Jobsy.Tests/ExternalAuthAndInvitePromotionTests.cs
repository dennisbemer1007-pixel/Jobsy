using Jobsy.Api.Models;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Web.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Jobsy.Api.Controllers;

namespace Jobsy.Tests;

public class ExternalAuthAndInvitePromotionTests
{
    [Fact]
    public async Task Ensure_external_creates_new_candidate_with_how_to_flag()
    {
        await using var db = CreateDb();
        var sut = CreateAuthController(db, secret: "test-secret");
        sut.ControllerContext = WithProvisionSecret("test-secret");

        var result = await sut.EnsureExternal(
            new EnsureExternalUserRequest("nieuw@example.com", "Nieuw Persoon"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<EnsureExternalUserResponse>(ok.Value);
        Assert.True(body.IsNewUser);
        Assert.Equal("Candidate", body.Role);
        Assert.True(body.ShowCandidateHowTo);
        Assert.False(body.HasCandidateApplications);
        Assert.Equal(1, await db.Users.CountAsync(u => u.Email == "nieuw@example.com"));
        Assert.NotNull(await db.Users.Where(u => u.Email == "nieuw@example.com")
            .Select(u => u.LastLoginAtUtc).SingleAsync());
    }

    [Fact]
    public async Task Ensure_external_second_login_skips_how_to_for_candidate()
    {
        await using var db = CreateDb();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "terug@example.com",
            FullName = "Terugkerend",
            Role = UserRole.Candidate,
            IsActive = true,
            LastLoginAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var sut = CreateAuthController(db, secret: "test-secret");
        sut.ControllerContext = WithProvisionSecret("test-secret");

        var result = await sut.EnsureExternal(
            new EnsureExternalUserRequest("terug@example.com", "Terugkerend"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<EnsureExternalUserResponse>(ok.Value);
        Assert.False(body.IsNewUser);
        Assert.Equal("Candidate", body.Role);
        Assert.False(body.ShowCandidateHowTo);
    }

    [Fact]
    public void CandidatePostLoginUrl_first_vs_returning()
    {
        Assert.Equal(
            AuthRedirects.CandidateHowToPath,
            AuthRedirects.CandidatePostLoginUrl(showCandidateHowTo: true));
        Assert.Equal(
            AuthRedirects.BanenkaartPath,
            AuthRedirects.CandidatePostLoginUrl(showCandidateHowTo: false));
    }

    [Fact]
    public async Task Ensure_external_returns_invited_manager_role()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Org",
            KvkNumber = "12345678",
            Address = "Straat 1",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer
        });
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "manager@example.com",
            FullName = "Manager",
            Role = UserRole.BranchManager,
            CompanyId = companyId,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var sut = CreateAuthController(db, secret: "test-secret");
        sut.ControllerContext = WithProvisionSecret("test-secret");

        var result = await sut.EnsureExternal(
            new EnsureExternalUserRequest("manager@example.com", "Manager"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<EnsureExternalUserResponse>(ok.Value);
        Assert.False(body.IsNewUser);
        Assert.Equal("BranchManager", body.Role);
        Assert.False(body.ShowCandidateHowTo);
        Assert.Equal(companyId, body.CompanyId);
    }

    [Fact]
    public async Task Ensure_external_marks_promoted_candidate_applications()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Org",
            KvkNumber = "12345678",
            Address = "Straat 1",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer
        });
        db.Users.Add(new User
        {
            Id = userId,
            Email = "both@example.com",
            FullName = "Was Candidate",
            Role = UserRole.BranchManager,
            CompanyId = companyId,
            IsActive = true,
            CandidateHowToCompletedAt = DateTime.UtcNow
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Kassamedewerker",
            Description = "Demo",
            HourlyWage = 14m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Status = VacancyStatus.Active,
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Bike
        });
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            CandidateUserId = userId,
            CandidateName = "Was Candidate",
            CandidateEmail = "both@example.com",
            PreferredTransport = "Bike",
            EstimatedTravelMinutes = 12,
            Status = ApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateAuthController(db, secret: "test-secret");
        sut.ControllerContext = WithProvisionSecret("test-secret");

        var result = await sut.EnsureExternal(
            new EnsureExternalUserRequest("both@example.com", "Was Candidate"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<EnsureExternalUserResponse>(ok.Value);
        Assert.Equal("BranchManager", body.Role);
        Assert.True(body.HasCandidateApplications);
    }

    [Fact]
    public void Invite_rules_still_allow_enterprise_to_assign_branch()
        => Assert.True(EmployerInviteRules.CanAssignRole(UserRole.EnterpriseManager, UserRole.BranchManager));

    private static AuthController CreateAuthController(JobsyDbContext db, string secret)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:DevelopmentAuthSecret"] = secret
            })
            .Build();
        return new AuthController(db, config);
    }

    private static ControllerContext WithProvisionSecret(string secret)
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Jobsy-Provision-Secret"] = secret;
        return new ControllerContext { HttpContext = http };
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
