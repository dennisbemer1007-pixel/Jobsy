using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Exceptions;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class VacancyProductService : IVacancyProductService
{
    private readonly JobsyDbContext _db;
    private readonly ITokenLedgerService _tokens;
    private readonly IPushNotificationService _push;
    private readonly IEmailService _email;
    private readonly IPlatformFeatureService _features;
    private readonly IRoutingService _routing;
    private readonly ILogger<VacancyProductService> _logger;

    public VacancyProductService(
        JobsyDbContext db,
        ITokenLedgerService tokens,
        IPushNotificationService push,
        IEmailService email,
        IPlatformFeatureService features,
        IRoutingService routing,
        ILogger<VacancyProductService> logger)
    {
        _db = db;
        _tokens = tokens;
        _push = push;
        _email = email;
        _features = features;
        _routing = routing;
        _logger = logger;
    }

    public async Task<VacancyProductOutcome> PublishAsync(
        Vacancy vacancy,
        VacancyPublishOptions options,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (vacancy.Status == VacancyStatus.PendingApproval)
        {
            return Fail(
                vacancy,
                "Vacature wacht al op goedkeuring. Alleen een bedrijfsmanager kan publiceren goedkeuren.");
        }

        if (vacancy.Status != VacancyStatus.Draft)
        {
            return Fail(vacancy, "Alleen conceptvacatures kunnen worden gepubliceerd.");
        }

        var reasons = BuildPublishReasons(options);
        Dictionary<TokenSpendReason, decimal>? costOverrides = null;
        List<User>? pushBomCandidates = null;

        if (options.PushBom)
        {
            var reach = await BuildPushBomReachAsync(vacancy, cancellationToken);
            if (reach.Candidates.Count == 0)
            {
                return Fail(
                    vacancy,
                    $"Geen geschikte OpenForWork-kandidaten binnen {reach.RadiusKm} km / {reach.MaxTravelMinutes} min — publiceer zonder PushBom of probeer later.");
            }

            if (!reach.HasPricing)
            {
                return Fail(vacancy, "Geen PushBom-tokenprijs geconfigureerd voor dit bereik.");
            }

            pushBomCandidates = reach.Candidates;
            costOverrides = new Dictionary<TokenSpendReason, decimal>
            {
                [TokenSpendReason.PushBom] = reach.CostTokens
            };
        }

        var costs = await _tokens.GetCostsAsync(
            reasons.Where(r => costOverrides is null || !costOverrides.ContainsKey(r)),
            cancellationToken);
        foreach (var reason in reasons)
        {
            if (costOverrides is not null && costOverrides.ContainsKey(reason))
            {
                continue;
            }

            if (!costs.ContainsKey(reason))
            {
                return Fail(vacancy, $"Geen actieve tokenkost geconfigureerd voor {reason}.");
            }
        }

        decimal CostOf(TokenSpendReason reason) =>
            costOverrides is not null && costOverrides.TryGetValue(reason, out var o)
                ? o
                : costs[reason];

        var publishCost = CostOf(TokenSpendReason.Publish);
        var totalCost = reasons.Sum(CostOf);
        var balance = await _tokens.GetBalanceAsync(vacancy.CompanyId, cancellationToken);

        if (balance < publishCost)
        {
            return await MarkPendingApprovalAsync(vacancy, options, cancellationToken);
        }

        if (balance < totalCost)
        {
            return Fail(
                vacancy,
                $"Onvoldoende tokens voor geselecteerde opties. Benodigd: {totalCost}, saldo: {balance}.");
        }

        TokenMultiSpendOutcome spend;
        try
        {
            spend = await _tokens.TrySpendManyAsync(
                vacancy.CompanyId,
                reasons,
                vacancyId: vacancy.Id,
                actorUserId: actorUserId,
                branchCompanyId: vacancy.CompanyId,
                note: "Publish",
                onSuccessBeforeCommit: async ct =>
                {
                    await _db.Entry(vacancy).ReloadAsync(ct);
                    if (vacancy.Status != VacancyStatus.Draft)
                    {
                        throw new VacancyProductConflictException(
                            "Vacature is ondertussen al verwerkt en kan niet meer worden gepubliceerd.");
                    }

                    ApplyPublishEffects(vacancy, options);
                    ClearRequestedOptions(vacancy);
                },
                costOverrides: costOverrides,
                cancellationToken);
        }
        catch (VacancyProductConflictException ex)
        {
            return Fail(vacancy, ex.Message);
        }

        if (!spend.Succeeded)
        {
            if (spend.ErrorMessage?.Contains("Onvoldoende", StringComparison.OrdinalIgnoreCase) == true
                && spend.Balance < publishCost)
            {
                return await MarkPendingApprovalAsync(vacancy, options, cancellationToken);
            }

            return Fail(vacancy, spend.ErrorMessage ?? "Tokenafschrijving mislukt.");
        }

        var recipientCount = 0;
        if (options.PushBom && pushBomCandidates is not null)
        {
            recipientCount = await DeliverPushBomToAsync(vacancy, pushBomCandidates, cancellationToken);
        }

        return new VacancyProductOutcome(true, null, vacancy, PushBomRecipientCount: recipientCount);
    }

    public async Task<VacancyProductOutcome> ApprovePublishAsync(
        Vacancy vacancy,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (vacancy.Status != VacancyStatus.PendingApproval)
        {
            return Fail(vacancy, "Alleen vacatures met status PendingApproval kunnen worden goedgekeurd.");
        }

        var options = new VacancyPublishOptions(
            vacancy.RequestedHighlight,
            vacancy.RequestedPushBom,
            vacancy.RequestedExtend);

        List<User>? pushBomCandidates = null;
        Dictionary<TokenSpendReason, decimal>? costOverrides = null;

        if (options.PushBom)
        {
            var reach = await BuildPushBomReachAsync(vacancy, cancellationToken);
            if (reach.Candidates.Count == 0)
            {
                // Don't block approval — drop unpaid PushBom so tokens aren't wasted.
                options = options with { PushBom = false };
                _logger.LogWarning(
                    "Approve publish {VacancyId}: PushBom skipped (no candidates within reach)",
                    vacancy.Id);
            }
            else if (!reach.HasPricing)
            {
                options = options with { PushBom = false };
                _logger.LogWarning(
                    "Approve publish {VacancyId}: PushBom skipped (no pricing for {Count} candidates)",
                    vacancy.Id,
                    reach.Candidates.Count);
            }
            else
            {
                pushBomCandidates = reach.Candidates;
                costOverrides = new Dictionary<TokenSpendReason, decimal>
                {
                    [TokenSpendReason.PushBom] = reach.CostTokens
                };
            }
        }

        // Snapshot for spend + effects (do not re-read Requested* after Reload).
        var approveOptions = options;
        var reasons = BuildPublishReasons(approveOptions);

        TokenMultiSpendOutcome spend;
        try
        {
            spend = await _tokens.TrySpendManyAsync(
                vacancy.CompanyId,
                reasons,
                vacancyId: vacancy.Id,
                actorUserId: actorUserId,
                branchCompanyId: vacancy.CompanyId,
                note: "Approve publish",
                onSuccessBeforeCommit: async ct =>
                {
                    await _db.Entry(vacancy).ReloadAsync(ct);
                    if (vacancy.Status != VacancyStatus.PendingApproval)
                    {
                        throw new VacancyProductConflictException(
                            "Vacature is ondertussen al verwerkt.");
                    }

                    ApplyPublishEffects(vacancy, approveOptions);
                    ClearRequestedOptions(vacancy);
                },
                costOverrides: costOverrides,
                cancellationToken);
        }
        catch (VacancyProductConflictException ex)
        {
            return Fail(vacancy, ex.Message);
        }

        if (!spend.Succeeded)
        {
            return Fail(vacancy, spend.ErrorMessage ?? "Goedkeuring mislukt.");
        }

        var recipientCount = 0;
        if (approveOptions.PushBom && pushBomCandidates is not null)
        {
            recipientCount = await DeliverPushBomToAsync(vacancy, pushBomCandidates, cancellationToken);
        }

        return new VacancyProductOutcome(true, null, vacancy, PushBomRecipientCount: recipientCount);
    }

    public async Task<VacancyProductOutcome> HighlightAsync(
        Vacancy vacancy,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (vacancy.Status != VacancyStatus.Active)
        {
            return Fail(vacancy, "Alleen actieve vacatures kunnen worden gehighlight.");
        }

        if (vacancy.IsHighlighted)
        {
            return Fail(vacancy, "Vacature is al gehighlight.");
        }

        TokenSpendOutcome spend;
        try
        {
            spend = await _tokens.TrySpendAsync(
                vacancy.CompanyId,
                TokenSpendReason.Highlight,
                vacancyId: vacancy.Id,
                actorUserId: actorUserId,
                branchCompanyId: vacancy.CompanyId,
                note: "Highlight",
                onSuccessBeforeCommit: async ct =>
                {
                    await _db.Entry(vacancy).ReloadAsync(ct);
                    if (vacancy.Status != VacancyStatus.Active || vacancy.IsHighlighted)
                    {
                        throw new VacancyProductConflictException(
                            "Vacature kan niet meer worden gehighlight.");
                    }

                    vacancy.IsHighlighted = true;
                },
                cancellationToken: cancellationToken);
        }
        catch (VacancyProductConflictException ex)
        {
            return Fail(vacancy, ex.Message);
        }

        return spend.Succeeded
            ? new VacancyProductOutcome(true, null, vacancy)
            : Fail(vacancy, spend.ErrorMessage ?? "Highlight mislukt.");
    }

    public async Task<PushBomPreview> PreviewPushBomAsync(
        Vacancy vacancy,
        CancellationToken cancellationToken = default)
    {
        var reach = await BuildPushBomReachAsync(vacancy, cancellationToken);
        return new PushBomPreview(
            reach.Candidates.Count,
            reach.CostTokens,
            reach.RadiusKm,
            reach.MaxTravelMinutes,
            reach.HasPricing);
    }

    public async Task<VacancyProductOutcome> PushBomAsync(
        Vacancy vacancy,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (vacancy.Status != VacancyStatus.Active)
        {
            return Fail(vacancy, "PushBom is alleen beschikbaar voor actieve vacatures.");
        }

        var reach = await BuildPushBomReachAsync(vacancy, cancellationToken);
        if (reach.Candidates.Count == 0)
        {
            return Fail(
                vacancy,
                $"Geen geschikte OpenForWork-kandidaten binnen {reach.RadiusKm} km / {reach.MaxTravelMinutes} min — geen tokens afgeschreven.");
        }

        if (!reach.HasPricing)
        {
            return Fail(vacancy, "Geen PushBom-tokenprijs geconfigureerd voor dit bereik.");
        }

        var costOverrides = new Dictionary<TokenSpendReason, decimal>
        {
            [TokenSpendReason.PushBom] = reach.CostTokens
        };

        TokenSpendOutcome spend;
        try
        {
            spend = await _tokens.TrySpendAsync(
                vacancy.CompanyId,
                TokenSpendReason.PushBom,
                vacancyId: vacancy.Id,
                actorUserId: actorUserId,
                branchCompanyId: vacancy.CompanyId,
                note: $"PushBom ({reach.Candidates.Count} kandidaten)",
                onSuccessBeforeCommit: async ct =>
                {
                    await _db.Entry(vacancy).ReloadAsync(ct);
                    if (vacancy.Status != VacancyStatus.Active)
                    {
                        throw new VacancyProductConflictException(
                            "Vacature is niet meer actief voor PushBom.");
                    }
                },
                costOverrides: costOverrides,
                cancellationToken: cancellationToken);
        }
        catch (VacancyProductConflictException ex)
        {
            return Fail(vacancy, ex.Message);
        }

        if (!spend.Succeeded)
        {
            return Fail(vacancy, spend.ErrorMessage ?? "PushBom mislukt.");
        }

        var recipientCount = await DeliverPushBomToAsync(vacancy, reach.Candidates, cancellationToken);
        return new VacancyProductOutcome(true, null, vacancy, PushBomRecipientCount: recipientCount);
    }

    public async Task<VacancyProductOutcome> ExtendAsync(
        Vacancy vacancy,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (vacancy.Status is not (VacancyStatus.Active or VacancyStatus.Archived))
        {
            return Fail(vacancy, "Alleen actieve of inactieve vacatures kunnen worden verlengd.");
        }

        TokenSpendOutcome spend;
        try
        {
            spend = await _tokens.TrySpendAsync(
                vacancy.CompanyId,
                TokenSpendReason.Extend,
                vacancyId: vacancy.Id,
                actorUserId: actorUserId,
                branchCompanyId: vacancy.CompanyId,
                note: $"Extend +{VacancyProductRules.ExtendDays}d",
                onSuccessBeforeCommit: async ct =>
                {
                    await _db.Entry(vacancy).ReloadAsync(ct);
                    if (vacancy.Status is not (VacancyStatus.Active or VacancyStatus.Archived))
                    {
                        throw new VacancyProductConflictException(
                            "Vacature kan niet meer worden verlengd.");
                    }

                    ApplyExtend(vacancy);
                },
                cancellationToken: cancellationToken);
        }
        catch (VacancyProductConflictException ex)
        {
            return Fail(vacancy, ex.Message);
        }

        return spend.Succeeded
            ? new VacancyProductOutcome(true, null, vacancy)
            : Fail(vacancy, spend.ErrorMessage ?? "Verlengen mislukt.");
    }

    public async Task<VacancyProductOutcome> DeactivateAsync(
        Vacancy vacancy,
        CancellationToken cancellationToken = default)
    {
        await _db.Entry(vacancy).ReloadAsync(cancellationToken);
        if (vacancy.Status != VacancyStatus.Active)
        {
            return Fail(vacancy, "Alleen actieve vacatures kunnen inactief worden gemaakt.");
        }

        vacancy.Status = VacancyStatus.Archived;
        vacancy.ClosedAtUtc ??= DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return new VacancyProductOutcome(true, null, vacancy);
    }

    private async Task<VacancyProductOutcome> MarkPendingApprovalAsync(
        Vacancy vacancy,
        VacancyPublishOptions options,
        CancellationToken cancellationToken)
    {
        await _db.Entry(vacancy).ReloadAsync(cancellationToken);
        if (vacancy.Status is VacancyStatus.Active or VacancyStatus.Archived)
        {
            return Fail(vacancy, "Vacature is ondertussen al verwerkt.");
        }

        if (vacancy.Status == VacancyStatus.PendingApproval)
        {
            // Already pending — refresh requested options, do not re-notify.
            vacancy.RequestedHighlight = options.Highlight;
            vacancy.RequestedPushBom = options.PushBom;
            vacancy.RequestedExtend = options.Extend;
            await _db.SaveChangesAsync(cancellationToken);
            return new VacancyProductOutcome(
                true,
                "Publicatieaanvraag wacht al op goedkeuring van de bedrijfsmanager.",
                vacancy,
                PendingApproval: true);
        }

        if (vacancy.Status != VacancyStatus.Draft)
        {
            return Fail(vacancy, "Alleen conceptvacatures kunnen wachten op goedkeuring.");
        }

        vacancy.Status = VacancyStatus.PendingApproval;
        vacancy.RequestedHighlight = options.Highlight;
        vacancy.RequestedPushBom = options.PushBom;
        vacancy.RequestedExtend = options.Extend;
        await _db.SaveChangesAsync(cancellationToken);
        await NotifyManagersPendingApprovalAsync(vacancy, cancellationToken);

        return new VacancyProductOutcome(
            true,
            "Onvoldoende tokens — publicatieaanvraag wacht op goedkeuring van de bedrijfsmanager.",
            vacancy,
            PendingApproval: true);
    }

    private static List<TokenSpendReason> BuildPublishReasons(VacancyPublishOptions options)
    {
        var reasons = new List<TokenSpendReason> { TokenSpendReason.Publish };
        if (options.Highlight)
        {
            reasons.Add(TokenSpendReason.Highlight);
        }

        if (options.PushBom)
        {
            reasons.Add(TokenSpendReason.PushBom);
        }

        if (options.Extend)
        {
            reasons.Add(TokenSpendReason.Extend);
        }

        return reasons;
    }

    private static void ApplyPublishEffects(Vacancy vacancy, VacancyPublishOptions options)
    {
        vacancy.Status = VacancyStatus.Active;
        vacancy.PublishedAtUtc ??= DateTime.UtcNow;
        if (options.Highlight)
        {
            vacancy.IsHighlighted = true;
        }

        if (options.Extend)
        {
            ApplyExtend(vacancy);
        }
    }

    private static void ApplyExtend(Vacancy vacancy)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var baseEnd = vacancy.EndDate < today ? today : vacancy.EndDate;
        vacancy.EndDate = baseEnd.AddDays(VacancyProductRules.ExtendDays);
        vacancy.ExtensionCount += 1;
        if (vacancy.Status == VacancyStatus.Archived)
        {
            vacancy.Status = VacancyStatus.Active;
        }
    }

    private static void ClearRequestedOptions(Vacancy vacancy)
    {
        vacancy.RequestedHighlight = false;
        vacancy.RequestedPushBom = false;
        vacancy.RequestedExtend = false;
    }

    private async Task<PushBomReach> BuildPushBomReachAsync(
        Vacancy vacancy,
        CancellationToken cancellationToken)
    {
        var settings = await GetPushBomSettingsAsync(cancellationToken);
        var withinRadius = await FindOpenForWorkWithinRadiusAsync(
            vacancy.Location,
            settings.RadiusKm,
            cancellationToken);

        // Crow-flies bound before routing (same approach as Discover).
        var shortlist = withinRadius
            .Where(c => c.HomeLocation is not null)
            .Where(c =>
            {
                var mode = ResolveTransportMode(c.PreferencesJson, vacancy.RequiredTransport);
                var reachKm = TravelReach.MaxCrowFliesKm(mode, settings.MaxTravelMinutes, settings.RadiusKm);
                return GeoDistance.IsWithinKm(vacancy.Location, c.HomeLocation!, reachKm);
            })
            .ToList();

        var routed = await Task.WhenAll(shortlist.Select(async candidate =>
        {
            var mode = ResolveTransportMode(candidate.PreferencesJson, vacancy.RequiredTransport);
            var route = await _routing.GetRouteAsync(
                candidate.HomeLocation!.Latitude,
                candidate.HomeLocation.Longitude,
                vacancy.Location.Latitude,
                vacancy.Location.Longitude,
                mode,
                cancellationToken);

            var travelMinutes = (int)Math.Ceiling(route.DurationSeconds / 60.0);
            return (candidate, travelMinutes);
        }));

        var suitable = new List<User>();
        foreach (var (candidate, travelMinutes) in routed)
        {
            if (travelMinutes > settings.MaxTravelMinutes)
            {
                continue;
            }

            // Respect candidate preference when set (must also be able/willing to travel this far).
            var preferredMax = TryReadMaxTravelMinutes(candidate.PreferencesJson);
            if (preferredMax is int maxPref && travelMinutes > maxPref)
            {
                continue;
            }

            suitable.Add(candidate);
        }

        var tiers = await _db.PushBomPricingTiers.AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);

        var tierCost = PushBomPricingRules.ResolveCost(tiers, suitable.Count);
        decimal costTokens;
        var hasPricing = false;
        if (tierCost is decimal fromTier)
        {
            costTokens = fromTier;
            hasPricing = suitable.Count > 0;
        }
        else if (suitable.Count > 0)
        {
            // Fallback to flat TokenSpendCost when no tier matches.
            var flat = await _tokens.GetCostAsync(TokenSpendReason.PushBom, cancellationToken);
            if (flat is decimal flatCost && flatCost > 0)
            {
                costTokens = flatCost;
                hasPricing = true;
            }
            else
            {
                costTokens = 0;
            }
        }
        else
        {
            costTokens = 0;
        }

        return new PushBomReach(
            suitable,
            costTokens,
            settings.RadiusKm,
            settings.MaxTravelMinutes,
            hasPricing);
    }

    private async Task<(double RadiusKm, int MaxTravelMinutes)> GetPushBomSettingsAsync(
        CancellationToken cancellationToken)
    {
        var row = await _db.PushBomSettings.AsNoTracking()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return (VacancyProductRules.PushBomRadiusKm, VacancyProductRules.PushBomMaxTravelMinutes);
        }

        var radius = row.RadiusKm > 0 ? row.RadiusKm : VacancyProductRules.PushBomRadiusKm;
        var minutes = row.MaxTravelMinutes > 0
            ? row.MaxTravelMinutes
            : VacancyProductRules.PushBomMaxTravelMinutes;
        return (radius, minutes);
    }

    private async Task<int> DeliverPushBomToAsync(
        Vacancy vacancy,
        IReadOnlyList<User> candidates,
        CancellationToken cancellationToken)
    {
        var settings = await GetPushBomSettingsAsync(cancellationToken);
        var deepLink = await BuildDeepLinkAsync($"/vacancies/{vacancy.Id}", cancellationToken);
        foreach (var candidate in candidates)
        {
            await _push.SendAsync(
                new PushMessage(
                    candidate.Email,
                    $"Nieuwe vacature bij jou in de buurt: {vacancy.Title}",
                    $"{vacancy.Company?.Name ?? "Werkgever"} zoekt {vacancy.Title}. Bekijk de vacature in Jobsy.",
                    deepLink,
                    "PushBom"),
                cancellationToken);
        }

        _logger.LogInformation(
            "PushBom for vacancy {VacancyId}: notified {Count} OpenForWork candidates within {Km} km / {Min} min",
            vacancy.Id,
            candidates.Count,
            settings.RadiusKm,
            settings.MaxTravelMinutes);

        return candidates.Count;
    }

    private async Task<List<User>> FindOpenForWorkWithinRadiusAsync(
        GeoPoint origin,
        double radiusKm,
        CancellationToken cancellationToken)
    {
        if (_db.Database.IsNpgsql())
        {
            var role = (int)UserRole.Candidate;
            // Use geometry ST_DWithin (degrees) so the existing GIST index on HomeLocation can be used.
            // Crow-flies shortlist only — routing still refines by travel minutes.
            var degrees = radiusKm / 111.32;
            var ids = await _db.Database
                .SqlQueryRaw<Guid>(
                    """
                    SELECT u."Id" AS "Value"
                    FROM "Users" u
                    WHERE u."OpenForWork" = TRUE
                      AND u."IsActive" = TRUE
                      AND u."Role" = {0}
                      AND u."HomeLocation" IS NOT NULL
                      AND ST_DWithin(
                        u."HomeLocation",
                        ST_SetSRID(ST_MakePoint({1}, {2}), 4326),
                        {3})
                    """,
                    role,
                    origin.Longitude,
                    origin.Latitude,
                    degrees)
                .ToListAsync(cancellationToken);

            if (ids.Count == 0)
            {
                return [];
            }

            return await _db.Users
                .AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .ToListAsync(cancellationToken);
        }

        var candidates = await _db.Users
            .AsNoTracking()
            .Where(u =>
                u.Role == UserRole.Candidate
                && u.IsActive
                && u.OpenForWork
                && u.HomeLocation != null)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(u => u.HomeLocation is not null
                        && GeoDistance.IsWithinKm(origin, u.HomeLocation, radiusKm))
            .ToList();
    }

    private static TransportMode ResolveTransportMode(string? preferencesJson, TransportMode vacancyRequired)
    {
        var preferred = TryReadPreferredTransport(preferencesJson);
        if (preferred is not null && preferred != TransportMode.None)
        {
            return preferred.Value;
        }

        if (vacancyRequired != TransportMode.None)
        {
            return vacancyRequired;
        }

        return TransportMode.Bike;
    }

    private static TransportMode? TryReadPreferredTransport(string? preferencesJson)
    {
        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(preferencesJson);
            if (!doc.RootElement.TryGetProperty("preferredTransport", out var el)
                || el.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return TransportLabels.Parse(el.GetString());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static int? TryReadMaxTravelMinutes(string? preferencesJson)
    {
        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(preferencesJson);
            if (doc.RootElement.TryGetProperty("maxTravelMinutes", out var el)
                && el.ValueKind == System.Text.Json.JsonValueKind.Number
                && el.TryGetInt32(out var minutes)
                && minutes > 0)
            {
                return minutes;
            }
        }
        catch (JsonException)
        {
            // ignore malformed prefs
        }

        return null;
    }

    private async Task NotifyManagersPendingApprovalAsync(Vacancy vacancy, CancellationToken cancellationToken)
    {
        var managers = await _db.Users.AsNoTracking()
            .Include(u => u.CompanyMemberships)
            .Where(u =>
                u.IsActive
                && u.Role == UserRole.EnterpriseManager
                && (u.CompanyId == vacancy.CompanyId
                    || u.CompanyMemberships.Any(m => m.CompanyId == vacancy.CompanyId)))
            .ToListAsync(cancellationToken);

        if (managers.Count == 0)
        {
            _logger.LogWarning(
                "PendingApproval for vacancy {VacancyId} but no EnterpriseManager found for company {CompanyId}",
                vacancy.Id,
                vacancy.CompanyId);
        }

        var deepLink = await BuildDeepLinkAsync("/employer/vacancies", cancellationToken);
        var companyName = vacancy.Company?.Name ?? "bedrijf";
        foreach (var manager in managers)
        {
            await _email.SendAsync(
                new EmailMessage(
                    manager.Email,
                    $"Publicatieaanvraag: {vacancy.Title}",
                    $"<p>Vacature <strong>{System.Net.WebUtility.HtmlEncode(vacancy.Title)}</strong> bij {System.Net.WebUtility.HtmlEncode(companyName)} wacht op goedkeuring (onvoldoende tokens).</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(deepLink)}\">Open vacaturebeheer</a></p>",
                    "PendingApproval"),
                cancellationToken);

            await _push.SendAsync(
                new PushMessage(
                    manager.Email,
                    "Publicatieaanvraag wacht op goedkeuring",
                    $"{vacancy.Title} — onvoldoende tokens.",
                    deepLink,
                    "PendingApproval"),
                cancellationToken);
        }
    }

    private async Task<string> BuildDeepLinkAsync(string relativePath, CancellationToken cancellationToken)
    {
        var features = await _features.GetAsync(cancellationToken);
        return features.PublicWebBaseUrl.TrimEnd('/') + relativePath;
    }

    private static VacancyProductOutcome Fail(Vacancy vacancy, string message)
        => new(false, message, vacancy);

    private sealed record PushBomReach(
        List<User> Candidates,
        decimal CostTokens,
        double RadiusKm,
        int MaxTravelMinutes,
        bool HasPricing);
}
