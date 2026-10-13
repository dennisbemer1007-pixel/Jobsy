using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

public class CrawlIndexCompanyPathsTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;

    public CrawlIndexCompanyPathsTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Theory]
    [InlineData(KvkVerificationStatus.Pending)]
    [InlineData(KvkVerificationStatus.Failed)]
    public async Task Unverified_companies_absent_even_with_public_vacancy(KvkVerificationStatus status)
    {
        var kvk = await SeedAsync(status, withLiveVacancy: true);
        var client = _factory.CreateClient();
        var response = await client.GetAsync("api/site/crawl-index");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(dto.TryGetProperty("companyPaths", out var paths)
            || dto.TryGetProperty("CompanyPaths", out paths));
        foreach (var path in paths.EnumerateArray())
        {
            var value = path.GetString() ?? "";
            Assert.DoesNotContain(kvk, value, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Verified_with_public_vacancy_appears_in_company_paths()
    {
        var kvk = await SeedAsync(KvkVerificationStatus.Verified, withLiveVacancy: true);
        var client = _factory.CreateClient();
        var response = await client.GetAsync("api/site/crawl-index");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(dto.TryGetProperty("companyPaths", out var paths)
            || dto.TryGetProperty("CompanyPaths", out paths));
        Assert.Contains(paths.EnumerateArray().Select(p => p.GetString() ?? ""), p => p.Contains(kvk, StringComparison.Ordinal));
    }

    private async Task<string> SeedAsync(KvkVerificationStatus kvkStatus, bool withLiveVacancy)
    {
        var kvk = Random.Shared.NextInt64(10_000_000, 99_999_999).ToString();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Crawl Index Co",
            KvkNumber = kvk,
            KvkEstablishmentId = $"{kvk}_0001",
            KvkVerificationStatus = kvkStatus,
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.Manual,
            Address = "Voorstraat 1, 2671 AB Naaldwijk",
            Location = new GeoPoint(52.0, 4.2),
            Type = CompanyType.Employer
        });

        if (withLiveVacancy)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            db.Vacancies.Add(new Vacancy
            {
                Id = Guid.NewGuid(),
                Title = "Kassamedewerker",
                Description = "Test",
                CompanyId = companyId,
                Status = VacancyStatus.Active,
                StartDate = today.AddDays(-1),
                EndDate = today.AddDays(14),
                HourlyWage = 14m,
                Location = new GeoPoint(52.0, 4.2),
                RequiredTransport = TransportMode.Bike,
                WorkTypes = WorkType.Winkel
            });
        }

        await db.SaveChangesAsync();
        var discovery = scope.ServiceProvider.GetService<Jobsy.Core.Interfaces.IVacancyDiscoveryIndex>();
        if (discovery is not null)
        {
            try { await discovery.RefreshAsync(); }
            catch { /* lazy rebuild */ }
        }

        return kvk;
    }
}
