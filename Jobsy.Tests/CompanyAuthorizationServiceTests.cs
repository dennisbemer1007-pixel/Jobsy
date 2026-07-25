using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class CompanyAuthorizationServiceTests
{
    [Fact]
    public async Task Admin_gets_null_company_scope_meaning_all()
    {
        await using var db = CreateDb();
        var sut = new CompanyAuthorizationService(db);
        var user = Principal("admin@jobsy.local", JobsyRoles.Admin);

        var ids = await sut.GetAccessibleCompanyIdsAsync(user);
        Assert.Null(ids);
        Assert.True(sut.IsAdmin(user));
    }

    [Fact]
    public async Task Candidate_gets_empty_company_scope()
    {
        await using var db = CreateDb();
        var sut = new CompanyAuthorizationService(db);
        var user = Principal("kandidaat@jobsy.local", JobsyRoles.Candidate);

        var ids = await sut.GetAccessibleCompanyIdsAsync(user);
        Assert.NotNull(ids);
        Assert.Empty(ids);
    }

    [Fact]
    public async Task Employer_claims_without_db_membership_yield_empty()
    {
        await using var db = CreateDb();
        var companyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var sut = new CompanyAuthorizationService(db);
        var user = Principal(
            "spoof@jobsy.local",
            JobsyRoles.BranchManager,
            (JobsyClaimTypes.CompanyIds, companyId.ToString()));

        var ids = await sut.GetAccessibleCompanyIdsAsync(user);
        Assert.NotNull(ids);
        Assert.Empty(ids!);
    }

    [Fact]
    public async Task Employer_claims_are_intersected_with_db_memberships()
    {
        var companyA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var companyB = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var foreign = Guid.Parse("99999999-9999-9999-9999-999999999999");
        await using var db = CreateDb();
        db.Companies.AddRange(
            new Company { Id = companyA, Name = "A", KvkNumber = "1", Address = "x", Location = new GeoPoint(51.9, 4.2) },
            new Company { Id = companyB, Name = "B", KvkNumber = "2", Address = "y", Location = new GeoPoint(52.0, 4.3) });
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "regio@jobsy.local",
            FullName = "Regio",
            Role = UserRole.RegionalManager,
            CompanyId = companyA,
            IsActive = true
        });
        db.UserCompanies.AddRange(
            new UserCompany { UserId = userId, CompanyId = companyA },
            new UserCompany { UserId = userId, CompanyId = companyB });
        await db.SaveChangesAsync();

        var sut = new CompanyAuthorizationService(db);
        var principal = Principal(
            "regio@jobsy.local",
            JobsyRoles.RegionalManager,
            (JobsyClaimTypes.CompanyIds, $"{companyA},{foreign}"));

        var ids = await sut.GetAccessibleCompanyIdsAsync(principal);
        Assert.Equal([companyA], ids);
    }

    [Fact]
    public async Task Employer_without_claims_uses_db_memberships()
    {
        var companyA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var companyB = Guid.Parse("22222222-2222-2222-2222-222222222222");
        await using var db = CreateDb();
        db.Companies.AddRange(
            new Company { Id = companyA, Name = "A", KvkNumber = "1", Address = "x", Location = new GeoPoint(51.9, 4.2) },
            new Company { Id = companyB, Name = "B", KvkNumber = "2", Address = "y", Location = new GeoPoint(52.0, 4.3) });
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "regio@jobsy.local",
            FullName = "Regio",
            Role = UserRole.RegionalManager,
            CompanyId = companyA,
            IsActive = true
        });
        db.UserCompanies.AddRange(
            new UserCompany { UserId = userId, CompanyId = companyA },
            new UserCompany { UserId = userId, CompanyId = companyB });
        await db.SaveChangesAsync();

        var sut = new CompanyAuthorizationService(db);
        var principal = Principal("regio@jobsy.local", JobsyRoles.RegionalManager);
        var ids = await sut.GetAccessibleCompanyIdsAsync(principal);

        Assert.NotNull(ids);
        Assert.Equal(2, ids!.Count);
        Assert.Contains(companyA, ids);
        Assert.Contains(companyB, ids);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static ClaimsPrincipal Principal(string email, string role, params (string type, string value)[] extra)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Role, role)
        };
        claims.AddRange(extra.Select(e => new Claim(e.type, e.value)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }
}
