using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

internal static class ApplicationsAndWagesSeeder
{
    public static readonly Guid DemoCandidateId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");

    public static async Task SeedApplicationsAndWagesAsync(JobsyDbContext db, ILogger logger)
    {
        if (!await db.MinimumWageRates.AnyAsync())
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            db.MinimumWageRates.AddRange(
                new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 21, HourlyRate = 14.06m, Label = "21 jaar en ouder", EffectiveFrom = today },
                new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 20, HourlyRate = 11.25m, Label = "20 jaar", EffectiveFrom = today },
                new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 19, HourlyRate = 8.44m, Label = "19 jaar", EffectiveFrom = today },
                new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 18, HourlyRate = 7.03m, Label = "18 jaar", EffectiveFrom = today },
                new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 17, HourlyRate = 5.55m, Label = "17 jaar", EffectiveFrom = today },
                new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 16, HourlyRate = 4.85m, Label = "16 jaar", EffectiveFrom = today },
                new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 15, HourlyRate = 4.22m, Label = "15 jaar", EffectiveFrom = today });
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded minimum wage rates.");
        }

        await WmlSalaryTableService.EnsureForAllCompaniesAsync(db);
        logger.LogInformation("Ensured default WML salary tables for companies.");

        if (await db.Applications.AnyAsync())
        {
            return;
        }

        var vacancyIds = await db.Vacancies.Select(v => new { v.Id, v.Title }).ToListAsync();
        if (vacancyIds.Count == 0)
        {
            return;
        }

        var candidate = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == DemoCandidateId);
        var now = DateTime.UtcNow;
        var first = true;
        foreach (var vacancy in vacancyIds)
        {
            if (first && candidate is not null)
            {
                db.Applications.Add(new Application
                {
                    Id = Guid.NewGuid(),
                    VacancyId = vacancy.Id,
                    CandidateUserId = candidate.Id,
                    CandidateName = candidate.FullName,
                    CandidateEmail = candidate.Email,
                    CandidateCity = "Den Haag",
                    PreferredTransport = "Fiets",
                    EstimatedTravelMinutes = 12,
                    DistanceKm = 3.2,
                    PreferencesSummary = candidate.PreferencesJson,
                    Status = ApplicationStatus.Pending,
                    CreatedAt = now.AddHours(-5)
                });
                first = false;
            }

            db.Applications.AddRange(
                new Application
                {
                    Id = Guid.NewGuid(),
                    VacancyId = vacancy.Id,
                    CandidateName = "Sara Jansen",
                    CandidateEmail = "sara@example.com",
                    CandidateCity = "Den Haag",
                    PreferredTransport = "Fiets",
                    EstimatedTravelMinutes = 12,
                    DistanceKm = 3.2,
                    Status = ApplicationStatus.Pending,
                    CreatedAt = now.AddHours(-5)
                },
                new Application
                {
                    Id = Guid.NewGuid(),
                    VacancyId = vacancy.Id,
                    CandidateName = "Mohamed El Amrani",
                    CandidateEmail = "mohamed@example.com",
                    CandidateCity = "Rijswijk",
                    PreferredTransport = "OV",
                    EstimatedTravelMinutes = 28,
                    DistanceKm = 8.1,
                    Status = ApplicationStatus.Pending,
                    CreatedAt = now.AddHours(-2)
                });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded demo applications.");
    }
}
