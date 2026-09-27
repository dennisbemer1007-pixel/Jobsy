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
        await WmlSalaryTableService.FillEmptySalaryTablesAsync(db);
        logger.LogInformation("Ensured default WML salary tables for companies.");

        if (await db.Applications.AnyAsync())
        {
            await EnsureDemoCandidateApplicationCardsAsync(db, logger);
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
                    CreatedAt = now.AddHours(-5),
                    EmailVerifiedAt = DateTime.UtcNow
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
                    CreatedAt = now.AddHours(-5),
                    EmailVerifiedAt = DateTime.UtcNow
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
                    CreatedAt = now.AddHours(-2),
                    EmailVerifiedAt = DateTime.UtcNow
                });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded demo applications.");

        await EnsureDemoCandidateApplicationCardsAsync(db, logger);
    }

    /// <summary>
    /// Ensures the public demo candidate has a mix of statuses + photo/logo cards
    /// for the fotokaarten UI / Playwright smoke (dev/e2e seed only).
    /// </summary>
    public static async Task EnsureDemoCandidateApplicationCardsAsync(JobsyDbContext db, ILogger logger)
    {
        var candidate = await db.Users.FirstOrDefaultAsync(u => u.Id == DemoCandidateId)
            ?? await db.Users.FirstOrDefaultAsync(u => u.Email == "kandidaat@jobsy.local");
        if (candidate is null)
        {
            return;
        }

        var vacancies = await db.Vacancies.AsTracking()
            .Include(v => v.Company)
            .Where(v => v.Status == VacancyStatus.Active)
            .OrderBy(v => v.CreatedAtUtc)
            .Take(8)
            .ToListAsync();
        if (vacancies.Count < 5)
        {
            return;
        }

        // One vacancy with a same-origin photo; one with logo only (no vacancy photo).
        var photoVacancy = vacancies[0];
        photoVacancy.ImageUrl = "/images/brand/dennis.jpg";
        var logoVacancy = vacancies[1];
        logoVacancy.ImageUrl = null;
        if (logoVacancy.Company is not null
            && string.IsNullOrWhiteSpace(logoVacancy.Company.LogoUrl))
        {
            logoVacancy.Company.LogoUrl = "/images/logos/westland.svg";
        }

        // Include unverified rows so we can promote them instead of inserting duplicates.
        var mine = await db.Applications
            .Where(a => a.CandidateUserId == candidate.Id)
            .ToListAsync();

        var needed = new (ApplicationStatus Status, Guid VacancyId, int DaysAgo)[]
        {
            (ApplicationStatus.Pending, photoVacancy.Id, 2),
            (ApplicationStatus.Pending, logoVacancy.Id, 3),
            (ApplicationStatus.Accepted, vacancies[2].Id, 5),
            (ApplicationStatus.Hired, vacancies[3].Id, 8),
            (ApplicationStatus.Rejected, vacancies[4].Id, 10),
        };

        var now = DateTime.UtcNow;
        foreach (var (status, vacancyId, daysAgo) in needed)
        {
            EnsureStatusCard(db, mine, candidate, status, vacancyId, now.AddDays(-daysAgo));
        }

        // Guarantee at least two Pending (withdraw + remain).
        var pendingCount = mine.Count(a =>
            a.EmailVerifiedAt != null && a.Status == ApplicationStatus.Pending);
        for (var i = pendingCount; i < 2; i++)
        {
            var v = vacancies[Math.Min(5 + i, vacancies.Count - 1)];
            var created = MakeApp(candidate, v.Id, ApplicationStatus.Pending, now.AddHours(-(6 + i)));
            db.Applications.Add(created);
            mine.Add(created);
        }

        // Hard guarantee: fotokaarten smoke needs a visible Hired card.
        if (!mine.Any(a => a.EmailVerifiedAt != null && a.Status == ApplicationStatus.Hired))
        {
            var used = mine.Select(a => a.VacancyId).ToHashSet();
            var free = vacancies.FirstOrDefault(v => !used.Contains(v.Id)) ?? vacancies[3];
            var hired = MakeApp(candidate, free.Id, ApplicationStatus.Hired, now.AddDays(-8));
            db.Applications.Add(hired);
            mine.Add(hired);
        }

        await db.SaveChangesAsync();
        logger.LogInformation(
            "Ensured demo candidate application cards for fotokaarten UI (pending={Pending}, hired={Hired}).",
            mine.Count(a => a.Status == ApplicationStatus.Pending && a.EmailVerifiedAt != null),
            mine.Count(a => a.Status == ApplicationStatus.Hired && a.EmailVerifiedAt != null));
    }

    private static void EnsureStatusCard(
        JobsyDbContext db,
        List<Application> mine,
        User candidate,
        ApplicationStatus status,
        Guid vacancyId,
        DateTime createdAt)
    {
        var existing = mine.FirstOrDefault(a => a.Status == status && a.VacancyId == vacancyId)
            ?? mine.FirstOrDefault(a => a.Status == status && a.EmailVerifiedAt != null);
        if (existing is not null)
        {
            existing.EmailVerifiedAt ??= createdAt;
            if (status == ApplicationStatus.Pending
                && existing.VacancyId != vacancyId
                && !mine.Any(a => a.Status == ApplicationStatus.Pending && a.VacancyId == vacancyId))
            {
                var extra = MakeApp(candidate, vacancyId, status, createdAt);
                db.Applications.Add(extra);
                mine.Add(extra);
            }

            return;
        }

        // Prefer an empty vacancy slot; never overwrite the only Pending when seeding Hired/etc.
        var onVacancy = mine.FirstOrDefault(a => a.VacancyId == vacancyId);
        if (onVacancy is not null
            && status != ApplicationStatus.Pending
            && onVacancy.Status is ApplicationStatus.Pending or ApplicationStatus.Withdrawn)
        {
            onVacancy = null;
        }

        if (onVacancy is not null
            && status == ApplicationStatus.Hired
            && onVacancy.Status is ApplicationStatus.Accepted or ApplicationStatus.EmployerContacting)
        {
            onVacancy.Status = ApplicationStatus.Hired;
            onVacancy.EmailVerifiedAt ??= createdAt;
            onVacancy.RespondedAt ??= createdAt.AddHours(6);
            return;
        }

        if (onVacancy is null)
        {
            var created = MakeApp(candidate, vacancyId, status, createdAt);
            db.Applications.Add(created);
            mine.Add(created);
        }
    }

    private static Application MakeApp(User candidate, Guid vacancyId, ApplicationStatus status, DateTime createdAt)
        => new()
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            CandidateUserId = candidate.Id,
            CandidateName = candidate.FullName,
            CandidateEmail = candidate.Email,
            CandidateCity = "Den Haag",
            PreferredTransport = "Fiets",
            EstimatedTravelMinutes = 15,
            DistanceKm = 4,
            Status = status,
            CreatedAt = createdAt,
            EmailVerifiedAt = createdAt,
            RespondedAt = status is ApplicationStatus.Pending ? null : createdAt.AddHours(6)
        };
}
