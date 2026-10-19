using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Removes or anonymises rows owned by a candidate user. Used by profile reset and account deletion.
/// Does not call <see cref="DbContext.SaveChangesAsync"/> — caller owns the unit of work.
/// </summary>
internal static class CandidateOwnedDataCleanup
{
    internal static async Task RemoveOwnedDataAsync(
        JobsyDbContext db,
        User user,
        CancellationToken cancellationToken)
    {
        var originalEmail = user.Email;
        var anonymizedEmail = $"deleted-{user.Id:N}@anonymized.jobsy.local";

        await db.Entry(user).Collection(u => u.CompanyMemberships).LoadAsync(cancellationToken);

        var applications = await db.Applications
            .Where(a => a.CandidateUserId == user.Id || a.CandidateEmail == originalEmail)
            .ToListAsync(cancellationToken);

        var draftAppIds = applications
            .Where(a => a.EmailVerifiedAt is null)
            .Select(a => a.Id)
            .ToList();

        if (draftAppIds.Count > 0)
        {
            var draftPlacements = await db.ApplicationPlacements
                .Where(p => draftAppIds.Contains(p.ApplicationId))
                .ToListAsync(cancellationToken);
            if (draftPlacements.Count > 0)
            {
                db.ApplicationPlacements.RemoveRange(draftPlacements);
            }

            var draftHours = await db.MaqqieHoursWeeks
                .Where(h => draftAppIds.Contains(h.ApplicationId))
                .ToListAsync(cancellationToken);
            if (draftHours.Count > 0)
            {
                db.MaqqieHoursWeeks.RemoveRange(draftHours);
            }

            var draftHistories = await db.ApplicationStatusHistories
                .Where(h => draftAppIds.Contains(h.ApplicationId))
                .ToListAsync(cancellationToken);
            if (draftHistories.Count > 0)
            {
                db.ApplicationStatusHistories.RemoveRange(draftHistories);
            }

            var draftCvs = await db.ApplicationUploadedCvs
                .Where(c => draftAppIds.Contains(c.ApplicationId))
                .ToListAsync(cancellationToken);
            if (draftCvs.Count > 0)
            {
                db.ApplicationUploadedCvs.RemoveRange(draftCvs);
            }

            var drafts = applications.Where(a => draftAppIds.Contains(a.Id)).ToList();
            db.Applications.RemoveRange(drafts);
            applications = applications.Where(a => a.EmailVerifiedAt is not null).ToList();
        }

        foreach (var app in applications)
        {
            app.CandidateName = "Verwijderde gebruiker";
            app.CandidateEmail = anonymizedEmail;
            app.CandidateCity = null;
            app.CandidateAddress = null;
            app.PreferencesSummary = null;
            app.CandidateUserId = null;
            app.DistanceKm = null;
            app.SnapshotAvailabilityJson = null;
            app.SnapshotDrivingLicenses = null;
            app.SnapshotEducations = null;
            app.SnapshotAboutMe = null;
            app.SnapshotWhoAmIJson = null;
            app.SnapshotPhoneNumber = null;
            app.SnapshotWhatsAppAllowed = false;
            app.SnapshotHomeLatitude = null;
            app.SnapshotHomeLongitude = null;
            app.SnapshotCertificatesJson = null;
            app.SnapshotDiplomaEvaluationsJson = null;
            app.SnapshotShowAddressOnCv = false;
            app.SnapshotDateOfBirth = null;
            app.Motivation = null;
            app.StudentNumber = null;
            app.SchoolEmail = null;
            app.StudyProgram = null;
            app.StudyYear = null;
            app.ExclusivityValidationStatus = null;
            app.CandidateEmployerCount = 0;
            app.CandidateReferenceCount = 0;
            app.HasUploadedCv = false;
            app.CandidateAgeYears = null;
            app.WorkPermitConfirmed = false;
            app.MatchPercent = null;
            app.MatchBreakdownJson = null;
            app.EmailVerificationCode = null;
            app.EmailVerificationExpiresAt = null;
            app.EmailVerificationFailedAttempts = 0;
        }

        var verifiedAppIds = applications.Select(a => a.Id).ToList();
        if (verifiedAppIds.Count > 0)
        {
            var applicationCvs = await db.ApplicationUploadedCvs
                .Where(c => verifiedAppIds.Contains(c.ApplicationId))
                .ToListAsync(cancellationToken);
            if (applicationCvs.Count > 0)
            {
                db.ApplicationUploadedCvs.RemoveRange(applicationCvs);
            }
        }

        await RemoveRangeAsync(
            db,
            db.ApplicationMessages.Where(m => m.SenderUserId == user.Id),
            cancellationToken);

        var dismissedSuggestions = await db.DismissedVestigingSuggestions
            .Where(d => d.DismissedByUserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var dismissed in dismissedSuggestions)
        {
            dismissed.DismissedByUserId = null;
        }

        var contentReports = await db.ContentReports
            .Where(r => r.ReporterUserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var report in contentReports)
        {
            report.ReporterUserId = null;
            report.ReporterEmail = null;
        }

        var supportGrants = await db.SupportAccessGrants
            .Where(g => g.SubjectUserId == user.Id)
            .ToListAsync(cancellationToken);
        if (supportGrants.Count > 0)
        {
            db.SupportAccessGrants.RemoveRange(supportGrants);
        }

        await RemoveRangeAsync(db, db.VacancyLikes.Where(l => l.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.VacancyShares.Where(s => s.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.VacancyClicks.Where(c => c.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.VacancySearchImpressions.Where(i => i.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.SiteVisits.Where(v => v.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.UserNotifications.Where(n => n.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateActionTokens.Where(t => t.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.OneTimeLinks.Where(l => l.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.PhoneVerificationChallenges.Where(c => c.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.PassportPartnerCandidateLinks.Where(l => l.CandidateUserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.PassportAccessLogs.Where(l => l.CandidateUserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.WebPushSubscriptions.Where(s => s.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.UserDeviceSessions.Where(s => s.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.MfaTrustedDevices.Where(d => d.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.DeviceLoginHandoffs.Where(h => h.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateKandidatenkaartSettings.Where(s => s.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.KandidatenkaartInviteDeliveries.Where(d => d.CandidateUserId == user.Id), cancellationToken);

        var viewedLogs = await db.PassportAccessLogs
            .Where(l => l.ViewerUserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var row in viewedLogs)
        {
            row.ViewerUserId = null;
        }

        await RemoveRangeAsync(db, db.CandidateUploadedCvs.Where(c => c.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateDiplomaEvaluations.Where(e => e.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.ReferenceMisuseReports.Where(r => r.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.ReferenceConfirmationConsentLogs.Where(r => r.UserId == user.Id), cancellationToken);

        var confirmations = await db.ReferenceConfirmations
            .Where(r => r.UserId == user.Id)
            .ToListAsync(cancellationToken);
        if (confirmations.Count > 0)
        {
            var confirmationIds = confirmations.Select(c => c.Id).ToList();
            var tokens = await db.ReferenceConfirmationTokens
                .Where(t => confirmationIds.Contains(t.ReferenceConfirmationId))
                .ToListAsync(cancellationToken);
            if (tokens.Count > 0)
            {
                db.ReferenceConfirmationTokens.RemoveRange(tokens);
            }

            db.ReferenceConfirmations.RemoveRange(confirmations);
        }

        var workExpRefs = await db.WorkExperienceReferenceRequests
            .Where(r => r.UserId == user.Id)
            .ToListAsync(cancellationToken);
        if (workExpRefs.Count > 0)
        {
            var refIds = workExpRefs.Select(r => r.Id).ToList();
            var refTokens = await db.WorkExperienceReferenceTokens
                .Where(t => refIds.Contains(t.RequestId))
                .ToListAsync(cancellationToken);
            if (refTokens.Count > 0)
            {
                db.WorkExperienceReferenceTokens.RemoveRange(refTokens);
            }

            db.WorkExperienceReferenceRequests.RemoveRange(workExpRefs);
        }

        await RemoveRangeAsync(db, db.CandidateReferences.Where(r => r.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateCompetencies.Where(c => c.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateCulturePersonalityProfiles.Where(c => c.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateValuesProfiles.Where(v => v.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateWhoAmIProfiles.Where(c => c.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateTestReflections.Where(r => r.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidatePrivatePreferences.Where(p => p.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateCareerInterests.Where(c => c.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateCareerStepProgress.Where(p => p.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateCareerGenerations.Where(g => g.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateCareerPlans.Where(p => p.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateOnboardings.Where(o => o.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateProactiveMatchingSettings.Where(s => s.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.ProactiveMatchSuggestions.Where(s => s.CandidateUserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateRoleFitChecks.Where(r => r.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateVacancyCultureFits.Where(r => r.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateMatchSnapshots.Where(r => r.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateDeepAnalyses.Where(d => d.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateAssessmentAdjustments.Where(a => a.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.CandidateAssessmentAttempts.Where(a => a.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(db, db.DeepAnalysisCheckouts.Where(c => c.UserId == user.Id), cancellationToken);

        var researchAttempts = await db.AssessmentResearchAttempts
            .Where(a => a.UserId == user.Id)
            .ToListAsync(cancellationToken);
        if (researchAttempts.Count > 0)
        {
            db.AssessmentResearchAttempts.RemoveRange(researchAttempts);
        }

        var externalVacancies = await db.CandidateExternalVacancies
            .Where(v => v.CandidateUserId == user.Id)
            .ToListAsync(cancellationToken);
        if (externalVacancies.Count > 0)
        {
            var externalIds = externalVacancies.Select(v => v.Id).ToList();
            var outbounds = await db.CandidateExternalVacancyOutbounds
                .Where(o => externalIds.Contains(o.ExternalVacancyId))
                .ToListAsync(cancellationToken);
            if (outbounds.Count > 0)
            {
                db.CandidateExternalVacancyOutbounds.RemoveRange(outbounds);
            }

            db.CandidateExternalVacancies.RemoveRange(externalVacancies);
        }

        await RemoveRangeAsync(db, db.TalentContactRequests.Where(r => r.CandidateUserId == user.Id), cancellationToken);

        var trainingClicks = await db.TrainingClicks
            .Where(c => c.UserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var click in trainingClicks)
        {
            click.UserId = null;
        }

        await RemoveRangeAsync(
            db,
            db.TrainingEnrollments.Where(e => e.UserId == user.Id),
            cancellationToken);

        var initiativeIds = await db.WorkInitiatives
            .Where(i => i.CandidateUserId == user.Id)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);
        if (initiativeIds.Count > 0)
        {
            await RemoveRangeAsync(
                db,
                db.WorkInitiativeAcceptances.Where(a => initiativeIds.Contains(a.InitiativeId)),
                cancellationToken);
            await RemoveRangeAsync(
                db,
                db.WorkInitiatives.Where(i => i.CandidateUserId == user.Id),
                cancellationToken);
        }

        await RemoveRangeAsync(
            db,
            db.WorkInitiativeDrafts.Where(d => d.CandidateUserId == user.Id),
            cancellationToken);

        var consumerInvoices = await db.ConsumerPurchaseInvoices
            .Where(i => i.UserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var invoice in consumerInvoices)
        {
            invoice.UserId = null;
        }

        var feedbackRows = await db.PlatformFeedbacks
            .Where(f => f.UserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var item in feedbackRows)
        {
            item.UserId = null;
            item.UserDisplayName = "Verwijderde gebruiker";
            item.UserRole = null;
            item.Description = PrivacyConstants.ForgottenFeedbackDescription;
            item.GeneratedPrompt = null;
            item.PageUrl = FeedbackPromptFormatter.NormalizePageUrl(item.PageUrl);
            item.ScreenshotBytes = null;
            item.ScreenshotContentType = null;
            item.BrowserInfo = null;
            item.DeviceInfo = null;
        }

        var reminderPreference = await db.CandidateReminderPreferences
            .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
        if (reminderPreference is not null)
        {
            db.CandidateReminderPreferences.Remove(reminderPreference);
        }

        var afterMatchReminder = await db.AfterMatchReminderPreferences
            .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
        if (afterMatchReminder is not null)
        {
            db.AfterMatchReminderPreferences.Remove(afterMatchReminder);
        }

        db.InterviewReminders.RemoveRange(
            await db.InterviewReminders.Where(r => r.UserId == user.Id).ToListAsync(cancellationToken));

        var reminderLogs = await db.ComebackReminderLogs
            .Where(l => l.UserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var reminderLog in reminderLogs)
        {
            reminderLog.UserId = null;
        }

        var accessLogs = await db.PersonalDataAccessLogs
            .Where(l => l.SubjectUserId == user.Id || l.ActorUserId == user.Id)
            .ToListAsync(cancellationToken);
        if (accessLogs.Count > 0)
        {
            db.PersonalDataAccessLogs.RemoveRange(accessLogs);
        }
    }

    private static async Task RemoveRangeAsync<TEntity>(
        JobsyDbContext db,
        IQueryable<TEntity> query,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var rows = await query.ToListAsync(cancellationToken);
        if (rows.Count > 0)
        {
            db.Set<TEntity>().RemoveRange(rows);
        }
    }
}
