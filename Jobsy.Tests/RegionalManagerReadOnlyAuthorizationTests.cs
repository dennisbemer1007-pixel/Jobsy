using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

/// <summary>
/// RegionalManager is read-only: mutating employer endpoints return 403;
/// GET surfaces stay available; foreign salary-table GET must not write.
/// </summary>
public class RegionalManagerReadOnlyAuthorizationTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;

    public RegionalManagerReadOnlyAuthorizationTests(RoleFunctionalWebAppFactory factory)
        => _factory = factory;

    [Theory]
    [InlineData("POST", "api/employer/talent/unlock")]
    [InlineData("POST", "api/employer/talent/{requestId}/withdraw")]
    [InlineData("PUT", "api/company/culture")]
    [InlineData("POST", "api/companies/{companyId}/onboarding/checkout")]
    [InlineData("POST", "api/companies/{companyId}/onboarding/complete")]
    public async Task RegionalManager_gets_403_on_mutating_endpoints(string method, string template)
    {
        using var client = Authed(_factory.RegionalId);
        var url = Expand(template);
        using var response = await SendAsync(client, method, url);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("branch")]
    [InlineData("enterprise")]
    public async Task Mutating_roles_still_get_non_403_on_mutating_endpoints(string who)
    {
        var userId = who == "branch" ? _factory.EmployerId : _factory.EnterpriseId;
        using var client = Authed(userId);

        var cases = new (string Method, string Url, HttpContent? Body)[]
        {
            ("POST", "api/employer/talent/unlock",
                JsonContent.Create(new { candidateUserId = _factory.CandidateId, message = "hi" })),
            ("POST", $"api/employer/talent/{Guid.NewGuid()}/withdraw", null),
            ("PUT", "api/company/culture",
                JsonContent.Create(new { answers = new Dictionary<string, int>(), complete = false })),
            ("POST", $"api/companies/{_factory.CompanyId}/onboarding/checkout", null),
            ("POST", $"api/companies/{_factory.CompanyId}/onboarding/complete",
                JsonContent.Create(new { paymentId = "pay_test" })),
        };

        foreach (var (method, url, body) in cases)
        {
            using var response = await SendAsync(client, method, url, body);
            Assert.NotEqual(
                HttpStatusCode.Forbidden,
                response.StatusCode);
            Assert.True(
                (int)response.StatusCode is >= 200 and < 500,
                $"{who} {method} {url} → {response.StatusCode}");
        }
    }

    [Fact]
    public async Task RegionalManager_gets_still_return_200()
    {
        using var client = Authed(_factory.RegionalId);

        var search = await client.GetAsync("api/employer/talent/search");
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);

        var requests = await client.GetAsync("api/employer/talent/requests");
        Assert.Equal(HttpStatusCode.OK, requests.StatusCode);

        var culture = await client.GetAsync("api/company/culture");
        Assert.Equal(HttpStatusCode.OK, culture.StatusCode);

        var tables = await client.GetAsync("api/salary-tables");
        Assert.Equal(HttpStatusCode.OK, tables.StatusCode);

        var ownTable = await client.GetAsync($"api/salary-tables/{_factory.SalaryTableId}");
        Assert.Equal(HttpStatusCode.OK, ownTable.StatusCode);
    }

    [Fact]
    public async Task SalaryTables_get_for_other_company_forbids_and_does_not_write()
    {
        var foreignTableId = Guid.Parse("c1000000-0000-0000-0000-000000000049");
        var foreignCompanyId = _factory.IntermediaryOrgId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            if (!await db.CompanySalaryTables.AnyAsync(t => t.Id == foreignTableId))
            {
                // Empty table so FillEmpty would add rates if auth were skipped.
                db.CompanySalaryTables.Add(new CompanySalaryTable
                {
                    Id = foreignTableId,
                    CompanyId = foreignCompanyId,
                    Name = "Foreign empty",
                    IsActive = true,
                    IsSystemWml = false
                });
                await db.SaveChangesAsync();
            }
            else
            {
                var rates = await db.CompanySalaryRates
                    .Where(r => r.SalaryTableId == foreignTableId)
                    .ToListAsync();
                if (rates.Count > 0)
                {
                    db.CompanySalaryRates.RemoveRange(rates);
                    await db.SaveChangesAsync();
                }
            }
        }

        int ratesBefore;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            ratesBefore = await db.CompanySalaryRates.CountAsync(r => r.SalaryTableId == foreignTableId);
        }

        using var client = Authed(_factory.RegionalId);
        using var response = await client.GetAsync($"api/salary-tables/{foreignTableId}");
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            $"Expected 403/404, got {response.StatusCode}");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var ratesAfter = await db.CompanySalaryRates.CountAsync(r => r.SalaryTableId == foreignTableId);
            Assert.Equal(ratesBefore, ratesAfter);
        }
    }

    [Theory]
    [InlineData(UserRole.BranchManager, true)]
    [InlineData(UserRole.EnterpriseManager, true)]
    [InlineData(UserRole.Intermediary, true)]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.RegionalManager, false)]
    [InlineData(UserRole.Candidate, false)]
    public void CanMutateEmployerData_matches_expected_roles(UserRole role, bool expected)
        => Assert.Equal(expected, JobsyRoles.CanMutateEmployerData(role));

    [Fact]
    public void EmployerMutateRoles_excludes_regional_manager()
    {
        Assert.DoesNotContain(JobsyRoles.RegionalManager, JobsyRoles.EmployerMutateRoles, StringComparison.Ordinal);
        Assert.DoesNotContain(JobsyRoles.RegionalManager, JobsyRoles.EmployerMutateRolesWithAdmin, StringComparison.Ordinal);
        Assert.Contains(JobsyRoles.Admin, JobsyRoles.EmployerMutateRolesWithAdmin, StringComparison.Ordinal);
    }

    private string Expand(string template)
        => template
            .Replace("{requestId}", Guid.NewGuid().ToString("D"), StringComparison.Ordinal)
            .Replace("{companyId}", _factory.CompanyId.ToString("D"), StringComparison.Ordinal);

    private HttpClient Authed(Guid userId)
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, userId);
        return client;
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        string method,
        string url,
        HttpContent? body = null)
    {
        body ??= method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            ? JsonContent.Create(new { answers = new Dictionary<string, int>(), complete = false })
            : method.Equals("POST", StringComparison.OrdinalIgnoreCase) && url.Contains("/unlock", StringComparison.Ordinal)
                ? JsonContent.Create(new { candidateUserId = Guid.Parse("c1000000-0000-0000-0000-000000000020"), message = "x" })
                : method.Equals("POST", StringComparison.OrdinalIgnoreCase) && url.Contains("/complete", StringComparison.Ordinal)
                    ? JsonContent.Create(new { paymentId = "pay_x" })
                    : null;

        return method.ToUpperInvariant() switch
        {
            "GET" => await client.GetAsync(url),
            "PUT" => await client.PutAsync(url, body),
            "POST" => await client.PostAsync(url, body),
            _ => throw new InvalidOperationException(method)
        };
    }
}
