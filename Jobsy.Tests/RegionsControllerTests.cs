using System.Security.Claims;
using Jobsy.Api.Controllers;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class RegionsControllerTests
{
    [Fact]
    public async Task List_handles_orphaned_region_links_without_500()
    {
        await using var db = CreateDb();
        var regionId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        db.Companies.Add(new Company
        {
            Id = orgId,
            Name = "Org BV",
            KvkNumber = "12345678",
            Address = "Straat 1",
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52.0, 4.0)
        });

        db.Regions.Add(new Region
        {
            Id = regionId,
            Name = "Regio West",
            OrganizationCompanyId = orgId
        });
        db.RegionCompanies.Add(new RegionCompany
        {
            RegionId = regionId,
            CompanyId = Guid.NewGuid()
        });
        await db.SaveChangesAsync();

        var controller = new RegionsController(db, new TestCompanyAuthorizationService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Role, JobsyRoles.EnterpriseManager)],
                    "TestAuth"))
            }
        };

        var result = await controller.List(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IEnumerable<RegionDto>>(ok.Value);
        var dto = Assert.Single(payload);
        Assert.Equal("Regio West", dto.Name);
        Assert.Equal("Org BV", dto.OrganizationCompanyName);
        Assert.Empty(dto.Companies);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class TestCompanyAuthorizationService : ICompanyAuthorizationService
    {
        public bool IsAdmin(ClaimsPrincipal user) => false;
        public bool IsEmployer(ClaimsPrincipal user) => true;
        public bool IsCandidate(ClaimsPrincipal user) => false;
        public UserRole? GetPrimaryRole(ClaimsPrincipal user) => UserRole.EnterpriseManager;
        public Task<IReadOnlyCollection<Guid>?> GetAccessibleCompanyIdsAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<Guid>?>(null);
        public Task<bool> CanAccessCompanyAsync(ClaimsPrincipal user, Guid companyId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
        public Task EnsureCanAccessCompanyAsync(ClaimsPrincipal user, Guid companyId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
