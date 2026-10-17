using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateProfileResetService : ICandidateProfileResetService
{
    /// <summary>Candidate onboarding re-entry after reset (new intro / wizard).</summary>
    public const string RedirectPathAfterReset = "/candidate/start";

    private readonly JobsyDbContext _db;
    private readonly IUserLookupService _users;

    public CandidateProfileResetService(JobsyDbContext db, IUserLookupService users)
    {
        _db = db;
        _users = users;
    }

    public async Task<ResetCandidateProfileResponse> ResetAsync(
        ClaimsPrincipal principal,
        string confirmPhrase,
        string? uiLanguage,
        CancellationToken cancellationToken = default)
    {
        if (!CandidateProfileResetConfirm.Matches(uiLanguage, confirmPhrase))
        {
            throw new ArgumentException("Bevestig met het exacte woord zoals in de dialoog.");
        }

        var user = await _users.FindByPrincipalAsync(principal, cancellationToken)
                   ?? throw new UnauthorizedAccessException("Gebruiker niet gevonden.");

        if (user.Role != UserRole.Candidate || !user.IsActive)
        {
            throw new InvalidOperationException("Alleen actieve kandidaten kunnen hun profiel resetten.");
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var useTransaction = _db.Database.IsRelational()
                                 && !string.Equals(
                                     _db.Database.ProviderName,
                                     "Microsoft.EntityFrameworkCore.InMemory",
                                     StringComparison.Ordinal);

            if (useTransaction)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
                await ResetCandidateDataCoreAsync(user, cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            else
            {
                await ResetCandidateDataCoreAsync(user, cancellationToken);
            }

            return new ResetCandidateProfileResponse(
                "Je profiel is gewist. Je kunt opnieuw beginnen.",
                RedirectPathAfterReset);
        });
    }

    internal async Task ResetCandidateDataCoreAsync(User user, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var originalEmail = user.Email;
        var anonymizedEmail = $"deleted-{user.Id:N}@anonymized.jobsy.local";
        var hadResearchConsent = user.ResearchConsentAt is not null;

        await _db.Entry(user).Collection(u => u.CompanyMemberships).LoadAsync(cancellationToken);

        var applications = await _db.Applications
            .Where(a => a.CandidateUserId == user.Id || a.CandidateEmail == originalEmail)
            .ToListAsync(cancellationToken);

        var draftAppIds = applications
            .Where(a => a.EmailVerifiedAt is null)
            .Select(a => a.Id)
            .ToList();

        if (draftAppIds.Count > 0)
        {
            var draftHistories = await _db.ApplicationStatusHistories
                .Where(h => draftAppIds.Contains(h.ApplicationId))
                .ToListAsync(cancellationToken);
            if (draftHistories.Count > 0)
            {
                _db.ApplicationStatusHistories.RemoveRange(draftHistories);
            }

            var draftCvs = await _db.ApplicationUploadedCvs
                .Where(c => draftAppIds.Contains(c.ApplicationId))
                .ToListAsync(cancellationToken);
            if (draftCvs.Count > 0)
            {
                _db.ApplicationUploadedCvs.RemoveRange(draftCvs);
            }

            var drafts = applications.Where(a => draftAppIds.Contains(a.Id)).ToList();
            _db.Applications.RemoveRange(drafts);
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
            var applicationCvs = await _db.ApplicationUploadedCvs
                .Where(c => verifiedAppIds.Contains(c.ApplicationId))
                .ToListAsync(cancellationToken);
            if (applicationCvs.Count > 0)
            {
                _db.ApplicationUploadedCvs.RemoveRange(applicationCvs);
            }
        }

        await RemoveRangeAsync(_db.VacancyLikes.Where(l => l.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.VacancyShares.Where(s => s.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.VacancyClicks.Where(c => c.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.VacancySearchImpressions.Where(i => i.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.SiteVisits.Where(v => v.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.UserNotifications.Where(n => n.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateActionTokens.Where(t => t.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.OneTimeLinks.Where(l => l.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.PhoneVerificationChallenges.Where(c => c.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.PassportPartnerCandidateLinks.Where(l => l.CandidateUserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.PassportAccessLogs.Where(l => l.CandidateUserId == user.Id), cancellationToken);

        var viewedLogs = await _db.PassportAccessLogs
            .Where(l => l.ViewerUserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var row in viewedLogs)
        {
            row.ViewerUserId = null;
        }

        await RemoveRangeAsync(_db.CandidateUploadedCvs.Where(c => c.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateDiplomaEvaluations.Where(e => e.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.ReferenceMisuseReports.Where(r => r.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.ReferenceConfirmationConsentLogs.Where(r => r.UserId == user.Id), cancellationToken);

        var confirmations = await _db.ReferenceConfirmations
            .Where(r => r.UserId == user.Id)
            .ToListAsync(cancellationToken);
        if (confirmations.Count > 0)
        {
            var confirmationIds = confirmations.Select(c => c.Id).ToList();
            var tokens = await _db.ReferenceConfirmationTokens
                .Where(t => confirmationIds.Contains(t.ReferenceConfirmationId))
                .ToListAsync(cancellationToken);
            if (tokens.Count > 0)
            {
                _db.ReferenceConfirmationTokens.RemoveRange(tokens);
            }

            _db.ReferenceConfirmations.RemoveRange(confirmations);
        }

        await RemoveRangeAsync(_db.CandidateReferences.Where(r => r.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateCompetencies.Where(c => c.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateCulturePersonalityProfiles.Where(c => c.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateValuesProfiles.Where(v => v.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateWhoAmIProfiles.Where(c => c.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidatePrivatePreferences.Where(p => p.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateCareerInterests.Where(c => c.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateCareerStepProgress.Where(p => p.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateCareerGenerations.Where(g => g.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateCareerPlans.Where(p => p.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateOnboardings.Where(o => o.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateProactiveMatchingSettings.Where(s => s.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.ProactiveMatchSuggestions.Where(s => s.CandidateUserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateRoleFitChecks.Where(r => r.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateVacancyCultureFits.Where(r => r.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateMatchSnapshots.Where(r => r.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateDeepAnalyses.Where(d => d.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateAssessmentAdjustments.Where(a => a.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.CandidateAssessmentAttempts.Where(a => a.UserId == user.Id), cancellationToken);
        await RemoveRangeAsync(_db.DeepAnalysisCheckouts.Where(c => c.UserId == user.Id), cancellationToken);

        var researchAttempts = await _db.AssessmentResearchAttempts
            .Where(a => a.UserId == user.Id)
            .ToListAsync(cancellationToken);
        if (researchAttempts.Count > 0)
        {
            _db.AssessmentResearchAttempts.RemoveRange(researchAttempts);
        }

        var externalVacancies = await _db.CandidateExternalVacancies
            .Where(v => v.CandidateUserId == user.Id)
            .ToListAsync(cancellationToken);
        if (externalVacancies.Count > 0)
        {
            var externalIds = externalVacancies.Select(v => v.Id).ToList();
            var outbounds = await _db.CandidateExternalVacancyOutbounds
                .Where(o => externalIds.Contains(o.ExternalVacancyId))
                .ToListAsync(cancellationToken);
            if (outbounds.Count > 0)
            {
                _db.CandidateExternalVacancyOutbounds.RemoveRange(outbounds);
            }

            _db.CandidateExternalVacancies.RemoveRange(externalVacancies);
        }

        await RemoveRangeAsync(_db.TalentContactRequests.Where(r => r.CandidateUserId == user.Id), cancellationToken);

        var trainingClicks = await _db.TrainingClicks
            .Where(c => c.UserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var click in trainingClicks)
        {
            click.UserId = null;
        }

        var consumerInvoices = await _db.ConsumerPurchaseInvoices
            .Where(i => i.UserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var invoice in consumerInvoices)
        {
            invoice.UserId = null;
        }

        var feedbackRows = await _db.PlatformFeedbacks
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

        var reminderPreference = await _db.CandidateReminderPreferences
            .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
        if (reminderPreference is not null)
        {
            _db.CandidateReminderPreferences.Remove(reminderPreference);
        }

        var reminderLogs = await _db.ComebackReminderLogs
            .Where(l => l.UserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var reminderLog in reminderLogs)
        {
            reminderLog.UserId = null;
        }

        var accessLogs = await _db.PersonalDataAccessLogs
            .Where(l => l.SubjectUserId == user.Id)
            .ToListAsync(cancellationToken);
        if (accessLogs.Count > 0)
        {
            _db.PersonalDataAccessLogs.RemoveRange(accessLogs);
        }

        user.FirstName = null;
        user.LastName = null;
        user.PhoneNumber = null;
        user.PhoneVerifiedAtUtc = null;
        user.PhoneVerifiedE164 = null;
        user.WhatsAppContactAllowed = false;
        user.DateOfBirth = null;
        user.HomeLocation = null;
        user.PreferencesJson = null;
        user.OpenForWork = false;
        user.AvailableFromDate = null;
        user.TalentPoolConsentAt = null;
        user.TalentPoolConsentVersion = null;
        user.TestAiConsentAt = null;
        user.TestAiConsentVersion = null;
        user.ResearchConsentAt = null;
        user.ResearchConsentVersion = null;
        user.ResearchAgeBand = null;
        user.ResearchGender = null;
        user.ResearchEducationLevel = null;
        user.ResearchNativeLanguage = null;
        user.ResearchTestLanguage = null;
        user.ParentalConsentAt = null;
        user.ParentalConsentEmail = null;
        user.ParentalConsentTokenHash = null;
        user.ParentalConsentTokenExpiresAt = null;
        user.CandidateHowToCompletedAt = null;
        user.ReferredByAmbassadeurUserId = null;
        user.ReferredByAmbassadeurTrackingCode = null;
        user.SessionVersion++;

        var idHash = ComputeIdHash(user.Id);
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Privacy",
            Message = $"Kandidaat-reset uitgevoerd id-hash={idHash}",
            DetailsJson = JsonSerializer.Serialize(new
            {
                Event = "candidate_profile_reset",
                IdHash = idHash,
                AtUtc = now,
                HadResearchConsent = hadResearchConsent
            }),
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task RemoveRangeAsync<TEntity>(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var rows = await query.ToListAsync(cancellationToken);
        if (rows.Count > 0)
        {
            _db.Set<TEntity>().RemoveRange(rows);
        }
    }

    private static string ComputeIdHash(Guid userId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(userId.ToString("N")));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
