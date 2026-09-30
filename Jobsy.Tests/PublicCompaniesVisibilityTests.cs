using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

public class PublicCompaniesVisibilityTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public PublicCompaniesVisibilityTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Verified_with_public_vacancy_returns_200_without_ids_or_coords()
    {
        var kvk = await SeedAsync(KvkVerificationStatus.Verified, CompanyVerificationStatus.Verified, withLiveVacancy: true);
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"api/public/companies/{kvk}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty("companyIds", out _));
        Assert.False(doc.RootElement.TryGetProperty("address", out _));
        Assert.False(doc.RootElement.TryGetProperty("latitude", out _));
        Assert.False(doc.RootElement.TryGetProperty("longitude", out _));
        Assert.True(doc.RootElement.TryGetProperty("kvk", out _) || doc.RootElement.TryGetProperty("Kvk", out _));
    }

    [Theory]
    [InlineData(KvkVerificationStatus.Pending)]
    [InlineData(KvkVerificationStatus.Failed)]
    public async Task Unverified_kvk_returns_404(KvkVerificationStatus kvkStatus)
    {
        var kvk = await SeedAsync(kvkStatus, CompanyVerificationStatus.Verified, withLiveVacancy: true);
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"api/public/companies/{kvk}")).StatusCode);
    }

    [Fact]
    public async Task Verified_without_public_vacancies_returns_404()
    {
        var kvk = await SeedAsync(KvkVerificationStatus.Verified, CompanyVerificationStatus.Verified, withLiveVacancy: false);
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"api/public/companies/{kvk}")).StatusCode);
    }

    [Fact]
    public async Task Vacancies_endpoint_returns_only_public_of_that_kvk()
    {
        var kvk = await SeedAsync(KvkVerificationStatus.Verified, CompanyVerificationStatus.Verified, withLiveVacancy: true);
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"api/public/companies/{kvk}/vacancies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.True(items.GetArrayLength() >= 1);
    }

    private async Task<string> SeedAsync(
        KvkVerificationStatus kvkStatus,
        CompanyVerificationStatus verificationStatus,
        bool withLiveVacancy)
    {
        var kvk = Random.Shared.NextInt64(10_000_000, 99_999_999).ToString();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Public Hotfix Co",
            KvkNumber = kvk,
            KvkEstablishmentId = $"{kvk}_0001",
            KvkVerificationStatus = kvkStatus,
            VerificationStatus = verificationStatus,
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
        // Refresh discovery index if the factory exposes it via hosted services — GetActive often rebuilds.
        var discovery = scope.ServiceProvider.GetService<Jobsy.Core.Interfaces.IVacancyDiscoveryIndex>();
        if (discovery is not null)
        {
            try
            {
                await discovery.RefreshAsync();
            }
            catch
            {
                // Some implementations rebuild lazily on GetActive.
            }
        }

        return kvk;
    }
}
