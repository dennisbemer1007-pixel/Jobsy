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
    private readonly IStatusNotificationDispatchGate _dispatchGate;

    public ApplicationStatusNotifier(
        JobsyDbContext db,
        IUserNotificationService notifications,
        IPushNotificationService push,
        ITransactionalMailer mailer,
        IPlatformFeatureService features,
        IStatusNotificationDispatchGate dispatchGate)
    {
        _db = db;
        _notifications = notifications;
        _push = push;
        _mailer = mailer;
        _features = features;
        _dispatchGate = dispatchGate;
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

        if (application.CandidateUserId is Guid userId)
        {
            var decision = await _dispatchGate.EvaluateAsync(userId, category, cancellationToken);
            if (decision.AllowMail && mail is not null)
            {
                await _mailer.SendAsync(mail, application.CandidateEmail, cancellationToken: cancellationToken);
            }

            if (sendPush && decision.AllowPush)
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

            if (decision.AllowBell)
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
            }

            return;
        }

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
