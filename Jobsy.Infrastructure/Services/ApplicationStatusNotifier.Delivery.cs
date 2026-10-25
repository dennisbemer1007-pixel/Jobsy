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
    private static ComposedEmail BuildStatusAlertMail(
        string baseUrl,
        Application application,
        ApplicationStatusNotificationTexts.Copy copy,
        string deepLink,
        string language,
        string? recipientName = null)
        => TransactionalEmails.ApplicationStatusAlert(
            baseUrl,
            recipientName ?? application.CandidateName,
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
            .Select(u => new { u.Id, u.Email, u.PreferencesJson, u.FullName })
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
                : BuildStatusAlertMail(baseUrl, application, effectiveCopy, deepLink, lang, contact.FullName);

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
        return string.IsNullOrWhiteSpace(panel) ? path : $"{path}?panel={Uri.EscapeDataString(panel)}";
    }

    private static string EmployerAfterMatchPath(Guid applicationId, string? panel = null)
    {
        var path = $"/werkgever/sollicitaties/{applicationId:D}/after-match";
        return string.IsNullOrWhiteSpace(panel) ? path : $"{path}?panel={Uri.EscapeDataString(panel)}";
    }

    private static string CandidateApplicationsSelPath(Guid applicationId)
        => $"/candidate/applications?tab=sollicitaties&sel={applicationId:D}";

    private static string EmployerApplicationPath(Guid applicationId)
        => $"/werkgever/sollicitaties/{applicationId:D}";
}
