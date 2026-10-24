using Jobsy.Core;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Infrastructure.Services.AfterMatch;

public sealed class AfterMatchService : IAfterMatchService
{
    private readonly JobsyDbContext _db;
    private readonly IApplicationStatusRecorder _statusRecorder;
    private readonly AfterMatchMailer _mailer;
    private readonly IConfiguration _configuration;

    public AfterMatchService(
        JobsyDbContext db,
        IApplicationStatusRecorder statusRecorder,
        AfterMatchMailer mailer,
        IConfiguration configuration)
    {
        _db = db;
        _statusRecorder = statusRecorder;
        _mailer = mailer;
        _configuration = configuration;
    }

    public Task<AfterMatchThreadDto?> GetThreadForCandidateAsync(
        Guid applicationId,
        Guid candidateUserId,
        CancellationToken cancellationToken = default) =>
        LoadThreadAsync(applicationId, candidateUserId, isEmployer: false, cancellationToken);

    public Task<AfterMatchThreadDto?> GetThreadForEmployerAsync(
        Guid applicationId,
        Guid employerUserId,
        CancellationToken cancellationToken = default) =>
        LoadThreadAsync(applicationId, employerUserId, isEmployer: true, cancellationToken);

    public async Task<AfterMatchThreadDto> EnsureThreadAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var application = await LoadApplicationAsync(applicationId, cancellationToken)
            ?? throw new InvalidOperationException("Application not found.");

        if (!AfterMatchInterviewRules.ApplicationEligible(application.Status))
        {
            throw new InvalidOperationException("Application not eligible for after-match.");
        }

        var interview = await EnsureInterviewEntityAsync(application, cancellationToken);
        return await MapThreadAsync(application, interview, viewerUserId: application.CandidateUserId ?? Guid.Empty, isEmployer: false, cancellationToken);
    }

    public async Task<AfterMatchThreadDto> ProposeEmployerSlotsAsync(
        Guid applicationId,
        Guid employerUserId,
        ProposeEmployerSlotsRequest request,
        CancellationToken cancellationToken = default)
    {
        var application = await RequireEmployerApplicationAsync(applicationId, employerUserId, cancellationToken);
        await EnsureInterviewEntityAsync(application, cancellationToken);
        var interview = await _db.Interviews
            .Include(i => i.Slots)
            .FirstAsync(i => i.ApplicationId == applicationId, cancellationToken);

        if (request.Slots.Count is < AfterMatchInterviewRules.MinEmployerSlots or > AfterMatchInterviewRules.MaxEmployerSlots)
        {
            throw new ArgumentException("Provide 2 or 3 time slots.");
        }

        foreach (var slot in request.Slots)
        {
            if (slot.EndUtc <= slot.StartUtc)
            {
                throw new ArgumentException("Invalid slot range.");
            }
        }

        interview.Format = request.Format;
        interview.LocationOrLink = request.LocationOrLink?.Trim();
        interview.WithWhom = request.WithWhom?.Trim();
        interview.Note = request.Note?.Trim();
        interview.Status = InterviewStatus.Proposing;
        interview.ChosenSlotId = null;
        interview.UpdatedAtUtc = DateTime.UtcNow;

        var existingSlots = await _db.InterviewSlots
            .Where(s => s.InterviewId == interview.Id)
            .ToListAsync(cancellationToken);
        if (existingSlots.Count > 0)
        {
            _db.InterviewSlots.RemoveRange(existingSlots);
            await _db.SaveChangesAsync(cancellationToken);
        }

        foreach (var slot in request.Slots)
        {
            _db.InterviewSlots.Add(new InterviewSlot
            {
                Id = Guid.NewGuid(),
                InterviewId = interview.Id,
                StartUtc = DateTime.SpecifyKind(slot.StartUtc, DateTimeKind.Utc),
                EndUtc = DateTime.SpecifyKind(slot.EndUtc, DateTimeKind.Utc),
                ProposedBy = InterviewSlotProposer.Employer
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MapThreadAsync(application, interview, employerUserId, isEmployer: true, cancellationToken);
    }

    public async Task<AfterMatchThreadDto> ProposeCandidateSlotAsync(
        Guid applicationId,
        Guid candidateUserId,
        ProposeCandidateSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        var application = await RequireCandidateApplicationAsync(applicationId, candidateUserId, cancellationToken);
        var interview = await EnsureInterviewEntityAsync(application, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Reason is required.");
        }

        if (request.EndUtc <= request.StartUtc)
        {
            throw new ArgumentException("Invalid slot range.");
        }

        interview.Status = InterviewStatus.RescheduleRequested;
        interview.UpdatedAtUtc = DateTime.UtcNow;
        _db.InterviewSlots.RemoveRange(interview.Slots.Where(s => !s.IsSelected));
        interview.Slots.Add(new InterviewSlot
        {
            Id = Guid.NewGuid(),
            InterviewId = interview.Id,
            StartUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc),
            EndUtc = DateTime.SpecifyKind(request.EndUtc, DateTimeKind.Utc),
            ProposedBy = InterviewSlotProposer.Candidate,
            Reason = request.Reason.Trim()
        });

        _db.InterviewChanges.Add(new InterviewChange
        {
            Id = Guid.NewGuid(),
            InterviewId = interview.Id,
            Kind = InterviewChangeKind.Reschedule,
            ActorRole = "Candidate",
            Reason = request.Reason.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return await MapThreadAsync(application, interview, candidateUserId, isEmployer: false, cancellationToken);
    }

    public async Task<AfterMatchThreadDto> SelectSlotAsync(
        Guid applicationId,
        Guid userId,
        bool isEmployer,
        SelectSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        var application = isEmployer
            ? await RequireEmployerApplicationAsync(applicationId, userId, cancellationToken)
            : await RequireCandidateApplicationAsync(applicationId, userId, cancellationToken);

        var interview = await EnsureInterviewEntityAsync(application, cancellationToken);
        var slot = interview.Slots.FirstOrDefault(s => s.Id == request.SlotId)
            ?? throw new InvalidOperationException("Slot not found.");

        foreach (var s in interview.Slots)
        {
            s.IsSelected = s.Id == slot.Id;
        }

        interview.ChosenSlotId = slot.Id;
        interview.Status = InterviewStatus.Scheduled;
        interview.UpdatedAtUtc = DateTime.UtcNow;

        await ScheduleRemindersAsync(interview, application, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        await _mailer.SendScheduledConfirmationAsync(
            interview,
            application,
            application.CandidateEmail,
            application.Vacancy.Company.ContactEmail ?? "",
            cancellationToken);

        return await MapThreadAsync(application, interview, userId, isEmployer, cancellationToken);
    }

    public async Task<AfterMatchThreadDto> RescheduleAsync(
        Guid applicationId,
        Guid userId,
        bool isEmployer,
        RescheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Reason is required.");
        }

        var application = isEmployer
            ? await RequireEmployerApplicationAsync(applicationId, userId, cancellationToken)
            : await RequireCandidateApplicationAsync(applicationId, userId, cancellationToken);

        var interview = await EnsureInterviewEntityAsync(application, cancellationToken);
        interview.Status = InterviewStatus.RescheduleRequested;
        interview.ChosenSlotId = null;
        interview.UpdatedAtUtc = DateTime.UtcNow;

        _db.InterviewChanges.Add(new InterviewChange
        {
            Id = Guid.NewGuid(),
            InterviewId = interview.Id,
            Kind = InterviewChangeKind.Reschedule,
            ActorRole = isEmployer ? "Employer" : "Candidate",
            Reason = request.Reason.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return await MapThreadAsync(application, interview, userId, isEmployer, cancellationToken);
    }

    public async Task<AfterMatchThreadDto> CancelInterviewAsync(
        Guid applicationId,
        Guid userId,
        bool isEmployer,
        RescheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Reason is required.");
        }

        var application = isEmployer
            ? await RequireEmployerApplicationAsync(applicationId, userId, cancellationToken)
            : await RequireCandidateApplicationAsync(applicationId, userId, cancellationToken);

        var interview = await EnsureInterviewEntityAsync(application, cancellationToken);
        interview.Status = InterviewStatus.Cancelled;
        interview.UpdatedAtUtc = DateTime.UtcNow;

        _db.InterviewChanges.Add(new InterviewChange
        {
            Id = Guid.NewGuid(),
            InterviewId = interview.Id,
            Kind = InterviewChangeKind.Cancel,
            ActorRole = isEmployer ? "Employer" : "Candidate",
            Reason = request.Reason.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return await MapThreadAsync(application, interview, userId, isEmployer, cancellationToken);
    }

    public async Task<SensitiveMessagePreview> PreviewMessageAsync(
        Guid applicationId,
        Guid senderUserId,
        bool senderIsEmployer,
        string body,
        CancellationToken cancellationToken = default)
    {
        var application = senderIsEmployer
            ? await RequireEmployerApplicationAsync(applicationId, senderUserId, cancellationToken)
            : await RequireCandidateApplicationAsync(applicationId, senderUserId, cancellationToken);

        var interview = await EnsureInterviewEntityAsync(application, cancellationToken);
        var outcome = await _db.ApplicationOutcomes.AsNoTracking()
            .FirstOrDefaultAsync(o => o.ApplicationId == applicationId, cancellationToken);
        var share = AfterMatchContactShareState.From(interview.Status, outcome?.Outcome);
        var kind = SensitiveMessageContentRules.Detect(body);
        var block = senderIsEmployer
            ? SensitiveMessageContentRules.BlockReasonForEmployer(kind, share)
            : ApplicationMessageBlockReason.None;

        return new SensitiveMessagePreview(
            kind,
            WouldBlock: block != ApplicationMessageBlockReason.None,
            CanConfirmSend: !senderIsEmployer && SensitiveMessageContentRules.CandidateMaySendDespiteWarning(kind));
    }

    public async Task<AfterMatchThreadDto> SendMessageAsync(
        Guid applicationId,
        Guid senderUserId,
        bool senderIsEmployer,
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var preview = await PreviewMessageAsync(applicationId, senderUserId, senderIsEmployer, request.Body, cancellationToken);
        if (preview.WouldBlock)
        {
            throw new InvalidOperationException("Message blocked for safety.");
        }

        if (preview.Kind != SensitiveMessageContentRules.SensitiveKind.None
            && !request.ConfirmSensitive
            && !senderIsEmployer)
        {
            throw new InvalidOperationException("Confirm sensitive content first.");
        }

        var application = senderIsEmployer
            ? await RequireEmployerApplicationAsync(applicationId, senderUserId, cancellationToken)
            : await RequireCandidateApplicationAsync(applicationId, senderUserId, cancellationToken);

        var interview = await EnsureInterviewEntityAsync(application, cancellationToken);
        var message = new ApplicationMessage
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            SenderUserId = senderUserId,
            Body = request.Body.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            SentDespiteWarning = request.ConfirmSensitive && preview.Kind != SensitiveMessageContentRules.SensitiveKind.None
        };

        _db.ApplicationMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapThreadAsync(application, interview, senderUserId, senderIsEmployer, cancellationToken);
    }

    public async Task MarkCandidateMessagesReadAsync(
        Guid applicationId,
        Guid candidateUserId,
        CancellationToken cancellationToken = default)
    {
        _ = await RequireCandidateApplicationAsync(applicationId, candidateUserId, cancellationToken);
        var now = DateTime.UtcNow;
        var unread = await _db.ApplicationMessages
            .Where(m => m.ApplicationId == applicationId
                        && m.SenderUserId != candidateUserId
                        && m.ReadAtUtc == null)
            .ToListAsync(cancellationToken);
        if (unread.Count == 0)
        {
            return;
        }

        foreach (var message in unread)
        {
            message.ReadAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AfterMatchThreadDto> RecordOutcomeAsync(
        Guid applicationId,
        Guid employerUserId,
        RecordOutcomeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!AfterMatchOutcomeToneRules.IsAcceptable(request.Reason))
        {
            throw new ArgumentException("Reason tone or length not acceptable.");
        }

        var application = await RequireEmployerApplicationAsync(applicationId, employerUserId, cancellationToken);
        var interview = await EnsureInterviewEntityAsync(application, cancellationToken);

        if (interview.Status != InterviewStatus.Scheduled && interview.Status != InterviewStatus.Done)
        {
            interview.Status = InterviewStatus.Done;
        }
        else if (interview.Status == InterviewStatus.Scheduled)
        {
            interview.Status = InterviewStatus.Done;
        }

        interview.UpdatedAtUtc = DateTime.UtcNow;

        var outcome = await _db.ApplicationOutcomes.FirstOrDefaultAsync(o => o.ApplicationId == applicationId, cancellationToken);
        if (outcome is null)
        {
            outcome = new ApplicationOutcome { ApplicationId = applicationId };
            _db.ApplicationOutcomes.Add(outcome);
        }

        outcome.Outcome = request.Outcome;
        outcome.Reason = request.Reason.Trim();
        outcome.ToneCheckOk = true;
        outcome.CreatedAtUtc = DateTime.UtcNow;
        outcome.RecordedByUserId = employerUserId;

        var newStatus = request.Outcome switch
        {
            AfterMatchOutcomeKind.Offer => ApplicationStatus.Hired,
            AfterMatchOutcomeKind.NotNow => ApplicationStatus.Rejected,
            _ => ApplicationStatus.EmployerContacting
        };

        _statusRecorder.SetStatus(
            application,
            newStatus,
            ApplicationStatusActorKind.Employer,
            employerUserId,
            DateTime.UtcNow);

        await _db.SaveChangesAsync(cancellationToken);
        return await MapThreadAsync(application, interview, employerUserId, isEmployer: true, cancellationToken);
    }

    public async Task<AfterMatchReminderPreferenceDto> UpdateReminderPreferenceAsync(
        Guid candidateUserId,
        UpdateReminderPreferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.AfterMatchReminderPreferences.FirstOrDefaultAsync(p => p.UserId == candidateUserId, cancellationToken);
        if (row is null)
        {
            row = new AfterMatchReminderPreference { UserId = candidateUserId };
            _db.AfterMatchReminderPreferences.Add(row);
        }

        row.WeekBefore = request.WeekBefore;
        row.DayBefore18 = request.DayBefore18;
        row.TwoHoursBefore = request.TwoHoursBefore;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return new AfterMatchReminderPreferenceDto(row.WeekBefore, row.DayBefore18, row.TwoHoursBefore);
    }

    public async Task<byte[]> BuildCalendarInviteAsync(
        Guid applicationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var application = await LoadApplicationAsync(applicationId, cancellationToken)
            ?? throw new InvalidOperationException("Application not found.");

        var interview = await _db.Interviews
            .Include(i => i.ChosenSlot)
            .FirstAsync(i => i.ApplicationId == applicationId, cancellationToken);

        var webBase = JobsyPublicUrl.NormalizeOrigin(
            _configuration["PublicWebBaseUrl"] ?? "http://localhost:5201");
        var reschedule = $"{webBase}/candidate/applications/{applicationId}/after-match?panel=reschedule";

        return InterviewCalendarBuilder.BuildIcs(
            interview,
            "gesprek@lobsy.nl",
            application.CandidateEmail,
            reschedule,
            $"{application.Vacancy.Title} · {application.Vacancy.Company.Name}",
            interview.Note ?? "Kennismakingsgesprek via Lobsy");
    }

    private async Task<AfterMatchThreadDto?> LoadThreadAsync(
        Guid applicationId,
        Guid userId,
        bool isEmployer,
        CancellationToken cancellationToken)
    {
        Application? application;
        if (isEmployer)
        {
            try
            {
                application = await RequireEmployerApplicationAsync(applicationId, userId, cancellationToken);
            }
            catch
            {
                return null;
            }
        }
        else
        {
            try
            {
                application = await RequireCandidateApplicationAsync(applicationId, userId, cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        if (!AfterMatchInterviewRules.ApplicationEligible(application.Status))
        {
            return null;
        }

        var interview = await EnsureInterviewEntityAsync(application, cancellationToken);
        return await MapThreadAsync(application, interview, userId, isEmployer, cancellationToken);
    }

    private async Task<Interview> EnsureInterviewEntityAsync(Application application, CancellationToken cancellationToken)
    {
        var interview = await _db.Interviews
            .Include(i => i.Slots)
            .Include(i => i.ChosenSlot)
            .FirstOrDefaultAsync(i => i.ApplicationId == application.Id, cancellationToken);

        if (interview is not null)
        {
            return interview;
        }

        var now = DateTime.UtcNow;
        interview = new Interview
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            CalendarUid = $"interview-{Guid.NewGuid():N}@lobsy.nl",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Status = InterviewStatus.Proposing
        };
        _db.Interviews.Add(interview);
        await _db.SaveChangesAsync(cancellationToken);
        return interview;
    }

    private async Task ScheduleRemindersAsync(
        Interview interview,
        Application application,
        CancellationToken cancellationToken)
    {
        if (interview.ChosenSlot is null || application.CandidateUserId is null)
        {
            return;
        }

        var prefs = await _db.AfterMatchReminderPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == application.CandidateUserId.Value, cancellationToken)
            ?? new AfterMatchReminderPreference { DayBefore18 = true, TwoHoursBefore = true };

        _db.InterviewReminders.RemoveRange(
            await _db.InterviewReminders.Where(r => r.InterviewId == interview.Id).ToListAsync(cancellationToken));

        var start = interview.ChosenSlot.StartUtc;
        AddReminder(interview.Id, application.CandidateUserId.Value, InterviewReminderMoment.DayBefore18, start.AddDays(-1).Date.AddHours(18), prefs.DayBefore18);
        AddReminder(interview.Id, application.CandidateUserId.Value, InterviewReminderMoment.TwoHoursBefore, start.AddHours(-2), prefs.TwoHoursBefore);
        if (prefs.WeekBefore)
        {
            AddReminder(interview.Id, application.CandidateUserId.Value, InterviewReminderMoment.WeekBefore, start.AddDays(-7), true);
        }
    }

    private void AddReminder(Guid interviewId, Guid userId, InterviewReminderMoment moment, DateTime when, bool enabled)
    {
        if (!enabled)
        {
            return;
        }

        _db.InterviewReminders.Add(new InterviewReminder
        {
            Id = Guid.NewGuid(),
            InterviewId = interviewId,
            UserId = userId,
            Moment = moment,
            Channel = InterviewReminderChannel.Email,
            ScheduledForUtc = DateTime.SpecifyKind(when, DateTimeKind.Utc)
        });
    }

    private async Task<AfterMatchThreadDto> MapThreadAsync(
        Application application,
        Interview interview,
        Guid viewerUserId,
        bool isEmployer,
        CancellationToken cancellationToken)
    {
        var messages = await _db.ApplicationMessages.AsNoTracking()
            .Where(m => m.ApplicationId == application.Id)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var outcome = await _db.ApplicationOutcomes.AsNoTracking()
            .FirstOrDefaultAsync(o => o.ApplicationId == application.Id, cancellationToken);

        var share = AfterMatchContactShareState.From(interview.Status, outcome?.Outcome);
        var prefs = application.CandidateUserId is Guid cid
            ? await _db.AfterMatchReminderPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == cid, cancellationToken)
            : null;

        var changes = await _db.InterviewChanges.AsNoTracking()
            .Where(c => c.InterviewId == interview.Id)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(5)
            .ToListAsync(cancellationToken);

        var similar = outcome?.Outcome == AfterMatchOutcomeKind.NotNow
            ? await LoadSimilarVacanciesAsync(application, cancellationToken)
            : [];

        var acceptedAt = await _db.ApplicationStatusHistories.AsNoTracking()
            .Where(h => h.ApplicationId == application.Id && h.ToStatus == ApplicationStatus.Accepted)
            .OrderBy(h => h.OccurredAtUtc)
            .Select(h => (DateTime?)h.OccurredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var travel = application.EstimatedTravelMinutes > 0
            ? $"{application.EstimatedTravelMinutes} min fietsen"
            : null;

        return new AfterMatchThreadDto(
            application.Id,
            interview.Id,
            interview.Status,
            share,
            outcome?.Outcome,
            application.Vacancy.Title,
            application.Vacancy.Company.Name,
            travel,
            interview.Format,
            interview.LocationOrLink,
            interview.WithWhom,
            interview.Note,
            interview.ChosenSlot?.StartUtc,
            interview.ChosenSlot?.EndUtc,
            interview.Slots.Where(s => !s.IsSelected).Select(s => new AfterMatchSlotDto(
                s.Id, s.StartUtc, s.EndUtc, s.ProposedBy, s.Reason, s.IsSelected)).ToList(),
            messages.Select(m => new AfterMatchMessageDto(
                m.Id,
                m.SenderUserId == viewerUserId,
                m.SenderUserId == viewerUserId ? "Jij" : (isEmployer ? application.CandidateName : application.Vacancy.Company.Name),
                m.Body,
                m.CreatedAtUtc,
                m.ReadAtUtc,
                m.BlockReason,
                m.SentDespiteWarning)).ToList(),
            new AfterMatchReminderPreferenceDto(
                prefs?.WeekBefore ?? false,
                prefs?.DayBefore18 ?? true,
                prefs?.TwoHoursBefore ?? true),
            changes.Select(c => new AfterMatchChangeDto(c.Kind, c.ActorRole, c.Reason, c.CreatedAtUtc)).ToList(),
            similar,
            application.CandidateName,
            application.CandidateEmail,
            acceptedAt);
    }

    private async Task<IReadOnlyList<AfterMatchSimilarVacancyDto>> LoadSimilarVacanciesAsync(
        Application application,
        CancellationToken cancellationToken)
    {
        var rows = await _db.Vacancies.AsNoTracking()
            .Where(v => v.Id != application.VacancyId && v.PublishedAtUtc != null && v.CompanyId == application.Vacancy.CompanyId)
            .OrderByDescending(v => v.PublishedAtUtc)
            .Take(3)
            .Select(v => new AfterMatchSimilarVacancyDto(v.Id, v.Title, v.Company.Name, ""))
            .ToListAsync(cancellationToken);

        return rows;
    }

    private async Task<Application?> LoadApplicationAsync(Guid applicationId, CancellationToken cancellationToken) =>
        await _db.Applications
            .Include(a => a.Vacancy).ThenInclude(v => v.Company)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

    private async Task<Application> RequireCandidateApplicationAsync(
        Guid applicationId,
        Guid candidateUserId,
        CancellationToken cancellationToken)
    {
        var application = await LoadApplicationAsync(applicationId, cancellationToken)
            ?? throw new InvalidOperationException("Application not found.");

        if (application.CandidateUserId != candidateUserId)
        {
            throw new UnauthorizedAccessException();
        }

        return application;
    }

    private async Task<Application> RequireEmployerApplicationAsync(
        Guid applicationId,
        Guid employerUserId,
        CancellationToken cancellationToken)
    {
        var application = await LoadApplicationAsync(applicationId, cancellationToken)
            ?? throw new InvalidOperationException("Application not found.");

        var companyId = application.Vacancy.CompanyId;
        var allowed = await _db.UserCompanies.AsNoTracking()
            .AnyAsync(uc => uc.UserId == employerUserId && uc.CompanyId == companyId, cancellationToken);

        if (!allowed)
        {
            throw new UnauthorizedAccessException();
        }

        return application;
    }
}
