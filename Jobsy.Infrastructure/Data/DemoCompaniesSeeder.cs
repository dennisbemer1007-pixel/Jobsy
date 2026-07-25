using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

internal static class DemoCompaniesSeeder
{
    public static async Task SeedCompaniesAsync(JobsyDbContext db, ILogger logger)
    {
        logger.LogInformation("Seeding Jobsy mock data for Westland & Den Haag...");

        var westlandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cafeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var supermarketId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var intermediaryCompanyId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var westland = new Company
        {
            Id = westlandId,
            Name = "Westland Fresh Logistics",
            KvkNumber = "12345678",
            KvkEstablishmentId = "12345678_0001",
            Address = "'s-Gravenzandseweg 10, Honselersdijk",
            LogoUrl = "/images/logos/westland.svg",
            Type = CompanyType.Employer,
            Location = new GeoPoint(51.9812, 4.2235)
        };

        var cafe = new Company
        {
            Id = cafeId,
            Name = "Boutique Café De Stad",
            KvkNumber = "87654321",
            KvkEstablishmentId = "87654321_0001",
            Address = "Grote Markt 14, Den Haag Centrum",
            LogoUrl = "/images/logos/cafe.svg",
            Type = CompanyType.Employer,
            Location = new GeoPoint(52.0735, 4.3120)
        };

        var supermarket = new Company
        {
            Id = supermarketId,
            Name = "Supermarkt De Fred",
            KvkNumber = "11223344",
            KvkEstablishmentId = "11223344_0001",
            Address = "Frederik Hendriklaan 88, Den Haag (Statenkwartier)",
            LogoUrl = "/images/logos/fred.svg",
            Type = CompanyType.Employer,
            Location = new GeoPoint(52.0910, 4.2815)
        };

        var intermediaryCompany = new Company
        {
            Id = intermediaryCompanyId,
            Name = "Demo Intermediair Flex BV",
            KvkNumber = "55667788",
            KvkEstablishmentId = "55667788_0001",
            Address = "Binckhorstlaan 36, Den Haag",
            LogoUrl = null,
            Type = CompanyType.Intermediary,
            Location = new GeoPoint(52.0680, 4.3350)
        };

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = today.AddMonths(3);

        var vacancies = new[]
        {
            new Vacancy
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Title = "Allround Orderpicker",
                Description = "Orderpicking in de glastuinbouwlogistiek. E-bike of auto vereist vanwege het Westlandse kasgebied.",
                HourlyWage = 14.50m,
                StartDate = today,
                EndDate = endDate,
                Status = VacancyStatus.Active,
                CompanyId = westlandId,
                Location = new GeoPoint(51.9812, 4.2235),
                RequiredTransport = TransportMode.Bike | TransportMode.Car,
                WorkTypes = WorkType.Logistiek | WorkType.Tuinbouw,
                ImageUrl = "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?auto=format&fit=crop&w=600&q=80"
            },
            new Vacancy
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Title = "Ervaren Barista / Bediening",
                Description = "Horecafunctie op de Grote Markt. Goed bereikbaar te voet, per fiets of met de tram.",
                HourlyWage = 13.80m,
                StartDate = today,
                EndDate = endDate,
                Status = VacancyStatus.Active,
                CompanyId = cafeId,
                Location = new GeoPoint(52.0735, 4.3120),
                RequiredTransport = TransportMode.Walking | TransportMode.Bike | TransportMode.PublicTransport,
                WorkTypes = WorkType.Horeca,
                ImageUrl = "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?auto=format&fit=crop&w=600&q=80"
            },
            new Vacancy
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Title = "Vakkenvuller / Kassamedewerker",
                Description = "Retailfunctie in het Statenkwartier. Bereikbaar per fiets of OV; leeftijdsafhankelijke loonschaal.",
                HourlyWage = 13.20m,
                StartDate = today,
                EndDate = endDate,
                Status = VacancyStatus.Active,
                CompanyId = supermarketId,
                Location = new GeoPoint(52.0910, 4.2815),
                RequiredTransport = TransportMode.Bike | TransportMode.PublicTransport,
                WorkTypes = WorkType.Winkel,
                // Geen ImageUrl: toont alleen logo (optioneel plaatje).
                ImageUrl = null
            }
        };

        var now = DateTime.UtcNow;
        var tokens = new[]
        {
            new TokenTransaction
            {
                Id = Guid.NewGuid(), CompanyId = westlandId, Amount = 5m,
                Kind = TokenTransactionKind.Grant, Reason = TokenSpendReason.None,
                OldBalance = 0, NewBalance = 5m, Note = "Seed grant", CreatedAt = now
            },
            new TokenTransaction
            {
                Id = Guid.NewGuid(), CompanyId = cafeId, Amount = 5m,
                Kind = TokenTransactionKind.Grant, Reason = TokenSpendReason.None,
                OldBalance = 0, NewBalance = 5m, Note = "Seed grant", CreatedAt = now
            },
            new TokenTransaction
            {
                Id = Guid.NewGuid(), CompanyId = supermarketId, Amount = 5m,
                Kind = TokenTransactionKind.Grant, Reason = TokenSpendReason.None,
                OldBalance = 0, NewBalance = 5m, Note = "Seed grant", CreatedAt = now
            },
            new TokenTransaction
            {
                Id = Guid.NewGuid(), CompanyId = intermediaryCompanyId, Amount = 20m,
                Kind = TokenTransactionKind.Grant, Reason = TokenSpendReason.None,
                OldBalance = 0, NewBalance = 20m, Note = "Seed grant intermediair", CreatedAt = now
            }
        };

        db.Companies.AddRange(westland, cafe, supermarket, intermediaryCompany);
        db.Vacancies.AddRange(vacancies);
        db.TokenTransactions.AddRange(tokens);
        await db.SaveChangesAsync();
    }
}
