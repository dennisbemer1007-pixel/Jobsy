using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>Rich DNA-paspoort demo profile (Marta Kowalska) for local PDF previews.</summary>
internal static class PassportPdfMartaDemoSeeder
{
    public static readonly Guid MartaUserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeee01");

    public static async Task EnsureAsync(JobsyDbContext db, ILogger logger)
    {
        var done = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == MartaUserId);
        if (user is null)
        {
            user = new User
            {
                Id = MartaUserId,
                Email = "marta.paspoort@jobsy.local",
                FullName = "Marta Kowalska",
                Role = UserRole.Candidate,
                IsActive = true,
                OpenForWork = true,
                PhoneNumber = "+31600000000",
                WhatsAppContactAllowed = true,
                EmailVerifiedAtUtc = done,
                HomeLocation = new Jobsy.Core.ValueObjects.GeoPoint(51.994, 4.209),
                PreferencesJson = MartaPreferencesJson
            };
            db.Users.Add(user);
            await db.LocalAuthCredentials.AddAsync(new LocalAuthCredential
            {
                Id = Guid.NewGuid(),
                UserId = MartaUserId,
                Email = user.Email,
                PasswordHash = JobsyPasswordHasher.Hash(DemoUsersSeeder.DemoPassword)
            });
        }
        else
        {
            user.FullName = "Marta Kowalska";
            user.OpenForWork = true;
            user.EmailVerifiedAtUtc ??= done;
            user.PreferencesJson = MartaPreferencesJson;
        }

        if (!await db.CandidateCompetencies.AnyAsync(c => c.UserId == MartaUserId && c.Status == CandidateCompetencyStatuses.Completed))
        {
            db.CandidateCompetencies.Add(new CandidateCompetency
            {
                Id = Guid.NewGuid(),
                UserId = MartaUserId,
                Status = CandidateCompetencyStatuses.Completed,
                CompletedAtUtc = done.AddDays(-7),
                SamenwerkenPercent = 73,
                ResultaatgerichtheidPercent = 55,
                StressbestendigheidPercent = 48,
                InnovatiePercent = 41,
                ExtraversiePercent = 60
            });
        }

        if (!await db.CandidateCareerInterests.AnyAsync(c => c.UserId == MartaUserId && c.Status == CandidateCompetencyStatuses.Completed))
        {
            db.CandidateCareerInterests.Add(new CandidateCareerInterest
            {
                Id = Guid.NewGuid(),
                UserId = MartaUserId,
                Status = CandidateCompetencyStatuses.Completed,
                CompletedAtUtc = done.AddDays(-6),
                RealisticPercent = 80,
                InvestigativePercent = 10,
                ArtisticPercent = 11,
                SocialPercent = 12,
                EnterprisingPercent = 13,
                ConventionalPercent = 14
            });
        }

        if (!await db.CandidateCulturePersonalityProfiles.AnyAsync(c => c.UserId == MartaUserId && c.Status == CandidateCompetencyStatuses.Completed))
        {
            db.CandidateCulturePersonalityProfiles.Add(new CandidateCulturePersonalityProfile
            {
                Id = Guid.NewGuid(),
                UserId = MartaUserId,
                Status = CandidateCompetencyStatuses.Completed,
                CompletedAtUtc = done.AddDays(-3),
                AutonomyPercent = 20,
                InformalPercent = 30,
                CollaborationPercent = 85,
                FlexibilityPercent = 35,
                InnovationPercent = 40,
                PeopleFirstPercent = 55
            });
        }

        if (!await db.CandidateValuesProfiles.AnyAsync(c => c.UserId == MartaUserId && c.Status == CandidateCompetencyStatuses.Completed))
        {
            db.CandidateValuesProfiles.Add(new CandidateValuesProfile
            {
                Id = Guid.NewGuid(),
                UserId = MartaUserId,
                Status = CandidateCompetencyStatuses.Completed,
                CompletedAtUtc = done,
                AutonomyPercent = 15,
                ConnectionPercent = 25,
                AchievementPercent = 35,
                StabilityPercent = 88,
                ImpactPercent = 42
            });
        }

        var settings = await db.PlatformFeatureSettings.OrderBy(s => s.Id).FirstOrDefaultAsync();
        if (settings is not null)
        {
            settings.PassportPdfV2Enabled = true;
            settings.PassportPdfDnaBlocksEnabled = true;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Passport PDF demo profile ensured for Marta Kowalska ({Email}).", user.Email);
    }

    private const string MartaPreferencesJson = """
        {"roles":["Orderpicker"],"maxTravelMinutes":30,"preferredTransport":"Fiets","language":"nl","aboutMe":"Ik werk graag met mijn handen.","drivingLicenses":["B"],"availability":{"Ma":["Ochtend","Middag"],"Di":["Ochtend","Middag"],"Wo":["Ochtend"]},"employers":[{"employerName":"Kwekerij De Voorbeeldtuin","role":"Oogstmedewerker","description":"Tomaten oogsten","startMonth":"2024-03"},{"employerName":"Logistiek Voorbeeld BV","role":"Magazijnmedewerker","description":"Orderpick","startMonth":"2022-01","endMonth":"2024-02"}],"educations":["MBO 1 / Entree"],"minHoursPerWeek":32,"maxHoursPerWeek":40,"certificates":[{"name":"VCA Basis","year":2024},{"name":"Heftruck","year":2023}],"spokenLanguages":[{"code":"pl","level":"moedertaal"}],"dutchLevel":"basis","workPreferences":{"indoor":"prefer","outdoor":"ok-not-frost","physicalWork":"lifting-15","pace":"norm-ok"},"shareEmployerPreferences":true,"employerPreferences":["small-team","clear-instructions"],"workRegion":"Westland","hasOwnCar":false,"contractPreferences":["vast"],"learningGoals":["Nederlands naar B1","Reachtruck-certificaat"]}
        """;
}
