using System.Net;
using System.Net.Http.Json;
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

/// <summary>
/// <c>POST api/reports</c> (public-pages 06): anonymous, rate-limited, no oracle on unknown
/// targets and no IP address anywhere near the table.
/// </summary>
public class ContentReportApiTests : IClassFixture<ContentReportApiFactory>
{
    private readonly ContentReportApiFactory _factory;

    public ContentReportApiTests(ContentReportApiFactory factory) => _factory = factory;

    [Fact]
    public void ContentReport_has_no_ip_column()
    {
        var names = typeof(ContentReport).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain(names, n => n.Contains("Ip", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Report_on_a_public_vacancy_is_stored_open_without_an_email()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "api/reports",
            new { type = "vacancy", id = _factory.PublicVacancyId, reason = ContentReportReason.Fake });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AcceptedDto>();
        Assert.True(body!.Accepted);
        Assert.False(body.EmailConfirmationSent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var report = await db.ContentReports
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstAsync(r => r.TargetId == _factory.PublicVacancyId);
        Assert.Equal(ContentReportStatus.Open, report.Status);
        Assert.Null(report.ReporterEmail);
    }

    [Fact]
    public async Task Unknown_target_gets_the_same_answer_but_is_closed_as_not_public()
    {
        var client = _factory.CreateClient();
        var unknown = Guid.NewGuid();
        var response = await client.PostAsJsonAsync(
            "api/reports",
            new { type = "vacancy", id = unknown, reason = ContentReportReason.Other });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AcceptedDto>();
        Assert.True(body!.Accepted);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var report = await db.ContentReports
            .Where(r => r.TargetId == Guid.Empty)
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstAsync();
        Assert.Equal(ContentReportStatus.NoAction, report.Status);
        Assert.Equal(ContentReportRules.TargetNotPublicReason, report.DecisionReason);
    }

    [Fact]
    public async Task Same_target_and_email_within_24_hours_is_stored_once()
    {
        var client = _factory.CreateClient();
        var email = $"melder-{Guid.NewGuid():N}@test.nl";
        for (var i = 0; i < 2; i++)
        {
            var response = await client.PostAsJsonAsync(
                "api/reports",
                new { type = "vacancy", id = _factory.PublicVacancyId, reason = ContentReportReason.Illegal, email });
            response.EnsureSuccessStatusCode();
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        Assert.Equal(1, await db.ContentReports.CountAsync(r => r.ReporterEmail == email));
    }

    [Fact]
    public async Task Details_are_trimmed_to_the_maximum_length()
    {
        var client = _factory.CreateClient();
        var details = new string('a', ContentReportRules.DetailsMaxLength + 500);
        var response = await client.PostAsJsonAsync(
            "api/reports",
            new { type = "vacancy", id = _factory.PublicVacancyId, reason = ContentReportReason.Unsafe, details });
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var stored = await db.ContentReports
            .Where(r => r.Reason == ContentReportReason.Unsafe)
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstAsync();
        Assert.Equal(ContentReportRules.DetailsMaxLength, stored.Details!.Length);
    }

    [Fact]
    public async Task Too_many_reports_from_one_visitor_answer_429()
    {
        await using var strict = new ContentReportApiFactory(hourlyLimit: 2);
        var client = strict.CreateClient();
        var payload = new { type = "vacancy", id = strict.PublicVacancyId, reason = ContentReportReason.WrongInfo };

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/reports", payload)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/reports", payload)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("api/reports", payload)).StatusCode);
    }

    private sealed record AcceptedDto(bool Accepted, bool EmailConfirmationSent);
}

public sealed class ContentReportApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "ContentReports-" + Guid.NewGuid();
    private readonly int _hourlyLimit;
    private bool _seeded;

    public ContentReportApiFactory() => _hourlyLimit = 500;

    internal ContentReportApiFactory(int hourlyLimit) => _hourlyLimit = hourlyLimit;

    public Guid CompanyId { get; } = Guid.NewGuid();
    public Guid PublicVacancyId { get; } = Guid.NewGuid();
    public string Kvk { get; } = "90000601";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        JobsyTestAuth.ApplyStandardAuthSettings(builder);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Swagger:Enabled", "false");
        builder.UseSetting("RateLimiting:ReportHourlyPermitLimit", _hourlyLimit.ToString());
        builder.UseSetting("RateLimiting:ReportDailyPermitLimit", (_hourlyLimit * 4).ToString());
        builder.UseSetting(
            "ConnectionStrings:JobsyDb",
            "Host=127.0.0.1;Port=5432;Database=JobsyTest;Username=postgres;Password=postgres");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            ContentReportTestDb.ReplaceWithInMemory(services, _dbName);
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

        _seeded = true;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        ContentReportTestDb.SeedPublicVacancy(db, CompanyId, PublicVacancyId, Kvk);
        db.SaveChanges();
    }
}

/// <summary>Shared in-memory seed for the 06 report suites.</summary>
internal static class ContentReportTestDb
{
    internal static void ReplaceWithInMemory(IServiceCollection services, string dbName)
    {
        var descriptors = services
            .Where(d =>
                d.ServiceType == typeof(JobsyDbContext)
                || d.ServiceType == typeof(DbContextOptions<JobsyDbContext>)
                || (d.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
                || (d.ServiceType.FullName?.Contains("EntityFrameworkCore", StringComparison.Ordinal) == true
                    && d.ServiceType.FullName.Contains("JobsyDbContext", StringComparison.Ordinal)))
            .ToList();
        foreach (var d in descriptors)
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

        services.AddDbContext<JobsyDbContext>(options => options.UseInMemoryDatabase(dbName));
    }

    internal static Company SeedPublicVacancy(JobsyDbContext db, Guid companyId, Guid vacancyId, string kvk)
    {
        var company = new Company
        {
            Id = companyId,
            Name = "Meld BV",
            Address = "Straat 1",
            KvkNumber = kvk,
            Location = new GeoPoint(52.0, 4.3),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow,
            KvkVerificationStatus = KvkVerificationStatus.Verified
        };
        db.Companies.Add(company);
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Vakkenvuller",
            Description = "x",
            HourlyWage = 14m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
            Status = VacancyStatus.Active,
            Location = new GeoPoint(52.0, 4.3),
            RequiredTransport = TransportMode.Bike
        });
        return company;
    }
}
