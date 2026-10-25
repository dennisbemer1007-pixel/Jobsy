using Jobsy.Core.Entities;
using Jobsy.Core.Notifications;

namespace Jobsy.Infrastructure.Services;

public sealed partial class ApplicationStatusNotifier
{
    public async Task NotifyApplicationNewMessageAsync(
        Application application,
        Guid senderUserId,
        bool senderIsEmployer,
        CancellationToken cancellationToken = default)
    {
        if (application.Vacancy is null)
        {
            return;
        }

        var baseUrl = (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl;
        var vacancyTitle = application.Vacancy.Title;

        if (senderIsEmployer)
        {
            var candidateLang = await ResolveCandidateLanguageAsync(application, cancellationToken);
            var counterpartCopy = ApplicationStatusNotificationTexts.ApplicationNewMessage(vacancyTitle, candidateLang);
            var candidateLink = CandidateAfterMatchPath(application.Id, "messages");
            await DeliverToCandidateAsync(
                application,
                counterpartCopy,
                "ApplicationNewMessage",
                candidateLink,
                BuildStatusAlertMail(baseUrl, application, counterpartCopy, candidateLink, candidateLang),
                cancellationToken);

            var selfLang = await ResolveUserLanguageAsync(senderUserId, cancellationToken);
            var selfCopy = ApplicationStatusNotificationTexts.ApplicationNewMessageSelf(vacancyTitle, selfLang);
            await DeliverSelfBellAsync(
                senderUserId,
                selfCopy,
                "ApplicationNewMessageSelf",
                EmployerAfterMatchPath(application.Id, "messages"),
                application.Id,
                cancellationToken);
            return;
        }

        await DeliverToEmployersAsync(
            application,
            ApplicationStatusNotificationTexts.ApplicationNewMessage(vacancyTitle, "nl"),
            "ApplicationNewMessage",
            EmployerAfterMatchPath(application.Id, "messages"),
            null,
            cancellationToken,
            perContactLanguage: true,
            copyFactory: lang => ApplicationStatusNotificationTexts.ApplicationNewMessage(vacancyTitle, lang));

        var candidateSelfLang = await ResolveUserLanguageAsync(senderUserId, cancellationToken);
        var candidateSelfCopy = ApplicationStatusNotificationTexts.ApplicationNewMessageSelf(vacancyTitle, candidateSelfLang);
        await DeliverSelfBellAsync(
            senderUserId,
            candidateSelfCopy,
            "ApplicationNewMessageSelf",
            CandidateAfterMatchPath(application.Id, "messages"),
            application.Id,
            cancellationToken);
    }
}
