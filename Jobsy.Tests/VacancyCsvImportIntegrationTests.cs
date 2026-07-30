using System.Net;
using System.Net.Http.Json;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class VacancyCsvImportIntegrationTests : IClassFixture<VacancyCsvImportWebAppFactory>
{
    private readonly VacancyCsvImportWebAppFactory _factory;

    public VacancyCsvImportIntegrationTests(VacancyCsvImportWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Import_happy_path_creates_draft_with_csv_source()
    {
        var client = AuthedClient();
        var response = await client.PostAsJsonAsync(
            "api/vacancies/csv-import",
            new CsvImportRequest(_factory.OrgId, [ValidRow(2, "Kassamedewerker")]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResultDto>();
        Assert.NotNull(result);
        Assert.Equal(1, result!.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.True(result.Rows[0].Success);
        Assert.NotNull(result.Rows[0].VacancyId);
        Assert.Contains("concept", result.PublishHint, StringComparison.OrdinalIgnoreCase);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var vacancy = await db.Vacancies.AsNoTracking()
            .SingleAsync(v => v.Id == result.Rows[0].VacancyId);
        Assert.Equal(VacancyStatus.Draft, vacancy.Status);
        Assert.Equal(VacancySource.Csv, vacancy.CreatedVia);
        Assert.Equal("Kassamedewerker", vacancy.Title);
    }

    [Fact]
    public async Task Import_mixed_rows_skips_corrupt_but_keeps_valid()
    {
        var client = AuthedClient();
        var rows = new List<CsvImportRowRequest>
        {
            ValidRow(2, "Geldig"),
            new(3, "", "Omschrijving", "2026-08-01", "2026-12-31", "Winkel", _factory.SalaryTableId.ToString()),
            new(4, "Kapot", "Omschrijving", "niet-een-datum", "2026-12-31", "Winkel", _factory.SalaryTableId.ToString()),
            new(5, "Branches te veel", "Omschrijving", "2026-08-01", "2026-12-31", "Winkel;Horeca;Logistiek", _factory.SalaryTableId.ToString()),
            new(6, "Eind voor start", "Omschrijving", "2026-12-31", "2026-08-01", "Winkel", _factory.SalaryTableId.ToString()),
            new(7, "Slechte video", "Omschrijving", "2026-08-01", "2026-12-31", "Winkel", _factory.SalaryTableId.ToString(),
                Video: "data:image/png;base64,aaa"),
            new(8, "Onbekend vervoer", "Omschrijving", "2026-08-01", "2026-12-31", "Winkel", _factory.SalaryTableId.ToString(),
                Transport: "Teleport"),
            ValidRow(9, "Ook geldig")
        };

        var response = await client.PostAsJsonAsync(
            "api/vacancies/csv-import",
            new CsvImportRequest(_factory.BranchId, rows));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResultDto>();
        Assert.NotNull(result);
        Assert.Equal(8, result!.TotalRows);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(6, result.FailedCount);

        Assert.True(result.Rows.Single(r => r.RowNumber == 2).Success);
        Assert.True(result.Rows.Single(r => r.RowNumber == 9).Success);

        var emptyTitle = result.Rows.Single(r => r.RowNumber == 3);
        Assert.False(emptyTitle.Success);
        Assert.Contains("Titel", emptyTitle.ErrorMessage);
        Assert.Equal("", emptyTitle.Data.Title); // data preserved for repair

        Assert.Contains("Startdatum", result.Rows.Single(r => r.RowNumber == 4).ErrorMessage);
        Assert.Contains("Branches", result.Rows.Single(r => r.RowNumber == 5).ErrorMessage);
        Assert.Contains("Einddatum", result.Rows.Single(r => r.RowNumber == 6).ErrorMessage);
        Assert.Contains("video", result.Rows.Single(r => r.RowNumber == 7).ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Teleport", result.Rows.Single(r => r.RowNumber == 8).ErrorMessage);
    }

    [Fact]
    public async Task Import_rejects_when_feature_disabled()
    {
        var client = AuthedClient();
        var response = await client.PostAsJsonAsync(
            "api/vacancies/csv-import",
            new CsvImportRequest(_factory.DisabledOrgId, [ValidRow(2, "Mag niet", _factory.DisabledOrgId, _factory.DisabledSalaryTableId)]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("niet ingeschakeld", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Import_rejects_cross_org_vestiging_id()
    {
        var client = AuthedClient();
        var row = ValidRow(2, "Cross org") with
        {
            CompanyId = _factory.DisabledOrgId.ToString(),
            SalaryTableId = _factory.DisabledSalaryTableId.ToString()
        };

        var response = await client.PostAsJsonAsync(
            "api/vacancies/csv-import",
            new CsvImportRequest(_factory.OrgId, [row]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResultDto>();
        Assert.NotNull(result);
        Assert.Equal(0, result!.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains("hoort niet bij", result.Rows[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Retry_row_succeeds_after_correction()
    {
        var client = AuthedClient();
        var broken = new CsvImportRowRequest(
            2,
            "",
            "Omschrijving",
            "2026-08-01",
            "2026-12-31",
            "Winkel",
            _factory.SalaryTableId.ToString());

        var fail = await client.PostAsJsonAsync(
            "api/vacancies/csv-import/row",
            new CsvImportRetryRequest(_factory.BranchId, broken));
        Assert.Equal(HttpStatusCode.OK, fail.StatusCode);
        var failed = await fail.Content.ReadFromJsonAsync<CsvImportRowResultDto>();
        Assert.False(failed!.Success);

        var fixedRow = failed.Data with { Title = "Hersteld" };
        var ok = await client.PostAsJsonAsync(
            "api/vacancies/csv-import/row",
            new CsvImportRetryRequest(_factory.BranchId, fixedRow));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var succeeded = await ok.Content.ReadFromJsonAsync<CsvImportRowResultDto>();
        Assert.True(succeeded!.Success);
        Assert.NotNull(succeeded.VacancyId);
    }

    [Fact]
    public async Task Import_requires_auth()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "api/vacancies/csv-import",
            new CsvImportRequest(_factory.OrgId, [ValidRow(2, "X")]));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient AuthedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Jobsy-Email", "admin@jobsy.local");
        client.DefaultRequestHeaders.Add("X-Jobsy-Dev-Secret", "test-secret");
        return client;
    }

    private CsvImportRowRequest ValidRow(
        int rowNumber,
        string title,
        Guid? companyId = null,
        Guid? salaryTableId = null) =>
        new(
            rowNumber,
            title,
            "Omschrijving voor test",
            "2026-08-01",
            "2026-12-31",
            "Winkel",
            (salaryTableId ?? _factory.SalaryTableId).ToString(),
            CompanyId: companyId?.ToString());
}

public sealed class VacancyCsvImportWebAppFactory : WebApplicationFactory<Program>
{
    public Guid OrgId { get; } = Guid.Parse("a1111111-1111-1111-1111-111111111111");
    public Guid BranchId { get; } = Guid.Parse("a2222222-2222-2222-2222-222222222222");
    public Guid SalaryTableId { get; } = Guid.Parse("a3333333-3333-3333-3333-333333333333");
    public Guid DisabledOrgId { get; } = Guid.Parse("b1111111-1111-1111-1111-111111111111");
    public Guid DisabledSalaryTableId { get; } = Guid.Parse("b3333333-3333-3333-3333-333333333333");

    private readonly string _dbName = "CsvImportTests-" + Guid.NewGuid();
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
                Id = OrgId,
                Name = "CSV Org",
                KvkNumber = "11111111",
                Address = "HQ",
                Location = new GeoPoint(52, 4),
                CsvBatchImportEnabled = true
            },
            new Company
            {
                Id = BranchId,
                Name = "CSV Vestiging",
                KvkNumber = "11111111",
                Address = "Branch",
                Location = new GeoPoint(52.01, 4.01),
                ParentCompanyId = OrgId
            },
            new Company
            {
                Id = DisabledOrgId,
                Name = "Disabled Org",
                KvkNumber = "22222222",
                Address = "Other",
                Location = new GeoPoint(52.1, 4.1),
                CsvBatchImportEnabled = false
            });

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@jobsy.local",
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true,
            CompanyId = OrgId
        });

        SeedTable(db, SalaryTableId, OrgId);
        SeedTable(db, DisabledSalaryTableId, DisabledOrgId);

        db.MasterdataOptions.Add(new MasterdataOption
        {
            Id = Guid.NewGuid(),
            Category = MasterdataCategories.Branch,
            Value = "Winkel",
            Label = "Winkel",
            IsActive = true,
            SortOrder = 1,
            ShowOnVacancy = true
        });

        db.SaveChanges();
        _seeded = true;
    }

    private static void SeedTable(JobsyDbContext db, Guid tableId, Guid companyId)
    {
        var table = new CompanySalaryTable
        {
            Id = tableId,
            CompanyId = companyId,
            Name = "WML",
            IsActive = true,
            IsSystemWml = true
        };
        table.Rates.Add(new CompanySalaryRate
        {
            Id = Guid.NewGuid(),
            SalaryTableId = tableId,
            AgeYears = 21,
            HourlyRate = 14.50m
        });
        db.CompanySalaryTables.Add(table);
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
