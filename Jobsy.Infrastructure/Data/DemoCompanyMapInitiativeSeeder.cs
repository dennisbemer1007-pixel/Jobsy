using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

internal static class DemoCompanyMapInitiativeSeeder
{
    private static readonly string[] HubPhotos =
    [
        "/images/demo/bakkerij-1.jpg",
        "/images/demo/bakkerij-2.jpg",
        "/images/demo/bakkerij-3.jpg"
    ];

    private static readonly Guid SupermarketId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid WestlandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DemoCandidateId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");

    public static async Task SeedAsync(JobsyDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (await db.CompanyInitiativeSettings.AnyAsync(cancellationToken))
        {
            return;
        }

        logger.LogInformation("Seeding company map initiative demo data…");

        var root = await db.Companies.FirstAsync(c => c.Id == SupermarketId, cancellationToken);
        root.HubAboutText =
            "Bakkerij De Korenaar is een familiebedrijf in het hart van Den Haag. We bakken dagvers brood en patisserie.";
        root.HubCultureText = "Klein team, korte lijnen, veel vakmanschap en ruimte om te groeien.";
        root.HubVideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        root.HubPhotoUrlsJson = JsonSerializer.Serialize(HubPhotos);
        root.InitiativeAvgResponseDays = 2;
        root.Name = "Bakkerij De Korenaar";

        var westland = await db.Companies.FirstAsync(c => c.Id == WestlandId, cancellationToken);
        westland.HubAboutText = "Logistiek en vers — altijd in beweging.";
        westland.InitiativeAvgResponseDays = 3;

        var settings = new CompanyInitiativeSettings
        {
            Id = Guid.Parse("77777777-7777-7777-7777-777777777701"),
            RootCompanyId = SupermarketId,
            IsEnabled = true,
            ContactEmail = "hr@korenaar.local",
            ContactEmailConfirmedAtUtc = DateTime.UtcNow.AddDays(-30),
            MaxPerWeek = 5,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.CompanyInitiativeSettings.Add(settings);

        var teamBakker = new InitiativeTeam
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
        var teamWinkel = new InitiativeTeam
        {
            Id = Guid.Parse("77777777-7777-7777-7777-777777777703"),
            RootCompanyId = SupermarketId,
            BranchCompanyId = WestlandId,
            Name = "Winkel & vitrine",
            Description = "Klantcontact en presentatie.",
            ContactEmail = "winkel@korenaar.local",
            ContactEmailConfirmedAtUtc = DateTime.UtcNow.AddDays(-20),
            MaxPerWeek = 2,
            IsOpen = true,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-40),
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.InitiativeTeams.AddRange(teamBakker, teamWinkel);

        if (await db.Users.AnyAsync(u => u.Id == DemoCandidateId, cancellationToken))
        {
            var created = DateTime.UtcNow.AddDays(-2);
            db.WorkInitiatives.Add(new WorkInitiative
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888801"),
                CandidateUserId = DemoCandidateId,
                RootCompanyId = SupermarketId,
                BranchCompanyId = WestlandId,
                InitiativeTeamId = teamWinkel.Id,
                Status = WorkInitiativeStatus.Viewed,
                MatchLabel = InitiativeMatchLabel.Good,
                Motivation = "Ik wil graag in de winkel werken en klanten helpen.",
                SnapshotRoleTitle = "Retail / vitrine",
                SnapshotRegionLabel = "Den Haag",
                SnapshotWorkDnaSummary = "Sociaal, nauwkeurig, ochtendmens",
                SnapshotAvailabilitySummary = "Do–zo",
                SnapshotHoursPerWeek = 24,
                CandidateDisplayName = "Demo Kandidaat",
                CandidateEmail = "kandidaat@jobsy.local",
                CountsTowardWeeklyLimit = true,
                CreatedAtUtc = created,
                EmployerViewedAtUtc = created.AddHours(4),
                ResponseDeadlineUtc = InitiativeBusinessDayRules.AddBusinessDaysUtc(created, 5)
            });

            db.WorkInitiatives.Add(new WorkInitiative
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888802"),
                CandidateUserId = DemoCandidateId,
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
                CandidateDisplayName = "Demo Kandidaat",
                CandidateEmail = "kandidaat@jobsy.local",
                CountsTowardWeeklyLimit = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12),
                RespondedAtUtc = DateTime.UtcNow.AddDays(-5),
                ResponseDeadlineUtc = DateTime.UtcNow.AddDays(-6)
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
