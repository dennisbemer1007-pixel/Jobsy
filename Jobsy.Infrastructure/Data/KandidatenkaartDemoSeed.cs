using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Microsoft.EntityFrameworkCore;
namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Local/demo candidates for kandidatenkaart evidence (test accounts only).
/// </summary>
public static class KandidatenkaartDemoSeed
{
    public const string SeedMarker = "kandidatenkaart-demo-v2";

    private static readonly (string Key, string Label, double Lat, double Lng, int Count)[] Areas =
    [
        ("amersfoort-leusden", "Amersfoort en Leusden", 52.1561, 5.3878, 42),
        ("soest-baarn", "Soest en Baarn", 52.1734, 5.2914, 38),
        ("hilversum", "Hilversum en omgeving", 52.2292, 5.1669, 35),
        ("utrecht-zuid", "Utrecht-Zuid", 52.0705, 5.1115, 31),
        ("barneveld", "Barneveld", 52.1400, 5.5847, 18),
    ];

    public static IReadOnlyList<(string Key, string Label, double Lat, double Lng, int Count)> DemoAreas => Areas;

    private static readonly Guid DemoEmployerCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Aligns demo employer open vacancy with seeded candidate clusters (Amersfoort).</summary>
    public static async Task EnsureEmployerVacancyForMapAsync(
        JobsyDbContext db,
        CancellationToken cancellationToken = default)
    {
        var anchor = new GeoPoint(52.1561, 5.3878);
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == DemoEmployerCompanyId, cancellationToken);
        if (company is not null && company.Location is null)
        {
            company.Location = anchor;
        }

        var vacancies = await db.Vacancies
            .Where(v => v.CompanyId == DemoEmployerCompanyId
                        && v.Status == VacancyStatus.Active
                        && v.PublishedAtUtc != null)
            .ToListAsync(cancellationToken);
        if (vacancies.Count == 0)
        {
            return;
        }

        foreach (var vacancy in vacancies)
        {
            vacancy.Location = anchor;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Seeds weekly-cap skips so invite preview shows cap messaging in evidence runs.</summary>
    public static async Task EnsureWeeklyCapEvidenceAsync(
        JobsyDbContext db,
        CancellationToken cancellationToken = default)
    {
        var vacancy = await db.Vacancies.AsNoTracking()
            .Where(v => v.CompanyId == DemoEmployerCompanyId && v.Status == VacancyStatus.Active)
            .OrderBy(v => v.CreatedAtUtc)
            .Select(v => new { v.Id, v.CompanyId })
            .FirstOrDefaultAsync(cancellationToken);
        if (vacancy is null)
        {
            return;
        }

        var campaignId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeee01");
        if (!await db.KandidatenkaartInviteCampaigns.AnyAsync(c => c.Id == campaignId, cancellationToken))
        {
            var now = DateTime.UtcNow;
            db.KandidatenkaartInviteCampaigns.Add(new KandidatenkaartInviteCampaign
            {
                Id = campaignId,
                VacancyId = vacancy.Id,
                CompanyId = vacancy.CompanyId,
                AreaKey = "kk-evidence",
                AreaLabel = "Evidence",
                MinMatchPercent = 70,
                MaxTravelKm = 60,
                Status = KandidatenkaartCampaignStatus.Completed,
                CurrentRound = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        var weekStart = DateTime.UtcNow.AddDays(-3);
        var cappedIds = await db.Users.AsNoTracking()
            .Where(u => u.IsTestAccount && u.Email.StartsWith("kk-demo-amersfoort"))
            .OrderBy(u => u.Email)
            .Select(u => u.Id)
            .Take(4)
            .ToListAsync(cancellationToken);
        if (cappedIds.Count == 0)
        {
            return;
        }

        foreach (var userId in cappedIds)
        {
            var count = await db.KandidatenkaartInviteDeliveries.CountAsync(
                d => d.CandidateUserId == userId && d.SentAtUtc >= weekStart.AddDays(-4),
                cancellationToken);
            if (count >= 2)
            {
                continue;
            }

            for (var i = count; i < 2; i++)
            {
                db.KandidatenkaartInviteDeliveries.Add(new KandidatenkaartInviteDelivery
                {
                    Id = Guid.NewGuid(),
                    CampaignId = campaignId,
                    CandidateUserId = userId,
                    SentAtUtc = weekStart.AddHours(i),
                    Status = KandidatenkaartInviteDeliveryStatus.Sent,
                    MatchPercent = 80,
                    RoundNumber = 1
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task EnsureAsync(JobsyDbContext db, CancellationToken cancellationToken = default)
    {
        var existing = await db.Users.CountAsync(
            u => u.IsTestAccount && u.Email.StartsWith("kk-demo-"),
            cancellationToken);
        if (existing >= 120)
        {
            await EnsureCompetenciesAsync(db, cancellationToken);
            return;
        }

        var rng = new Random(707);
        var now = DateTime.UtcNow;
        var createdUsers = new List<User>();

        foreach (var area in Areas)
        {
            for (var i = 0; i < area.Count; i++)
            {
                var email = $"kk-demo-{area.Key}-{i:D3}@lobsy.local.test";
                if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
                {
                    continue;
                }

                var lat = area.Lat + (rng.NextDouble() - 0.5) * 0.08;
                var lng = area.Lng + (rng.NextDouble() - 0.5) * 0.12;
                var hoursBucket = rng.Next(0, 4);
                var (minH, maxH) = hoursBucket switch
                {
                    0 => (8m, 15m),
                    1 => (16m, 24m),
                    2 => (24m, 32m),
                    _ => (32m, 40m)
                };
                var availImmediate = rng.NextDouble() > 0.15;
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    FullName = $"Demo kandidaat {area.Key}-{i}",
                    Role = UserRole.Candidate,
                    OpenForWork = true,
                    IsActive = true,
                    IsTestAccount = true,
                    EmailVerifiedAtUtc = now,
                    LastLoginAtUtc = now.AddDays(-rng.Next(0, 10)),
                    TalentPoolConsentAt = now.AddMonths(-2),
                    TalentPoolConsentVersion = SeedMarker,
                    HomeLocation = new GeoPoint(lat, lng),
                    AvailableFromDate = availImmediate
                        ? null
                        : DateOnly.FromDateTime(now.AddDays(rng.Next(5, 90))),
                    PreferencesJson = $$"""
                        {
                          "minHoursPerWeek": {{minH}},
                          "maxHoursPerWeek": {{maxH}},
                          "homeAddress": "{{area.Label}}"
                        }
                        """,
                    ReminderEmailsEnabled = true
                };
                db.Users.Add(user);
                createdUsers.Add(user);
            }
        }

        if (createdUsers.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        await EnsureCompetenciesAsync(db, cancellationToken);
    }

    private static async Task EnsureCompetenciesAsync(JobsyDbContext db, CancellationToken cancellationToken)
    {
        var demoIds = await db.Users.AsNoTracking()
            .Where(u => u.IsTestAccount && u.Email.StartsWith("kk-demo-"))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        if (demoIds.Count == 0)
        {
            return;
        }

        var haveCompetency = await db.CandidateCompetencies.AsNoTracking()
            .Where(c => demoIds.Contains(c.UserId))
            .Select(c => c.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
        if (haveCompetency >= demoIds.Count - 5)
        {
            return;
        }

        var rng = new Random(808);
        var now = DateTime.UtcNow;
        foreach (var userId in demoIds)
        {
            if (await db.CandidateCompetencies.AnyAsync(c => c.UserId == userId, cancellationToken))
            {
                continue;
            }

            var baseScore = rng.Next(62, 94);
            db.CandidateCompetencies.Add(new CandidateCompetency
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CandidateCompetencyStatuses.Completed,
                SamenwerkenPercent = baseScore,
                ResultaatgerichtheidPercent = Math.Clamp(baseScore + rng.Next(-8, 8), 55, 98),
                StressbestendigheidPercent = Math.Clamp(baseScore + rng.Next(-10, 10), 55, 98),
                InnovatiePercent = Math.Clamp(baseScore + rng.Next(-6, 6), 55, 98),
                ExtraversiePercent = Math.Clamp(baseScore + rng.Next(-5, 5), 55, 98),
                MatchTagsJson = """["Samenwerken","Praktisch"]""",
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CompletedAtUtc = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
