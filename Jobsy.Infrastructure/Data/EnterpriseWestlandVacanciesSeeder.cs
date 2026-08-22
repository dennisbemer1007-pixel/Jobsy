using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// 50 Active Westland vacancies owned by Westland Fresh Logistics — the vestiging
/// managed by <c>enterprise@jobsy.local</c> (parent: Supermarkt De Fred).
/// Idempotent via platform-log marker so existing demo DBs pick this up on next API start.
/// </summary>
internal static class EnterpriseWestlandVacanciesSeeder
{
    public const string SeedMarker = "Enterprise Westland vacancies seed 50";
    public const int VacancyCount = 50;

    internal static readonly Guid WestlandFreshId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FredSupermarketId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid EnterpriseUserId = Guid.Parse("dddddddd-1111-1111-1111-111111111111");

    internal static Guid VacancyId(int n) => Guid.Parse($"a5000000-0000-4000-8000-{n:D12}");

    public static async Task SeedAsync(JobsyDbContext db, ILogger logger)
    {
        if (await db.PlatformLogs.AnyAsync(l =>
                l.Category == "Seed" && l.Message == SeedMarker))
        {
            return;
        }

        if (await db.Vacancies.AnyAsync(v => v.Id == VacancyId(1)))
        {
            db.PlatformLogs.Add(NewMarker());
            await db.SaveChangesAsync();
            return;
        }

        await EnsureWestlandFreshAsync(db);
        await EnsureEnterpriseMembershipAsync(db);
        await db.SaveChangesAsync();
        await WmlSalaryTableService.EnsureForAllCompaniesAsync(db);

        Guid? salaryTableId = await db.CompanySalaryTables
            .AsNoTracking()
            .Where(t => t.CompanyId == WestlandFreshId && t.IsActive && t.Rates.Any())
            .OrderByDescending(t => t.IsSystemWml)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var vacancies = BuildVacancies(today, today.AddMonths(4), salaryTableId);
        db.Vacancies.AddRange(vacancies);
        db.PlatformLogs.Add(NewMarker());
        await db.SaveChangesAsync();
        logger.LogInformation(
            "Enterprise Westland seed: {Count} active vacancies for Westland Fresh (enterprise@jobsy.local).",
            vacancies.Length);
    }

    private static PlatformLog NewMarker() => new()
    {
        Id = Guid.NewGuid(),
        Level = PlatformLogLevel.Info,
        Category = "Seed",
        Message = SeedMarker,
        CreatedAt = DateTime.UtcNow
    };

    private static async Task EnsureWestlandFreshAsync(JobsyDbContext db)
    {
        if (await db.Companies.AnyAsync(c => c.Id == WestlandFreshId)
            || db.Companies.Local.Any(c => c.Id == WestlandFreshId))
        {
            return;
        }

        var parentExists = await db.Companies.AnyAsync(c => c.Id == FredSupermarketId)
            || db.Companies.Local.Any(c => c.Id == FredSupermarketId);

        db.Companies.Add(new Company
        {
            Id = WestlandFreshId,
            Name = "Westland Fresh Logistics",
            KvkNumber = "12345678",
            KvkEstablishmentId = "12345678_0001",
            Address = "'s-Gravenzandseweg 10, Honselersdijk",
            LogoUrl = "/images/logos/westland.svg",
            Type = CompanyType.Employer,
            Location = new GeoPoint(51.9812, 4.2235),
            ParentCompanyId = parentExists ? FredSupermarketId : null,
            TokensManagedByEnterprise = parentExists
        });

        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = WestlandFreshId,
            Amount = 20m,
            Kind = TokenTransactionKind.Grant,
            Reason = TokenSpendReason.None,
            OldBalance = 0,
            NewBalance = 20m,
            Note = "Enterprise Westland vacancies seed grant",
            CreatedAt = DateTime.UtcNow
        });
    }

    private static async Task EnsureEnterpriseMembershipAsync(JobsyDbContext db)
    {
        if (!await db.Users.AnyAsync(u => u.Id == EnterpriseUserId)
            && !db.Users.Local.Any(u => u.Id == EnterpriseUserId))
        {
            return;
        }

        if (await db.UserCompanies.AnyAsync(m => m.UserId == EnterpriseUserId && m.CompanyId == WestlandFreshId)
            || db.UserCompanies.Local.Any(m => m.UserId == EnterpriseUserId && m.CompanyId == WestlandFreshId))
        {
            return;
        }

        db.UserCompanies.Add(new UserCompany { UserId = EnterpriseUserId, CompanyId = WestlandFreshId });
    }

    private static Vacancy[] BuildVacancies(DateOnly today, DateOnly endDate, Guid? salaryTableId)
    {
        var list = new List<Vacancy>(VacancyCount);
        for (var i = 0; i < VacancyCount; i++)
        {
            var town = Towns[i % Towns.Length];
            var role = Roles[i / Towns.Length];
            var vacancyId = VacancyId(i + 1);
            var title = $"{role.Title} {town.Name}";
            var jitterLat = ((i % 7) - 3) * 0.0008;
            var jitterLng = ((i % 5) - 2) * 0.0010;
            var transport = role.Transport[i % role.Transport.Length];
            var wage = Math.Round(role.BaseWage + (i % 5) * 0.20m, 2);

            var vacancy = new Vacancy
            {
                Id = vacancyId,
                Title = title,
                Description = MockVacancyMedia.BuildRichDescription(
                    title,
                    $"{role.Blurb} Locatie: {town.Name} ({town.Address}). " +
                    "Westland Fresh Logistics (enterprise demo, vestiging van De Fred).",
                    "Westland Fresh Logistics",
                    role.WorkTypes,
                    wage,
                    i + 50),
                HourlyWage = wage,
                StartDate = today,
                EndDate = endDate,
                Status = VacancyStatus.Active,
                PublishedAtUtc = DateTime.UtcNow,
                CompanyId = WestlandFreshId,
                Location = new GeoPoint(town.Lat + jitterLat, town.Lng + jitterLng),
                RequiredTransport = transport,
                WorkTypes = role.WorkTypes,
                WorkTypeLabels = string.Join(", ", WorkTypeLabels.Expand(role.WorkTypes).Take(2)),
                ImageUrl = MockVacancyMedia.ImageUrl(vacancyId, role.WorkTypes),
                VideoUrl = MockVacancyMedia.VideoUrl(i + 50),
                MaxApplications = 8,
                SalaryTableId = salaryTableId,
                MinHoursPerWeek = role.MinHours,
                MaxHoursPerWeek = role.MaxHours,
                RequiredDrivingLicense = role.License
            };
            SeedVacancyCategoryMix.Apply(vacancy, i + 1, keepExistingHighlight: true);
            list.Add(vacancy);
        }

        return list.ToArray();
    }

    private static readonly TownSpec[] Towns =
    [
        new("Naaldwijk", "Dijkweg 12, Naaldwijk", 51.9944, 4.2097),
        new("De Lier", "Burgemeester Elsenweg 40, De Lier", 51.9750, 4.2480),
        new("Honselersdijk", "Veilinglaan 5, Honselersdijk", 51.9825, 4.2210),
        new("Monster", "Zeeweg 8, Monster", 52.0240, 4.1750),
        new("Poeldijk", "Voorstraat 22, Poeldijk", 52.0150, 4.2200),
        new("Wateringen", "Plein 3, Wateringen", 52.0235, 4.2730),
        new("Maasdijk", "Maasdijkseweg 18, Maasdijk", 51.9590, 4.2150),
        new("Kwintsheul", "Heulweg 9, Kwintsheul", 52.0050, 4.2400),
        new("'s-Gravenzande", "Naaldwijkseweg 100, 's-Gravenzande", 51.9980, 4.1650),
        new("Heenweg", "Industrieweg 4, Heenweg", 51.9900, 4.1550)
    ];

    private static readonly RoleSpec[] Roles =
    [
        new("Orderpicker", WorkType.Logistiek, 14.50m, 16, 32, null,
            [
                TransportMode.Bike | TransportMode.Car,
                TransportMode.Bike | TransportMode.PublicTransport,
                TransportMode.Car
            ],
            "Orders verzamelen in het DC voor retail en glastuinbouw."),
        new("Kasmedewerker", WorkType.Tuinbouw, 13.80m, 12, 32, null,
            [
                TransportMode.Bike | TransportMode.Car,
                TransportMode.Bike | TransportMode.Walking,
                TransportMode.Bike | TransportMode.PublicTransport
            ],
            "Plukken, sorteren en gewaswerk in de kas."),
        new("Inpakker verse producten", WorkType.Productie | WorkType.Tuinbouw, 13.60m, 12, 28, null,
            [
                TransportMode.Bike | TransportMode.Car,
                TransportMode.Bike,
                TransportMode.Bike | TransportMode.PublicTransport
            ],
            "Inpaklijn verse groenten en fruit; tempo en netheid."),
        new("Magazijnmedewerker", WorkType.Logistiek, 14.20m, 16, 36, null,
            [
                TransportMode.Bike | TransportMode.Walking,
                TransportMode.Bike | TransportMode.Car,
                TransportMode.Bike | TransportMode.PublicTransport
            ],
            "Inkomend, opslag en uitgaand in de Westlandse loods."),
        new("Chauffeur intern", WorkType.Logistiek, 16.20m, 24, 40, "B",
            [
                TransportMode.Car,
                TransportMode.Car | TransportMode.Bike
            ],
            "Interne ritten tussen loodsen en vestigingen in het Westland.")
    ];

    private sealed record TownSpec(string Name, string Address, double Lat, double Lng);

    private sealed record RoleSpec(
        string Title,
        WorkType WorkTypes,
        decimal BaseWage,
        decimal MinHours,
        decimal MaxHours,
        string? License,
        TransportMode[] Transport,
        string Blurb);
}
