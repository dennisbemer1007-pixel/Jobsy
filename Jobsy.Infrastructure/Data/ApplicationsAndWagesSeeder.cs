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

        // Unique index is (VacancyId, CandidateEmail) — never INSERT on a vacancy the
        // demo candidate already occupies; update Status in place instead.
        var mine = await db.Applications
            .Where(a => a.CandidateUserId == candidate.Id
                        || a.CandidateEmail == candidate.Email)
            .ToListAsync();

        var now = DateTime.UtcNow;
        UpsertStatus(db, mine, candidate, ApplicationStatus.Pending, photoVacancy.Id, now.AddDays(-2));
        UpsertStatus(db, mine, candidate, ApplicationStatus.Pending, logoVacancy.Id, now.AddDays(-3));
        UpsertStatus(db, mine, candidate, ApplicationStatus.Accepted, vacancies[2].Id, now.AddDays(-5));
        UpsertStatus(db, mine, candidate, ApplicationStatus.Hired, vacancies[3].Id, now.AddDays(-8));
        UpsertStatus(db, mine, candidate, ApplicationStatus.Rejected, vacancies[4].Id, now.AddDays(-10));

        // Second Pending for withdraw smoke — only on a vacancy not already used.
        var pending = mine.Where(a =>
            a.EmailVerifiedAt != null && a.Status == ApplicationStatus.Pending).ToList();
        if (pending.Count < 2)
        {
            var used = mine.Select(a => a.VacancyId).ToHashSet();
            foreach (var v in vacancies.Skip(5))
            {
                if (used.Contains(v.Id))
                {
                    continue;
                }

                var created = MakeApp(candidate, v.Id, ApplicationStatus.Pending, now.AddHours(-6));
                db.Applications.Add(created);
                mine.Add(created);
                used.Add(v.Id);
                if (mine.Count(a => a.Status == ApplicationStatus.Pending && a.EmailVerifiedAt != null) >= 2)
                {
                    break;
                }
            }
        }

        // Absolute Hired guarantee: promote Accepted/EmployerContacting, else insert on a free vacancy.
        if (!mine.Any(a => a.EmailVerifiedAt != null && a.Status == ApplicationStatus.Hired))
        {
            var promote = mine.FirstOrDefault(a =>
                a.Status is ApplicationStatus.Accepted or ApplicationStatus.EmployerContacting);
            if (promote is not null)
            {
                // Keep at least one Accepted for Lopend if we can free another slot for Hired.
                var used = mine.Select(a => a.VacancyId).ToHashSet();
                var free = vacancies.FirstOrDefault(v => !used.Contains(v.Id));
                if (free is not null && mine.Count(a => a.Status == ApplicationStatus.Accepted) <= 1)
                {
                    var hired = MakeApp(candidate, free.Id, ApplicationStatus.Hired, now.AddDays(-8));
                    db.Applications.Add(hired);
                    mine.Add(hired);
                }
                else
                {
                    promote.Status = ApplicationStatus.Hired;
                    promote.EmailVerifiedAt ??= now.AddDays(-8);
                    promote.RespondedAt ??= now.AddDays(-8).AddHours(6);
                }
            }
            else
            {
                var used = mine.Select(a => a.VacancyId).ToHashSet();
                var free = vacancies.FirstOrDefault(v => !used.Contains(v.Id));
                if (free is not null)
                {
                    var hired = MakeApp(candidate, free.Id, ApplicationStatus.Hired, now.AddDays(-8));
                    db.Applications.Add(hired);
                    mine.Add(hired);
                }
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation(
            "Ensured demo candidate application cards for fotokaarten UI (pending={Pending}, hired={Hired}).",
            mine.Count(a => a.Status == ApplicationStatus.Pending && a.EmailVerifiedAt != null),
            mine.Count(a => a.Status == ApplicationStatus.Hired && a.EmailVerifiedAt != null));
    }

    /// <summary>
    /// Ensures one verified application with <paramref name="status"/> on <paramref name="vacancyId"/>.
    /// Respects IX_Applications_VacancyId_CandidateEmail by updating in place when occupied.
    /// </summary>
    private static void UpsertStatus(
        JobsyDbContext db,
        List<Application> mine,
        User candidate,
        ApplicationStatus status,
        Guid vacancyId,
        DateTime createdAt)
    {
        if (mine.Any(a => a.Status == status && a.VacancyId == vacancyId && a.EmailVerifiedAt != null))
        {
            return;
        }

        var sameStatus = mine.FirstOrDefault(a => a.Status == status && a.EmailVerifiedAt != null);
        if (sameStatus is not null)
        {
            // Already have this status on another vacancy — good enough (except second Pending).
            if (status != ApplicationStatus.Pending
                || mine.Count(a => a.Status == ApplicationStatus.Pending && a.EmailVerifiedAt != null) >= 2)
            {
                return;
            }
        }

        var onVacancy = mine.FirstOrDefault(a => a.VacancyId == vacancyId);
        if (onVacancy is not null)
        {
            // Don't steal Pending when placing Hired/Accepted/Rejected if another Pending remains.
            if (status != ApplicationStatus.Pending
                && onVacancy.Status == ApplicationStatus.Pending
                && mine.Count(a => a.Status == ApplicationStatus.Pending) <= 1)
            {
                return;
            }

            onVacancy.Status = status;
            onVacancy.EmailVerifiedAt ??= createdAt;
            onVacancy.RespondedAt = status is ApplicationStatus.Pending
                ? null
                : onVacancy.RespondedAt ?? createdAt.AddHours(6);
            onVacancy.CandidateUserId ??= candidate.Id;
            onVacancy.CandidateEmail = candidate.Email;
            return;
        }

        var created = MakeApp(candidate, vacancyId, status, createdAt);
        db.Applications.Add(created);
        mine.Add(created);
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
