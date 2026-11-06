using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Notifications;

namespace Jobsy.Infrastructure.Services;

public sealed partial class ApplicationStatusNotifier
{
    public async Task NotifyApplicationWithdrawnAsync(
        Application application,
        Vacancy vacancy,
        CancellationToken cancellationToken = default)
    {
        application.Vacancy ??= vacancy;
        var baseUrl = (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl;
        var companyName = vacancy.Company?.Name ?? string.Empty;

        var employerCopyNl = ApplicationStatusNotificationTexts.CandidateWithdrawn(
            application.CandidateName,
            vacancy.Title,
            "nl");
        var employerLink = EmployerApplicationPath(application.Id);
        var employerMail = TransactionalEmails.CandidateWithdrawn(
            baseUrl,
            vacancy.Title,
            companyName: companyName);

        await DeliverToEmployersAsync(
            application,
            employerCopyNl,
            "CandidateWithdrawn",
            employerLink,
            employerMail,
            cancellationToken,
            perContactLanguage: true,
            copyFactory: lang => ApplicationStatusNotificationTexts.CandidateWithdrawn(
                application.CandidateName,
                vacancy.Title,
                lang),
            sendPush: false);

        if (application.CandidateUserId is not Guid candidateUserId)
        {
            return;
        }

        var selfLang = await ResolveUserLanguageAsync(candidateUserId, cancellationToken);
        var selfCopy = ApplicationStatusNotificationTexts.ApplicationWithdrawnSelf(
            companyName,
            vacancy.Title,
            selfLang);
        await DeliverSelfBellAsync(
            candidateUserId,
            selfCopy,
            "ApplicationWithdrawnSelf",
            CandidateApplicationsSelPath(application.Id),
            application.Id,
            cancellationToken);
    }
}
