using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Notifications;

namespace Jobsy.Infrastructure.Services;

public sealed partial class ApplicationStatusNotifier
{
    public async Task NotifyCvViewedAsync(
        Application application,
        Guid? employerUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (application.Vacancy?.Company is null)
        {
            return;
        }

        var language = await ResolveCandidateLanguageAsync(application, cancellationToken);
        var copy = ApplicationStatusNotificationTexts.CvViewed(
            application.Vacancy.Company.Name,
            application.Vacancy.Title,
            language);
        var deepLink = CandidateApplicationsSelPath(application.Id);
        await DeliverToCandidateAsync(
            application,
            copy,
            category: "ApplicationCvViewed",
            deepLink,
            mail: TransactionalEmails.ApplicationCvViewed(
                (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl,
                application.CandidateName,
                application.Vacancy.Title,
                application.Vacancy.Company.Name,
                application.Id,
                ToEmailCulture(language)),
            cancellationToken);

        if (employerUserId is Guid employerId)
        {
            var selfLang = await ResolveUserLanguageAsync(employerId, cancellationToken);
            var selfCopy = ApplicationStatusNotificationTexts.CvViewedSelf(
                application.CandidateName,
                application.Vacancy.Title,
                selfLang);
            await DeliverSelfBellAsync(
                employerId,
                selfCopy,
                "ApplicationCvViewedSelf",
                EmployerApplicationPath(application.Id),
                application.Id,
                cancellationToken);
        }
    }

    public async Task NotifyMutualYesAsync(
        Application application,
        Guid? employerUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (application.Vacancy?.Company is null)
        {
            return;
        }

        var language = await ResolveCandidateLanguageAsync(application, cancellationToken);
        var copy = ApplicationStatusNotificationTexts.MutualYes(
            application.Vacancy.Company.Name,
            application.Vacancy.Title,
            language);
        var deepLink = CandidateAfterMatchPath(application.Id);
        await DeliverToCandidateAsync(
            application,
            copy,
            category: "ApplicationMutualYes",
            deepLink,
            mail: TransactionalEmails.ApplicationMutualYes(
                (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl,
                application.CandidateName,
                application.Vacancy.Title,
                application.Vacancy.Company.Name,
                application.Id,
                ToEmailCulture(language)),
            cancellationToken);

        if (employerUserId is Guid employerId)
        {
            var selfLang = await ResolveUserLanguageAsync(employerId, cancellationToken);
            var selfCopy = ApplicationStatusNotificationTexts.MutualYesSelf(
                application.CandidateName,
                application.Vacancy.Title,
                selfLang);
            await DeliverSelfBellAsync(
                employerId,
                selfCopy,
                "ApplicationMutualYesSelf",
                EmployerApplicationPath(application.Id),
                application.Id,
                cancellationToken);
        }
    }
}
