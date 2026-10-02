using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests.Errors;

/// <summary>
/// Minimal in-memory API host for the closed-vacancy (410) stack (errors 03): one verified
/// company, one hidden-mode intermediary, and a vacancy for every closed/unknown/owner case
/// the spec distinguishes.
/// </summary>
public sealed class ClosedVacancyApiFactory : WebApplicationFactory<Jobsy.Api.ApiAssemblyMarker>
{
    public Guid CompanyId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000001");
    public Guid IntermediaryCompanyId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000002");
    public Guid EmployerId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000003");
    public Guid AdminId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000004");

    /// <summary>Archived — was published, status Archived, closed → 410.</summary>
    public Guid ArchivedId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000010");

    /// <summary>Fulfilled — was published, status Fulfilled, closed → 410.</summary>
    public Guid FulfilledId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000011");

    /// <summary>Active but EndDate in the past — ran out, closed → 410.</summary>
    public Guid ExpiredId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000012");

    /// <summary>Never published Draft — unknown → 404 (not closed).</summary>
    public Guid DraftId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000013");

    /// <summary>PendingApproval, never published → 404.</summary>
    public Guid PendingId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000014");

    /// <summary>Active with a future StartDate, never actually live yet → 404.</summary>
    public Guid FutureStartId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000015");

    /// <summary>Currently public (Active, in window) — 200, and /similar on it is 404.</summary>
    public Guid PublicId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000016");

    /// <summary>Public, same category, 2 km from <see cref="ArchivedId"/> — a similar-list hit.</summary>
    public Guid NearbySameCategoryId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000017");

    /// <summary>Public, same category, 40 km away — outside the 25 km similar-list radius.</summary>
    public Guid FarSameCategoryId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000018");

    /// <summary>Closed, posted via the hidden-mode intermediary — company name must never leak.</summary>
    public Guid HiddenIntermediaryClosedId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000019");

    private readonly string _dbName = "ClosedVacancy-" + Guid.NewGuid();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        JobsyTestAuth.ApplyStandardAuthSettings(builder);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Swagger:Enabled", "false");
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

            services.AddDbContext<JobsyDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        EnsureSeeded();
        base.ConfigureClient(client);
    }

    public void EnsureSeeded()
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

        var categories = new VacancyCategoryService(db);
        categories.EnsureDefaultsAsync().GetAwaiter().GetResult();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        db.Companies.Add(new Company
        {
            Id = CompanyId,
            Name = "Test Vestiging Gesloten",
            KvkNumber = "11112222",
            Address = "Marktstraat 1, Den Haag",
            Location = new GeoPoint(52.07, 4.31),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });

        db.Companies.Add(new Company
        {
            Id = IntermediaryCompanyId,
            Name = "Geheim Uitzendbureau BV",
            KvkNumber = "33334444",
            Address = "Bureauweg 9, Rotterdam",
            Location = new GeoPoint(51.92, 4.48),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });

        db.Users.AddRange(
            new User
            {
                Id = EmployerId,
                Email = "werkgever@closed.jobsy.local",
                FullName = "Werkgever Gesloten",
                Role = UserRole.BranchManager,
                IsActive = true,
                CompanyId = CompanyId
            },
            new User
            {
                Id = AdminId,
                Email = "admin@closed.jobsy.local",
                FullName = "Admin Gesloten",
                Role = UserRole.Admin,
                IsActive = true
            });

        db.UserCompanies.Add(new UserCompany { UserId = EmployerId, CompanyId = CompanyId });

        db.Vacancies.Add(new Vacancy
        {
            Id = ArchivedId,
            Title = "Archiefbaan Magazijn",
            Description = "Was ooit live, nu gearchiveerd.",
            HourlyWage = 14.0m,
            StartDate = today.AddMonths(-3),
            EndDate = today.AddMonths(-1),
            Status = VacancyStatus.Archived,
            CompanyId = CompanyId,
            Location = new GeoPoint(52.07, 4.31),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Winkel,
            WorkTypeLabels = "Winkel",
            CreatedAtUtc = DateTime.UtcNow.AddMonths(-3),
            PublishedAtUtc = DateTime.UtcNow.AddMonths(-3),
            ClosedAtUtc = DateTime.UtcNow.AddMonths(-1),
            CategoryId = VacancyCategoryDefaults.RegulierId,
            MaxApplications = 5
        });

        db.Vacancies.Add(new Vacancy
        {
            Id = FulfilledId,
            Title = "Vervulde Vacature Kassa",
            Description = "Werd vervuld.",
            HourlyWage = 13.5m,
            StartDate = today.AddMonths(-2),
            EndDate = today.AddMonths(1),
            Status = VacancyStatus.Fulfilled,
            CompanyId = CompanyId,
            Location = new GeoPoint(52.07, 4.31),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Winkel,
            WorkTypeLabels = "Winkel",
            CreatedAtUtc = DateTime.UtcNow.AddMonths(-2),
            PublishedAtUtc = DateTime.UtcNow.AddMonths(-2),
            ClosedAtUtc = DateTime.UtcNow.AddDays(-5),
            CategoryId = VacancyCategoryDefaults.RegulierId,
            MaxApplications = 5
        });

        db.Vacancies.Add(new Vacancy
        {
            Id = ExpiredId,
            Title = "Verlopen Terrasbaan",
            Description = "Einddatum is voorbij.",
            HourlyWage = 12.5m,
            StartDate = today.AddMonths(-2),
            EndDate = today.AddDays(-3),
            Status = VacancyStatus.Active,
            CompanyId = CompanyId,
            Location = new GeoPoint(52.07, 4.31),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Horeca,
            WorkTypeLabels = "Horeca",
            CreatedAtUtc = DateTime.UtcNow.AddMonths(-2),
            PublishedAtUtc = DateTime.UtcNow.AddMonths(-2),
            CategoryId = VacancyCategoryDefaults.RegulierId,
            MaxApplications = 5
        });

        db.Vacancies.Add(new Vacancy
        {
            Id = DraftId,
            Title = "Nooit Gepubliceerde Draft",
            Description = "Nog nooit live geweest.",
            HourlyWage = 12.0m,
            StartDate = today,
            EndDate = today.AddMonths(1),
            Status = VacancyStatus.Draft,
            CompanyId = CompanyId,
            Location = new GeoPoint(52.07, 4.31),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Winkel,
            WorkTypeLabels = "Winkel",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            PublishedAtUtc = null,
            CategoryId = VacancyCategoryDefaults.RegulierId,
            MaxApplications = 5
        });

        db.Vacancies.Add(new Vacancy
        {
            Id = PendingId,
            Title = "In Afwachting Van Goedkeuring",
            Description = "Nooit live geweest, wacht op admin.",
            HourlyWage = 12.0m,
            StartDate = today,
            EndDate = today.AddMonths(1),
            Status = VacancyStatus.PendingApproval,
            CompanyId = CompanyId,
            Location = new GeoPoint(52.07, 4.31),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Winkel,
            WorkTypeLabels = "Winkel",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            PublishedAtUtc = null,
            CategoryId = VacancyCategoryDefaults.RegulierId,
            MaxApplications = 5
        });

        db.Vacancies.Add(new Vacancy
        {
            Id = FutureStartId,
            Title = "Start Volgende Maand",
            Description = "Nog niet begonnen.",
            HourlyWage = 12.0m,
            StartDate = today.AddMonths(1),
            EndDate = today.AddMonths(3),
            Status = VacancyStatus.Active,
            CompanyId = CompanyId,
            Location = new GeoPoint(52.07, 4.31),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Winkel,
            WorkTypeLabels = "Winkel",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            PublishedAtUtc = null,
            CategoryId = VacancyCategoryDefaults.RegulierId,
            MaxApplications = 5
        });

        db.Vacancies.Add(new Vacancy
        {
            Id = PublicId,
            Title = "Live En Zichtbare Vacature",
            Description = "Nu actief en publiek.",
            HourlyWage = 13.0m,
            StartDate = today.AddDays(-1),
            EndDate = today.AddMonths(2),
            Status = VacancyStatus.Active,
            CompanyId = CompanyId,
            Location = new GeoPoint(52.07, 4.31),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Winkel,
            WorkTypeLabels = "Winkel",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            PublishedAtUtc = DateTime.UtcNow.AddDays(-1),
            CategoryId = VacancyCategoryDefaults.RegulierId,
            MaxApplications = 5
        });

        // ~2 km from the Archived vacancy's Den Haag location — well inside the 25 km same-category radius.
        db.Vacancies.Add(new Vacancy
        {
            Id = NearbySameCategoryId,
            Title = "Vulploeg Vlakbij",
            Description = "Vlakbij de gesloten vacature.",
            HourlyWage = 13.2m,
            StartDate = today.AddDays(-1),
            EndDate = today.AddMonths(2),
            Status = VacancyStatus.Active,
            CompanyId = CompanyId,
            Location = new GeoPoint(52.09, 4.31),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Winkel,
            WorkTypeLabels = "Winkel",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            PublishedAtUtc = DateTime.UtcNow.AddDays(-1),
            CategoryId = VacancyCategoryDefaults.RegulierId,
            MaxApplications = 5
        });

        // ~40 km away — same category, but outside the 25 km radius.
        db.Vacancies.Add(new Vacancy
        {
            Id = FarSameCategoryId,
            Title = "Vulploeg Ver Weg",
            Description = "Te ver voor de gelijkende lijst.",
            HourlyWage = 13.2m,
            StartDate = today.AddDays(-1),
            EndDate = today.AddMonths(2),
            Status = VacancyStatus.Active,
            CompanyId = CompanyId,
            Location = new GeoPoint(52.45, 4.31),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Winkel,
            WorkTypeLabels = "Winkel",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            PublishedAtUtc = DateTime.UtcNow.AddDays(-1),
            CategoryId = VacancyCategoryDefaults.RegulierId,
            MaxApplications = 5
        });

        db.Vacancies.Add(new Vacancy
        {
            Id = HiddenIntermediaryClosedId,
            Title = "Geheime Intermediair Baan",
            Description = "Gesloten vacature via verborgen intermediair.",
            HourlyWage = 13.8m,
            StartDate = today.AddMonths(-3),
            EndDate = today.AddMonths(-1),
            Status = VacancyStatus.Archived,
            CompanyId = CompanyId,
            IntermediaryCompanyId = IntermediaryCompanyId,
            ShowClientAddressOnMap = false,
            Location = new GeoPoint(52.07, 4.31),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Logistiek,
            WorkTypeLabels = "Logistiek",
            CreatedAtUtc = DateTime.UtcNow.AddMonths(-3),
            PublishedAtUtc = DateTime.UtcNow.AddMonths(-3),
            ClosedAtUtc = DateTime.UtcNow.AddMonths(-1),
            CategoryId = VacancyCategoryDefaults.UitzendbureauId,
            MaxApplications = 5
        });

        db.SaveChanges();
        _seeded = true;
    }
}
