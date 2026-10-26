using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Notifications;

namespace Jobsy.Infrastructure.Services;

public sealed partial class ApplicationStatusNotifier
{
    public Task NotifyApplicationOutcomeRecordedAsync(
        Application application,
        Guid employerUserId,
        AfterMatchOutcomeKind outcome,
        CancellationToken cancellationToken = default)
    {
        if (application.Vacancy?.Company is null)
        {
            return Task.CompletedTask;
        }

        return outcome switch
        {
            AfterMatchOutcomeKind.Offer => NotifyOutcomeOfferAsync(application, employerUserId, cancellationToken),
            AfterMatchOutcomeKind.NextRound => NotifyOutcomeNextRoundAsync(application, employerUserId, cancellationToken),
            AfterMatchOutcomeKind.NotNow => NotifyOutcomeNotNowAsync(application, employerUserId, cancellationToken),
            _ => Task.CompletedTask
        };
    }

    private async Task NotifyOutcomeOfferAsync(
        Application application,
        Guid employerUserId,
        CancellationToken cancellationToken)
    {
        var baseUrl = (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl;
        var companyName = application.Vacancy!.Company.Name;
        var vacancyTitle = application.Vacancy.Title;

        var candidateLang = await ResolveCandidateLanguageAsync(application, cancellationToken);
        var candidateCopy = ApplicationStatusNotificationTexts.ApplicationOutcomeOffer(companyName, vacancyTitle, candidateLang);
        var candidateLink = CandidateAfterMatchPath(application.Id, "outcome");
        await DeliverToCandidateAsync(
            application,
            candidateCopy,
            "ApplicationOutcomeOffer",
            candidateLink,
            BuildStatusAlertMail(baseUrl, application, candidateCopy, candidateLink, candidateLang),
            cancellationToken);

        var selfLang = await ResolveUserLanguageAsync(employerUserId, cancellationToken);
        var selfCopy = ApplicationStatusNotificationTexts.ApplicationOutcomeOfferSelf(
            application.CandidateName,
            vacancyTitle,
            selfLang);
        await DeliverSelfBellAsync(
            employerUserId,
            selfCopy,
            "ApplicationOutcomeOfferSelf",
            EmployerAfterMatchPath(application.Id),
            application.Id,
            cancellationToken);
    }

    private async Task NotifyOutcomeNextRoundAsync(
        Application application,
        Guid employerUserId,
        CancellationToken cancellationToken)
    {
        var baseUrl = (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl;
        var companyName = application.Vacancy!.Company.Name;
        var vacancyTitle = application.Vacancy.Title;

        var candidateLang = await ResolveCandidateLanguageAsync(application, cancellationToken);
        var candidateCopy = ApplicationStatusNotificationTexts.ApplicationOutcomeNextRound(companyName, vacancyTitle, candidateLang);
        var candidateLink = CandidateAfterMatchPath(application.Id);
        await DeliverToCandidateAsync(
            application,
            candidateCopy,
            "ApplicationOutcomeNextRound",
            candidateLink,
            BuildStatusAlertMail(baseUrl, application, candidateCopy, candidateLink, candidateLang),
            cancellationToken);

        var selfLang = await ResolveUserLanguageAsync(employerUserId, cancellationToken);
        var selfCopy = ApplicationStatusNotificationTexts.ApplicationOutcomeNextRoundSelf(
            application.CandidateName,
            vacancyTitle,
            selfLang);
        await DeliverSelfBellAsync(
            employerUserId,
            selfCopy,
            "ApplicationOutcomeNextRoundSelf",
            EmployerAfterMatchPath(application.Id),
            application.Id,
            cancellationToken);
    }

    private async Task NotifyOutcomeNotNowAsync(
        Application application,
        Guid employerUserId,
        CancellationToken cancellationToken)
    {
        var baseUrl = (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl;
        var companyName = application.Vacancy!.Company.Name;
        var vacancyTitle = application.Vacancy.Title;

        var candidateLang = await ResolveCandidateLanguageAsync(application, cancellationToken);
        var candidateCopy = ApplicationStatusNotificationTexts.ApplicationOutcomeNotNow(companyName, vacancyTitle, candidateLang);
        var candidateLink = CandidateAfterMatchPath(application.Id, "outcome");
        await DeliverToCandidateAsync(
            application,
            candidateCopy,
            "ApplicationOutcomeNotNow",
            candidateLink,
            BuildStatusAlertMail(baseUrl, application, candidateCopy, candidateLink, candidateLang),
            cancellationToken);

        var selfLang = await ResolveUserLanguageAsync(employerUserId, cancellationToken);
        var selfCopy = ApplicationStatusNotificationTexts.ApplicationOutcomeNotNowSelf(
            application.CandidateName,
            vacancyTitle,
            selfLang);
        await DeliverSelfBellAsync(
            employerUserId,
            selfCopy,
            "ApplicationOutcomeNotNowSelf",
            EmployerAfterMatchPath(application.Id),
            application.Id,
            cancellationToken);
    }
}
