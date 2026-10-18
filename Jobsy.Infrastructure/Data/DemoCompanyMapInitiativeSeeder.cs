using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Media;
using Jobsy.Core.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

internal static class DemoCompanyMapInitiativeSeeder
{
    private static readonly string[] HubPhotos =
    [
        "/images/teaser/westland-hero.jpg"
    ];

    private static readonly Guid SupermarketId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid WestlandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CafeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SanneUserId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
    private static readonly Guid PendingInitiativeId = Guid.Parse("88888888-8888-8888-8888-888888888803");

    public static async Task SeedAsync(JobsyDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        await EnsureBakkerijBrandingAsync(db, logger, cancellationToken);

        if (await db.CompanyInitiativeSettings.AnyAsync(cancellationToken))
        {
            if (await db.WorkInitiatives.AnyAsync(cancellationToken))
            {
                return;
            }

            logger.LogInformation("Company initiative settings exist; seeding missing WorkInitiatives…");
        }
        else
        {
            logger.LogInformation("Seeding company map initiative demo data…");
        }

        InitiativeTeam teamBakker;
        InitiativeTeam teamWinkel;
        if (!await db.CompanyInitiativeSettings.AnyAsync(cancellationToken))
        {
            db.CompanyInitiativeSettings.Add(new CompanyInitiativeSettings
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777701"),
                RootCompanyId = SupermarketId,
                IsEnabled = true,
                ContactEmail = "hr@korenaar.local",
                ContactEmailConfirmedAtUtc = DateTime.UtcNow.AddDays(-30),
                MaxPerWeek = 5,
                UpdatedAtUtc = DateTime.UtcNow
            });

            teamBakker = new InitiativeTeam
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777702"),
                RootCompanyId = SupermarketId,
                BranchCompanyId = null,
                Name = "Bakkerij",
                Description = "Brood, patisserie en vitrine.",
                ContactEmail = "bakkerij@korenaar.local",
                ContactEmailConfirmedAtUtc = DateTime.UtcNow.AddDays(-20),
                MaxPerWeek = 3,
                IsOpen = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-40),
                UpdatedAtUtc = DateTime.UtcNow
            };
            teamWinkel = new InitiativeTeam
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777703"),
                RootCompanyId = SupermarketId,
                BranchCompanyId = WestlandId,
                Name = "Winkel en lunchroom",
                Description = "Klantcontact en presentatie in de winkel.",
                ContactEmail = "winkel@korenaar.local",
                ContactEmailConfirmedAtUtc = DateTime.UtcNow.AddDays(-20),
                MaxPerWeek = 2,
                IsOpen = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-40),
                UpdatedAtUtc = DateTime.UtcNow
            };
            db.InitiativeTeams.AddRange(teamBakker, teamWinkel);
        }
        else
        {
            teamBakker = await db.InitiativeTeams.FirstAsync(
                t => t.Id == Guid.Parse("77777777-7777-7777-7777-777777777702"),
                cancellationToken);
            teamWinkel = await db.InitiativeTeams.FirstAsync(
                t => t.Id == Guid.Parse("77777777-7777-7777-7777-777777777703"),
                cancellationToken);
        }

        if (await db.Users.AnyAsync(u => u.Id == SanneUserId, cancellationToken))
        {
            var created = DateTime.UtcNow.AddDays(-2);
            db.WorkInitiatives.Add(new WorkInitiative
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888801"),
                CandidateUserId = SanneUserId,
                RootCompanyId = SupermarketId,
                BranchCompanyId = WestlandId,
                InitiativeTeamId = teamWinkel.Id,
                Status = WorkInitiativeStatus.Accepted,
                MatchLabel = InitiativeMatchLabel.Good,
                Motivation = "Ik wil graag in de winkel werken en klanten helpen.",
                SnapshotRoleTitle = "Verkoopmedewerker bakkerij",
                SnapshotRegionLabel = "Den Haag",
                SnapshotWorkDnaSummary = "Sociaal, nauwkeurig, ochtendmens",
                SnapshotAvailabilitySummary = "Do–zo",
                SnapshotHoursPerWeek = 24,
                CandidateDisplayName = "Sanne de Vries",
                CandidateEmail = "sanne@jobsy.local",
                CountsTowardWeeklyLimit = true,
                CreatedAtUtc = created,
                EmployerViewedAtUtc = created.AddHours(4),
                ResponseDeadlineUtc = InitiativeBusinessDayRules.AddBusinessDaysUtc(created, 5)
            });

            db.WorkInitiatives.Add(new WorkInitiative
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888802"),
                CandidateUserId = SanneUserId,
                RootCompanyId = SupermarketId,
                InitiativeTeamId = teamBakker.Id,
                Status = WorkInitiativeStatus.AutoDeclined,
                MatchLabel = InitiativeMatchLabel.Strong,
                Motivation = "Passie voor ambachtelijk bakwerk.",
                SnapshotRoleTitle = "Bakkerij",
                SnapshotRegionLabel = "Den Haag",
                SnapshotWorkDnaSummary = "Creatief, punctueel",
                SnapshotAvailabilitySummary = "Ma–vr",
                SnapshotHoursPerWeek = 32,
                CandidateDisplayName = "Sanne de Vries",
                CandidateEmail = "sanne@jobsy.local",
                CountsTowardWeeklyLimit = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12),
                RespondedAtUtc = DateTime.UtcNow.AddDays(-5),
                ResponseDeadlineUtc = DateTime.UtcNow.AddDays(-6)
            });

            db.WorkInitiatives.Add(new WorkInitiative
            {
                Id = PendingInitiativeId,
                CandidateUserId = SanneUserId,
                RootCompanyId = SupermarketId,
                InitiativeTeamId = teamBakker.Id,
                Status = WorkInitiativeStatus.Pending,
                MatchLabel = InitiativeMatchLabel.Good,
                Motivation = "Ik wil graag starten in de bakkerij.",
                SnapshotRoleTitle = "Medewerker bakkerij",
                SnapshotRegionLabel = "Den Haag Centrum",
                SnapshotWorkDnaSummary = "Nauwkeurig, vroege diensten",
                SnapshotAvailabilitySummary = "Ma–za ochtend",
                SnapshotHoursPerWeek = 20,
                CandidateDisplayName = "Sanne de Vries",
                CandidateEmail = "sanne@jobsy.local",
                CountsTowardWeeklyLimit = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                ResponseDeadlineUtc = InitiativeBusinessDayRules.AddBusinessDaysUtc(DateTime.UtcNow.AddDays(-1), 5)
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task EnsureBakkerijBrandingAsync(
        JobsyDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var root = await db.Companies.FirstOrDefaultAsync(c => c.Id == SupermarketId, cancellationToken);
        if (root is null)
        {
            return;
        }

        root.Name = "Bakkerij De Korenaar";
        root.KvkNumber = "12345678";
        root.KvkEstablishmentId = "12345678_0001";
        root.HubAboutText =
            "Bakkerij De Korenaar is een familiebedrijf in het hart van Den Haag. We bakken dagvers brood en patisserie.";
        root.HubCultureText =
            "Klein team, korte lijnen, veel vakmanschap en ruimte om te groeien.";
        root.HubVideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        root.HubPhotoUrlsJson = JsonSerializer.Serialize(HubPhotos);
        root.InitiativeAvgResponseDays = 2;

        var westland = await db.Companies.FirstOrDefaultAsync(c => c.Id == WestlandId, cancellationToken);
        if (westland is not null)
        {
            westland.KvkNumber = "12345678";
            westland.KvkEstablishmentId = "12345678_0002";
            westland.Name = "Den Haag Centrum";
        }

        var cafe = await db.Companies.FirstOrDefaultAsync(c => c.Id == CafeId, cancellationToken);
        if (cafe is not null)
        {
            cafe.KvkNumber = "12345678";
            cafe.KvkEstablishmentId = "12345678_0003";
            cafe.Name = "Den Haag Loosduinen";
        }

        var vacancies = await db.Vacancies
            .Where(v => v.CompanyId == WestlandId || v.CompanyId == CafeId || v.CompanyId == SupermarketId)
            .Take(5)
            .ToListAsync(cancellationToken);
        var titles = new[]
        {
            "Verkoopmedewerker bakkerij",
            "Allround medewerker bakkerij",
            "Hulp bakker",
            "Medewerker lunchroom",
            "Vakantiewerk vitrine"
        };
        for (var i = 0; i < vacancies.Count; i++)
        {
            var title = titles[i % titles.Length];
            vacancies[i].Title = title;
            vacancies[i].WorkTypes = WorkType.Winkel | WorkType.Horeca;
            var bakeryLike = title.Contains("bakker", StringComparison.OrdinalIgnoreCase)
                             || title.Contains("vitrine", StringComparison.OrdinalIgnoreCase);
            vacancies[i].ImageUrl = VacancyImageUrls.Placeholder(
                vacancies[i].Id,
                bakeryLike ? WorkType.Horeca : WorkType.Winkel);
        }

        var cafeVacancies = await db.Vacancies.Where(v => v.CompanyId == CafeId).ToListAsync(cancellationToken);
        foreach (var v in cafeVacancies)
        {
            v.Status = VacancyStatus.Archived;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Bakkerij De Korenaar demo branding applied (KVK 12345678).");
    }
}
