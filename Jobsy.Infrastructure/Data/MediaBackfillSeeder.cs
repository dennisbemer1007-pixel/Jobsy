using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

internal static class MediaBackfillSeeder
{
    public static async Task BackfillMediaAsync(JobsyDbContext db, ILogger logger)
    {
        await DemoUsersSeeder.SeedUsersAsync(db, logger);
        await ApplicationsAndWagesSeeder.SeedApplicationsAndWagesAsync(db, logger);
        await EnsureIntermediaryCompanyAsync(db, logger);

        var updated = false;

        var companies = await db.Companies.ToListAsync();
        foreach (var company in companies)
        {
            if (!string.IsNullOrWhiteSpace(company.LogoUrl))
            {
                continue;
            }

            var logo = company.Name switch
            {
                "Westland Fresh Logistics" => "/images/logos/westland.svg",
                "Boutique Café De Stad" => "/images/logos/cafe.svg",
                "Supermarkt De Fred" => "/images/logos/fred.svg",
                _ => null
            };
            if (logo is null)
            {
                continue;
            }

            company.LogoUrl = logo;
            updated = true;
        }

        var vacancies = await db.Vacancies.Include(v => v.Company).ToListAsync();
        var index = 0;
        foreach (var vacancy in vacancies)
        {
            var touched = false;

            if (MockVacancyMedia.NeedsImageBackfill(vacancy.ImageUrl))
            {
                vacancy.ImageUrl = MockVacancyMedia.ImageUrl(vacancy.Id);
                touched = true;
            }

            if (MockVacancyMedia.NeedsVideoBackfill(vacancy.VideoUrl))
            {
                vacancy.VideoUrl = MockVacancyMedia.VideoUrl(vacancy.Id);
                touched = true;
            }

            if (MockVacancyMedia.NeedsDescriptionBackfill(vacancy.Description))
            {
                vacancy.Description = MockVacancyMedia.BuildRichDescription(
                    vacancy.Title,
                    vacancy.Description,
                    vacancy.Company?.Name,
                    vacancy.WorkTypes,
                    vacancy.HourlyWage,
                    index);
                touched = true;
            }

            if (touched)
            {
                updated = true;
            }

            index++;
        }

        if (updated)
        {
            await db.SaveChangesAsync();
            logger.LogInformation(
                "Backfilled vacancy images, videos and rich descriptions on existing mock data ({Count} vacancies scanned).",
                vacancies.Count);
        }
    }

    private static async Task EnsureIntermediaryCompanyAsync(JobsyDbContext db, ILogger logger)
    {
        var intermediaryCompanyId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        if (await db.Companies.AnyAsync(c => c.Id == intermediaryCompanyId || c.Type == Core.Enums.CompanyType.Intermediary))
        {
            return;
        }

        db.Companies.Add(new Core.Entities.Company
        {
            Id = intermediaryCompanyId,
            Name = "Demo Intermediair Flex BV",
            KvkNumber = "55667788",
            KvkEstablishmentId = "55667788_0001",
            Address = "Binckhorstlaan 36, Den Haag",
            Type = Core.Enums.CompanyType.Intermediary,
            Location = new Core.ValueObjects.GeoPoint(52.0680, 4.3350)
        });
        db.TokenTransactions.Add(new Core.Entities.TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = intermediaryCompanyId,
            Amount = 20m,
            Kind = Core.Enums.TokenTransactionKind.Grant,
            Reason = Core.Enums.TokenSpendReason.None,
            OldBalance = 0,
            NewBalance = 20m,
            Note = "Seed grant intermediair",
            CreatedAt = DateTime.UtcNow
        });

        var intermediaryUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "intermediair@jobsy.local");
        if (intermediaryUser is not null && intermediaryUser.CompanyId is null)
        {
            intermediaryUser.CompanyId = intermediaryCompanyId;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Backfilled intermediary company for Sprint 6 admin KPIs.");
    }
}
