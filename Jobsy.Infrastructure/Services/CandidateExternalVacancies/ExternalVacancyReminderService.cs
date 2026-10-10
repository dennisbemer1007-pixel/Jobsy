using Jobsy.Core;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services.CandidateExternalVacancies;

public interface IExternalVacancyReminderService
{
    Task<int> SendDueRemindersAsync(CancellationToken cancellationToken = default);
}

public sealed class ExternalVacancyReminderService : IExternalVacancyReminderService
{
    private readonly JobsyDbContext _db;
    private readonly IOneTimeLinkService _oneTimeLinks;
    private readonly IExternalVacancySuppressionService _suppression;
    private readonly ITransactionalMailer _mailer;
    private readonly IPlatformFeatureService _features;
    private readonly ILogger<ExternalVacancyReminderService> _logger;

    public ExternalVacancyReminderService(
        JobsyDbContext db,
        IOneTimeLinkService oneTimeLinks,
        IExternalVacancySuppressionService suppression,
        ITransactionalMailer mailer,
        IPlatformFeatureService features,
        ILogger<ExternalVacancyReminderService> logger)
    {
        _db = db;
        _oneTimeLinks = oneTimeLinks;
        _suppression = suppression;
        _mailer = mailer;
        _features = features;
        _logger = logger;
    }

    public async Task<int> SendDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-CandidateExternalVacancyRules.ReminderAfterDays);
        var candidates = await _db.CandidateExternalVacancyOutbounds
            .Include(o => o.ExternalVacancy).ThenInclude(v => v.CandidateUser)
            .Where(o =>
                o.ReminderSentAtUtc == null
                && o.InitialSentAtUtc <= cutoff
                && o.ClickedAtUtc == null
                && o.EmployerAccountCreatedAtUtc == null)
            .OrderBy(o => o.InitialSentAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return 0;
        }

        var platform = await _features.GetAsync(cancellationToken);
        var baseUrl = platform.PublicWebBaseUrl.TrimEnd('/');
        var apiBase = JobsyPublicUrl.NormalizeBaseUrl(platform.PublicWebBaseUrl, "http://localhost:5200/");
        var sent = 0;

        foreach (var outbound in candidates)
        {
            if (await _suppression.IsSuppressedAsync(outbound.EmployerEmailNormalized, cancellationToken))
            {
                continue;
            }

            var vacancy = outbound.ExternalVacancy;
            var shared = ParseSharedFacts(outbound.SharedFactsJson);
            var link = await _oneTimeLinks.CreateAsync(
                OneTimeLinkPurpose.ExternalVacancyEmployerInvite,
                userId: null,
                companyId: null,
                outbound.EmployerEmailNormalized,
                OneTimeLinkRules.ExternalVacancyEmployerInviteLifetime,
                vacancy.CandidateUserId,
                cancellationToken);

            outbound.OneTimeLinkId = link.Id;
            outbound.ReminderSentAtUtc = DateTime.UtcNow;

            var inviteUrl =
                $"{baseUrl}/register/externe-sollicitatie?token={Uri.EscapeDataString(link.Token)}";
            var unsubToken = _suppression.CreateUnsubscribeToken(outbound.EmployerEmailNormalized);
            var unsubUrl =
                $"{apiBase}api/public/external-vacancy/unsubscribe?token={Uri.EscapeDataString(unsubToken)}";
            var openUrl =
                $"{apiBase}api/public/external-vacancy/open?token={Uri.EscapeDataString(link.Token)}";

            var mail = TransactionalEmails.ExternalVacancyApplication(
                platform.PublicWebBaseUrl,
                vacancy.CandidateUser.FullName,
                vacancy.Title,
                vacancy.CompanyName,
                outbound.Motivation,
                shared,
                inviteUrl,
                unsubUrl,
                openTrackingPixelUrl: openUrl);

            var outcome = await _mailer.SendAsync(mail, outbound.EmployerEmailNormalized, cancellationToken: cancellationToken);
            if (outcome.Sent)
            {
                sent++;
            }
            else
            {
                _logger.LogWarning(
                    "External vacancy reminder suppressed for {Email}",
                    outbound.EmployerEmailNormalized);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return sent;
    }

    private static List<(string Label, string Value)> ParseSharedFacts(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                return [];
            }

            return doc.RootElement.EnumerateObject()
                .Select(p => (p.Name, p.Value.GetString() ?? ""))
                .Where(t => !string.IsNullOrWhiteSpace(t.Item2))
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
