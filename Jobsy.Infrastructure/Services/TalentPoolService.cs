using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Infrastructure.Services;

public sealed class TalentPoolService : ITalentPoolService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly JobsyDbContext _db;
    private readonly ITokenLedgerService _tokens;
    private readonly IRoutingService _routing;
    private readonly IUserNotificationService _notifications;
    private readonly IFlexCommercialService _commercial;
    private readonly IFeatureFlags? _flags;
    private readonly IConfiguration? _configuration;

    public TalentPoolService(
        JobsyDbContext db,
        ITokenLedgerService tokens,
        IRoutingService routing,
        IUserNotificationService notifications,
        IFlexCommercialService commercial,
        IFeatureFlags? flags = null,
        IConfiguration? configuration = null)
    {
        _db = db;
        _tokens = tokens;
        _routing = routing;
        _notifications = notifications;
        _commercial = commercial;
        _flags = flags;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<AnonymousTalentCardDto>> SearchAsync(
        Guid companyId,
        TalentPoolSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        // Explicitly reject age-based filtering (equal treatment).
        // Callers must not pass age; the query type has no age fields by design.

        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Bedrijf niet gevonden.");

        var take = Math.Clamp(query.Take <= 0 ? 50 : query.Take, 1, 100);
        var users = await _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Candidate
                        && u.IsActive
                        && u.OpenForWork
                        && u.TalentPoolConsentAt != null)
            .Take(500)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();
        var competencyRows = await _db.CandidateCompetencies.AsNoTracking()
            .Where(c => userIds.Contains(c.UserId) && c.Status == CandidateCompetencyStatuses.Completed)
            .ToListAsync(cancellationToken);
        var competencies = competencyRows
            .GroupBy(c => c.UserId)
            .ToDictionary(g => g.Key, g => g.First());
        var careerRows = await _db.CandidateCareerInterests.AsNoTracking()
            .Where(c => userIds.Contains(c.UserId) && c.Status == CandidateCompetencyStatuses.Completed)
            .ToListAsync(cancellationToken);
        var careers = careerRows
            .GroupBy(c => c.UserId)
            .ToDictionary(g => g.Key, g => g.First());
        var showRiasec = _configuration?.GetValue(TalentPoolRiasecVisibility.ConfigKey, false) ?? false;
        var tagsFilter = query.Tags?
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        // Off: RIASEC codes in the tags query never match. Competency match tags still do.
        var matchFilter = showRiasec || tagsFilter is null
            ? tagsFilter
            : tagsFilter
                .Where(t => !TalentPoolRiasecVisibility.IsRiasecCode(t))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var results = new List<AnonymousTalentCardDto>();
        foreach (var user in users)
        {
            if (!CandidateConsentRules.CanAppearInTalentPool(user))
            {
                continue;
            }

            competencies.TryGetValue(user.Id, out var competency);
            careers.TryGetValue(user.Id, out var career);
            if (competency is null && career is null)
            {
                continue;
            }

            var matchTags = competency is null
                ? new List<string>()
                : CompetencyTestCatalog.ParseTagsJson(competency.MatchTagsJson).ToList();
            var riasec = career is not null
                ? CareerTestCatalog.ParseTagsJson(career.RiasecTagsJson)
                : competency is not null
                    ? CompetencyTestCatalog.ParseTagsJson(competency.RiasecTagsJson)
                    : [];
            if (career is not null)
            {
                foreach (var tag in CareerTestCatalog.ParseTagsJson(career.MatchTagsJson))
                {
                    if (!matchTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    {
                        matchTags.Add(tag);
                    }
                }
            }

            if (tagsFilter is { Count: > 0 })
            {
                var matchesCompetency = matchFilter is { Count: > 0 }
                    && matchTags.Any(t => matchFilter.Contains(t));
                var matchesRiasec = showRiasec && riasec.Any(t => tagsFilter.Contains(t));
                if (!matchesCompetency && !matchesRiasec)
                {
                    continue;
                }
            }

            var prefs = DeserializePrefs(user.PreferencesJson);
            var licenses = prefs.DrivingLicenses?.ToList() ?? [];
            if (!string.IsNullOrWhiteSpace(query.DrivingLicense)
                && !DrivingLicenseLabels.CandidateMeetsRequirement(licenses, query.DrivingLicense))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(query.AvailabilityHint)
                && !AvailabilityMatchesHint(prefs, query.AvailabilityHint))
            {
                continue;
            }

            int? travelMinutes = null;
            if (user.HomeLocation is not null && company.Location is not null)
            {
                var route = await _routing.GetRouteAsync(
                    company.Location.Latitude,
                    company.Location.Longitude,
                    user.HomeLocation.Latitude,
                    user.HomeLocation.Longitude,
                    query.Transport,
                    cancellationToken);
                travelMinutes = (int)Math.Round(route.DurationSeconds / 60.0, MidpointRounding.AwayFromZero);
                if (query.MaxTravelMinutes is int max && travelMinutes > max)
                {
                    continue;
                }
            }
            else if (query.MaxTravelMinutes is not null)
            {
                continue;
            }

            var availability = LobsyCvModelFactory.FormatAvailability(
                prefs.Availability,
                prefs.FlexibleTimes == true);

            results.Add(new AnonymousTalentCardDto(
                user.Id,
                matchTags,
                showRiasec ? riasec : [],
                showRiasec ? career?.HollandCode : null,
                availability,
                licenses,
                travelMinutes,
                LobsyCvModelFactory.ExtractCity(prefs.HomeAddress) ?? "Westland / Den Haag"));

            if (results.Count >= take)
            {
                break;
            }
        }

        return results;
    }

    public async Task<TalentContactRequestDto> UnlockAsync(
        Guid companyId,
        Guid employerUserId,
        Guid candidateUserId,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new InvalidOperationException("Bericht is verplicht om contact te starten.");
        }

        var candidate = await _db.Users
            .FirstOrDefaultAsync(
                u => u.Id == candidateUserId
                     && u.Role == UserRole.Candidate
                     && u.IsActive
                     && u.OpenForWork
                     && u.TalentPoolConsentAt != null,
                cancellationToken)
            ?? throw new InvalidOperationException("Kandidaat niet beschikbaar in de talentpool.");
        if (!CandidateConsentRules.CanAppearInTalentPool(candidate))
        {
            throw new InvalidOperationException("Kandidaat niet beschikbaar in de talentpool.");
        }

        var competencyDone = await _db.CandidateCompetencies.AnyAsync(
            c => c.UserId == candidateUserId && c.Status == CandidateCompetencyStatuses.Completed,
            cancellationToken);
        var careerDone = await _db.CandidateCareerInterests.AnyAsync(
            c => c.UserId == candidateUserId && c.Status == CandidateCompetencyStatuses.Completed,
            cancellationToken);
        if (!competencyDone && !careerDone)
        {
            throw new InvalidOperationException("Kandidaat heeft de competentie- of beroepentest nog niet afgerond.");
        }

        var openExisting = await _db.TalentContactRequests.AnyAsync(
            r => r.CompanyId == companyId
                 && r.CandidateUserId == candidateUserId
                 && (r.Status == TalentContactStatus.Pending
                     || r.Status == TalentContactStatus.RefundEligible
                     || r.Status == TalentContactStatus.ContactShared),
            cancellationToken);
        if (openExisting)
        {
            throw new InvalidOperationException("Er loopt al een contactverzoek voor deze kandidaat.");
        }

        var now = DateTime.UtcNow;
        var request = new TalentContactRequest
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            EmployerUserId = employerUserId,
            CandidateUserId = candidateUserId,
            Status = TalentContactStatus.Pending,
            Message = message.Trim(),
            CreatedAtUtc = now,
            RespondByUtc = TalentContactRules.ComputeRespondByUtc(now)
        };

        TokenTransaction? spendTx = null;
        var commercial = await _commercial.GetAsync(cancellationToken);
        var unlockCost = commercial.ContactUnlockCostTokens;
        var outcome = await _tokens.TrySpendAsync(
            companyId,
            TokenSpendReason.ContactUnlock,
            vacancyId: null,
            actorUserId: employerUserId,
            note: $"ContactUnlock:{request.Id}",
            onSuccessBeforeCommit: async ct =>
            {
                _db.TalentContactRequests.Add(request);
                await _db.SaveChangesAsync(ct);
            },
            costOverrides: new Dictionary<TokenSpendReason, decimal>
            {
                [TokenSpendReason.ContactUnlock] = unlockCost
            },
            cancellationToken: cancellationToken);

        if (!outcome.Succeeded)
        {
            throw new InvalidOperationException(outcome.ErrorMessage ?? "Onvoldoende tokens.");
        }

        spendTx = outcome.Transaction;
        if (spendTx is not null)
        {
            request.SpendTransactionId = spendTx.Id;
            await _db.SaveChangesAsync(cancellationToken);
        }

        // D18: with the Werkgevers gate off the candidate inbox is unreachable, so no ping.
        var employersOn = _flags is null
            || (await _flags.GetAsync(cancellationToken)).EmployersEnabled;
        if (employersOn)
        {
            await _notifications.CreateAsync(
                new NotificationCreateRequest(
                    candidate.Id,
                    "Nieuw contactverzoek van een werkgever",
                    "Een werkgever wil contact via Lobsy. Reageer binnen 48 uur.",
                    "TalentContact",
                    DeepLink: "/candidate/talent-contacts",
                    ActionLabel: "Bekijk verzoek",
                    ActionUrl: "/candidate/talent-contacts",
                    RelatedEntityType: nameof(TalentContactRequest),
                    RelatedEntityId: request.Id),
                cancellationToken);
        }

        return await ToDtoAsync(request.Id, revealPii: false, cancellationToken);
    }

    public async Task<TalentContactRequestDto> CandidateRespondAsync(
        Guid candidateUserId,
        Guid requestId,
        bool accept,
        bool alreadyPlaced,
        CancellationToken cancellationToken = default)
    {
        var request = await _db.TalentContactRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.CandidateUserId == candidateUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Contactverzoek niet gevonden.");

        if (!TalentContactRules.CanCandidateRespond(request.Status))
        {
            throw new InvalidOperationException("Op dit verzoek kun je niet meer reageren.");
        }

        var now = DateTime.UtcNow;
        request.RespondedAtUtc = now;
        if (accept && !alreadyPlaced)
        {
            request.Status = TalentContactStatus.ContactShared;
            request.ContactSharedAtUtc = now;
            request.CandidateDeclineReason = null;
        }
        else
        {
            request.Status = TalentContactStatus.CandidateDeclined;
            request.CandidateDeclineReason = TalentContactDeclineReasons.ForResponse(alreadyPlaced);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await ToDtoAsync(request.Id, revealPii: request.Status == TalentContactStatus.ContactShared, cancellationToken);
    }

    public async Task<TalentContactRequestDto> WithdrawAndRefundAsync(
        Guid companyId,
        Guid employerUserId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await _db.TalentContactRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.CompanyId == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Contactverzoek niet gevonden.");

        if (TalentContactRules.IsRefundBlockedAfterContactShared(request.Status))
        {
            throw new InvalidOperationException(
                "Na succesvolle contactuitwisseling is geen token-refund mogelijk.");
        }

        var now = DateTime.UtcNow;
        if (request.Status == TalentContactStatus.Pending
            && TalentContactRules.IsPastDeadline(now, request.RespondByUtc))
        {
            request.Status = TalentContactStatus.RefundEligible;
        }

        if (!TalentContactRules.CanEmployerWithdraw(request.Status, now, request.RespondByUtc))
        {
            throw new InvalidOperationException(
                "Intrekken met refund kan pas na 48 uur zonder reactie, of wanneer de kandidaat heeft aangegeven reeds voorzien te zijn.");
        }

        if (request.RefundTransactionId is not null
            || request.Status == TalentContactStatus.WithdrawnRefunded)
        {
            return await ToDtoAsync(request.Id, revealPii: false, cancellationToken);
        }

        decimal refundAmount;
        if (request.SpendTransactionId is Guid spendId)
        {
            var spend = await _db.TokenTransactions.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == spendId, cancellationToken);
            refundAmount = spend is null ? 0m : Math.Abs(spend.Amount);
        }
        else
        {
            refundAmount = (await _commercial.GetAsync(cancellationToken)).ContactUnlockCostTokens;
        }

        if (refundAmount <= 0)
        {
            refundAmount = FlexCommercialSettings.DefaultContactUnlockCostTokens;
        }

        var refund = await _tokens.GrantAsync(
            companyId,
            refundAmount,
            employerUserId,
            note: $"ContactUnlockRefund:{request.Id}",
            cancellationToken);

        request.RefundTransactionId = refund.Id;
        request.WithdrawnAtUtc = now;
        request.Status = TalentContactStatus.WithdrawnRefunded;
        await _db.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(request.Id, revealPii: false, cancellationToken);
    }

    public async Task<IReadOnlyList<TalentContactRequestDto>> ListForEmployerAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.TalentContactRequests.AsNoTracking()
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return [];
        }

        var revealIds = rows
            .Where(r => r.Status == TalentContactStatus.ContactShared)
            .Select(r => r.CandidateUserId)
            .Distinct()
            .ToList();

        var candidates = revealIds.Count == 0
            ? new Dictionary<Guid, TalentContactPii>()
            : (await _db.Users.AsNoTracking()
                .Where(u => revealIds.Contains(u.Id))
                .ToListAsync(cancellationToken))
                .ToDictionary(u => u.Id, TalentContactPii.For);

        var companyName = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return rows.Select(request =>
        {
            var reveal = request.Status == TalentContactStatus.ContactShared;
            var pii = reveal && candidates.TryGetValue(request.CandidateUserId, out var candidate)
                ? candidate
                : TalentContactPii.None;

            return new TalentContactRequestDto(
                request.Id,
                request.CompanyId,
                request.CandidateUserId,
                request.Status,
                request.Message,
                request.CreatedAtUtc,
                request.RespondByUtc,
                request.RespondedAtUtc,
                request.ContactSharedAtUtc,
                PiiRevealed: reveal,
                pii.Name,
                pii.Email,
                pii.Phone,
                companyName,
                request.CandidateDeclineReason);
        }).ToList();
    }

    public async Task<IReadOnlyList<TalentContactRequestDto>> ListForCandidateAsync(
        Guid candidateUserId,
        CancellationToken cancellationToken = default)
    {
        var ids = await _db.TalentContactRequests.AsNoTracking()
            .Where(r => r.CandidateUserId == candidateUserId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => r.Id)
            .Take(100)
            .ToListAsync(cancellationToken);

        var list = new List<TalentContactRequestDto>();
        foreach (var id in ids)
        {
            // Candidate inbox never includes the candidate's own PII; company name is always attached.
            list.Add(await ToDtoAsync(id, revealPii: false, cancellationToken));
        }

        return list;
    }

    public async Task<TalentContactSharePreviewDto?> GetSharePreviewAsync(
        Guid candidateUserId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await _db.TalentContactRequests.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == requestId && r.CandidateUserId == candidateUserId,
                cancellationToken);
        if (request is null)
        {
            return null;
        }

        var pii = TalentContactPii.For(await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == candidateUserId, cancellationToken));
        var companyName = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == request.CompanyId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return new TalentContactSharePreviewDto(companyName, pii.Name, pii.Email, pii.Phone);
    }

    public async Task<int> MarkExpiredAsRefundEligibleAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expired = await _db.TalentContactRequests
            .Where(r => r.Status == TalentContactStatus.Pending && r.RespondByUtc <= now)
            .ToListAsync(cancellationToken);
        foreach (var row in expired)
        {
            row.Status = TalentContactStatus.RefundEligible;
        }

        if (expired.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return expired.Count;
    }

    private async Task<TalentContactRequestDto> ToDtoAsync(
        Guid requestId,
        bool revealPii,
        CancellationToken cancellationToken)
    {
        var request = await _db.TalentContactRequests.AsNoTracking()
            .FirstAsync(r => r.Id == requestId, cancellationToken);
        var reveal = revealPii || request.Status == TalentContactStatus.ContactShared;
        var pii = reveal
            ? TalentContactPii.For(await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.CandidateUserId, cancellationToken))
            : TalentContactPii.None;

        var companyName = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == request.CompanyId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return new TalentContactRequestDto(
            request.Id,
            request.CompanyId,
            request.CandidateUserId,
            request.Status,
            request.Message,
            request.CreatedAtUtc,
            request.RespondByUtc,
            request.RespondedAtUtc,
            request.ContactSharedAtUtc,
            PiiRevealed: reveal,
            pii.Name,
            pii.Email,
            pii.Phone,
            companyName,
            request.CandidateDeclineReason);
    }

    private static CandidatePreferencesDto DeserializePrefs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new CandidatePreferencesDto([], null, null);
        }

        try
        {
            return JsonSerializer.Deserialize<CandidatePreferencesDto>(json, JsonOptions)
                   ?? new CandidatePreferencesDto([], null, null);
        }
        catch (JsonException)
        {
            return new CandidatePreferencesDto([], null, null);
        }
    }

    private static bool AvailabilityMatchesHint(CandidatePreferencesDto prefs, string hint)
    {
        var h = hint.Trim().ToLowerInvariant();
        var mid = prefs.MinHoursPerWeek is decimal min && prefs.MaxHoursPerWeek is decimal max
            ? (min + max) / 2m
            : (decimal?)null;

        return h switch
        {
            "immediate" or "per direct" or "direct" => prefs.FlexibleTimes == true || mid is not null,
            "parttime" => mid is < 32,
            "fulltime" => mid is >= 32,
            "seasonal" or "seizoen" or "seizoenswerk" =>
                prefs.Roles?.Any(r => r.Contains("seizoen", StringComparison.OrdinalIgnoreCase)) == true
                || prefs.FlexibleTimes == true,
            _ => true
        };
    }

}
