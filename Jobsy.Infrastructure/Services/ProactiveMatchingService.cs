using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class ProactiveMatchingService : IProactiveMatchingService
{
    private readonly JobsyDbContext _db;
    private readonly IProfileVacancyMatchService _matches;
    private readonly IVacancyDiscoveryIndex _discovery;
    private readonly IUserNotificationService _notifications;
    private readonly ITransactionalMailer _mailer;
    private readonly IFeatureFlags _flags;
    private readonly ProactiveMatchingOptions _options;
    private readonly ILogger<ProactiveMatchingService> _logger;

    public ProactiveMatchingService(
        JobsyDbContext db,
        IProfileVacancyMatchService matches,
        IVacancyDiscoveryIndex discovery,
        IUserNotificationService notifications,
        ITransactionalMailer mailer,
        IFeatureFlags flags,
        IOptions<ProactiveMatchingOptions> options,
        ILogger<ProactiveMatchingService> logger)
    {
        _db = db;
        _matches = matches;
        _discovery = discovery;
        _notifications = notifications;
        _mailer = mailer;
        _flags = flags;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ProactiveMatchingSettingsDto?> GetSettingsAsync(
        Guid candidateUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _flags.IsEnabledAsync(PlatformFeature.ProactiveMatching, cancellationToken))
        {
            return null;
        }

        var row = await _db.CandidateProactiveMatchingSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == candidateUserId, cancellationToken);

        return MapSettings(row);
    }

    public async Task<ProactiveMatchingSettingsDto> UpsertSettingsAsync(
        Guid candidateUserId,
        ProactiveMatchingSettingsUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (!await _flags.IsEnabledAsync(PlatformFeature.ProactiveMatching, cancellationToken))
        {
            throw new InvalidOperationException("Proactieve matching is niet beschikbaar.");
        }

        var row = await _db.CandidateProactiveMatchingSettings
            .FirstOrDefaultAsync(s => s.UserId == candidateUserId, cancellationToken);
        if (row is null)
        {
            row = new CandidateProactiveMatchingSettings { UserId = candidateUserId };
            _db.CandidateProactiveMatchingSettings.Add(row);
        }

        if (update.Enabled is bool enabled)
        {
            if (enabled && !ProactiveMatchingRules.HasValidConsent(row.ConsentAcceptedAtUtc, row.ConsentVersion))
            {
                throw new InvalidOperationException("Geef eerst toestemming voor proactieve matching.");
            }

            row.Enabled = enabled;
        }

        if (update.Frequency is ProactiveMatchingFrequency frequency)
        {
            row.Frequency = frequency;
        }

        if (update.ClearPause == true)
        {
            row.PausedUntilUtc = null;
        }
        else if (update.PausedUntilUtc is DateTime pause)
        {
            row.PausedUntilUtc = DateTime.SpecifyKind(pause, DateTimeKind.Utc);
        }

        if (update.AcceptConsent == true)
        {
            row.ConsentAcceptedAtUtc = DateTime.UtcNow;
            row.ConsentVersion = ProactiveMatchingRules.ConsentVersion;
        }

        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return MapSettings(row)!;
    }

    public async Task<IReadOnlyList<ProactiveMatchSuggestionDto>> ListSuggestionsAsync(
        Guid candidateUserId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        var rows = await _db.ProactiveMatchSuggestions.AsNoTracking()
            .Where(s => s.CandidateUserId == candidateUserId)
            .OrderByDescending(s => s.DeliveredAtUtc)
            .Take(Math.Clamp(take * 3, take, 150))
            .ToListAsync(cancellationToken);

        var mapped = rows.Select(MapSuggestion).ToList();
        var deduped = DedupeSuggestions(mapped, take);
        return await EnrichRationalesAsync(candidateUserId, deduped, cancellationToken);
    }

    public async Task<ProactiveMatchSuggestionDto?> SubmitFeedbackAsync(
        Guid candidateUserId,
        Guid suggestionId,
        ProactiveMatchFeedback feedback,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.ProactiveMatchSuggestions
            .FirstOrDefaultAsync(s => s.Id == suggestionId && s.CandidateUserId == candidateUserId, cancellationToken);
        if (row is null)
        {
            return null;
        }

        row.Feedback = feedback;
        row.FeedbackAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        if (feedback == ProactiveMatchFeedback.NotInterested && row.VacancyId is Guid vacancyId)
        {
            await ApplyDislikeFeedbackAsync(candidateUserId, vacancyId, cancellationToken);
        }

        return MapSuggestion(row);
    }

    public async Task<int> RunDailyMatchingAsync(CancellationToken cancellationToken = default)
    {
        if (!await _flags.IsEnabledAsync(PlatformFeature.ProactiveMatching, cancellationToken))
        {
            return 0;
        }

        var minScore = ProactiveMatchingRules.ClampMinScore(_options.MinScorePercent);
        var maxPerWeek = ProactiveMatchingRules.ClampMaxPerWeek(_options.MaxNotificationsPerWeek);
        var weekStart = ProactiveMatchingRules.WeekStartUtc(DateTime.UtcNow);
        var now = DateTime.UtcNow;
        var externalOn = await _flags.IsEnabledAsync(PlatformFeature.CandidateExternalVacancies, cancellationToken);

        var settingsRows = (await _db.CandidateProactiveMatchingSettings.AsNoTracking()
                .Where(s => s.Enabled)
                .ToListAsync(cancellationToken))
            .Where(s => ProactiveMatchingRules.HasValidConsent(s.ConsentAcceptedAtUtc, s.ConsentVersion))
            .ToList();

        var activeVacancies = await _discovery.GetActiveAsync(cancellationToken);
        var sent = 0;

        foreach (var settings in settingsRows)
        {
            if (settings.PausedUntilUtc is DateTime pause && pause > now)
            {
                continue;
            }

            if (!ProactiveMatchingRules.ShouldDeliverNow(settings.Frequency, now, settings.LastProcessedAtUtc))
            {
                continue;
            }

            var deliveredThisWeek = await _db.ProactiveMatchSuggestions.CountAsync(
                s => s.CandidateUserId == settings.UserId && s.DeliveredAtUtc >= weekStart,
                cancellationToken);
            if (deliveredThisWeek >= maxPerWeek)
            {
                continue;
            }

            var context = await _matches.TryLoadForUserIdAsync(settings.UserId, cancellationToken);
            if (context is null)
            {
                continue;
            }

            var priorSuggestions = await _db.ProactiveMatchSuggestions.AsNoTracking()
                .Where(s => s.CandidateUserId == settings.UserId)
                .Select(s => new
                {
                    s.VacancyId,
                    s.ExternalVacancyId,
                    s.Title,
                    s.CompanyLabel,
                    s.DeliveredAtUtc,
                    s.Feedback
                })
                .ToListAsync(cancellationToken);
            var deliveryHistory = priorSuggestions
                .Select(s => new ProactiveDeliveryHistoryEntry(
                    s.Title,
                    s.CompanyLabel,
                    s.DeliveredAtUtc,
                    s.Feedback))
                .ToList();
            var knownVacancyIds = priorSuggestions
                .Where(a => a.VacancyId is not null)
                .Select(a => a.VacancyId!.Value)
                .ToHashSet();
            var knownExternalIds = priorSuggestions
                .Where(a => a.ExternalVacancyId is not null)
                .Select(a => a.ExternalVacancyId!.Value)
                .ToHashSet();
            var cooldownDays = ProactiveMatchingRules.ClampSameVacancyCooldownDays(_options.SameVacancyCooldownDays);

            var dislikeCodes = await LoadDislikeCodesAsync(settings.UserId, cancellationToken);
            var batch = activeVacancies
                .Where(v => !knownVacancyIds.Contains(v.Id))
                .Where(v => !ProactiveMatchingRules.ShouldSkipProactiveDelivery(
                    deliveryHistory,
                    v.Title,
                    v.CompanyName,
                    now,
                    cooldownDays))
                .Select(v => (v, (int?)null))
                .ToList();

            if (batch.Count == 0 && (!externalOn || knownExternalIds.Count > 0))
            {
                await TouchProcessedAsync(settings.UserId, cancellationToken);
                continue;
            }

            var scores = batch.Count > 0
                ? await _matches.ScoreAsync(context, batch.Select(b => (b.v, b.Item2)), cancellationToken)
                : new Dictionary<Guid, ProfileVacancyMatch>();

            var candidates = scores
                .Where(kv => kv.Value.Core.LegalEligible && kv.Value.TotalPercent >= minScore)
                .Select(kv =>
                {
                    var record = batch.First(b => b.v.Id == kv.Key).v;
                    return (Record: record, Match: kv.Value);
                })
                .ToList();

            candidates = DislikeMatchRules.DownRankNightShifts(
                candidates,
                dislikeCodes,
                c => c.Record.LegalNightShift23To06 == true).ToList();

            var remaining = maxPerWeek - deliveredThisWeek;
            foreach (var item in candidates.Take(remaining))
            {
                if (item.Match.TotalPercent < minScore)
                {
                    continue;
                }

                if (ProactiveMatchingRules.ShouldSkipProactiveDelivery(
                        deliveryHistory,
                        item.Record.Title,
                        item.Record.CompanyName,
                        now,
                        cooldownDays))
                {
                    continue;
                }

                var rationale = ProactiveMatchingRules.FormatProactiveMatchReason(item.Match);

                await DeliverAsync(
                    settings.UserId,
                    item.Record.Id,
                    null,
                    item.Record.Title,
                    item.Record.CompanyName,
                    item.Match.TotalPercent,
                    rationale,
                    cancellationToken);
                deliveryHistory.Add(new ProactiveDeliveryHistoryEntry(
                    item.Record.Title,
                    item.Record.CompanyName,
                    now,
                    null));
                sent++;
            }

            if (externalOn && sent < remaining)
            {
                var externals = await _db.CandidateExternalVacancies.AsNoTracking()
                    .Where(e => e.CandidateUserId == settings.UserId
                                && e.Status == CandidateExternalVacancyStatus.Saved
                                && !knownExternalIds.Contains(e.Id))
                    .OrderByDescending(e => e.SavedAtUtc)
                    .Take(remaining)
                    .ToListAsync(cancellationToken);

                foreach (var ext in externals)
                {
                    if (sent >= maxPerWeek)
                    {
                        break;
                    }

                    if (ProactiveMatchingRules.ShouldSkipProactiveDelivery(
                            deliveryHistory,
                            ext.Title,
                            ext.CompanyName,
                            now,
                            cooldownDays))
                    {
                        continue;
                    }

                    var rationale = "Deze externe vacature sluit aan bij wat je eerder hebt opgeslagen.";
                    await DeliverAsync(
                        settings.UserId,
                        ext.LinkedVacancyId,
                        ext.Id,
                        ext.Title,
                        ext.CompanyName,
                        minScore,
                        rationale,
                        cancellationToken);
                    deliveryHistory.Add(new ProactiveDeliveryHistoryEntry(
                        ext.Title,
                        ext.CompanyName,
                        now,
                        null));
                    sent++;
                }
            }

            await TouchProcessedAsync(settings.UserId, cancellationToken);
        }

        if (sent > 0)
        {
            _logger.LogInformation("Proactive matching delivered {Count} suggestions.", sent);
        }

        return sent;
    }

    public async Task<int> RunInstantMatchingForVacancyAsync(
        Guid vacancyId,
        CancellationToken cancellationToken = default)
    {
        if (!await _flags.IsEnabledAsync(PlatformFeature.ProactiveMatching, cancellationToken))
        {
            return 0;
        }

        var vacancy = await _db.Vacancies.AsNoTracking()
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == vacancyId, cancellationToken);
        if (vacancy is null || vacancy.Status != VacancyStatus.Active)
        {
            return 0;
        }

        var record = VacancyDiscoveryIndex.ToRecord(vacancy);
        var minScore = ProactiveMatchingRules.ClampMinScore(_options.MinScorePercent);
        var maxPerWeek = ProactiveMatchingRules.ClampMaxPerWeek(_options.MaxNotificationsPerWeek);
        var weekStart = ProactiveMatchingRules.WeekStartUtc(DateTime.UtcNow);
        var now = DateTime.UtcNow;
        var cooldownDays = ProactiveMatchingRules.ClampSameVacancyCooldownDays(_options.SameVacancyCooldownDays);

        var settingsRows = await _db.CandidateProactiveMatchingSettings.AsNoTracking()
            .Where(s => s.Enabled)
            .ToListAsync(cancellationToken);
        if (settingsRows.Count == 0)
        {
            return 0;
        }

        var userIds = settingsRows.Select(s => s.UserId).Distinct().ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
        var userById = users.ToDictionary(u => u.Id);
        var eligibleSettings = settingsRows
            .Where(s =>
            {
                if (!ProactiveMatchingRules.HasValidConsent(s.ConsentAcceptedAtUtc, s.ConsentVersion))
                {
                    return false;
                }

                if (s.PausedUntilUtc is DateTime pause && pause > now)
                {
                    return false;
                }

                return userById.TryGetValue(s.UserId, out var user)
                       && ProactiveTalentAvailabilityRules.IsEligibleForEmployerShortlist(user);
            })
            .ToList();

        if (eligibleSettings.Count == 0)
        {
            return 0;
        }

        var sent = 0;
        var batch = new List<(VacancyDiscoveryRecord Record, int? TravelMinutes)> { (record, null) };

        foreach (var settings in eligibleSettings)
        {
            var deliveredThisWeek = await _db.ProactiveMatchSuggestions.CountAsync(
                s => s.CandidateUserId == settings.UserId && s.DeliveredAtUtc >= weekStart,
                cancellationToken);
            if (deliveredThisWeek >= maxPerWeek)
            {
                continue;
            }

            var priorSuggestions = await _db.ProactiveMatchSuggestions.AsNoTracking()
                .Where(s => s.CandidateUserId == settings.UserId)
                .Select(s => new
                {
                    s.VacancyId,
                    s.Title,
                    s.CompanyLabel,
                    s.DeliveredAtUtc,
                    s.Feedback
                })
                .ToListAsync(cancellationToken);

            if (priorSuggestions.Any(s => s.VacancyId == vacancyId))
            {
                continue;
            }

            var deliveryHistory = priorSuggestions
                .Select(s => new ProactiveDeliveryHistoryEntry(
                    s.Title,
                    s.CompanyLabel,
                    s.DeliveredAtUtc,
                    s.Feedback))
                .ToList();

            if (ProactiveMatchingRules.ShouldSkipProactiveDelivery(
                    deliveryHistory,
                    record.Title,
                    record.CompanyName,
                    now,
                    cooldownDays))
            {
                continue;
            }

            var context = await _matches.TryLoadForUserIdAsync(settings.UserId, cancellationToken);
            if (context is null)
            {
                continue;
            }

            var scores = await _matches.ScoreAsync(context, batch, cancellationToken);
            if (!scores.TryGetValue(vacancyId, out var match)
                || !match.Core.LegalEligible
                || match.TotalPercent < minScore)
            {
                continue;
            }

            var dislikeCodes = await LoadDislikeCodesAsync(settings.UserId, cancellationToken);
            var downranked = DislikeMatchRules.DownRankNightShifts(
                [(Record: record, Match: match)],
                dislikeCodes,
                c => c.Record.LegalNightShift23To06 == true).ToList();
            if (downranked.Count == 0 || downranked[0].Match.TotalPercent < minScore)
            {
                continue;
            }

            match = downranked[0].Match;
            var rationale = ProactiveMatchingRules.FormatProactiveMatchReason(match);

            await DeliverAsync(
                settings.UserId,
                vacancyId,
                null,
                record.Title,
                record.CompanyName,
                match.TotalPercent,
                rationale,
                cancellationToken);
            sent++;
        }

        if (sent > 0)
        {
            _logger.LogInformation(
                "Instant proactive matching for vacancy {VacancyId} delivered {Count} suggestions.",
                vacancyId,
                sent);
        }

        return sent;
    }

    private async Task DeliverAsync(
        Guid candidateUserId,
        Guid? vacancyId,
        Guid? externalVacancyId,
        string title,
        string? companyLabel,
        int matchPercent,
        string rationale,
        CancellationToken cancellationToken)
    {
        var suggestion = new ProactiveMatchSuggestion
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidateUserId,
            VacancyId = vacancyId,
            ExternalVacancyId = externalVacancyId,
            MatchPercent = matchPercent,
            Rationale = rationale,
            Title = title,
            CompanyLabel = companyLabel,
            DeliveredAtUtc = DateTime.UtcNow
        };
        _db.ProactiveMatchSuggestions.Add(suggestion);

        var displayTitle = ProactiveMatchingRules.NormalizeVacancyTitle(title);
        var company = string.IsNullOrWhiteSpace(companyLabel) ? "werkgever" : companyLabel.Trim();
        var notification = await _notifications.CreateAsync(
            new NotificationCreateRequest(
                candidateUserId,
                ProactiveMatchingRules.FormatInAppNewMatchTitle(displayTitle, company, matchPercent),
                rationale,
                "ProactiveMatch",
                DeepLink: "/candidate/proactive-matching",
                ActionLabel: "Bekijk match",
                ActionUrl: "/candidate/proactive-matching",
                RelatedEntityType: nameof(ProactiveMatchSuggestion),
                RelatedEntityId: suggestion.Id),
            cancellationToken);
        suggestion.NotificationId = notification.Id;
        await _db.SaveChangesAsync(cancellationToken);

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == candidateUserId, cancellationToken);
        if (user?.Email is { Length: > 0 } email)
        {
            try
            {
                var mail = TransactionalEmails.ProactiveMatchSuggestion(
                    title,
                    companyLabel ?? "Werkgever",
                    rationale,
                    matchPercent);
                await _mailer.SendAsync(mail, email, cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Proactive match e-mail failed for {UserId}", candidateUserId);
            }
        }
    }

    private async Task TouchProcessedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tracked = await _db.CandidateProactiveMatchingSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (tracked is null)
        {
            return;
        }

        tracked.LastProcessedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyDislikeFeedbackAsync(
        Guid userId,
        Guid vacancyId,
        CancellationToken cancellationToken)
    {
        var record = (await _discovery.GetActiveAsync(cancellationToken))
            .FirstOrDefault(v => v.Id == vacancyId);
        if (record?.LegalNightShift23To06 != true)
        {
            return;
        }

        var prefs = await _db.CandidatePrivatePreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (prefs is null)
        {
            prefs = new CandidatePrivatePreferences { UserId = userId, UpdatedAtUtc = DateTime.UtcNow };
            _db.CandidatePrivatePreferences.Add(prefs);
        }

        var codes = ParseStringArray(prefs.DislikesJson);
        if (!codes.Contains(DislikeMatchRules.NightShifts, StringComparer.OrdinalIgnoreCase))
        {
            codes.Add(DislikeMatchRules.NightShifts);
            prefs.DislikesJson = JsonSerializer.Serialize(codes);
            prefs.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<IReadOnlyList<string>> LoadDislikeCodesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var json = await _db.CandidatePrivatePreferences.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.DislikesJson)
            .FirstOrDefaultAsync(cancellationToken);
        return ParseStringArray(json);
    }

    private static List<string> ParseStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private ProactiveMatchingSettingsDto MapSettings(CandidateProactiveMatchingSettings? row)
    {
        var minScore = ProactiveMatchingRules.ClampMinScore(_options.MinScorePercent);
        var maxPerWeek = ProactiveMatchingRules.ClampMaxPerWeek(_options.MaxNotificationsPerWeek);
        if (row is null)
        {
            var cooldown = ProactiveMatchingRules.ClampSameVacancyCooldownDays(_options.SameVacancyCooldownDays);
            return new ProactiveMatchingSettingsDto(
                false,
                ProactiveMatchingFrequency.Daily,
                null,
                false,
                null,
                minScore,
                maxPerWeek,
                cooldown);
        }

        return new ProactiveMatchingSettingsDto(
            row.Enabled,
            row.Frequency,
            row.PausedUntilUtc,
            ProactiveMatchingRules.HasValidConsent(row.ConsentAcceptedAtUtc, row.ConsentVersion),
            row.ConsentAcceptedAtUtc,
            minScore,
            maxPerWeek,
            ProactiveMatchingRules.ClampSameVacancyCooldownDays(_options.SameVacancyCooldownDays));
    }

    private static ProactiveMatchSuggestionDto MapSuggestion(ProactiveMatchSuggestion row)
        => new(
            row.Id,
            row.VacancyId,
            row.ExternalVacancyId,
            row.MatchPercent,
            row.Rationale,
            row.Title,
            row.CompanyLabel,
            row.DeliveredAtUtc,
            row.Feedback,
            1,
            null);

    private async Task<IReadOnlyList<ProactiveMatchSuggestionDto>> EnrichRationalesAsync(
        Guid candidateUserId,
        IReadOnlyList<ProactiveMatchSuggestionDto> suggestions,
        CancellationToken cancellationToken)
    {
        var vacancyIds = suggestions
            .Where(s => s.VacancyId is Guid)
            .Select(s => s.VacancyId!.Value)
            .Distinct()
            .ToList();
        if (vacancyIds.Count == 0)
        {
            return suggestions;
        }

        var context = await _matches.TryLoadForUserIdAsync(candidateUserId, cancellationToken);
        if (context is null)
        {
            return suggestions;
        }

        var active = await _discovery.GetActiveAsync(cancellationToken);
        var batch = active
            .Where(v => vacancyIds.Contains(v.Id))
            .Select(v => (v, (int?)null))
            .ToList();
        if (batch.Count == 0)
        {
            return suggestions;
        }

        var scores = await _matches.ScoreAsync(context, batch, cancellationToken);
        var enriched = new List<ProactiveMatchSuggestionDto>(suggestions.Count);
        foreach (var tip in suggestions)
        {
            if (tip.VacancyId is not Guid vid || !scores.TryGetValue(vid, out var match))
            {
                enriched.Add(tip with { Title = ProactiveMatchingRules.NormalizeVacancyTitle(tip.Title) });
                continue;
            }

            var rationale = tip.Rationale;
            var highlights = ProactiveMatchingRules.ProactiveMatchHighlightLabels(match);
            if (ProactiveMatchingRules.IsGenericStoredRationale(rationale))
            {
                rationale = ProactiveMatchingRules.FormatProactiveMatchReason(match);
            }

            enriched.Add(tip with
            {
                Title = ProactiveMatchingRules.NormalizeVacancyTitle(tip.Title),
                MatchPercent = Math.Max(tip.MatchPercent, match.TotalPercent),
                Rationale = rationale,
                MatchHighlights = highlights.Count > 0 ? highlights : null
            });
        }

        return enriched;
    }

    private static List<ProactiveMatchSuggestionDto> DedupeSuggestions(
        IReadOnlyList<ProactiveMatchSuggestionDto> items,
        int take)
    {
        return items
            .GroupBy(s => ProactiveMatchingRules.NormalizeVacancyTitle(s.Title), StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var best = g.OrderByDescending(x => x.MatchPercent)
                    .ThenByDescending(x => x.DeliveredAtUtc)
                    .First();
                var count = g.Count();
                return best with
                {
                    Title = ProactiveMatchingRules.NormalizeVacancyTitle(best.Title),
                    SimilarVacancyCount = count
                };
            })
            .OrderByDescending(x => x.MatchPercent)
            .ThenByDescending(x => x.DeliveredAtUtc)
            .Take(take)
            .ToList();
    }

}
