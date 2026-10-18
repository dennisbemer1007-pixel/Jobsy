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
    public static readonly Guid UserId = Guid.Parse("aaaaaaaa-9999-9999-9999-999999999999");
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
    public static async Task EnsureAsync(JobsyDbContext db, ILogger logger)
    {
        try
        {
            await EnsureUserAndReferencesAsync(db);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reference verification demo seed failed; continuing.");
        }
    }

    private static async Task EnsureUserAndReferencesAsync(JobsyDbContext db)
    {
        var prefsJson = JsonSerializer.Serialize(new
        {
            language = "nl",
            employers = new object[]
            {
                new
                {
                    entryId = ReferenceVerificationDemoIds.WorkEntryBakkerij,
                    employerName = "Bakkerij Jansen",
                    role = "Verkoopmedewerker",
                    startMonth = "2021-03",
                    isCurrent = true
                },
                new
                {
                    entryId = ReferenceVerificationDemoIds.WorkEntryAh,
                    employerName = "Albert Heijn",
                    role = "Kassamedewerker",
                    startMonth = "2018-09",
                    endMonth = "2021-02"
                }
            }
        });

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
                PreferencesJson = prefsJson
            };
            db.Users.Add(user);
        }
        else
        {
            user.PreferencesJson = prefsJson;
            user.FirstName = "Sanne";
            user.FullName = "Sanne de Vries";
        }

        await EnsureDnaCompleteAsync(db, user.Id);
        await EnsureReferenceRowsAsync(db, user.Id);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureDnaCompleteAsync(JobsyDbContext db, Guid userId)
    {
        if (!await db.CandidateCompetencies.AnyAsync(c => c.UserId == userId))
        {
            db.CandidateCompetencies.Add(new CandidateCompetency
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CandidateCompetencyStatuses.Completed,
                AnswersJson = "{}",
                RiasecTagsJson = "[]",
                MatchTagsJson = "[]",
                CompletedAtUtc = DateTime.UtcNow.AddMonths(-2)
            });
        }

        if (!await db.CandidateCareerInterests.AnyAsync(c => c.UserId == userId))
        {
            db.CandidateCareerInterests.Add(new CandidateCareerInterest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CandidateCompetencyStatuses.Completed,
                AnswersJson = "{}"
            });
        }

        if (!await db.CandidateCulturePersonalityProfiles.AnyAsync(c => c.UserId == userId))
        {
            db.CandidateCulturePersonalityProfiles.Add(new CandidateCulturePersonalityProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CandidateCompetencyStatuses.Completed,
                AnswersJson = "{}"
            });
        }

        if (!await db.CandidateValuesProfiles.AnyAsync(c => c.UserId == userId))
        {
            db.CandidateValuesProfiles.Add(new CandidateValuesProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CandidateCompetencyStatuses.Completed,
                AnswersJson = "{}"
            });
        }
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
                ReferentName = "Lisa Bakker",
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
    }
}
