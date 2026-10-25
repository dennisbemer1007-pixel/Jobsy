using System.Text.Json;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Notifications;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed partial class ApplicationStatusNotifier
{
    private readonly JobsyDbContext _db;
    private readonly IUserNotificationService _notifications;
    private readonly IPushNotificationService _push;
    private readonly ITransactionalMailer _mailer;
    private readonly IPlatformFeatureService _features;

    public ApplicationStatusNotifier(
        JobsyDbContext db,
        IUserNotificationService notifications,
        IPushNotificationService push,
        ITransactionalMailer mailer,
        IPlatformFeatureService features)
    {
        _db = db;
        _notifications = notifications;
        _push = push;
        _mailer = mailer;
        _features = features;
    }

    public async Task NotifyCvViewedAsync(Application application, CancellationToken cancellationToken = default)
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
        var deepLink = $"/candidate/applications?tab=sollicitaties&sel={application.Id:D}";
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
    }

    public async Task NotifyMutualYesAsync(Application application, CancellationToken cancellationToken = default)
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
        var deepLink = $"/candidate/applications/{application.Id:D}/after-match";
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
    }

    private async Task DeliverToCandidateAsync(
        Application application,
        ApplicationStatusNotificationTexts.Copy copy,
        string category,
        string deepLink,
        ComposedEmail? mail,
        CancellationToken cancellationToken,
        bool sendPush = true)
    {
        var absoluteLink = EmailLayout.Absolute(
            (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl,
            deepLink);

        if (mail is not null)
        {
            await _mailer.SendAsync(mail, application.CandidateEmail, cancellationToken: cancellationToken);
        }

        if (sendPush)
        {
            await _push.SendAsync(
                new PushMessage(
                    application.CandidateEmail,
                    copy.Title,
                    copy.Body,
                    absoluteLink,
                    category),
                cancellationToken);
        }

        if (application.CandidateUserId is Guid userId)
        {
            await _notifications.CreateAsync(
                new NotificationCreateRequest(
                    userId,
                    copy.Title,
                    copy.Body,
                    category,
                    deepLink,
                    RelatedEntityType: "Application",
                    RelatedEntityId: application.Id),
                cancellationToken);
            return;
        }

        await _notifications.CreateForEmailAsync(
            application.CandidateEmail,
            copy.Title,
            copy.Body,
            category,
            deepLink,
            relatedEntityType: "Application",
            relatedEntityId: application.Id,
            cancellationToken: cancellationToken);
    }

    private async Task<string> ResolveCandidateLanguageAsync(
        Application application,
        CancellationToken cancellationToken)
    {
        if (application.CandidateUserId is not Guid userId)
        {
            return JobsyLanguages.Default;
        }

        var json = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PreferencesJson)
            .FirstOrDefaultAsync(cancellationToken);
        return ResolveLanguageFromPreferencesJson(json);
    }

    internal static string ResolveLanguageFromPreferencesJson(string? preferencesJson)
    {
        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return JobsyLanguages.Default;
        }

        try
        {
            using var doc = JsonDocument.Parse(preferencesJson);
            if (doc.RootElement.TryGetProperty("language", out var langProp))
            {
                return JobsyLanguages.Normalize(langProp.GetString());
            }
        }
        catch (JsonException)
        {
            // fall through
        }

        return JobsyLanguages.Default;
    }

    private static EmailCulture ToEmailCulture(string language)
        => string.Equals(JobsyLanguages.Normalize(language), "en", StringComparison.OrdinalIgnoreCase)
            ? EmailCulture.En
            : EmailCulture.Nl;
}
