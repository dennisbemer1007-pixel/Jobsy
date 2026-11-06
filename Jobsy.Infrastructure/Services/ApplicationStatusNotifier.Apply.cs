using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Notifications;

namespace Jobsy.Infrastructure.Services;

public sealed partial class ApplicationStatusNotifier
{
    public async Task NotifyApplicationSubmittedAsync(
        Application application,
        Vacancy vacancy,
        string candidateDisplayName,
        CancellationToken cancellationToken = default)
    {
        application.Vacancy ??= vacancy;
        var baseUrl = (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl;
        var companyName = vacancy.Company?.Name ?? string.Empty;

        var employerCopyNl = ApplicationStatusNotificationTexts.EmployerNewApplication(
            companyName,
            vacancy.Title,
            "nl");
        var employerLink = EmployerApplicationPath(application.Id);
        var employerMail = TransactionalEmails.EmployerNewApplication(
            baseUrl,
            vacancy.Title,
            branchName: companyName,
            applicationId: application.Id,
            receivedAtUtc: application.CreatedAt,
            matchPercent: null,
            companyName: companyName);

        await DeliverToEmployersAsync(
            application,
            employerCopyNl,
            "EmployerNewApplication",
            employerLink,
            employerMail,
            cancellationToken,
            perContactLanguage: true,
            copyFactory: lang => ApplicationStatusNotificationTexts.EmployerNewApplication(
                companyName,
                vacancy.Title,
                lang));

        var candidateLang = await ResolveCandidateLanguageAsync(application, cancellationToken);
        var candidateCopy = ApplicationStatusNotificationTexts.ApplicationSubmittedSelf(
            companyName,
            vacancy.Title,
            candidateLang);
        var candidateLink = CandidateApplicationsSelPath(application.Id);
        var candidateMail = TransactionalEmails.ApplicationConfirmation(
            baseUrl,
            candidateDisplayName,
            vacancy.Title,
            companyName);

        await DeliverToCandidateAsync(
            application,
            candidateCopy,
            "ApplicationConfirmation",
            candidateLink,
            candidateMail,
            cancellationToken,
            sendPush: false);
    }
}
