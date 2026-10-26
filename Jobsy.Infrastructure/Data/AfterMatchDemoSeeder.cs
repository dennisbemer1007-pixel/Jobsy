using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Time;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>Deterministic after-match demo threads for local E2E / screenshot runs.</summary>
internal static class AfterMatchDemoSeeder
{
    private static readonly Guid DemoCompanyId = Guid.Parse("77777771-00bb-4000-8000-0000000000bb");
    private static readonly Guid BranchManagerId = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111");
    private static readonly Guid PrimaryCandidateId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");

    private static readonly DateTime YesAt = new(2026, 10, 13, 10, 0, 0, DateTimeKind.Utc);

    private static readonly Guid WestlandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CafeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SupermarketId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly Guid[] RetailBranchCompanyIds = [WestlandId, CafeId, SupermarketId];

    private static readonly Guid[] KeepApplicationIds =
    [
        AfterMatchDemoIds.WaitApp,
        AfterMatchDemoIds.PickSlotsApp,
        AfterMatchDemoIds.ScheduledApp,
        AfterMatchDemoIds.RescheduleApp,
        AfterMatchDemoIds.NotNowApp,
        AfterMatchDemoIds.Verkoop4790App,
    ];

    public static async Task EnsureAsync(JobsyDbContext db, ILogger logger)
    {
        await EnsureCompanyAsync(db);
        await EnsureDemoVacanciesAsync(db);
        await RemoveStaleRetailBranchApplicationsAsync(db, logger);
        await RemoveStaleCompanyApplicationsAsync(db, logger);
        await EnsureApplicationsAsync(db, logger);
    }

    private static DateTime[] DemoSlotStartsUtc()
        => AfterMatchEmployerDemoSlots.InitialUtc(DateTime.UtcNow);

    private static async Task EnsureCompanyAsync(JobsyDbContext db)
    {
        if (!await db.Companies.AnyAsync(c => c.Id == DemoCompanyId))
        {
            db.Companies.Add(new Company
            {
                Id = DemoCompanyId,
                Name = "Bakkerij De Korenaar",
                KvkNumber = "88776655",
                KvkEstablishmentId = "88776655_0001",
                Address = "Loosduinseweg 120, Den Haag",
                ContactEmail = "linda@korenaar.local",
                Type = CompanyType.Employer,
                Location = new GeoPoint(52.0780, 4.3100),
                VerificationStatus = CompanyVerificationStatus.Verified,
                VerificationMethod = CompanyVerificationMethod.AdminCreated,
                VerifiedAtUtc = DateTime.UtcNow,
                VerificationUpdatedAtUtc = DateTime.UtcNow
            });
        }

        if (!await db.UserCompanies.AnyAsync(m => m.UserId == BranchManagerId && m.CompanyId == DemoCompanyId))
        {
            db.UserCompanies.Add(new UserCompany { UserId = BranchManagerId, CompanyId = DemoCompanyId });
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureDemoVacanciesAsync(JobsyDbContext db)
    {
        var start = DateOnly.FromDateTime(YesAt);
        var appIds = new[]
        {
            AfterMatchDemoIds.WaitApp,
            AfterMatchDemoIds.PickSlotsApp,
            AfterMatchDemoIds.ScheduledApp,
            AfterMatchDemoIds.RescheduleApp,
            AfterMatchDemoIds.NotNowApp,
            AfterMatchDemoIds.Verkoop4790App
        };

        foreach (var appId in appIds)
        {
            var vacancyId = AfterMatchDemoIds.VacancyForApplication(appId);
            if (await db.Vacancies.AnyAsync(v => v.Id == vacancyId))
            {
                continue;
            }

            var title = appId == AfterMatchDemoIds.Verkoop4790App
                ? "Verkoopmedewerker"
                : "Medewerker bakkerij";
            db.Vacancies.Add(new Vacancy
            {
                Id = vacancyId,
                CompanyId = WestlandId,
                Title = title,
                Description = "After-match demo vacancy.",
                HourlyWage = 14.5m,
                StartDate = start,
                EndDate = start.AddMonths(6),
                Status = VacancyStatus.Active,
                Location = new GeoPoint(52.0780, 4.3100),
                PublishedAtUtc = YesAt.AddDays(-7),
                CreatedAtUtc = YesAt.AddDays(-14)
            });
        }

        var reassign = await db.Vacancies
            .Where(v => appIds.Contains(v.Id) && v.CompanyId != WestlandId)
            .ToListAsync();
        foreach (var vacancy in reassign)
        {
            vacancy.CompanyId = WestlandId;
        }

        await db.SaveChangesAsync();
    }

    private static async Task<Guid?> ResolveDemoCandidateUserIdAsync(JobsyDbContext db)
    {
        if (await db.Users.AsNoTracking().AnyAsync(u => u.Id == PrimaryCandidateId))
        {
            return PrimaryCandidateId;
        }

        foreach (var email in new[]
                 {
                     "kandidaat@jobsy.local",
                     TestAccountsSollicitatiesDemoSeeder.AcceptatiePrimaryCandidateEmail,
                     "kandidaat.denhaag@jobsy.local",
                 })
        {
            var byEmail = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
            if (byEmail is not null)
            {
                return byEmail.Id;
            }
        }

        return null;
    }

    private static async Task EnsureApplicationsAsync(JobsyDbContext db, ILogger logger)
    {
        var candidateUserId = await ResolveDemoCandidateUserIdAsync(db);
        if (candidateUserId is null)
        {
            logger.LogInformation("After-match demo seed skipped (no demo candidate user).");
            return;
        }

        await UpsertAndSaveAppAsync(db, candidateUserId.Value, AfterMatchDemoIds.WaitApp, InterviewStatus.Proposing, null, hasMessage: true);
        await UpsertAndSaveAppAsync(db, candidateUserId.Value, AfterMatchDemoIds.PickSlotsApp, InterviewStatus.Proposing, "slots", hasMessage: true);
        await UpsertAndSaveAppAsync(db, candidateUserId.Value, AfterMatchDemoIds.ScheduledApp, InterviewStatus.Scheduled, "scheduled", hasMessage: true);
        await UpsertAndSaveAppAsync(db, candidateUserId.Value, AfterMatchDemoIds.RescheduleApp, InterviewStatus.RescheduleRequested, "reschedule", hasMessage: true);
        await UpsertAndSaveAppAsync(db, candidateUserId.Value, AfterMatchDemoIds.NotNowApp, InterviewStatus.Done, "notnow", hasMessage: false, outcome: AfterMatchOutcomeKind.NotNow);
        await UpsertAndSaveAppAsync(db, candidateUserId.Value, AfterMatchDemoIds.Verkoop4790App, InterviewStatus.Scheduled, "scheduled4790", hasMessage: true);

        logger.LogInformation("After-match demo applications ensured.");
    }

    private static async Task UpsertAndSaveAppAsync(
        JobsyDbContext db,
        Guid candidateUserId,
        Guid appId,
        InterviewStatus interviewStatus,
        string? scenario,
        bool hasMessage,
        AfterMatchOutcomeKind? outcome = null)
    {
        await UpsertAppAsync(db, candidateUserId, appId, interviewStatus, scenario, hasMessage, outcome);
        await db.SaveChangesAsync();
        if (scenario is "scheduled" or "scheduled4790" or "reschedule")
        {
            await LinkChosenInterviewSlotAsync(db, appId);
            await db.SaveChangesAsync();
        }
    }

    private static async Task LinkChosenInterviewSlotAsync(JobsyDbContext db, Guid appId)
    {
        var interview = await db.Interviews.Include(i => i.Slots).FirstOrDefaultAsync(i => i.ApplicationId == appId);
        if (interview is null)
        {
            return;
        }

        var chosen = interview.Slots.FirstOrDefault(s => s.IsSelected) ?? interview.Slots.OrderBy(s => s.StartUtc).FirstOrDefault();
        if (chosen is null)
        {
            return;
        }

        chosen.IsSelected = true;
        interview.ChosenSlotId = chosen.Id;
    }

    private static async Task UpsertAppAsync(
        JobsyDbContext db,
        Guid candidateUserId,
        Guid appId,
        InterviewStatus interviewStatus,
        string? scenario,
        bool hasMessage,
        AfterMatchOutcomeKind? outcome = null)
    {
        var app = await db.Applications
            .Include(a => a.Vacancy)
            .FirstOrDefaultAsync(a => a.Id == appId);

        if (app is null)
        {
            app = new Application
            {
                Id = appId,
                VacancyId = AfterMatchDemoIds.VacancyForApplication(appId),
                CandidateUserId = candidateUserId,
                CandidateName = "Sanne de Vries",
                CandidateEmail = "sanne.devries@mail.nl",
                CandidateCity = "Den Haag",
                PreferredTransport = "Fiets",
                EstimatedTravelMinutes = 9,
                DistanceKm = 2.1,
                Status = ApplicationStatus.Accepted,
                CreatedAt = YesAt.AddDays(-2),
                RespondedAt = YesAt,
                EmailVerifiedAt = YesAt
            };
            db.Applications.Add(app);
            db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
            {
                Id = Guid.NewGuid(),
                ApplicationId = appId,
                Kind = ApplicationStatusEventKind.StatusChanged,
                FromStatus = ApplicationStatus.Pending,
                ToStatus = ApplicationStatus.Accepted,
                OccurredAtUtc = YesAt,
                ActorKind = ApplicationStatusActorKind.Employer
            });
        }
        else
        {
            app.CandidateUserId = candidateUserId;
        }

        var candidateUser = await db.Users.AsNoTracking()
            .Where(u => u.Id == candidateUserId)
            .Select(u => new { u.FullName, u.Email })
            .FirstOrDefaultAsync();
        if (candidateUser is not null)
        {
            if (!string.IsNullOrWhiteSpace(candidateUser.FullName)
                && appId != AfterMatchDemoIds.Verkoop4790App)
            {
                app.CandidateName = candidateUser.FullName;
            }

            if (!string.IsNullOrWhiteSpace(candidateUser.Email)
                && appId != AfterMatchDemoIds.Verkoop4790App)
            {
                app.CandidateEmail = candidateUser.Email;
            }
        }

        await ResetDemoInterviewGraphAsync(db, appId);

        var interview = await db.Interviews
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ApplicationId == appId);

        if (interview is null)
        {
            interview = new Interview
            {
                Id = Guid.NewGuid(),
                ApplicationId = appId,
                CalendarUid = $"demo-interview-{appId:N}@lobsy.nl",
                CreatedAtUtc = YesAt,
                UpdatedAtUtc = YesAt,
                Format = InterviewFormat.OnSite,
                LocationOrLink = "Loosduinseweg 120, Den Haag",
                WithWhom = "Linda de Wit, eigenaar",
                Note = "Je hoeft niets voor te bereiden. Kom gerust in je gewone kleren."
            };
            db.Interviews.Add(interview);
        }

        interview.Status = interviewStatus;
        interview.Format = InterviewFormat.OnSite;
        interview.LocationOrLink = "Loosduinseweg 120, Den Haag · ingang naast de winkel";
        interview.WithWhom = "Linda de Wit, eigenaar";
        interview.UpdatedAtUtc = DateTime.UtcNow;
        interview.ChosenSlotId = null;

        if (appId == AfterMatchDemoIds.Verkoop4790App)
        {
            app.CandidateName = string.Empty;
            app.CandidateEmail = string.Empty;
        }

        db.InterviewSlots.RemoveRange(interview.Slots);
        interview.Slots.Clear();

        var slotStarts = DemoSlotStartsUtc();
        var slot0 = slotStarts.Length > 0 ? slotStarts[0] : DateTime.UtcNow.AddDays(1);
        var slot1 = slotStarts.Length > 1 ? slotStarts[1] : slot0.AddDays(1);
        var slot2 = slotStarts.Length > 2 ? slotStarts[2] : slot1.AddDays(1);

        if (scenario is "slots" or "reschedule")
        {
            AddSlot(interview, slot0);
            AddSlot(interview, slot1);
            AddSlot(interview, slot2);
        }

        if (scenario == "scheduled")
        {
            var chosen = AddSlot(interview, slot0);
            chosen.IsSelected = true;
        }

        if (scenario == "scheduled4790")
        {
            var chosen = AddSlot(interview, slot1);
            chosen.IsSelected = true;
        }

        if (scenario == "reschedule")
        {
            var chosen = AddSlot(interview, slot0);
            chosen.IsSelected = true;
            var candidateSlot = AddSlot(
                interview,
                slot2,
                InterviewSlotProposer.Candidate,
                "zorg voor familie. \"Mijn zoon is ziek, ik kan donderdag niet weg.\"");
            interview.Status = InterviewStatus.RescheduleRequested;
            db.InterviewChanges.Add(new InterviewChange
            {
                Id = Guid.NewGuid(),
                InterviewId = interview.Id,
                Kind = InterviewChangeKind.Reschedule,
                ActorRole = "Candidate",
                Reason = candidateSlot.Reason!,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        if (outcome is AfterMatchOutcomeKind kind)
        {
            interview.Status = InterviewStatus.Done;
            var existing = await db.ApplicationOutcomes.FirstOrDefaultAsync(o => o.ApplicationId == appId);
            if (existing is null)
            {
                db.ApplicationOutcomes.Add(new ApplicationOutcome
                {
                    ApplicationId = appId,
                    Outcome = kind,
                    Reason = "Je was heel zorgvuldig en vriendelijk. We kozen nu voor iemand die al zelfstandig aan de oven kan staan.",
                    CreatedAtUtc = DateTime.UtcNow,
                    RecordedByUserId = BranchManagerId,
                    ToneCheckOk = true
                });
            }
        }

        if (hasMessage)
        {
            var existingMessages = await db.ApplicationMessages
                .Where(m => m.ApplicationId == appId)
                .OrderBy(m => m.CreatedAtUtc)
                .ToListAsync();
            if (existingMessages.Count > 1)
            {
                db.ApplicationMessages.RemoveRange(existingMessages.Skip(1));
            }

            if (existingMessages.Count == 0)
            {
                db.ApplicationMessages.Add(new ApplicationMessage
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = appId,
                    SenderUserId = BranchManagerId,
                    Body = "Hoi Sanne, leuk dat je wilt komen kennismaken! Ik stuur je vandaag nog een paar momenten.",
                    CreatedAtUtc = YesAt.AddHours(3),
                    BlockReason = ApplicationMessageBlockReason.None
                });
            }
        }
        else
        {
            db.ApplicationMessages.RemoveRange(
                await db.ApplicationMessages.Where(m => m.ApplicationId == appId).ToListAsync());
        }
    }

    private static async Task RemoveStaleRetailBranchApplicationsAsync(JobsyDbContext db, ILogger logger)
    {
        var keep = KeepApplicationIds.ToHashSet();
        var staleIds = await db.Applications.AsNoTracking()
            .Where(a => RetailBranchCompanyIds.Contains(a.Vacancy.CompanyId) && !keep.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync();
        if (staleIds.Count == 0)
        {
            return;
        }

        await AcceptatieApplicationDemoGraphRemover.RemoveApplicationsAsync(db, staleIds);
        logger.LogInformation(
            "Acceptatie employer demo reset: removed {Count} applications on retail branch companies (Den Haag Centrum inbox).",
            staleIds.Count);
    }

    private static async Task RemoveStaleCompanyApplicationsAsync(JobsyDbContext db, ILogger logger)
    {
        var keep = KeepApplicationIds.ToHashSet();
        var staleIds = await db.Applications.AsNoTracking()
            .Where(a => a.Vacancy.CompanyId == DemoCompanyId && !keep.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync();
        if (staleIds.Count == 0)
        {
            return;
        }

        await AcceptatieApplicationDemoGraphRemover.RemoveApplicationsAsync(db, staleIds);
        logger.LogInformation(
            "After-match demo reset: removed {Count} stale applications for demo company {CompanyId}.",
            staleIds.Count,
            DemoCompanyId);
    }

    private static async Task ResetDemoInterviewGraphAsync(JobsyDbContext db, Guid appId)
    {
        var interviewId = await db.Interviews.AsNoTracking()
            .Where(i => i.ApplicationId == appId)
            .Select(i => i.Id)
            .FirstOrDefaultAsync();
        if (interviewId == Guid.Empty)
        {
            return;
        }

        await db.InterviewChanges.Where(c => c.InterviewId == interviewId).ExecuteDeleteAsync();
        await db.InterviewSlots.Where(s => s.InterviewId == interviewId).ExecuteDeleteAsync();
        await db.Interviews.Where(i => i.Id == interviewId).ExecuteDeleteAsync();
    }

    private static InterviewSlot AddSlot(
        Interview interview,
        DateTime start,
        InterviewSlotProposer proposer = InterviewSlotProposer.Employer,
        string? reason = null)
    {
        var slot = new InterviewSlot
        {
            Id = Guid.NewGuid(),
            InterviewId = interview.Id,
            StartUtc = start,
            EndUtc = start.AddMinutes(30),
            ProposedBy = proposer,
            Reason = reason
        };
        interview.Slots.Add(slot);
        return slot;
    }
}
