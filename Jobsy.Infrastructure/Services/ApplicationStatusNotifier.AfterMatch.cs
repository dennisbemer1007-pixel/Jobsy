using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed partial class ApplicationStatusNotifier
{
    public async Task NotifyEmployerSlotsProposedAsync(
        Application application,
        Guid employerUserId,
        CancellationToken cancellationToken = default)
    {
        if (application.Vacancy?.Company is null)
        {
            return;
        }

        var baseUrl = (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl;
        await NotifyAfterMatchCoreAsync(
            application,
            employerUserId,
            actorIsEmployer: true,
            cancellationToken,
            candidateCounterpart: async (lang, ct) =>
            {
                var copy = ApplicationStatusNotificationTexts.InterviewSlotsProposed(
                    application.Vacancy.Company.Name,
                    application.Vacancy.Title,
                    lang);
                await DeliverToCandidateAsync(
                    application,
                    copy,
                    "InterviewSlotsProposed",
                    CandidateAfterMatchPath(application.Id, "pick-slot"),
                    BuildStatusAlertMail(baseUrl, application, copy, CandidateAfterMatchPath(application.Id, "pick-slot"), lang),
                    ct);
            },
            employerSelf: async (lang, ct) =>
            {
                var copy = ApplicationStatusNotificationTexts.InterviewSlotsProposedSelf(
                    application.CandidateName,
                    application.Vacancy.Title,
                    lang);
                await DeliverSelfBellAsync(
                    employerUserId,
                    copy,
                    "InterviewSlotsProposedSelf",
                    EmployerAfterMatchPath(application.Id, "propose"),
                    application.Id,
                    ct);
            });
    }

    public async Task NotifyCandidateAlternativeSlotProposedAsync(
        Application application,
        Guid candidateUserId,
        CancellationToken cancellationToken = default)
    {
        if (application.Vacancy?.Company is null)
        {
            return;
        }

        var baseUrl = (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl;
        await NotifyAfterMatchCoreAsync(
            application,
            candidateUserId,
            actorIsEmployer: false,
            cancellationToken,
            employerCounterpart: async (lang, ct) =>
            {
                var copy = ApplicationStatusNotificationTexts.InterviewAlternativeProposed(
                    application.CandidateName,
                    application.Vacancy.Title,
                    lang);
                await DeliverToEmployersAsync(
                    application,
                    copy,
                    "InterviewAlternativeProposed",
                    EmployerAfterMatchPath(application.Id, "propose"),
                    BuildStatusAlertMail(baseUrl, application, copy, EmployerAfterMatchPath(application.Id, "propose"), lang),
                    ct);
            },
            candidateSelf: async (lang, ct) =>
            {
                var copy = ApplicationStatusNotificationTexts.InterviewAlternativeProposedSelf(
                    application.Vacancy.Company.Name,
                    application.Vacancy.Title,
                    lang);
                await DeliverSelfBellAsync(
                    candidateUserId,
                    copy,
                    "InterviewAlternativeProposedSelf",
                    CandidateAfterMatchPath(application.Id, "pick-slot"),
                    application.Id,
                    ct);
            });
    }

    public async Task NotifyInterviewSlotChosenAsync(
        Application application,
        bool actorIsEmployer,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (application.Vacancy?.Company is null)
        {
            return;
        }

        var baseUrl = (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl;
        if (actorIsEmployer)
        {
            await NotifyAfterMatchCoreAsync(
                application,
                actorUserId,
                actorIsEmployer: true,
                cancellationToken,
                candidateCounterpart: async (lang, ct) =>
                {
                    var copy = ApplicationStatusNotificationTexts.InterviewSlotChosenByEmployer(
                        application.Vacancy.Company.Name,
                        application.Vacancy.Title,
                        lang);
                    await DeliverToCandidateAsync(
                        application,
                        copy,
                        "InterviewSlotChosen",
                        CandidateAfterMatchPath(application.Id),
                        BuildStatusAlertMail(baseUrl, application, copy, CandidateAfterMatchPath(application.Id), lang),
                        ct);
                },
                employerSelf: async (lang, ct) =>
                {
                    var copy = ApplicationStatusNotificationTexts.InterviewSlotChosenByEmployerSelf(
                        application.CandidateName,
                        application.Vacancy.Title,
                        lang);
                    await DeliverSelfBellAsync(
                        actorUserId,
                        copy,
                        "InterviewSlotChosenSelf",
                        EmployerAfterMatchPath(application.Id),
                        application.Id,
                        ct);
                });
            return;
        }

        await NotifyAfterMatchCoreAsync(
            application,
            actorUserId,
            actorIsEmployer: false,
            cancellationToken,
            employerCounterpart: async (lang, ct) =>
            {
                var copy = ApplicationStatusNotificationTexts.InterviewSlotChosenByCandidate(
                    application.CandidateName,
                    application.Vacancy.Title,
                    lang);
                await DeliverToEmployersAsync(
                    application,
                    copy,
                    "InterviewSlotChosen",
                    EmployerAfterMatchPath(application.Id),
                    BuildStatusAlertMail(baseUrl, application, copy, EmployerAfterMatchPath(application.Id), lang),
                    ct);
            },
            candidateSelf: async (lang, ct) =>
            {
                var copy = ApplicationStatusNotificationTexts.InterviewSlotChosenByCandidateSelf(
                    application.Vacancy.Company.Name,
                    application.Vacancy.Title,
                    lang);
                await DeliverSelfBellAsync(
                    actorUserId,
                    copy,
                    "InterviewSlotChosenSelf",
                    CandidateAfterMatchPath(application.Id),
                    application.Id,
                    ct);
            });
    }

    public async Task NotifyInterviewScheduledAsync(
        Application application,
        CancellationToken cancellationToken = default)
    {
        if (application.Vacancy?.Company is null)
        {
            return;
        }

        var candidateLang = await ResolveCandidateLanguageAsync(application, cancellationToken);
        var candidateCopy = ApplicationStatusNotificationTexts.InterviewScheduledCandidate(
            application.Vacancy.Company.Name,
            application.Vacancy.Title,
            candidateLang);
        if (!await WasRecentlyNotifiedAsync(application.Id, "InterviewScheduled", candidateCopy.Title, cancellationToken))
        {
            await DeliverToCandidateAsync(
                application,
                candidateCopy,
                "InterviewScheduled",
                CandidateAfterMatchPath(application.Id),
                mail: null,
                cancellationToken);
        }

        var employerCopyNl = ApplicationStatusNotificationTexts.InterviewScheduledEmployer(
            application.CandidateName,
            application.Vacancy.Title,
            "nl");
        if (!await WasRecentlyNotifiedAsync(application.Id, "InterviewScheduled", employerCopyNl.Title, cancellationToken))
        {
            await DeliverToEmployersAsync(
                application,
                employerCopyNl,
                "InterviewScheduled",
                EmployerAfterMatchPath(application.Id),
                null,
                cancellationToken,
                perContactLanguage: true,
                copyFactory: lang => ApplicationStatusNotificationTexts.InterviewScheduledEmployer(
                    application.CandidateName,
                    application.Vacancy.Title,
                    lang));
        }
    }

    public async Task NotifyInterviewRescheduleRequestedAsync(
        Application application,
        bool actorIsEmployer,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (application.Vacancy?.Company is null)
        {
            return;
        }

        var baseUrl = (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl;
        var candidateLink = CandidateAfterMatchPath(application.Id, "reschedule");
        var employerLink = EmployerAfterMatchPath(application.Id, "reschedule");

        if (actorIsEmployer)
        {
            await NotifyAfterMatchCoreAsync(
                application,
                actorUserId,
                actorIsEmployer: true,
                cancellationToken,
                candidateCounterpart: async (lang, ct) =>
                {
                    var copy = ApplicationStatusNotificationTexts.InterviewRescheduleRequested(
                        application.Vacancy.Title,
                        lang);
                    await DeliverToCandidateAsync(
                        application,
                        copy,
                        "InterviewRescheduleRequested",
                        candidateLink,
                        BuildStatusAlertMail(baseUrl, application, copy, candidateLink, lang),
                        ct);
                },
                employerSelf: async (lang, ct) =>
                {
                    var copy = ApplicationStatusNotificationTexts.InterviewRescheduleRequestedSelf(
                        application.Vacancy.Title,
                        lang);
                    await DeliverSelfBellAsync(
                        actorUserId,
                        copy,
                        "InterviewRescheduleRequestedSelf",
                        employerLink,
                        application.Id,
                        ct);
                });
            return;
        }

        await NotifyAfterMatchCoreAsync(
            application,
            actorUserId,
            actorIsEmployer: false,
            cancellationToken,
            employerCounterpart: async (lang, ct) =>
            {
                var copy = ApplicationStatusNotificationTexts.InterviewRescheduleRequested(
                    application.Vacancy.Title,
                    lang);
                await DeliverToEmployersAsync(
                    application,
                    copy,
                    "InterviewRescheduleRequested",
                    employerLink,
                    BuildStatusAlertMail(baseUrl, application, copy, employerLink, lang),
                    ct);
            },
            candidateSelf: async (lang, ct) =>
            {
                var copy = ApplicationStatusNotificationTexts.InterviewRescheduleRequestedSelf(
                    application.Vacancy.Title,
                    lang);
                await DeliverSelfBellAsync(
                    actorUserId,
                    copy,
                    "InterviewRescheduleRequestedSelf",
                    candidateLink,
                    application.Id,
                    ct);
            });
    }

    public async Task NotifyInterviewCancelledAsync(
        Application application,
        bool actorIsEmployer,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (application.Vacancy?.Company is null)
        {
            return;
        }

        var baseUrl = (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl;
        var candidateLink = CandidateAfterMatchPath(application.Id, "cancel");
        var employerLink = EmployerAfterMatchPath(application.Id, "cancel");

        if (actorIsEmployer)
        {
            await NotifyAfterMatchCoreAsync(
                application,
                actorUserId,
                actorIsEmployer: true,
                cancellationToken,
                candidateCounterpart: async (lang, ct) =>
                {
                    var copy = ApplicationStatusNotificationTexts.InterviewCancelled(
                        application.Vacancy.Title,
                        lang);
                    await DeliverToCandidateAsync(
                        application,
                        copy,
                        "InterviewCancelled",
                        candidateLink,
                        BuildStatusAlertMail(baseUrl, application, copy, candidateLink, lang),
                        ct);
                },
                employerSelf: async (lang, ct) =>
                {
                    var copy = ApplicationStatusNotificationTexts.InterviewCancelledSelf(
                        application.Vacancy.Title,
                        lang);
                    await DeliverSelfBellAsync(
                        actorUserId,
                        copy,
                        "InterviewCancelledSelf",
                        employerLink,
                        application.Id,
                        ct);
                });
            return;
        }

        await NotifyAfterMatchCoreAsync(
            application,
            actorUserId,
            actorIsEmployer: false,
            cancellationToken,
            employerCounterpart: async (lang, ct) =>
            {
                var copy = ApplicationStatusNotificationTexts.InterviewCancelled(
                    application.Vacancy.Title,
                    lang);
                await DeliverToEmployersAsync(
                    application,
                    copy,
                    "InterviewCancelled",
                    employerLink,
                    BuildStatusAlertMail(baseUrl, application, copy, employerLink, lang),
                    ct);
            },
            candidateSelf: async (lang, ct) =>
            {
                var copy = ApplicationStatusNotificationTexts.InterviewCancelledSelf(
                    application.Vacancy.Title,
                    lang);
                await DeliverSelfBellAsync(
                    actorUserId,
                    copy,
                    "InterviewCancelledSelf",
                    candidateLink,
                    application.Id,
                    ct);
            });
    }

    private async Task NotifyAfterMatchCoreAsync(
        Application application,
        Guid actorUserId,
        bool actorIsEmployer,
        CancellationToken cancellationToken,
        Func<string, CancellationToken, Task>? candidateCounterpart = null,
        Func<string, CancellationToken, Task>? employerCounterpart = null,
        Func<string, CancellationToken, Task>? candidateSelf = null,
        Func<string, CancellationToken, Task>? employerSelf = null)
    {
        if (actorIsEmployer)
        {
            if (candidateCounterpart is not null)
            {
                var lang = await ResolveCandidateLanguageAsync(application, cancellationToken);
                await candidateCounterpart(lang, cancellationToken);
            }

            if (employerSelf is not null)
            {
                var lang = await ResolveUserLanguageAsync(actorUserId, cancellationToken);
                await employerSelf(lang, cancellationToken);
            }

            return;
        }

        if (employerCounterpart is not null)
        {
            await employerCounterpart(JobsyLanguages.Default, cancellationToken);
        }

        if (candidateSelf is not null)
        {
            var lang = await ResolveUserLanguageAsync(actorUserId, cancellationToken);
            await candidateSelf(lang, cancellationToken);
        }
    }

    private static ComposedEmail BuildStatusAlertMail(
        string baseUrl,
        Application application,
        ApplicationStatusNotificationTexts.Copy copy,
        string deepLink,
        string language)
        => TransactionalEmails.ApplicationStatusAlert(
            baseUrl,
            application.CandidateName,
            copy.Title,
            copy.Body,
            deepLink,
            ToEmailCulture(language));

    private async Task DeliverToEmployersAsync(
        Application application,
        ApplicationStatusNotificationTexts.Copy copy,
        string category,
        string deepLink,
        ComposedEmail? mailTemplate,
        CancellationToken cancellationToken,
        bool perContactLanguage = false,
        Func<string, ApplicationStatusNotificationTexts.Copy>? copyFactory = null)
    {
        var companyId = application.Vacancy!.CompanyId;
        var contacts = await _db.Users.AsNoTracking()
            .Where(u =>
                u.IsActive
                && u.Role != UserRole.Candidate
                && (u.CompanyId == companyId || u.CompanyMemberships.Any(m => m.CompanyId == companyId)))
            .Select(u => new { u.Id, u.Email, u.PreferencesJson })
            .Distinct()
            .ToListAsync(cancellationToken);

        var baseUrl = (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl;
        var absoluteLink = EmailLayout.Absolute(baseUrl, deepLink);

        foreach (var contact in contacts)
        {
            var lang = perContactLanguage
                ? ResolveLanguageFromPreferencesJson(contact.PreferencesJson)
                : JobsyLanguages.Default;
            var effectiveCopy = copyFactory?.Invoke(lang) ?? copy;
            var mail = mailTemplate is not null && !perContactLanguage
                ? mailTemplate
                : BuildStatusAlertMail(baseUrl, application, effectiveCopy, deepLink, lang);

            if (mail is not null)
            {
                await _mailer.SendAsync(mail, contact.Email, cancellationToken: cancellationToken);
            }

            await _push.SendAsync(
                new PushMessage(contact.Email, effectiveCopy.Title, effectiveCopy.Body, absoluteLink, category),
                cancellationToken);
            await _notifications.CreateAsync(
                new NotificationCreateRequest(
                    contact.Id,
                    effectiveCopy.Title,
                    effectiveCopy.Body,
                    category,
                    deepLink,
                    RelatedEntityType: "Application",
                    RelatedEntityId: application.Id),
                cancellationToken);
        }
    }

    private async Task DeliverSelfBellAsync(
        Guid userId,
        ApplicationStatusNotificationTexts.Copy copy,
        string category,
        string deepLink,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        await _notifications.CreateAsync(
            new NotificationCreateRequest(
                userId,
                copy.Title,
                copy.Body,
                category,
                deepLink,
                RelatedEntityType: "Application",
                RelatedEntityId: applicationId),
            cancellationToken);
    }

    private async Task<bool> WasRecentlyNotifiedAsync(
        Guid applicationId,
        string category,
        string title,
        CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        return await _db.UserNotifications.AsNoTracking()
            .AnyAsync(
                n => n.RelatedEntityId == applicationId
                     && n.Category == category
                     && n.Title == title
                     && n.CreatedAtUtc >= since,
                cancellationToken);
    }

    private async Task<string> ResolveUserLanguageAsync(Guid userId, CancellationToken cancellationToken)
    {
        var json = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PreferencesJson)
            .FirstOrDefaultAsync(cancellationToken);
        return ResolveLanguageFromPreferencesJson(json);
    }

    private static string CandidateAfterMatchPath(Guid applicationId, string? panel = null)
    {
        var path = $"/candidate/applications/{applicationId:D}/after-match";
        if (string.IsNullOrWhiteSpace(panel))
        {
            return path;
        }

        return $"{path}?panel={Uri.EscapeDataString(panel)}";
    }

    private static string EmployerAfterMatchPath(Guid applicationId, string? panel = null)
    {
        var path = $"/werkgever/sollicitaties/{applicationId:D}/after-match";
        if (string.IsNullOrWhiteSpace(panel))
        {
            return path;
        }

        return $"{path}?panel={Uri.EscapeDataString(panel)}";
    }
}
