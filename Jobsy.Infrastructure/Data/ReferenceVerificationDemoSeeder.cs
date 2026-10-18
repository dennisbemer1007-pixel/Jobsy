using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>Deterministic demo data for reference-verification UI screenshots (Development).</summary>
public static class ReferenceVerificationDemoIds
{
    public const string Email = "referenties.demo@jobsy.local";
    public const string IncompleteEmail = "referenties.incomplete@jobsy.local";
    public static readonly Guid UserId = Guid.Parse("aaaaaaaa-9999-9999-9999-999999999999");
    public static readonly Guid IncompleteUserId = Guid.Parse("aaaaaaaa-9998-9998-9998-999999999998");
    public static readonly Guid WorkEntryBakkerij = Guid.Parse("88888888-1111-1111-1111-111111111111");
    public static readonly Guid WorkEntryAh = Guid.Parse("88888888-2222-2222-2222-222222222222");
    public static readonly Guid ConfirmedRequestId = Guid.Parse("88888888-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid PendingRequestId = Guid.Parse("88888888-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid ExpiredRequestId = Guid.Parse("88888888-cccc-cccc-cccc-cccccccccccc");
    public static readonly string OpenReferentToken = new('e', 64);
    public static readonly string ExpiredReferentToken = new('f', 64);
}

internal static class ReferenceVerificationDemoSeeder
{
    private const string DemoPreferencesJson = """
        {"language":"nl","aboutMe":"Ik werk graag in de winkel en vind contact met klanten belangrijk. Betrouwbaar en collegiaal.","workRegion":"Haaglanden","maxTravelMinutes":25,"preferredTransport":"Fiets","minHoursPerWeek":24,"maxHoursPerWeek":36,"spokenLanguages":[{"code":"nl","level":"moedertaal"}],"dutchLevel":"goed","employers":[{"entryId":"88888888-1111-1111-1111-111111111111","employerName":"Bakkerij Jansen","role":"Verkoopmedewerker","startMonth":"2021-03","isCurrent":true},{"entryId":"88888888-2222-2222-2222-222222222222","employerName":"Albert Heijn","role":"Kassamedewerker","startMonth":"2018-09","endMonth":"2021-02"}]}
        """;

    public static async Task EnsureAsync(JobsyDbContext db, ILogger logger)
    {
        try
        {
            await EnsureUserAndReferencesAsync(db);
            await EnsureIncompleteGateUserAsync(db);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reference verification demo seed failed; continuing.");
        }
    }

    private static async Task EnsureUserAndReferencesAsync(JobsyDbContext db)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == ReferenceVerificationDemoIds.UserId);
        if (user is null)
        {
            user = new User
            {
                Id = ReferenceVerificationDemoIds.UserId,
                Email = ReferenceVerificationDemoIds.Email,
                FirstName = "Sanne",
                FullName = "Sanne de Vries",
                Role = UserRole.Candidate,
                IsActive = true,
                OpenForWork = true,
                PreferencesJson = DemoPreferencesJson
            };
            db.Users.Add(user);
        }
        else
        {
            user.PreferencesJson = DemoPreferencesJson;
            user.FirstName = "Sanne";
            user.FullName = "Sanne de Vries";
            user.OpenForWork = true;
        }

        await EnsureDnaCompleteAsync(db, user.Id);
        await EnsureReferenceRowsAsync(db, user.Id);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureDnaCompleteAsync(JobsyDbContext db, Guid userId)
    {
        var done = DateTime.UtcNow.AddMonths(-2);
        var competency = await db.CandidateCompetencies.FirstOrDefaultAsync(c => c.UserId == userId);
        if (competency is null)
        {
            db.CandidateCompetencies.Add(new CandidateCompetency
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CandidateCompetencyStatuses.Completed,
                AnswersJson = "{}",
                RiasecTagsJson = "[]",
                MatchTagsJson = "[]",
                CompletedAtUtc = done,
                SamenwerkenPercent = 73,
                ResultaatgerichtheidPercent = 55,
                StressbestendigheidPercent = 48,
                InnovatiePercent = 41,
                ExtraversiePercent = 60
            });
        }
        else
        {
            competency.Status = CandidateCompetencyStatuses.Completed;
            competency.CompletedAtUtc = done;
            competency.SamenwerkenPercent = 73;
            competency.ResultaatgerichtheidPercent = 55;
            competency.StressbestendigheidPercent = 48;
            competency.InnovatiePercent = 41;
            competency.ExtraversiePercent = 60;
        }

        var career = await db.CandidateCareerInterests.FirstOrDefaultAsync(c => c.UserId == userId);
        if (career is null)
        {
            db.CandidateCareerInterests.Add(new CandidateCareerInterest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CandidateCompetencyStatuses.Completed,
                AnswersJson = "{}",
                CompletedAtUtc = done.AddDays(1),
                RealisticPercent = 22,
                InvestigativePercent = 18,
                ArtisticPercent = 14,
                SocialPercent = 72,
                EnterprisingPercent = 48,
                ConventionalPercent = 35
            });
        }
        else
        {
            career.Status = CandidateCompetencyStatuses.Completed;
            career.CompletedAtUtc = done.AddDays(1);
            career.RealisticPercent = 22;
            career.InvestigativePercent = 18;
            career.ArtisticPercent = 14;
            career.SocialPercent = 72;
            career.EnterprisingPercent = 48;
            career.ConventionalPercent = 35;
        }

        var culture = await db.CandidateCulturePersonalityProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
        if (culture is null)
        {
            db.CandidateCulturePersonalityProfiles.Add(new CandidateCulturePersonalityProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CandidateCompetencyStatuses.Completed,
                AnswersJson = "{}",
                CompletedAtUtc = done.AddDays(2),
                AutonomyPercent = 35,
                InformalPercent = 55,
                CollaborationPercent = 78,
                FlexibilityPercent = 42,
                InnovationPercent = 38,
                PeopleFirstPercent = 62,
                OpennessPercent = 52,
                ConscientiousnessPercent = 61,
                ExtraversionPercent = 58,
                AgreeablenessPercent = 67,
                EmotionalStabilityPercent = 54
            });
        }
        else
        {
            culture.Status = CandidateCompetencyStatuses.Completed;
            culture.CompletedAtUtc = done.AddDays(2);
            culture.AutonomyPercent = 35;
            culture.InformalPercent = 55;
            culture.CollaborationPercent = 78;
            culture.FlexibilityPercent = 42;
            culture.InnovationPercent = 38;
            culture.PeopleFirstPercent = 62;
            culture.OpennessPercent = 52;
            culture.ConscientiousnessPercent = 61;
            culture.ExtraversionPercent = 58;
            culture.AgreeablenessPercent = 67;
            culture.EmotionalStabilityPercent = 54;
        }

        var values = await db.CandidateValuesProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
        if (values is null)
        {
            db.CandidateValuesProfiles.Add(new CandidateValuesProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CandidateCompetencyStatuses.Completed,
                AnswersJson = "{}",
                CompletedAtUtc = done.AddDays(3),
                AutonomyPercent = 28,
                ConnectionPercent = 62,
                AchievementPercent = 45,
                StabilityPercent = 70,
                ImpactPercent = 40
            });
        }
        else
        {
            values.Status = CandidateCompetencyStatuses.Completed;
            values.CompletedAtUtc = done.AddDays(3);
            values.AutonomyPercent = 28;
            values.ConnectionPercent = 62;
            values.AchievementPercent = 45;
            values.StabilityPercent = 70;
            values.ImpactPercent = 40;
        }
    }

    private static async Task EnsureIncompleteGateUserAsync(JobsyDbContext db)
    {
        var prefsJson = JsonSerializer.Serialize(new
        {
            language = "nl",
            employers = new[]
            {
                new
                {
                    entryId = Guid.Parse("88888888-dddd-dddd-dddd-dddddddddddd"),
                    employerName = "Demo Winkel",
                    role = "Hulpkracht",
                    startMonth = "2023-01"
                }
            }
        });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == ReferenceVerificationDemoIds.IncompleteUserId);
        if (user is null)
        {
            user = new User
            {
                Id = ReferenceVerificationDemoIds.IncompleteUserId,
                Email = ReferenceVerificationDemoIds.IncompleteEmail,
                FirstName = "Noah",
                FullName = "Noah Incomplete",
                Role = UserRole.Candidate,
                IsActive = true,
                OpenForWork = true,
                PreferencesJson = prefsJson
            };
            db.Users.Add(user);
        }
        else
        {
            user.PreferencesJson = prefsJson;
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureReferenceRowsAsync(JobsyDbContext db, Guid userId)
    {
        var now = DateTime.UtcNow;
        var confirmed = await db.WorkExperienceReferenceRequests
            .FirstOrDefaultAsync(r => r.Id == ReferenceVerificationDemoIds.ConfirmedRequestId);
        if (confirmed is null)
        {
            confirmed = new WorkExperienceReferenceRequest
            {
                Id = ReferenceVerificationDemoIds.ConfirmedRequestId,
                UserId = userId,
                WorkExperienceEntryId = ReferenceVerificationDemoIds.WorkEntryBakkerij,
                ReferentName = "Mark Peters",
                Relation = "leidinggevende",
                ReferentEmail = "mark.peters@demo.local",
                Status = WorkExperienceReferenceStatus.Confirmed,
                EmployerName = "Bakkerij Jansen",
                RoleTitle = "Verkoopmedewerker",
                StartMonth = "2021-03",
                IsCurrent = true,
                ConfirmedEmployer = true,
                ConfirmedPeriod = true,
                ConfirmedRole = true,
                EndorsedCompetenceCodesJson = ReferenceVerificationRules.SerializeEndorsedCodes(
                [
                    CompetencyTestCatalog.Samenwerken,
                    CompetencyTestCatalog.Resultaatgerichtheid,
                    CompetencyTestCatalog.Stressbestendigheid,
                    CompetencyTestCatalog.Innovatie
                ]),
                ReferentComment = "Sanne is betrouwbaar en klantgericht in de winkel. Fijn om mee te werken.",
                CandidateConsentAtUtc = now.AddDays(-20),
                ConsentVersion = ReferenceVerificationRules.ConsentVersion,
                CreatedAtUtc = now.AddDays(-20),
                UpdatedAtUtc = now.AddDays(-8),
                CompletedAtUtc = now.AddDays(-8)
            };
            db.WorkExperienceReferenceRequests.Add(confirmed);
        }

        var pending = await db.WorkExperienceReferenceRequests
            .FirstOrDefaultAsync(r => r.Id == ReferenceVerificationDemoIds.PendingRequestId);
        if (pending is null)
        {
            pending = new WorkExperienceReferenceRequest
            {
                Id = ReferenceVerificationDemoIds.PendingRequestId,
                UserId = userId,
                WorkExperienceEntryId = ReferenceVerificationDemoIds.WorkEntryBakkerij,
                ReferentName = "Mark Peters",
                Relation = "collega",
                ReferentEmail = "lisa.bakker@demo.local",
                Status = WorkExperienceReferenceStatus.Pending,
                EmployerName = "Bakkerij Jansen",
                RoleTitle = "Verkoopmedewerker",
                StartMonth = "2021-03",
                IsCurrent = true,
                CandidateConsentAtUtc = now.AddDays(-3),
                ConsentVersion = ReferenceVerificationRules.ConsentVersion,
                CreatedAtUtc = now.AddDays(-3),
                UpdatedAtUtc = now.AddDays(-3)
            };
            db.WorkExperienceReferenceRequests.Add(pending);
            db.WorkExperienceReferenceTokens.Add(new WorkExperienceReferenceToken
            {
                Id = Guid.NewGuid(),
                RequestId = pending.Id,
                TokenHash = VerificationCodes.Hash(ReferenceVerificationDemoIds.OpenReferentToken),
                ExpiresAtUtc = now.AddDays(11),
                CreatedAtUtc = now.AddDays(-3)
            });
        }
        else
        {
            pending.ReferentName = "Mark Peters";
            pending.Status = WorkExperienceReferenceStatus.Pending;
            pending.CompletedAtUtc = null;
            pending.RemovedByCandidate = false;
            pending.UpdatedAtUtc = now;
        }

        var expired = await db.WorkExperienceReferenceRequests
            .FirstOrDefaultAsync(r => r.Id == ReferenceVerificationDemoIds.ExpiredRequestId);
        if (expired is null)
        {
            expired = new WorkExperienceReferenceRequest
            {
                Id = ReferenceVerificationDemoIds.ExpiredRequestId,
                UserId = userId,
                WorkExperienceEntryId = ReferenceVerificationDemoIds.WorkEntryAh,
                ReferentName = "Ahmed Yilmaz",
                Relation = "leidinggevende",
                ReferentEmail = "ahmed.yilmaz@demo.local",
                Status = WorkExperienceReferenceStatus.Pending,
                EmployerName = "Albert Heijn",
                RoleTitle = "Kassamedewerker",
                StartMonth = "2018-09",
                EndMonth = "2021-02",
                CandidateConsentAtUtc = now.AddDays(-20),
                ConsentVersion = ReferenceVerificationRules.ConsentVersion,
                CreatedAtUtc = now.AddDays(-20),
                UpdatedAtUtc = now.AddDays(-20)
            };
            db.WorkExperienceReferenceRequests.Add(expired);
            db.WorkExperienceReferenceTokens.Add(new WorkExperienceReferenceToken
            {
                Id = Guid.NewGuid(),
                RequestId = expired.Id,
                TokenHash = VerificationCodes.Hash(ReferenceVerificationDemoIds.ExpiredReferentToken),
                ExpiresAtUtc = now.AddDays(-2),
                CreatedAtUtc = now.AddDays(-20)
            });
        }

        // Playwright flows add tokens; reset demo user to the seeded set on each CI boot.
        var keepRequestIds = new[]
        {
            ReferenceVerificationDemoIds.ConfirmedRequestId,
            ReferenceVerificationDemoIds.PendingRequestId,
            ReferenceVerificationDemoIds.ExpiredRequestId
        };
        var extraRequests = await db.WorkExperienceReferenceRequests
            .Where(r => r.UserId == userId && !keepRequestIds.Contains(r.Id))
            .ToListAsync();
        if (extraRequests.Count > 0)
        {
            var extraIds = extraRequests.Select(r => r.Id).ToList();
            db.WorkExperienceReferenceTokens.RemoveRange(
                await db.WorkExperienceReferenceTokens.Where(t => extraIds.Contains(t.RequestId)).ToListAsync());
            db.WorkExperienceReferenceRequests.RemoveRange(extraRequests);
        }

        var openHash = VerificationCodes.Hash(ReferenceVerificationDemoIds.OpenReferentToken);
        var expiredHash = VerificationCodes.Hash(ReferenceVerificationDemoIds.ExpiredReferentToken);

        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthTokens = await db.WorkExperienceReferenceTokens
            .Where(t => t.Request.UserId == userId && t.CreatedAtUtc >= monthStart)
            .Where(t => !(t.RequestId == ReferenceVerificationDemoIds.PendingRequestId && t.TokenHash == openHash))
            .ToListAsync();
        if (monthTokens.Count > 0)
        {
            db.WorkExperienceReferenceTokens.RemoveRange(monthTokens);
        }

        var strayExpiredTokens = await db.WorkExperienceReferenceTokens
            .Where(t => t.RequestId == ReferenceVerificationDemoIds.ExpiredRequestId && t.TokenHash != expiredHash)
            .ToListAsync();
        if (strayExpiredTokens.Count > 0)
        {
            db.WorkExperienceReferenceTokens.RemoveRange(strayExpiredTokens);
        }

        var hasExpired = await db.WorkExperienceReferenceTokens.AnyAsync(
            t => t.RequestId == ReferenceVerificationDemoIds.ExpiredRequestId && t.TokenHash == expiredHash);
        if (!hasExpired)
        {
            db.WorkExperienceReferenceTokens.Add(new WorkExperienceReferenceToken
            {
                Id = Guid.NewGuid(),
                RequestId = ReferenceVerificationDemoIds.ExpiredRequestId,
                TokenHash = expiredHash,
                ExpiresAtUtc = now.AddDays(-2),
                CreatedAtUtc = now.AddDays(-20)
            });
        }
        var hasOpen = await db.WorkExperienceReferenceTokens.AnyAsync(
            t => t.RequestId == ReferenceVerificationDemoIds.PendingRequestId && t.TokenHash == openHash);
        if (!hasOpen)
        {
            db.WorkExperienceReferenceTokens.Add(new WorkExperienceReferenceToken
            {
                Id = Guid.NewGuid(),
                RequestId = ReferenceVerificationDemoIds.PendingRequestId,
                TokenHash = openHash,
                ExpiresAtUtc = now.AddDays(11),
                CreatedAtUtc = now.AddDays(-3)
            });
        }

        var openReferentTokens = await db.WorkExperienceReferenceTokens
            .Where(t => t.RequestId == ReferenceVerificationDemoIds.PendingRequestId && t.TokenHash == openHash)
            .ToListAsync();
        foreach (var token in openReferentTokens)
        {
            token.UsedAtUtc = null;
            token.ExpiresAtUtc = now.AddDays(11);
        }
    }
}
