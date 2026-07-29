using System.Net;
using System.Net.Http.Json;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

/// <summary>
/// End-to-end checks for X-API-Key auth on /api/external/vacancies using an in-memory DB.
/// </summary>
public class ExternalApiKeyIntegrationTests : IClassFixture<ExternalApiKeyWebAppFactory>
{
    private readonly ExternalApiKeyWebAppFactory _factory;

    public ExternalApiKeyIntegrationTests(ExternalApiKeyWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task External_create_requires_api_key()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/external/vacancies", MinimalCreate(_factory.CompanyId));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task External_create_and_status_roundtrip_with_valid_key()
    {
        var client = AuthedClient();

        var create = await client.PostAsJsonAsync("api/external/vacancies", MinimalCreate(_factory.CompanyId));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ExternalVacancyStatusDto>();
        Assert.NotNull(created);
        Assert.Equal("Api", created!.CreatedVia);
        Assert.Equal("Draft", created.Status);
        Assert.Equal(_factory.CompanyId, created.CompanyId);

        var status = await client.GetAsync($"api/external/vacancies/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        var dto = await status.Content.ReadFromJsonAsync<ExternalVacancyStatusDto>();
        Assert.Equal(created.Id, dto!.Id);
    }

    [Fact]
    public async Task External_status_hides_foreign_company_vacancy()
    {
        var client = AuthedClient();
        var status = await client.GetAsync($"api/external/vacancies/{_factory.ForeignVacancyId}");
        Assert.Equal(HttpStatusCode.NotFound, status.StatusCode);
    }

    [Fact]
    public async Task External_create_rejects_foreign_company_id()
    {
        var client = AuthedClient();
        var response = await client.PostAsJsonAsync(
            "api/external/vacancies",
            MinimalCreate(_factory.ForeignCompanyId));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task External_update_works_for_own_vacancy()
    {
        var client = AuthedClient();

        var create = await client.PostAsJsonAsync("api/external/vacancies", MinimalCreate(_factory.CompanyId));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ExternalVacancyStatusDto>();

        var update = await client.PutAsJsonAsync(
            $"api/external/vacancies/{created!.Id}",
            new UpdateExternalVacancyRequest(Title: "Bijgewerkt via API"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var dto = await update.Content.ReadFromJsonAsync<ExternalVacancyStatusDto>();
        Assert.Equal("Bijgewerkt via API", dto!.Title);
    }

    [Fact]
    public async Task Inactive_api_key_is_rejected()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthDefaults.HeaderName, _factory.InactivePlaintextApiKey);

        var response = await client.GetAsync($"api/external/vacancies/{_factory.OwnVacancyId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Development_auth_cannot_call_external_api_without_api_key()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Jobsy-Email", "admin@jobsy.local");
        client.DefaultRequestHeaders.Add("X-Jobsy-Dev-Secret", "test-secret");

        var response = await client.GetAsync($"api/external/vacancies/{_factory.OwnVacancyId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient AuthedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthDefaults.HeaderName, _factory.PlaintextApiKey);
        return client;
    }

    private static CreateVacancyRequest MinimalCreate(Guid companyId) =>
        new(
            companyId,
            "API Test Vacature",
            "Omschrijving voor integratietest van de externe API.",
            16.00m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            TransportMode.Bike,
            ["Horeca"],
            SalaryTableId: ExternalApiKeyWebAppFactory.SalaryTableId);
}

public sealed class ExternalApiKeyWebAppFactory : WebApplicationFactory<Program>
{
    public static readonly Guid SalaryTableId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public Guid CompanyId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public Guid ForeignCompanyId { get; } = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public Guid ForeignVacancyId { get; } = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    public Guid OwnVacancyId { get; } = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    public string PlaintextApiKey { get; private set; } = "";
    public string InactivePlaintextApiKey { get; private set; } = "";

    private readonly string _dbName = "ExternalApiKeyTests-" + Guid.NewGuid();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("JobsyAuth:AllowDevelopmentAuth", "true");
        builder.UseSetting("JobsyAuth:DevelopmentAuthSecret", "test-secret");
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting(
            "ConnectionStrings:JobsyDb",
            "Host=127.0.0.1;Port=5432;Database=JobsyTest;Username=postgres;Password=postgres");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();

            // Strip every EF Core DbContext registration so Npgsql and InMemory never coexist.
            var efDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(JobsyDbContext)
                    || d.ServiceType == typeof(DbContextOptions<JobsyDbContext>)
                    || (d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericTypeDefinition().Name.Contains("DbContext", StringComparison.Ordinal))
                    || (d.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
                    || (d.ServiceType.FullName?.Contains("EntityFrameworkCore", StringComparison.Ordinal) == true
                        && d.ServiceType.FullName.Contains("JobsyDbContext", StringComparison.Ordinal)))
                .ToList();
            foreach (var d in efDescriptors)
            {
                services.Remove(d);
            }

            // Also remove IDbContextOptionsConfiguration<JobsyDbContext> by open generic match.
            foreach (var d in services.Where(d =>
                         d.ServiceType.IsGenericType
                         && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)
                         && d.ServiceType.GenericTypeArguments[0] == typeof(JobsyDbContext)).ToList())
            {
                services.Remove(d);
            }

            services.AddDbContext<JobsyDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.RemoveAll<IVacancyContentModerationService>();
            services.AddSingleton<IVacancyContentModerationService>(new AllowAllModeration());
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        EnsureSeeded();
        base.ConfigureClient(client);
    }

    private void EnsureSeeded()
    {
        if (_seeded)
        {
            return;
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        if (db.Companies.Any())
        {
            _seeded = true;
            return;
        }

        db.Companies.AddRange(
            new Company
            {
                Id = CompanyId,
                Name = "Own Co",
                KvkNumber = "11111111",
                Address = "A",
                Location = new GeoPoint(52, 4)
            },
            new Company
            {
                Id = ForeignCompanyId,
                Name = "Foreign Co",
                KvkNumber = "22222222",
                Address = "B",
                Location = new GeoPoint(52.1, 4.1)
            });

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@jobsy.local",
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true
        });

        var table = new CompanySalaryTable
        {
            Id = SalaryTableId,
            CompanyId = CompanyId,
            Name = "WML",
            IsActive = true,
            IsSystemWml = true
        };
        table.Rates.Add(new CompanySalaryRate
        {
            Id = Guid.NewGuid(),
            SalaryTableId = SalaryTableId,
            AgeYears = 21,
            HourlyRate = 14.50m
        });
        db.CompanySalaryTables.Add(table);

        db.Vacancies.AddRange(
            new Vacancy
            {
                Id = OwnVacancyId,
                Title = "Own",
                Description = "Own vacancy",
                HourlyWage = 15,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                Status = VacancyStatus.Draft,
                CompanyId = CompanyId,
                CreatedVia = VacancySource.Manual,
                Location = new GeoPoint(52, 4),
                RequiredTransport = TransportMode.Bike,
                SalaryTableId = SalaryTableId
            },
            new Vacancy
            {
                Id = ForeignVacancyId,
                Title = "Foreign",
                Description = "Foreign vacancy",
                HourlyWage = 15,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                Status = VacancyStatus.Draft,
                CompanyId = ForeignCompanyId,
                CreatedVia = VacancySource.Manual,
                Location = new GeoPoint(52.1, 4.1),
                RequiredTransport = TransportMode.Bike
            });

        PlaintextApiKey = ApiKeyHasher.GeneratePlaintext();
        InactivePlaintextApiKey = ApiKeyHasher.GeneratePlaintext();
        db.ApiKeys.AddRange(
            new ApiKey
            {
                Id = Guid.NewGuid(),
                CompanyId = CompanyId,
                ApiKeyHash = ApiKeyHasher.Hash(PlaintextApiKey),
                Name = "Active",
                KeyPrefix = ApiKeyHasher.ToDisplayPrefix(PlaintextApiKey),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new ApiKey
            {
                Id = Guid.NewGuid(),
                CompanyId = CompanyId,
                ApiKeyHash = ApiKeyHasher.Hash(InactivePlaintextApiKey),
                Name = "Inactive",
                KeyPrefix = ApiKeyHasher.ToDisplayPrefix(InactivePlaintextApiKey),
                IsActive = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });

        db.SaveChanges();
        _seeded = true;
    }

    private sealed class AllowAllModeration : IVacancyContentModerationService
    {
        public Task<VacancyContentModerationResult> CheckAsync(
            string title,
            string description,
            CancellationToken cancellationToken = default)
            => Task.FromResult(VacancyContentModerationResult.Allowed());
    }
}
