using Jobsy.Api.Authorization;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Exceptions;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Media;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Jobsy.Core.Features;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequiresFeature(PlatformFeature.Employers)]
public class VacanciesController : ControllerBase
{
    /// <summary>Vacancy content is authored in Dutch unless a source language is stored later.</summary>
    private const string VacancySourceLanguage = JobsyLanguages.Default;

    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IVacancyProductService _products;
    private readonly IUserLookupService _users;
    private readonly ISalaryService _salary;
    private readonly IVacancyContentModerationService _moderation;
    private readonly ITranslationService _translation;
    private readonly IVacancyCategoryService _categories;
    private readonly IPlatformFeatureService _features;
    private readonly IUserNotificationService _notifications;
    private readonly IVacancyDiscoveryIndex _discoveryIndex;
    private readonly IExactRoutingService _exactRouting;
    private readonly IProfileVacancyMatchService _profileMatch;
    private readonly ICandidateVacancyCultureFitService _cultureFit;
    private readonly IKbDislikeSource _dislikeSource;

    public VacanciesController(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        IVacancyProductService products,
        IExactRoutingService exactRouting,
        IUserLookupService users,
        ISalaryService salary,
        IVacancyContentModerationService moderation,
        ITranslationService translation,
        IVacancyCategoryService categories,
        IPlatformFeatureService features,
        IUserNotificationService notifications,
        IVacancyDiscoveryIndex discoveryIndex,
        IProfileVacancyMatchService profileMatch,
        ICandidateVacancyCultureFitService cultureFit,
        IKbDislikeSource dislikeSource)
    {
        _db = db;
        _companyAuth = companyAuth;
        _products = products;
        _users = users;
        _salary = salary;
        _moderation = moderation;
        _translation = translation;
        _categories = categories;
        _features = features;
        _notifications = notifications;
        _discoveryIndex = discoveryIndex;
        _exactRouting = exactRouting;
        _profileMatch = profileMatch;
        _cultureFit = cultureFit;
        _dislikeSource = dislikeSource;
    }

    /// <summary>
    /// Public Funda feed: all currently active vacancies.
    /// List payloads omit full descriptions (detail endpoint keeps them).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IEnumerable<VacancyListItemDto>>> GetActive(
        CancellationToken cancellationToken)
    {
        var showWage = await CanViewerSeeWageAsync(cancellationToken);
        var records = await _discoveryIndex.GetActiveAsync(cancellationToken);
        var mapped = records
            .Select(r => MapRecordToDto(r, showWage, ageYears: null, includeDescription: false))
            .ToList();
        ApplyPublicListCacheHeaders();
        return Ok(mapped);
    }

    /// <summary>
    /// Precomputed MapLibre camera for the public banenkaart (centroid + zoom of active pins).
    /// No vacancy titles, addresses, or other PII.
    /// </summary>
    [HttpGet("map-view")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<VacancyMapViewDto>> GetMapView(CancellationToken cancellationToken)
    {
        var view = await _discoveryIndex.GetMapViewAsync(cancellationToken);
        return Ok(new VacancyMapViewDto(view.CenterLat, view.CenterLng, view.Zoom, view.PinCount));
    }

    /// <summary>
    /// Compact map pins (id, lat, lng, colour, optional match%). Cached with ETag;
    /// invalidated when the discovery index refreshes. jobMap fetches this over HTTP
    /// instead of receiving full vacancy payloads over the Blazor circuit.
    /// </summary>
    [HttpGet("pins")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IEnumerable<VacancyPinDto>>> GetPins(
        [FromQuery] double? originLat,
        [FromQuery] double? originLng,
        [FromQuery] string transport = TransportLabels.Bike,
        [FromQuery] int maxMinutes = 30,
        [FromQuery] double? radiusKm = null,
        [FromQuery] string[]? workType = null,
        [FromQuery] string? q = null,
        [FromQuery] Guid[]? categoryId = null,
        [FromQuery] bool? suitableFor65Plus = null,
        [FromQuery] Guid[]? companyId = null,
        [FromQuery] int? minMatchPercent = null,
        CancellationToken cancellationToken = default)
    {
        maxMinutes = Math.Clamp(maxMinutes, 5, 90);
        var mode = TransportLabels.Parse(transport);
        var records = VacancyDiscoveryQuery.Filter(
            await _discoveryIndex.GetActiveAsync(cancellationToken),
            companyIds: companyId,
            categoryIds: categoryId,
            suitableFor65Plus,
            workTypes: workType,
            searchQuery: q,
            minHoursPerWeek: 0,
            maxHoursPerWeek: 40);

        List<(VacancyDiscoveryRecord Record, int? TravelMinutes)> candidates;
        if (originLat is null || originLng is null)
        {
            candidates = records
                .Select(v => (Record: v, TravelMinutes: (int?)null))
                .ToList();
        }
        else
        {
            var lat = originLat.Value;
            var lng = originLng.Value;
            var reachKm = TravelReach.MaxCrowFliesKm(mode, maxMinutes, radiusKm);
            candidates = records
                .Where(v => VacancyDiscoveryQuery.MatchesTransport(v, transport))
                .Select(v =>
                {
                    var (travelMinutes, distanceKm) = TravelReach.Estimate(
                        lat, lng, v.Latitude, v.Longitude, mode);
                    return (Record: v, TravelMinutes: (int?)travelMinutes, DistanceKm: (double?)distanceKm);
                })
                .Where(r => r.TravelMinutes is int minutes && minutes <= maxMinutes)
                .Where(r => r.DistanceKm is double km && km <= reachKm)
                .Where(r => !(radiusKm is > 0 && r.DistanceKm > radiusKm.Value))
                .Select(r => (r.Record, r.TravelMinutes))
                .ToList();
        }

        IReadOnlyDictionary<Guid, ProfileVacancyMatch>? matches = null;
        CandidateFitGate fitGate = CandidateFitGate.Closed;
        ProfileVacancyMatchContext? matchContext = null;
        var matchFloor = minMatchPercent is int requestedFloor
            ? Math.Clamp(requestedFloor, 0, 100)
            : (int?)null;
        var isCandidate = _companyAuth.IsCandidate(User);
        if (isCandidate)
        {
            matchContext = await _profileMatch.TryLoadForPrincipalAsync(User, cancellationToken);
            if (matchContext is not null)
            {
                fitGate = CandidateFitGate.FromContext(matchContext);
                matches = await _profileMatch.ScoreAsync(matchContext, candidates, cancellationToken);
                candidates = candidates
                    .Where(c =>
                    {
                        if (!matches.TryGetValue(c.Record.Id, out var match))
                        {
                            return true;
                        }

                        if (match.Core.LegalAgeKnown && !match.Core.LegalEligible)
                        {
                            return false;
                        }

                        if (matchFloor is not int floor)
                        {
                            return true;
                        }

                        // Closed gate: no percentages — ignore floor (chip should be hidden client-side).
                        if (!fitGate.IsOpen)
                        {
                            return true;
                        }

                        var fit = CandidateFitDisplay.Build(match, fitGate);
                        return fit is not null && fit.Percent >= floor;
                    })
                    .ToList();
            }
        }

        // Content hash (not a 60s clock) so Cloudflare can cache stable pin sets.
        var contentHash = ComputePinsContentHash(candidates, matches);
        var etag = $"\"pins-{contentHash}-{(isCandidate ? "c" : "a")}\"";
        if (Request.Headers.IfNoneMatch.ToString().Contains(etag, StringComparison.Ordinal))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = isCandidate
            ? "private,max-age=15"
            : "public,max-age=60,stale-while-revalidate=300";

        // Stable seed so featured pin order is deterministic for this content hash.
        var highlightSeed = contentHash;
        var pins = candidates.Select(c =>
        {
            var colour = c.Record.SuitableFor65Plus
                ? VacancyCategoryDefaults.SeniorPlusColorHex
                : c.Record.CategoryColorHex;
            int? matchPercent = null;
            string? matchBand = null;
            if (matches is not null && matches.TryGetValue(c.Record.Id, out var match))
            {
                var applied = CandidateFitApply.Apply(match, fitGate);
                matchPercent = applied.MatchPercent;
                matchBand = applied.MatchColorBand;
            }

            var highlighted = VacancyHighlightRules.IsActive(
                c.Record.IsHighlighted, c.Record.HighlightedUntil, DateTime.UtcNow);
            var workType = c.Record.WorkTypeLabelList.FirstOrDefault()
                           ?? c.Record.WorkTypeLabels;

            return new VacancyPinDto(
                c.Record.Id,
                c.Record.Latitude,
                c.Record.Longitude,
                colour,
                matchPercent,
                highlighted,
                highlighted ? HighlightShuffleRules.Rank(highlightSeed, c.Record.Id) : 0u,
                workType,
                matchBand);
        });

        return Ok(pins);
    }

    /// <summary>
    /// Lightweight popup card from the in-memory index (no DB). Used by jobMap single-pin popups.
    /// </summary>
    [HttpGet("{id:guid}/card")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<VacancyCardDto>> GetCard(
        Guid id,
        [FromQuery] double? originLat,
        [FromQuery] double? originLng,
        [FromQuery] string transport = TransportLabels.Bike,
        CancellationToken cancellationToken = default)
    {
        var records = await _discoveryIndex.GetActiveAsync(cancellationToken);
        var record = records.FirstOrDefault(r => r.Id == id);
        if (record is null)
        {
            return NotFound();
        }

        var showWage = await CanViewerSeeWageAsync(cancellationToken);
        int? travelMinutes = null;
        if (originLat is double lat && originLng is double lng && IsFiniteCoordinate(lat, lng))
        {
            var mode = TransportLabels.Parse(transport);
            var (minutes, _) = TravelReach.Estimate(lat, lng, record.Latitude, record.Longitude, mode);
            travelMinutes = (int?)minutes;
        }

        int? matchPercent = null;
        string? matchBand = null;
        string? fitGateValue = null;
        string? fitWhyLine = null;
        if (_companyAuth.IsCandidate(User))
        {
            var matchContext = await _profileMatch.TryLoadForPrincipalAsync(User, cancellationToken);
            if (matchContext is not null)
            {
                var gate = CandidateFitGate.FromContext(matchContext);
                var scored = await _profileMatch.ScoreAsync(matchContext, [(record, travelMinutes)], cancellationToken);
                if (scored.TryGetValue(id, out var match))
                {
                    var applied = CandidateFitApply.Apply(match, gate);
                    matchPercent = applied.MatchPercent;
                    matchBand = applied.MatchColorBand;
                    fitGateValue = applied.FitGate;
                    fitWhyLine = applied.FitWhyLineNl;
                }
            }
        }

        return Ok(MapCard(record, showWage, travelMinutes, matchPercent, matchBand, fitGateValue, fitWhyLine));
    }

    /// <summary>
    /// Batch popup cards (max 25) from the in-memory index. Used by cluster pager + prefetch.
    /// </summary>
    [HttpGet("cards")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IEnumerable<VacancyCardDto>>> GetCards(
        [FromQuery] string? ids,
        [FromQuery] double? originLat,
        [FromQuery] double? originLng,
        [FromQuery] string transport = TransportLabels.Bike,
        CancellationToken cancellationToken = default)
    {
        var parsed = ParseCardIds(ids);
        if (parsed.Count == 0)
        {
            return Ok(Array.Empty<VacancyCardDto>());
        }

        var records = await _discoveryIndex.GetActiveAsync(cancellationToken);
        var byId = records.Where(r => parsed.Contains(r.Id)).ToDictionary(r => r.Id);
        var showWage = await CanViewerSeeWageAsync(cancellationToken);
        var mode = TransportLabels.Parse(transport);
        var hasOrigin = originLat is double lat && originLng is double lng && IsFiniteCoordinate(lat, lng);

        IReadOnlyDictionary<Guid, ProfileVacancyMatch>? matches = null;
        var fitGate = CandidateFitGate.Closed;
        if (_companyAuth.IsCandidate(User))
        {
            var matchContext = await _profileMatch.TryLoadForPrincipalAsync(User, cancellationToken);
            if (matchContext is not null)
            {
                fitGate = CandidateFitGate.FromContext(matchContext);
                var scoreInput = parsed
                    .Where(byId.ContainsKey)
                    .Select(id =>
                    {
                        var r = byId[id];
                        int? travel = null;
                        if (hasOrigin)
                        {
                            var (minutes, _) = TravelReach.Estimate(
                                originLat!.Value, originLng!.Value, r.Latitude, r.Longitude, mode);
                            travel = (int?)minutes;
                        }

                        return (Record: r, TravelMinutes: travel);
                    })
                    .ToList();
                matches = await _profileMatch.ScoreAsync(matchContext, scoreInput, cancellationToken);
            }
        }

        var cards = new List<VacancyCardDto>(parsed.Count);
        foreach (var id in parsed)
        {
            if (!byId.TryGetValue(id, out var record))
            {
                continue;
            }

            int? travelMinutes = null;
            if (hasOrigin)
            {
                var (minutes, _) = TravelReach.Estimate(
                    originLat!.Value, originLng!.Value, record.Latitude, record.Longitude, mode);
                travelMinutes = (int?)minutes;
            }

            int? matchPercent = null;
            string? matchBand = null;
            string? fitGateValue = null;
            string? fitWhyLine = null;
            if (matches is not null && matches.TryGetValue(id, out var match))
            {
                var applied = CandidateFitApply.Apply(match, fitGate);
                matchPercent = applied.MatchPercent;
                matchBand = applied.MatchColorBand;
                fitGateValue = applied.FitGate;
                fitWhyLine = applied.FitWhyLineNl;
            }

            cards.Add(MapCard(record, showWage, travelMinutes, matchPercent, matchBand, fitGateValue, fitWhyLine));
        }

        return Ok(cards);
    }

    /// <summary>
    /// Banenkaart discover: without origin returns all active vacancies (optional workType/wage).
    /// With origin, filters by transport, travel time and optional radius via IRoutingService.
    /// Optional ageYears resolves salary-table wages; min/max hourly filters apply when age is set.
    /// Pass repeated workType query values (or comma-separated) to match any selected branch.
    /// Optional q filters by title/description/requirements (assistant keyword search / hidden filter).
    /// </summary>
    [HttpGet("discover")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IEnumerable<VacancyListItemDto>>> Discover(
        [FromQuery] double? originLat,
        [FromQuery] double? originLng,
        [FromQuery] string transport = TransportLabels.Bike,
        [FromQuery] int maxMinutes = 30,
        [FromQuery] double? radiusKm = null,
        [FromQuery] int? ageYears = null,
        [FromQuery] decimal? minHourlyWage = null,
        [FromQuery] decimal? maxHourlyWage = null,
        [FromQuery] int? minHoursPerWeek = null,
        [FromQuery] int? maxHoursPerWeek = null,
        [FromQuery] string[]? workType = null,
        [FromQuery] string? q = null,
        [FromQuery] Guid[]? categoryId = null,
        [FromQuery] bool? suitableFor65Plus = null,
        [FromQuery] Guid[]? companyId = null,
        [FromQuery] int? take = null,
        [FromQuery] int? minMatchPercent = null,
        CancellationToken cancellationToken = default)
    {
        maxMinutes = Math.Clamp(maxMinutes, 5, 90);
        var filterMinHours = Math.Clamp(minHoursPerWeek ?? 0, 0, 40);
        var filterMaxHours = Math.Clamp(maxHoursPerWeek ?? 40, 0, 40);
        if (filterMaxHours < filterMinHours)
        {
            (filterMinHours, filterMaxHours) = (filterMaxHours, filterMinHours);
        }

        int? age = ageYears is int a ? AgeRules.ClampFilterAge(a) : null;
        var mode = TransportLabels.Parse(transport);
        var showWage = age is not null || await CanViewerSeeWageAsync(cancellationToken);

        var records = VacancyDiscoveryQuery.Filter(
            await _discoveryIndex.GetActiveAsync(cancellationToken),
            companyIds: companyId,
            categoryIds: categoryId,
            suitableFor65Plus,
            workTypes: workType,
            searchQuery: q,
            minHoursPerWeek: filterMinHours,
            maxHoursPerWeek: filterMaxHours);

        List<(VacancyDiscoveryRecord Record, int? TravelMinutes, double? DistanceKm)> candidates;
        if (originLat is null || originLng is null)
        {
            // No origin: show all matching vacancies (transport is only a routing preference once located).
            candidates = records
                .OrderBy(v => v.Title, StringComparer.CurrentCultureIgnoreCase)
                .Select(v => (Record: v, TravelMinutes: (int?)null, DistanceKm: (double?)null))
                .ToList();
        }
        else
        {
            var lat = originLat.Value;
            var lng = originLng.Value;
            var reachKm = TravelReach.MaxCrowFliesKm(mode, maxMinutes, radiusKm);
            candidates = records
                .Where(v => VacancyDiscoveryQuery.MatchesTransport(v, transport))
                .Select(v =>
                {
                    var (travelMinutes, distanceKm) = TravelReach.Estimate(
                        lat, lng, v.Latitude, v.Longitude, mode);
                    return (Record: v, TravelMinutes: (int?)travelMinutes, DistanceKm: (double?)distanceKm);
                })
                .Where(r => r.TravelMinutes is int minutes && minutes <= maxMinutes)
                .Where(r => r.DistanceKm is double km && km <= reachKm)
                .Where(r => !(radiusKm is > 0 && r.DistanceKm > radiusKm.Value))
                .OrderBy(r => r.TravelMinutes)
                .ThenBy(r => r.Record.Title, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        IReadOnlyDictionary<Guid, ProfileVacancyMatch>? matches = null;
        var fitGate = CandidateFitGate.Closed;
        ProfileVacancyMatchContext? matchContext = null;
        Dictionary<Guid, string>? dislikeReasons = null;
        var matchFloor = minMatchPercent is int requestedFloor
            ? Math.Clamp(requestedFloor, 0, 100)
            : (int?)null;
        if (_companyAuth.IsCandidate(User))
        {
            matchContext = await _profileMatch.TryLoadForPrincipalAsync(User, cancellationToken);
            if (matchContext is not null)
            {
                fitGate = CandidateFitGate.FromContext(matchContext);
                matches = await _profileMatch.ScoreAsync(
                    matchContext,
                    candidates.Select(c => (c.Record, c.TravelMinutes)),
                    cancellationToken);
                candidates = candidates
                    .Where(c =>
                    {
                        if (!matches.TryGetValue(c.Record.Id, out var match))
                        {
                            return true;
                        }

                        if (match.Core.LegalAgeKnown && !match.Core.LegalEligible)
                        {
                            return false;
                        }

                        if (matchFloor is not int floor || !fitGate.IsOpen)
                        {
                            return true;
                        }

                        var fit = CandidateFitDisplay.Build(match, fitGate);
                        return fit is not null && fit.Percent >= floor;
                    })
                    .ToList();

                dislikeReasons = await LoadDislikeReasonsAsync(
                    matchContext.UserId,
                    candidates.Select(c => c.Record.Id),
                    cancellationToken);
            }
        }

        var results = new List<VacancyListItemDto>(candidates.Count);
        foreach (var c in candidates)
        {
            var dto = MapRecordToDto(c.Record, showWage, age, c.TravelMinutes, c.DistanceKm, includeDescription: false);
            if (age is not null && (minHourlyWage is not null || maxHourlyWage is not null)
                && !VacancyDiscoveryQuery.MatchesWageFilter(dto.HourlyWage, minHourlyWage, maxHourlyWage))
            {
                continue;
            }

            if (matches is not null && matches.TryGetValue(c.Record.Id, out var match))
            {
                string? rankLower = null;
                dislikeReasons?.TryGetValue(c.Record.Id, out rankLower);
                dto = WithCandidateMatch(dto, match, fitGate, rankLower);
            }

            results.Add(dto);
        }

        if (matches is not null)
        {
            results = KbRanking.OrderByFitThenTitle(
                    results,
                    r => r.FitPercent ?? r.MatchPercent,
                    r => !string.IsNullOrWhiteSpace(r.RankLowerReason),
                    r => r.Title,
                    r => r.Id)
                .ToList();

            var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
            if (lookup is not null)
            {
                var dislikeJson = await _db.CandidatePrivatePreferences.AsNoTracking()
                    .Where(p => p.UserId == lookup.Id)
                    .Select(p => p.DislikesJson)
                    .FirstOrDefaultAsync(cancellationToken);
                IReadOnlyList<string> dislikeCodes = [];
                if (!string.IsNullOrWhiteSpace(dislikeJson))
                {
                    try
                    {
                        dislikeCodes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(dislikeJson) ?? [];
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        dislikeCodes = [];
                    }
                }

                results = DislikeMatchRules.DownRankNightShifts(
                    results,
                    dislikeCodes,
                    r => r.LegalNightShift23To06 == true).ToList();
            }
        }

        if (take is int cap)
        {
            cap = Math.Clamp(cap, 1, 200);
            if (results.Count > cap)
            {
                results = results.Take(cap).ToList();
            }
        }

        ApplyPublicListCacheHeaders();
        // Banenkaart list stays on the indexed snapshot — no OpenAI wait on open.
        return Ok(results);
    }

    /// <summary>
    /// Cacheable vacancy photo. List JSON points here instead of embedding Base64.
    /// Remote/picsum URLs redirect so Render does not proxy third-party bytes.
    /// </summary>
    [HttpGet("{id:guid}/image")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<IActionResult> GetPublicImage(Guid id, CancellationToken cancellationToken)
    {
        var records = await _discoveryIndex.GetActiveAsync(cancellationToken);
        var record = records.FirstOrDefault(r => r.Id == id);
        if (record is null)
        {
            return NotFound();
        }

        var workType = record.WorkTypeLabelList.FirstOrDefault();
        if (VacancyImageUrls.TryDecodeInlineImage(record.ImageUrl, out var bytes, out var contentType))
        {
            Response.Headers.CacheControl = "public,max-age=86400,stale-while-revalidate=604800";
            return File(bytes, contentType);
        }

        var target = VacancyImageUrls.ForPublicList(record.ImageUrl, id, workType)
                     ?? VacancyImageUrls.Placeholder(id, workType);
        if (target.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            target = VacancyImageUrls.Placeholder(id, workType);
        }

        return Redirect(target);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<VacancyListItemDto>> GetById(
        Guid id,
        [FromQuery] double? originLat,
        [FromQuery] double? originLng,
        [FromQuery] string? transport,
        [FromQuery] int? ageYears = null,
        CancellationToken cancellationToken = default)
    {
        int? age = ageYears is int a ? Math.Clamp(a, 15, 67) : null;
        var vacancy = await _db.Vacancies
            .AsNoTracking()
            .Include(v => v.Company)
            .Include(v => v.IntermediaryCompany)
            .Include(v => v.Category)
            .Include(v => v.ExclusivitySetting!)
                .ThenInclude(s => s.Educations)
            .Include(v => v.SalaryTable!)
                .ThenInclude(t => t.Rates)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (vacancy is null)
        {
            return NotFound();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (VacancyVisibilityRules.IsPubliclyVisible(vacancy, today))
        {
            var showWage = age is not null || await CanViewerSeeWageAsync(cancellationToken);
            return Ok(await MapWithOptionalRouteAsync(vacancy, originLat, originLng, transport, showWage, age, isPreview: false, cancellationToken));
        }

        // Drafts / pending / archived / unverified publisher: only for authenticated employers with company access (or admin).
        // Intermediaries may also access via IntermediaryCompanyId (end-client CompanyId alone is insufficient).
        if (User.Identity?.IsAuthenticated == true
            && (_companyAuth.IsAdmin(User) || _companyAuth.IsEmployer(User))
            && await CanManageVacancyAsync(vacancy, cancellationToken))
        {
            // Employers always see wage on managed vacancies. Preview is noindex.
            Response.Headers["X-Robots-Tag"] = "noindex";
            return Ok(await MapWithOptionalRouteAsync(vacancy, originLat, originLng, transport, showWage: true, age, isPreview: true, cancellationToken));
        }

        // Was once public and is no longer: a useful 410, not a bare 404 (errors 03).
        if (VacancyVisibilityRules.IsClosed(vacancy, today))
        {
            Response.Headers["X-Robots-Tag"] = "noindex";
            return StatusCode(StatusCodes.Status410Gone, MapClosed(vacancy));
        }

        return NotFound();
    }

    /// <summary>
    /// Up to <paramref name="limit"/> similar vacancies nearby for a closed vacancy's 410 page.
    /// Reuses the discovery index/query — no new ranking. Same category within 25 km (nearest
    /// first), or without a category the nearest publicly visible vacancy within 10 km.
    /// </summary>
    [HttpGet("{id:guid}/similar")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IEnumerable<VacancyCardDto>>> GetSimilar(
        Guid id,
        [FromQuery] int limit = 3,
        [FromQuery] double? originLat = null,
        [FromQuery] double? originLng = null,
        [FromQuery] string transport = TransportLabels.Bike,
        CancellationToken cancellationToken = default)
    {
        var vacancy = await _db.Vacancies
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!VacancyVisibilityRules.IsClosed(vacancy, today))
        {
            return NotFound();
        }

        limit = Math.Clamp(limit, 1, 3);
        var origin = vacancy.Location;
        var records = await _discoveryIndex.GetActiveAsync(cancellationToken);
        var withDistance = records
            .Where(r => r.Id != id)
            .Select(r => (Record: r, DistanceKm: GeoDistance.HaversineKm(
                origin,
                new Jobsy.Core.ValueObjects.GeoPoint(r.Latitude, r.Longitude))));

        var nearest = (vacancy.CategoryId is Guid categoryId
                ? withDistance.Where(c => c.Record.CategoryId == categoryId && c.DistanceKm <= 25)
                : withDistance.Where(c => c.DistanceKm <= 10))
            .OrderBy(c => c.DistanceKm)
            .Take(limit)
            .Select(c => c.Record)
            .ToList();

        var showWage = await CanViewerSeeWageAsync(cancellationToken);
        var hasOrigin = originLat is double lat && originLng is double lng && IsFiniteCoordinate(lat, lng);
        var mode = TransportLabels.Parse(transport);
        var cards = nearest.Select(r =>
        {
            int? travelMinutes = null;
            double? distanceKm = null;
            if (hasOrigin)
            {
                var (minutes, km) = TravelReach.Estimate(originLat!.Value, originLng!.Value, r.Latitude, r.Longitude, mode);
                travelMinutes = minutes;
                distanceKm = km;
            }
            else
            {
                distanceKm = Math.Round(GeoDistance.HaversineKm(origin, new Jobsy.Core.ValueObjects.GeoPoint(r.Latitude, r.Longitude)), 1);
            }

            return MapCard(r, showWage, travelMinutes, matchPercent: null, matchBand: null, distanceKm: distanceKm);
        });
        Response.Headers["X-Robots-Tag"] = "noindex";
        ApplyPublicListCacheHeaders();
        return Ok(cards);
    }

    /// <summary>
    /// Minimal public payload for a closed vacancy — city/category respect the intermediary-hidden
    /// display rules, but the company name itself is never included (<see cref="ClosedVacancyDto"/>).
    /// </summary>
    private static ClosedVacancyDto MapClosed(Vacancy vacancy)
    {
        var (_, displayAddress, _, _, _, _) = IntermediaryVacancyRules.ResolvePublicDisplay(
            vacancy, vacancy.Company, vacancy.IntermediaryCompany);
        var city = PlaceFromAddress(displayAddress);
        return new ClosedVacancyDto(
            vacancy.Id,
            vacancy.Title,
            string.IsNullOrWhiteSpace(city) ? null : city,
            vacancy.CategoryId,
            vacancy.Category?.Name);
    }

    /// <summary>
    /// Exact travel time and distance from the caller's origin for one transport mode.
    /// Recalculated independently of the full vacancy payload so changing "Jouw vervoer" stays cheap.
    /// </summary>
    [HttpGet("{id:guid}/travel")]
    [AllowAnonymous]
    [EnableRateLimiting("public-travel")]
    public async Task<ActionResult<VacancyTravelDto>> GetTravel(
        Guid id,
        [FromQuery] double originLat,
        [FromQuery] double originLng,
        [FromQuery] string? transport,
        CancellationToken cancellationToken = default)
    {
        if (!IsFiniteCoordinate(originLat, originLng))
        {
            return BadRequest();
        }

        var vacancy = await _db.Vacancies
            .AsNoTracking()
            .Include(v => v.Company)
            .Include(v => v.IntermediaryCompany)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (vacancy is null)
        {
            return NotFound();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!VacancyVisibilityRules.IsPubliclyVisible(vacancy, today))
        {
            return NotFound();
        }

        var (minutes, km) = await TryExactRouteAsync(
            originLat,
            originLng,
            vacancy.Location.Latitude,
            vacancy.Location.Longitude,
            transport,
            cancellationToken);
        return Ok(new VacancyTravelDto(minutes, km));
    }

    /// <summary>
    /// Lightweight culture-fit poll for the vacancy detail page (no full vacancy remap / no AI wait).
    /// </summary>
    [HttpGet("{id:guid}/culture-fit")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<VacancyCultureFitDto>> GetCultureFit(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vacancy = await _db.Vacancies.AsNoTracking()
            .Include(v => v.Company)
            .Include(v => v.IntermediaryCompany)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!VacancyVisibilityRules.IsPubliclyVisible(vacancy, today))
        {
            return NotFound();
        }

        if (!_companyAuth.IsCandidate(User))
        {
            return Ok(new VacancyCultureFitDto(null, null, null, null, InsightsStatuses.Ready, false));
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Ok(new VacancyCultureFitDto(null, null, null, null, InsightsStatuses.Ready, false));
        }
        if (!CandidateConsentRules.CanUseCandidateFeatures(user))
        {
            return Ok(new VacancyCultureFitDto(null, null, null, null, InsightsStatuses.Ready, false));
        }

        var matchContext = await _profileMatch.TryLoadForPrincipalAsync(User, cancellationToken);
        if (matchContext is null)
        {
            return Ok(new VacancyCultureFitDto(null, null, null, null, InsightsStatuses.Ready, false));
        }

        var record = VacancyDiscoveryIndex.ToRecord(vacancy);
        var matches = await _profileMatch.ScoreAsync(matchContext, [(record, (int?)null)], cancellationToken);
        if (!matches.TryGetValue(vacancy.Id, out var match) || match.CultureFit is null)
        {
            return Ok(new VacancyCultureFitDto(null, null, null, null, InsightsStatuses.Ready, false));
        }

        var (result, status) = await _cultureFit.ResolveForGetAsync(
            user.Id,
            vacancy.Id,
            record.CulturePillars ?? [],
            matchContext.Competencies,
            matchContext.CultureScores,
            match.CultureFit,
            cancellationToken);
        return Ok(new VacancyCultureFitDto(
            result?.Percent,
            result?.Band,
            result?.Label,
            result?.Why,
            status,
            result?.FromOpenAi ?? false));
    }

    /// <summary>
    /// Employer-managed vacancies, scoped to companies the caller may access.
    /// Optional <paramref name="companyIds"/> narrows further (intersection with accessible ids).
    /// Branch managers only see their own company.
    /// </summary>
    [HttpGet("manage")]
    [Authorize(Policy = JobsyPolicies.RequireAdminOrEmployer)]
    public async Task<ActionResult<IEnumerable<VacancyListItemDto>>> GetManaged(
        [FromQuery] List<Guid>? companyIds,
        CancellationToken cancellationToken)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        if (accessible is { Count: 0 })
        {
            return Ok(Array.Empty<VacancyListItemDto>());
        }

        IReadOnlyCollection<Guid>? scope = accessible;
        if (companyIds is { Count: > 0 } && accessible is not null)
        {
            var intersection = companyIds.Where(accessible.Contains).Distinct().ToList();
            if (intersection.Count == 0)
            {
                return Ok(Array.Empty<VacancyListItemDto>());
            }

            scope = intersection;
        }

        // Defense-in-depth: enable EF tenant filter for this manage request.
        CompanyTenantScope.Enforce(_db, scope);

        var query = _db.Vacancies
            .AsNoTracking()
            .AsSplitQuery()
            .Include(v => v.Company)
            .Include(v => v.IntermediaryCompany)
            .Include(v => v.Category)
            .Include(v => v.ExclusivitySetting!)
                .ThenInclude(s => s.Educations)
            .AsQueryable();

        if (scope is not null)
        {
            // End-client company OR intermediary org that posted the vacancy.
            query = query.Where(v =>
                scope.Contains(v.CompanyId)
                || (v.IntermediaryCompanyId != null && scope.Contains(v.IntermediaryCompanyId.Value)));
        }

        var vacancies = await query.OrderBy(v => v.Title).ToListAsync(cancellationToken);
        if (vacancies.Count == 0)
        {
            return Ok(Array.Empty<VacancyListItemDto>());
        }

        var ids = vacancies.Select(v => v.Id).ToList();

        var impressionCounts = await _db.VacancySearchImpressions.AsNoTracking()
            .Where(i => ids.Contains(i.VacancyId))
            .GroupBy(i => i.VacancyId)
            .Select(g => new { VacancyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VacancyId, x => x.Count, cancellationToken);
        var clickCounts = await _db.VacancyClicks.AsNoTracking()
            .Where(c => ids.Contains(c.VacancyId))
            .GroupBy(c => c.VacancyId)
            .Select(g => new { VacancyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VacancyId, x => x.Count, cancellationToken);
        var shareCounts = await _db.VacancyShares.AsNoTracking()
            .Where(s => ids.Contains(s.VacancyId))
            .GroupBy(s => s.VacancyId)
            .Select(g => new { VacancyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VacancyId, x => x.Count, cancellationToken);
        var applicationCounts = await _db.Applications.AsNoTracking()
            .Where(a => ids.Contains(a.VacancyId) && a.EmailVerifiedAt != null)
            .GroupBy(a => a.VacancyId)
            .Select(g => new { VacancyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VacancyId, x => x.Count, cancellationToken);
        var newApplicationCounts = await _db.Applications.AsNoTracking()
            .Where(a => ids.Contains(a.VacancyId)
                        && a.EmailVerifiedAt != null
                        && a.Status == ApplicationStatus.Pending)
            .GroupBy(a => a.VacancyId)
            .Select(g => new { VacancyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VacancyId, x => x.Count, cancellationToken);
        var likeCounts = await _db.VacancyLikes.AsNoTracking()
            .Where(l => ids.Contains(l.VacancyId))
            .GroupBy(l => l.VacancyId)
            .Select(g => new { VacancyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VacancyId, x => x.Count, cancellationToken);
        var pushBomVacancyIds = await _db.TokenTransactions.AsNoTracking()
            .Where(t => t.VacancyId != null
                        && ids.Contains(t.VacancyId.Value)
                        && t.Reason == TokenSpendReason.PushBom
                        && t.Kind == TokenTransactionKind.Spend)
            .Select(t => t.VacancyId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var pushBomSet = pushBomVacancyIds.ToHashSet();

        var pendingCompanyIds = vacancies
            .Where(v => v.Status == VacancyStatus.PendingApproval)
            .Select(v => v.CompanyId)
            .Distinct()
            .ToList();
        var requesterByCompany = new Dictionary<Guid, string>();
        if (pendingCompanyIds.Count > 0)
        {
            var managers = await _db.Users.AsNoTracking()
                .Where(u => u.Role == UserRole.BranchManager
                            && u.CompanyMemberships.Any(m => pendingCompanyIds.Contains(m.CompanyId)))
                .Select(u => new
                {
                    u.FullName,
                    u.FirstName,
                    u.LastName,
                    CompanyIds = u.CompanyMemberships
                        .Where(m => pendingCompanyIds.Contains(m.CompanyId))
                        .Select(m => m.CompanyId)
                        .ToList()
                })
                .ToListAsync(cancellationToken);
            foreach (var m in managers)
            {
                var display = FormatRequesterName(m.FirstName, m.LastName, m.FullName);
                if (string.IsNullOrWhiteSpace(display))
                {
                    continue;
                }

                foreach (var cid in m.CompanyIds)
                {
                    requesterByCompany.TryAdd(cid, display);
                }
            }
        }

        var freePublishUntil = (await _features.GetAsync(cancellationToken)).FreePublishUntil;
        var mapped = new List<VacancyListItemDto>(vacancies.Count);
        foreach (var v in vacancies)
        {
            try
            {
                var dto = MapToDto(
                    v,
                    showWage: true,
                    impressionCount: impressionCounts.GetValueOrDefault(v.Id),
                    clickCount: clickCounts.GetValueOrDefault(v.Id),
                    applicationCount: applicationCounts.GetValueOrDefault(v.Id),
                    shareCount: shareCounts.GetValueOrDefault(v.Id),
                    likeCount: likeCounts.GetValueOrDefault(v.Id),
                    includeDescription: false,
                    includeCategoryInternals: true,
                    freePublishUntil: freePublishUntil);
                mapped.Add(dto with
                {
                    RequestedHighlight = v.RequestedHighlight,
                    RequestedPushBom = v.RequestedPushBom,
                    RequestedExtend = v.RequestedExtend,
                    NewApplicationCount = newApplicationCounts.GetValueOrDefault(v.Id),
                    HasPushBom = pushBomSet.Contains(v.Id),
                    IncompleteFieldCount = v.Status == VacancyStatus.Draft
                        ? VacancyDraftCompletenessRules.CountMissingFields(v)
                        : 0,
                    RequesterDisplayName = v.Status == VacancyStatus.PendingApproval
                        ? requesterByCompany.GetValueOrDefault(v.CompanyId)
                        : null
                });
            }
            catch
            {
                // Skip corrupt rows so one bad vacancy cannot 500 the whole manage page.
            }
        }

        return Ok(mapped);
    }

    private static string FormatRequesterName(string? first, string? last, string? full)
    {
        if (!string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(last))
        {
            return $"{first.Trim()[0]}. {last.Trim()}";
        }

        if (string.IsNullOrWhiteSpace(full))
        {
            return "";
        }

        var parts = full.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return parts[0];
        }

        return $"{parts[0][0]}. {parts[^1]}";
    }

    /// <summary>
    /// Create a vacancy for a company the employer is allowed to manage.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = JobsyRoles.VacancyLifecycleRoles)]
    [RequireCompanyAccess]
    public async Task<ActionResult<VacancyListItemDto>> Create(
        [FromBody] CreateVacancyRequest request,
        CancellationToken cancellationToken)
    {
        return await SaveDraftAsync(request, existing: null, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = JobsyRoles.VacancyLifecycleRoles)]
    [RequireCompanyAccess]
    public async Task<ActionResult<VacancyListItemDto>> Update(
        Guid id,
        [FromBody] CreateVacancyRequest request,
        CancellationToken cancellationToken)
    {
        var vacancy = await LoadManagedVacancyAsync(id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var access = await EnsureVacancyManageAccessAsync(vacancy, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        if (vacancy.Status is not (VacancyStatus.Draft or VacancyStatus.Active))
        {
            return BadRequest(new { message = "Alleen concept- of actieve vacatures kunnen worden bewerkt." });
        }

        return await SaveDraftAsync(request, vacancy, cancellationToken);
    }

    private async Task<ActionResult<VacancyListItemDto>> SaveDraftAsync(
        CreateVacancyRequest request,
        Core.Entities.Vacancy? existing,
        CancellationToken cancellationToken)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);
        if (company is null)
        {
            return NotFound(new { message = "Bedrijf niet gevonden." });
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        var isIntermediary = actor?.Role == UserRole.Intermediary
            || User.IsInRole(JobsyRoles.Intermediary);
        var kvkError = IntermediaryVacancyRules.ValidateEndClientKvk(company, isIntermediary);
        if (kvkError is not null)
        {
            return BadRequest(new { message = kvkError });
        }

        Guid? intermediaryCompanyId = null;
        Company? intermediaryCompany = null;
        if (isIntermediary)
        {
            intermediaryCompanyId = await ResolveIntermediaryOrganizationIdAsync(actor, cancellationToken);
            if (intermediaryCompanyId is null)
            {
                return BadRequest(new { message = "Intermediair-organisatie ontbreekt op je account." });
            }

            intermediaryCompany = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == intermediaryCompanyId.Value, cancellationToken);
        }

        if (request.SalaryTableId is not Guid tableId)
        {
            return BadRequest(new { message = "Salaristabel is verplicht." });
        }

        var organizationId = company.ParentCompanyId ?? company.Id;
        var salaryTable = await _db.CompanySalaryTables
            .Include(t => t.Rates)
            .Include(t => t.AllowedBranches)
            .FirstOrDefaultAsync(t => t.Id == tableId && t.IsActive, cancellationToken);
        var allowed = salaryTable is not null
            && (WmlSalaryTableService.IsAllowedForBranch(salaryTable, request.CompanyId, organizationId)
                || salaryTable.CompanyId == request.CompanyId);
        if (!allowed)
        {
            return BadRequest(new { message = "Salaristabel niet gevonden voor deze vestiging." });
        }

        if (salaryTable!.Rates.Count == 0)
        {
            return BadRequest(new { message = "Salaristabel heeft geen tarieven." });
        }

        var adultRate = salaryTable.Rates
            .Where(r => r.AgeYears >= 21)
            .OrderBy(r => r.AgeYears)
            .Select(r => r.HourlyRate)
            .FirstOrDefault();
        if (adultRate <= 0)
        {
            adultRate = salaryTable.Rates.Max(r => r.HourlyRate);
        }

        var hourlyWage = adultRate > 0 ? adultRate : request.HourlyWage;
        if (!_salary.MeetsMinimumWage(hourlyWage, ageYears: 21))
        {
            return BadRequest(new { message = "Uurloon ligt onder het wettelijk minimumloon (21+)." });
        }

        var branchLabels = NormalizeBranchLabels(request.WorkTypes);
        if (branchLabels.Length == 0 && existing is null)
        {
            // New vacancy defaults to the company's first branche (D12).
            var rootId = company.ParentCompanyId ?? company.Id;
            var rootLabels = rootId == company.Id
                ? company.WorkTypeLabels
                : await _db.Companies.AsNoTracking()
                    .Where(c => c.Id == rootId)
                    .Select(c => c.WorkTypeLabels)
                    .FirstOrDefaultAsync(cancellationToken);
            var first = WorkTypeLabels.NormalizeCompanyLabels(WorkTypeLabels.SplitStored(rootLabels))
                .FirstOrDefault();
            if (first is not null)
            {
                branchLabels = [first];
            }
        }

        if (branchLabels.Length is < 1 or > WorkTypeLabels.MaxPerVacancy)
        {
            return BadRequest(new { message = $"Kies 1 of {WorkTypeLabels.MaxPerVacancy} branches." });
        }

        if (!await AreBranchLabelsAllowedAsync(branchLabels, cancellationToken))
        {
            return BadRequest(new { message = "Een of meer branches zijn ongeldig of niet actief." });
        }

        string? imageUrl = null;
        string? imageError = null;
        if (!string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            imageUrl = HtmlSanitize.NormalizeImageInput(request.ImageUrl, out imageError);
            if (imageUrl is null)
            {
                return BadRequest(new { message = imageError ?? "Ongeldige afbeelding-URL of Base64." });
            }
        }

        var videoUrl = HtmlSanitize.NormalizeMediaUrl(request.VideoUrl);
        if (request.VideoUrl is not null && videoUrl is null)
        {
            return BadRequest(new { message = "Ongeldige video-URL (alleen http/https)." });
        }

        if (request.OverrideContactPreference)
        {
            Company? parent = null;
            if (company.ParentCompanyId is Guid parentId)
            {
                parent = await _db.Companies.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == parentId, cancellationToken);
            }

            var email = FirstNonEmpty(company.ContactEmail, parent?.ContactEmail);
            var phone = FirstNonEmpty(company.ContactPhone, parent?.ContactPhone);
            var whatsApp = FirstNonEmpty(company.ContactWhatsApp, parent?.ContactWhatsApp, phone);
            var contactError = EmployerContactPreferenceRules.Validate(
                request.DirectContactEnabled,
                request.ContactPreferMail,
                request.ContactPreferPhone,
                request.ContactPreferWhatsApp,
                email,
                phone,
                whatsApp);
            if (contactError is not null)
            {
                return BadRequest(new { message = contactError });
            }
        }

        var categoryResolve = await ResolveCategoryAsync(
            IntermediaryVacancyRules.ResolveCategoryId(isIntermediary, request.CategoryId),
            isIntermediary ? VacancyKind.Regular : request.Kind,
            cancellationToken);
        if (categoryResolve.Error is not null)
        {
            return BadRequest(new { message = categoryResolve.Error });
        }

        var category = categoryResolve.Category!;
        if (category.IsAlwaysFree && request.Kind == VacancyKind.Regular && !isIntermediary)
        {
            return BadRequest(new
            {
                message = "Gratis/vrijwilligerscategorie is niet bedoeld voor reguliere betaalde vacatures. Kies een passende categorie."
            });
        }

        var categoryFieldsJson = SerializeCategoryFields(category, isIntermediary ? null : request.CategoryFields);
        if (!isIntermediary
            && category.Id == VacancyCategoryDefaults.InclusiefId
            && !HasCategoryField(categoryFieldsJson, VacancyCategoryExtraFields.TargetGroup))
        {
            return BadRequest(new { message = "Doelgroep is verplicht voor inclusieve vacatures." });
        }

        var suitableFor65Plus = !isIntermediary
            && category.Id == VacancyCategoryDefaults.RegulierId
            && request.SuitableFor65Plus;

        var exclusivityError = await ResolveExclusivitySettingIdAsync(
            category.PlacementKind,
            request.ExclusivitySettingId,
            cancellationToken);
        if (exclusivityError.Error is not null)
        {
            return BadRequest(new { message = exclusivityError.Error });
        }

        var moderation = await _moderation.CheckAsync(request.Title, request.Description, cancellationToken);
        var moderationWarning = moderation.IsAllowed
            ? null
            : moderation.Warning ?? "De vacaturetekst vraagt om een aanpassing voordat je kunt publiceren.";

        var vacancy = existing ?? new Core.Entities.Vacancy
        {
            Id = Guid.NewGuid(),
            Status = VacancyStatus.Draft,
            CreatedVia = VacancySource.Manual,
            CreatedAtUtc = DateTime.UtcNow
        };

        vacancy.Title = request.Title;
        vacancy.Description = request.Description;
        vacancy.HourlyWage = hourlyWage;
        vacancy.StartDate = request.StartDate;
        vacancy.EndDate = request.EndDate;
        vacancy.CompanyId = request.CompanyId;
        vacancy.Location = company.Location;
        vacancy.RequiredTransport = request.RequiredTransport;
        vacancy.WorkTypes = WorkTypeLabels.Combine(branchLabels);
        vacancy.WorkTypeLabels = WorkTypeLabels.CombineStored(branchLabels);
        vacancy.ImageUrl = imageUrl;
        vacancy.VideoUrl = videoUrl;
        vacancy.SalaryTableId = tableId;
        vacancy.RequiredDrivingLicense = string.IsNullOrWhiteSpace(request.RequiredDrivingLicense) ? null : request.RequiredDrivingLicense.Trim();
        vacancy.RequiredEducation = string.IsNullOrWhiteSpace(request.RequiredEducation) ? null : request.RequiredEducation.Trim();
        vacancy.MinimumEmployers = request.MinimumEmployers is > 0 ? request.MinimumEmployers : null;
        vacancy.MinimumReferences = request.MinimumReferences is > 0
            ? Math.Min(request.MinimumReferences.Value, CandidateReferenceRules.MaxMinimumOnVacancy)
            : null;
        var culturePillars = CulturePillarCatalog.Normalize(request.CulturePillars);
        if (request.CulturePillars is { Length: > 0 }
            && (culturePillars.Count < CulturePillarCatalog.MinSelected
                || culturePillars.Count > CulturePillarCatalog.MaxSelected))
        {
            return BadRequest(new { message = "Kies 3 tot 5 cultuurpijlers die bij het team passen." });
        }

        vacancy.CulturePillarsJson = CulturePillarCatalog.Serialize(culturePillars);
        vacancy.BarrierRequirementsJson = VacancyBarrierCatalog.Serialize(
            VacancyBarrierCatalog.Normalize(
                ParseBarrierKind(request.BarrierKind),
                request.BarrierDiplomas,
                request.BarrierCertifications,
                request.BarrierMinExperienceYears,
                request.BarrierMinExperienceHours,
                request.BarrierHardChecks));
        vacancy.OverrideContactPreference = request.OverrideContactPreference;
        vacancy.DirectContactEnabled = request.OverrideContactPreference && request.DirectContactEnabled;
        vacancy.ContactPreferMail = request.OverrideContactPreference && request.DirectContactEnabled && request.ContactPreferMail;
        vacancy.ContactPreferPhone = request.OverrideContactPreference && request.DirectContactEnabled && request.ContactPreferPhone;
        vacancy.ContactPreferWhatsApp = request.OverrideContactPreference && request.DirectContactEnabled && request.ContactPreferWhatsApp;
        vacancy.IntermediaryCompanyId = intermediaryCompanyId;
        vacancy.ShowClientAddressOnMap = isIntermediary && request.ShowClientAddressOnMap;
        vacancy.Kind = category.PlacementKind;
        vacancy.CategoryId = category.Id;
        vacancy.Category = category;
        vacancy.CategoryFieldsJson = categoryFieldsJson;
        vacancy.SuitableFor65Plus = suitableFor65Plus;
        vacancy.ExclusivitySettingId = category.PlacementKind == VacancyKind.Internship
            ? exclusivityError.SettingId
            : null;
        vacancy.ContentModerationPassed = moderation.IsAllowed;

        if (existing is null)
        {
            // Inherit org default unless explicitly provided on create.
            vacancy.RequireEmailVerification = request.RequireEmailVerification
                ?? company.RequireEmailVerificationForApplications;
        }
        else if (request.RequireEmailVerification is bool requireVerify)
        {
            vacancy.RequireEmailVerification = requireVerify;
        }

        // Goodwill: after 14-day engagement reminder, an edit before EndDate extends deadline +7 days (once).
        var appliedGoodwill = false;
        if (existing is not null
            && existing.Status == VacancyStatus.Active
            && VacancyEngagementReminderRules.CanApplyGoodwillExtension(
                existing.EngagementReminderSentAtUtc,
                existing.EngagementGoodwillExtendedAtUtc,
                existing.EndDate,
                DateOnly.FromDateTime(DateTime.UtcNow)))
        {
            vacancy.EndDate = vacancy.EndDate.AddDays(VacancyEngagementReminderRules.GoodwillExtendDays);
            vacancy.EngagementGoodwillExtendedAtUtc = DateTime.UtcNow;
            appliedGoodwill = true;
        }

        var hoursError = ApplyHoursAndSchedule(vacancy, request);
        if (hoursError is not null)
        {
            return BadRequest(new { message = hoursError });
        }

        ApplyLegalFlags(vacancy, request);

        if (existing is null)
        {
            _db.Vacancies.Add(vacancy);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _discoveryIndex.Invalidate();
        await _cultureFit.InvalidateForVacancyAsync(vacancy.Id, cancellationToken);
        await _translation.InvalidateVacancyAsync(vacancy.Id, cancellationToken);

        if (appliedGoodwill && actor is not null)
        {
            await _notifications.CreateAsync(
                new NotificationCreateRequest(
                    actor.Id,
                    "Deadline verlengd — fijn dat je de tekst hebt aangescherpt",
                    $"Je update van '{vacancy.Title}' is opgeslagen. Als goodwill staat de einddatum nu op {vacancy.EndDate:dd-MM-yyyy} (+{VacancyEngagementReminderRules.GoodwillExtendDays} dagen).",
                    "VacancyEngagementGoodwill",
                    $"/branch/vacancies/new?edit={vacancy.Id}",
                    RelatedEntityType: "Vacancy",
                    RelatedEntityId: vacancy.Id),
                cancellationToken);
        }

        vacancy.Company = company;
        vacancy.IntermediaryCompany = intermediaryCompany;
        var freePublishUntil = (await _features.GetAsync(cancellationToken)).FreePublishUntil;
        var dto = MapToDto(
            vacancy,
            showWage: true,
            includeCategoryInternals: true,
            freePublishUntil: freePublishUntil,
            moderationWarning: moderationWarning);

        return existing is null
            ? CreatedAtAction(nameof(GetById), new { id = vacancy.Id }, dto)
            : Ok(dto);
    }

    [HttpPost("publish")]
    [Authorize(Roles = JobsyRoles.VacancyLifecycleRoles)]
    [RequiresVerifiedCompany]
    public async Task<ActionResult<VacancyProductActionResultDto>> Publish(
        [FromBody] PublishVacancyRequest request,
        CancellationToken cancellationToken)
    {
        var vacancy = await LoadManagedVacancyAsync(request.VacancyId, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var access = await EnsureVacancyManageAccessAsync(vacancy, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        if (VacancyDraftCompletenessRules.IsIncomplete(vacancy))
        {
            return BadRequest(new { message = "Conceptvacature is incompleet en kan nog niet worden gepubliceerd." });
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        var canPurchase = await CanPurchaseTokensForCompanyAsync(vacancy.CompanyId, cancellationToken);
        var result = await _products.PublishAsync(
            vacancy,
            new VacancyPublishOptions(request.Highlight, request.PushBom, request.Extend),
            actor?.Id,
            allowPendingApproval: !canPurchase,
            cancellationToken);

        if (result.InsufficientTokens)
        {
            return PaymentRequired(ToInsufficientTokensDto(
                result,
                "Publish",
                request.Highlight,
                request.PushBom,
                request.Extend));
        }

        if (!result.Succeeded)
        {
            if (string.Equals(result.ErrorCode, LenderRegistrationRules.PendingErrorCode, StringComparison.Ordinal))
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    code = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(await ToProductResultAsync(result, cancellationToken));
    }

    [HttpPost("{id:guid}/approve-publish")]
    [Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.Admin}")]
    public async Task<ActionResult<VacancyProductActionResultDto>> ApprovePublish(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vacancy = await LoadManagedVacancyAsync(id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var access = await EnsureVacancyManageAccessAsync(vacancy, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        // Unverified: BM may only approve a "klaar" request (PendingApproval + PublishOnVerification).
        var rootStatus = await ResolveRootVerificationStatusForCompanyAsync(vacancy.CompanyId, cancellationToken);
        if (!CompanyVerificationRules.CanPublish(rootStatus))
        {
            if (vacancy.Status == VacancyStatus.PendingApproval && vacancy.PublishOnVerification)
            {
                vacancy.Status = VacancyStatus.Draft;
                vacancy.PublishOnVerification = true;
                vacancy.ReadyMarkedAtUtc ??= DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                var freeUntil = (await _features.GetAsync(cancellationToken)).FreePublishUntil;
                return Ok(new VacancyProductActionResultDto(
                    MapToDto(vacancy, showWage: true, includeCategoryInternals: true, freePublishUntil: freeUntil),
                    PendingApproval: false,
                    Message: "Klaar — gaat live na verificatie."));
            }

            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = CompanyVerificationRules.UnverifiedErrorCode,
                message = CompanyVerificationRules.BlockedMessageNl
            });
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        var result = await _products.ApprovePublishAsync(vacancy, actor?.Id, cancellationToken);
        if (result.InsufficientTokens)
        {
            return PaymentRequired(ToInsufficientTokensDto(
                result,
                "Publish",
                vacancy.RequestedHighlight,
                vacancy.RequestedPushBom,
                vacancy.RequestedExtend));
        }

        if (!result.Succeeded)
        {
            if (string.Equals(result.ErrorCode, LenderRegistrationRules.PendingErrorCode, StringComparison.Ordinal))
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    code = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(await ToProductResultAsync(result, cancellationToken));
    }

    [HttpPost("{id:guid}/highlight")]
    [Authorize(Roles = JobsyRoles.VacancyLifecycleRoles)]
    [RequiresVerifiedCompany]
    public async Task<ActionResult<VacancyProductActionResultDto>> Highlight(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await RunProductAsync(
            id,
            (v, actorId, ct) => _products.HighlightAsync(v, actorId, ct),
            cancellationToken,
            actionName: "Highlight");
    }

    [HttpGet("{id:guid}/pushbom/preview")]
    [Authorize(Roles = JobsyRoles.VacancyLifecycleRoles)]
    public async Task<ActionResult<PushBomPreviewDto>> PreviewPushBom(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vacancy = await LoadManagedVacancyAsync(id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var access = await EnsureVacancyManageAccessAsync(vacancy, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        var preview = await _products.PreviewPushBomAsync(vacancy, cancellationToken);
        var balance = await _db.TokenTransactions
            .Where(t => t.CompanyId == vacancy.CompanyId)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        return Ok(new PushBomPreviewDto(
            preview.CandidateCount,
            preview.CostTokens,
            preview.RadiusKm,
            preview.MaxTravelMinutes,
            preview.HasPricing,
            balance,
            preview.HasPricing && preview.CandidateCount > 0 && balance >= preview.CostTokens));
    }

    [HttpPost("{id:guid}/pushbom")]
    [Authorize(Roles = JobsyRoles.VacancyLifecycleRoles)]
    [RequiresVerifiedCompany]
    public async Task<ActionResult<VacancyProductActionResultDto>> PushBom(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await RunProductAsync(
            id,
            (v, actorId, ct) => _products.PushBomAsync(v, actorId, ct),
            cancellationToken,
            actionName: "PushBom");
    }

    [HttpPost("{id:guid}/extend")]
    [Authorize(Roles = JobsyRoles.VacancyLifecycleRoles)]
    [RequiresVerifiedCompany]
    public async Task<ActionResult<VacancyProductActionResultDto>> Extend(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await RunProductAsync(
            id,
            (v, actorId, ct) => _products.ExtendAsync(v, actorId, ct),
            cancellationToken,
            actionName: "Extend");
    }

    [HttpPost("{id:guid}/inactive")]
    [Authorize(Roles = JobsyRoles.VacancyLifecycleRoles)]
    public async Task<ActionResult<VacancyProductActionResultDto>> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vacancy = await LoadManagedVacancyAsync(id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var access = await EnsureVacancyManageAccessAsync(vacancy, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        var result = await _products.DeactivateAsync(vacancy, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(await ToProductResultAsync(result, cancellationToken));
    }

    /// <summary>
    /// Mark a draft "klaar" so it auto-publishes when the company is verified (D4).
    /// Only available while the company is not verified.
    /// </summary>
    [HttpPost("{id:guid}/ready")]
    [Authorize(Roles = JobsyRoles.VacancyLifecycleRoles)]
    public async Task<ActionResult<VacancyProductActionResultDto>> MarkReady(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vacancy = await LoadManagedVacancyAsync(id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var access = await EnsureVacancyManageAccessAsync(vacancy, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        var rootStatus = await ResolveRootVerificationStatusForCompanyAsync(vacancy.CompanyId, cancellationToken);
        if (CompanyVerificationRules.CanPublish(rootStatus))
        {
            return BadRequest(new
            {
                message = "Je bedrijf is al geverifieerd — publiceer de vacature normaal."
            });
        }

        if (vacancy.Status is not (VacancyStatus.Draft or VacancyStatus.PendingApproval))
        {
            return BadRequest(new { message = "Alleen conceptvacatures kunnen klaargezet worden." });
        }

        if (VacancyDraftCompletenessRules.IsIncomplete(vacancy))
        {
            return BadRequest(new { message = "Conceptvacature is incompleet en kan nog niet klaargezet worden." });
        }

        if (!vacancy.ContentModerationPassed)
        {
            return BadRequest(new { message = "De vacaturetekst moet eerst de contentcontrole doorstaan." });
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        var canPurchase = await CanPurchaseTokensForCompanyAsync(vacancy.CompanyId, cancellationToken);

        vacancy.PublishOnVerification = true;
        vacancy.ReadyMarkedAtUtc = DateTime.UtcNow;
        vacancy.ReadyMarkedByUserId = actor?.Id;

        // Keep PendingApproval semantics for vestigingsmanagers without purchase rights.
        if (!canPurchase && vacancy.Status == VacancyStatus.Draft)
        {
            vacancy.Status = VacancyStatus.PendingApproval;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var freeUntil = (await _features.GetAsync(cancellationToken)).FreePublishUntil;
        return Ok(new VacancyProductActionResultDto(
            MapToDto(vacancy, showWage: true, includeCategoryInternals: true, freePublishUntil: freeUntil),
            PendingApproval: vacancy.Status == VacancyStatus.PendingApproval,
            Message: "Klaar — gaat live na verificatie."));
    }

    [HttpDelete("{id:guid}/ready")]
    [Authorize(Roles = JobsyRoles.VacancyLifecycleRoles)]
    public async Task<ActionResult<VacancyProductActionResultDto>> ClearReady(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vacancy = await LoadManagedVacancyAsync(id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var access = await EnsureVacancyManageAccessAsync(vacancy, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        vacancy.PublishOnVerification = false;
        vacancy.ReadyMarkedAtUtc = null;
        vacancy.ReadyMarkedByUserId = null;
        if (vacancy.Status == VacancyStatus.PendingApproval)
        {
            vacancy.Status = VacancyStatus.Draft;
        }

        await _db.SaveChangesAsync(cancellationToken);
        var freeUntil = (await _features.GetAsync(cancellationToken)).FreePublishUntil;
        return Ok(new VacancyProductActionResultDto(
            MapToDto(vacancy, showWage: true, includeCategoryInternals: true, freePublishUntil: freeUntil)));
    }

    /// <summary>
    /// Vacancy-level contact preference override (employer manage only). Never exposed on public vacancy GET.
    /// </summary>
    [HttpGet("{id:guid}/contact-preference")]
    [Authorize(Roles = JobsyRoles.VacancyLifecycleRoles)]
    public async Task<ActionResult<VacancyContactPreferenceDto>> GetContactPreference(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vacancy = await LoadManagedVacancyAsync(id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var access = await EnsureVacancyManageAccessAsync(vacancy, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        return Ok(ToContactPreferenceDto(vacancy));
    }

    [HttpPut("{id:guid}/contact-preference")]
    [Authorize(Roles = JobsyRoles.VacancyLifecycleRoles)]
    public async Task<ActionResult<VacancyContactPreferenceDto>> UpdateContactPreference(
        Guid id,
        [FromBody] UpdateVacancyContactPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var vacancy = await LoadManagedVacancyAsync(id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var access = await EnsureVacancyManageAccessAsync(vacancy, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        if (request.OverrideContactPreference)
        {
            var company = vacancy.Company;
            Company? parent = null;
            if (company.ParentCompanyId is Guid parentId)
            {
                parent = await _db.Companies.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == parentId, cancellationToken);
            }

            var email = FirstNonEmpty(company.ContactEmail, parent?.ContactEmail);
            var phone = FirstNonEmpty(company.ContactPhone, parent?.ContactPhone);
            var whatsApp = FirstNonEmpty(company.ContactWhatsApp, parent?.ContactWhatsApp, phone);
            var contactError = EmployerContactPreferenceRules.Validate(
                request.DirectContactEnabled,
                request.ContactPreferMail,
                request.ContactPreferPhone,
                request.ContactPreferWhatsApp,
                email,
                phone,
                whatsApp);
            if (contactError is not null)
            {
                return BadRequest(new { message = contactError });
            }
        }

        vacancy.OverrideContactPreference = request.OverrideContactPreference;
        vacancy.DirectContactEnabled = request.OverrideContactPreference && request.DirectContactEnabled;
        vacancy.ContactPreferMail = request.OverrideContactPreference && request.DirectContactEnabled && request.ContactPreferMail;
        vacancy.ContactPreferPhone = request.OverrideContactPreference && request.DirectContactEnabled && request.ContactPreferPhone;
        vacancy.ContactPreferWhatsApp = request.OverrideContactPreference && request.DirectContactEnabled && request.ContactPreferWhatsApp;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToContactPreferenceDto(vacancy));
    }

    [HttpGet("{id:guid}/email-verification")]
    [Authorize(Roles = JobsyRoles.VacancyLifecycleRoles)]
    public async Task<ActionResult<VacancyEmailVerificationDto>> GetEmailVerification(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vacancy = await LoadManagedVacancyAsync(id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var access = await EnsureVacancyManageAccessAsync(vacancy, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        return Ok(new VacancyEmailVerificationDto(vacancy.Id, vacancy.RequireEmailVerification));
    }

    [HttpPut("{id:guid}/email-verification")]
    [Authorize(Roles = JobsyRoles.VacancyLifecycleRoles)]
    public async Task<ActionResult<VacancyEmailVerificationDto>> UpdateEmailVerification(
        Guid id,
        [FromBody] UpdateVacancyEmailVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var vacancy = await LoadManagedVacancyAsync(id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var access = await EnsureVacancyManageAccessAsync(vacancy, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        vacancy.RequireEmailVerification = request.RequireEmailVerification;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new VacancyEmailVerificationDto(vacancy.Id, vacancy.RequireEmailVerification));
    }

    private static VacancyContactPreferenceDto ToContactPreferenceDto(Core.Entities.Vacancy vacancy) =>
        new(
            vacancy.Id,
            vacancy.OverrideContactPreference,
            vacancy.DirectContactEnabled,
            vacancy.ContactPreferMail,
            vacancy.ContactPreferPhone,
            vacancy.ContactPreferWhatsApp);

    private async Task<ActionResult<VacancyProductActionResultDto>> RunProductAsync(
        Guid vacancyId,
        Func<Core.Entities.Vacancy, Guid?, CancellationToken, Task<VacancyProductOutcome>> action,
        CancellationToken cancellationToken,
        string actionName = "Action")
    {
        var vacancy = await LoadManagedVacancyAsync(vacancyId, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var access = await EnsureVacancyManageAccessAsync(vacancy, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        var result = await action(vacancy, actor?.Id, cancellationToken);
        if (result.InsufficientTokens)
        {
            return PaymentRequired(ToInsufficientTokensDto(result, actionName));
        }

        if (!result.Succeeded)
        {
            if (string.Equals(result.ErrorCode, LenderRegistrationRules.PendingErrorCode, StringComparison.Ordinal))
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    code = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(await ToProductResultAsync(result, cancellationToken));
    }

    private ObjectResult PaymentRequired(InsufficientTokensDto body)
        => StatusCode(StatusCodes.Status402PaymentRequired, body);

    private static InsufficientTokensDto ToInsufficientTokensDto(
        VacancyProductOutcome result,
        string action,
        bool highlight = false,
        bool pushBom = false,
        bool extend = false)
    {
        var required = result.RequiredTokens;
        var balance = result.Balance;
        var deficit = Math.Max(0m, required - balance);
        return new InsufficientTokensDto(
            "InsufficientTokens",
            result.ErrorMessage ?? "Je tokens zijn op. Koop tokens om door te gaan.",
            result.SpendCompanyId ?? result.Vacancy.CompanyId,
            result.Vacancy.Id,
            action,
            required,
            balance,
            deficit,
            highlight,
            pushBom,
            extend);
    }

    /// <summary>
    /// Prepaid checkout is offered when the caller may buy tokens for this company.
    /// Branch managers with enterprise-managed tokens keep the PendingApproval path instead.
    /// </summary>
    private async Task<bool> CanPurchaseTokensForCompanyAsync(Guid companyId, CancellationToken cancellationToken)
    {
        if (_companyAuth.IsAdmin(User)
            || User.IsInRole(JobsyRoles.EnterpriseManager)
            || User.IsInRole(JobsyRoles.Intermediary))
        {
            return true;
        }

        if (!User.IsInRole(JobsyRoles.BranchManager))
        {
            return false;
        }

        var managedByEnterprise = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => c.TokensManagedByEnterprise)
            .FirstOrDefaultAsync(cancellationToken);
        return !managedByEnterprise;
    }

    private async Task<CompanyVerificationStatus> ResolveRootVerificationStatusForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var row = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => new { c.ParentCompanyId, c.VerificationStatus })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return CompanyVerificationStatus.Unverified;
        }

        if (row.ParentCompanyId is Guid parentId)
        {
            return await _db.Companies.AsNoTracking()
                .Where(c => c.Id == parentId)
                .Select(c => c.VerificationStatus)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return row.VerificationStatus;
    }

    private async Task<Core.Entities.Vacancy?> LoadManagedVacancyAsync(Guid id, CancellationToken cancellationToken)
        => await _db.Vacancies
            .Include(v => v.Company)
            .Include(v => v.IntermediaryCompany)
            .Include(v => v.Category)
            .Include(v => v.ExclusivitySetting!)
                .ThenInclude(s => s.Educations)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    private async Task<bool> CanManageVacancyAsync(Vacancy vacancy, CancellationToken cancellationToken)
    {
        if (await _companyAuth.CanAccessCompanyAsync(User, vacancy.CompanyId, cancellationToken))
        {
            return true;
        }

        if (vacancy.IntermediaryCompanyId is Guid intermediaryId
            && await _companyAuth.CanAccessCompanyAsync(User, intermediaryId, cancellationToken))
        {
            return true;
        }

        return false;
    }

    private async Task<ActionResult?> EnsureVacancyManageAccessAsync(
        Vacancy vacancy,
        CancellationToken cancellationToken)
    {
        if (await CanManageVacancyAsync(vacancy, cancellationToken))
        {
            return null;
        }

        return Forbid();
    }

    private async Task<VacancyProductActionResultDto> ToProductResultAsync(
        VacancyProductOutcome result,
        CancellationToken cancellationToken)
    {
        _discoveryIndex.Invalidate();
        var freePublishUntil = (await _features.GetAsync(cancellationToken)).FreePublishUntil;
        return new(
            MapToDto(
                result.Vacancy,
                showWage: true,
                includeCategoryInternals: true,
                freePublishUntil: freePublishUntil),
            result.PendingApproval,
            result.ErrorMessage,
            result.PushBomRecipientCount);
    }

    private void ApplyPublicListCacheHeaders()
    {
        Response.Headers.CacheControl = User.Identity?.IsAuthenticated == true
            ? "private,max-age=15"
            : "public,max-age=20,stale-while-revalidate=60";
    }

    private async Task<bool> CanViewerSeeWageAsync(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return WageVisibilityRules.CanShowWage(false, false, false);
        }

        if (!_companyAuth.IsCandidate(User))
        {
            return WageVisibilityRules.CanShowWage(true, false, false);
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        return WageVisibilityRules.CanShowWage(true, true, user?.DateOfBirth.HasValue == true);
    }

    private async Task<VacancyListItemDto> TranslateDtoAsync(
        VacancyListItemDto dto,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        if (JobsyLanguages.AreSame(VacancySourceLanguage, targetLanguage))
        {
            return dto;
        }

        var translated = await _translation.TranslateVacancyAsync(
            dto.Title,
            dto.Description ?? string.Empty,
            VacancySourceLanguage,
            targetLanguage,
            dto.Id,
            cancellationToken);

        return dto with
        {
            Title = translated.Title,
            Description = translated.Description
        };
    }

    private async Task<VacancyListItemDto> MapWithOptionalRouteAsync(
        Core.Entities.Vacancy vacancy,
        double? originLat,
        double? originLng,
        string? transport,
        bool showWage,
        int? ageYears = null,
        bool isPreview = false,
        CancellationToken cancellationToken = default)
    {
        var targetLanguage = await ResolveTargetLanguageAsync(cancellationToken);
        int? travelMinutes = null;
        double? distanceKm = null;

        if (originLat is not null && originLng is not null)
        {
            (travelMinutes, distanceKm) = await TryExactRouteAsync(
                originLat.Value,
                originLng.Value,
                vacancy.Location.Latitude,
                vacancy.Location.Longitude,
                transport,
                cancellationToken);
        }

        var dto = await MapToDtoAsync(
            vacancy,
            showWage,
            targetLanguage,
            ageYears,
            travelMinutes: travelMinutes,
            distanceKm: distanceKm,
            isPreview: isPreview,
            cancellationToken: cancellationToken);

        return await AttachCandidateMatchAsync(dto, vacancy, travelMinutes, cancellationToken);
    }

    private async Task<(int? Minutes, double? DistanceKm)> TryExactRouteAsync(
        double fromLat,
        double fromLng,
        double toLat,
        double toLng,
        string? transport,
        CancellationToken cancellationToken)
    {
        try
        {
            var mode = TransportLabels.Parse(transport);
            var route = await _exactRouting.TryGetRouteAsync(
                fromLat,
                fromLng,
                toLat,
                toLng,
                mode,
                cancellationToken);
            if (route is null)
            {
                return (null, null);
            }

            var minutes = Math.Max(1, (int)Math.Round(route.DurationSeconds / 60.0, MidpointRounding.AwayFromZero));
            var km = Math.Round(route.DistanceMeters / 1000.0, 2);
            return (minutes, km);
        }
        catch
        {
            return (null, null);
        }
    }

    private static bool IsFiniteCoordinate(double lat, double lng)
        => double.IsFinite(lat)
           && double.IsFinite(lng)
           && Math.Abs(lat) <= 90
           && Math.Abs(lng) <= 180
           && !(lat == 0 && lng == 0);

    private static uint ComputePinsContentHash(
        List<(VacancyDiscoveryRecord Record, int? TravelMinutes)> candidates,
        IReadOnlyDictionary<Guid, ProfileVacancyMatch>? matches)
    {
        unchecked
        {
            uint h = 2166136261;
            foreach (var c in candidates.OrderBy(x => x.Record.Id))
            {
                foreach (var b in c.Record.Id.ToByteArray())
                {
                    h ^= b;
                    h *= 16777619u;
                }

                h ^= (uint)BitConverter.SingleToInt32Bits((float)c.Record.Latitude);
                h *= 16777619u;
                h ^= (uint)BitConverter.SingleToInt32Bits((float)c.Record.Longitude);
                h *= 16777619u;
                if (matches is not null && matches.TryGetValue(c.Record.Id, out var match))
                {
                    h ^= (uint)match.TotalPercent;
                    h *= 16777619u;
                }

                if (VacancyHighlightRules.IsActive(c.Record.IsHighlighted, c.Record.HighlightedUntil, DateTime.UtcNow))
                {
                    h ^= 1u;
                    h *= 16777619u;
                }
            }

            return h;
        }
    }

    private static List<Guid> ParseCardIds(string? ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
        {
            return [];
        }

        var result = new List<Guid>(25);
        foreach (var part in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (result.Count >= 25)
            {
                break;
            }

            if (Guid.TryParse(part, out var id) && id != Guid.Empty && !result.Contains(id))
            {
                result.Add(id);
            }
        }

        return result;
    }

    private static VacancyCardDto MapCard(
        VacancyDiscoveryRecord record,
        bool showWage,
        int? travelMinutes,
        int? matchPercent,
        string? matchBand,
        string? fitGate = null,
        string? fitWhyLine = null,
        string? rankLowerReason = null,
        double? distanceKm = null)
    {
        var workType = record.WorkTypeLabelList.FirstOrDefault() ?? record.WorkTypeLabels;
        var thumbnail = VacancyImageUrls.ForCard(
            record.ImageUrl, record.CompanyLogoUrl, record.Id, workType);
        var highlighted = VacancyHighlightRules.IsActive(
            record.IsHighlighted, record.HighlightedUntil, DateTime.UtcNow);
        return new VacancyCardDto(
            record.Id,
            record.Title,
            record.CompanyName,
            record.OfferedByLabel,
            PlaceFromAddress(record.CompanyAddress),
            thumbnail,
            record.CompanyLogoUrl,
            showWage ? record.HourlyWage : null,
            showWage,
            record.WorkTypeLabelList.Length > 0 ? record.WorkTypeLabelList : null,
            highlighted,
            travelMinutes,
            matchPercent,
            matchBand,
            record.SuitableFor65Plus
                ? VacancyCategoryDefaults.SeniorPlusColorHex
                : record.CategoryColorHex,
            record.CompanyAddress,
            record.KvkNumber,
            record.Vestigingsnummer,
            fitGate,
            fitWhyLine,
            rankLowerReason,
            record.MinHoursPerWeek,
            record.MaxHoursPerWeek,
            distanceKm);
    }

    private static string PlaceFromAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return "";
        }

        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? address.Trim() : parts[^1];
    }

    private async Task<string> ResolveTargetLanguageAsync(CancellationToken cancellationToken)
    {
        if (Request.Query.TryGetValue("lang", out var langQuery) && JobsyLanguages.IsSupported(langQuery.ToString()))
        {
            return JobsyLanguages.Normalize(langQuery.ToString());
        }

        if (Request.Headers.TryGetValue("X-Jobsy-Language", out var langHeader)
            && JobsyLanguages.IsSupported(langHeader.ToString()))
        {
            return JobsyLanguages.Normalize(langHeader.ToString());
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _users.FindByPrincipalAsync(User, cancellationToken);
            var preferred = MeController.ParsePreferences(user?.PreferencesJson).Language;
            if (!string.IsNullOrWhiteSpace(preferred))
            {
                return JobsyLanguages.Normalize(preferred);
            }
        }

        return JobsyLanguages.Default;
    }

    private async Task<VacancyListItemDto> MapToDtoAsync(
        Core.Entities.Vacancy v,
        bool showWage,
        string targetLanguage,
        int? ageYears = null,
        int? travelMinutes = null,
        double? distanceKm = null,
        bool includeDescription = true,
        bool isPreview = false,
        CancellationToken cancellationToken = default)
    {
        var dto = MapToDto(
            v,
            showWage,
            ageYears,
            travelMinutes,
            distanceKm,
            includeDescription: includeDescription,
            isPreview: isPreview);
        return await TranslateDtoAsync(dto, targetLanguage, cancellationToken);
    }

    private static VacancyListItemDto MapRecordToDto(
        VacancyDiscoveryRecord r,
        bool showWage,
        int? ageYears = null,
        int? travelMinutes = null,
        double? distanceKm = null,
        bool includeDescription = false)
    {
        decimal? hourly = null;
        IReadOnlyList<WageByAgeDto>? wageByAge = null;
        int? resolvedForAge = null;

        if (showWage)
        {
            if (ageYears is int age)
            {
                hourly = VacancyWageResolver.ResolveHourlyWage(
                    r.HourlyWage,
                    r.SalaryRates.Count == 0
                        ? null
                        : r.SalaryRates.Select(b => new CompanySalaryRate
                        {
                            AgeYears = b.AgeYears,
                            HourlyRate = b.HourlyRate,
                            Label = b.Label
                        }),
                    age);
                resolvedForAge = age;
            }
            else
            {
                wageByAge = VacancyWageResolver.GetWageBands(
                        r.HourlyWage,
                        r.SalaryRates.Count == 0
                            ? null
                            : r.SalaryRates.Select(b => new CompanySalaryRate
                            {
                                AgeYears = b.AgeYears,
                                HourlyRate = b.HourlyRate,
                                Label = b.Label
                            }))
                    .Select(b => new WageByAgeDto(b.AgeYears, b.HourlyRate, b.Label))
                    .ToList();
            }
        }

        var featured = VacancyHighlightRules.IsActive(r.IsHighlighted, r.HighlightedUntil, DateTime.UtcNow);
        var compact = !includeDescription;
        var workType = r.WorkTypeLabelList.FirstOrDefault();

        return new VacancyListItemDto(
            r.Id,
            r.Title,
            includeDescription ? r.Description : null,
            hourly,
            r.StartDate,
            r.EndDate,
            r.Status.ToString(),
            r.CompanyId,
            r.CompanyName,
            r.CompanyAddress,
            VacancyImageUrls.Normalize(r.CompanyLogoUrl),
            VacancyImageUrls.ForCard(r.ImageUrl, r.CompanyLogoUrl, r.Id, workType),
            r.Latitude,
            r.Longitude,
            r.RequiredTransportLabels,
            showWage,
            travelMinutes,
            distanceKm,
            featured,
            featured ? r.HighlightedUntil : null,
            r.ExtensionCount,
            compact ? null : r.VideoUrl,
            compact ? null : r.SalaryTableId,
            wageByAge,
            resolvedForAge,
            r.WorkTypeLabelList,
            0,
            0,
            0,
            compact ? null : r.RequiredDrivingLicense,
            compact ? null : r.RequiredEducation,
            compact ? null : r.MinimumEmployers,
            compact ? null : r.FulfilledByApplicationId,
            r.CreatedVia.ToString(),
            r.MinHoursPerWeek,
            r.MaxHoursPerWeek,
            r.FlexibleTimes,
            compact ? null : r.ScheduleJson,
            compact ? null : r.LegalWorksAfter19,
            compact ? null : r.LegalNightShift23To06,
            compact ? null : r.LegalAdultSupervisorPresent,
            compact ? null : r.LegalHandlesMoneyOrClosing,
            compact ? null : r.LegalHeavyOrHazardousWork,
            0,
            0,
            r.OfferedByLabel,
            r.ShowClientAddressOnMap,
            compact ? null : r.IntermediaryCompanyId,
            r.Kind.ToString(),
            compact ? null : r.ExclusivitySettingId,
            compact ? null : r.ExclusivityName,
            r.ExclusivityIsOpen,
            compact ? null : r.ExclusivitySchoolDomain,
            null,
            null,
            r.CategoryId,
            r.CategoryName,
            r.CategoryColorHex,
            false,
            false,
            null,
            null,
            null,
            false,
            null,
            r.SuitableFor65Plus,
            r.KvkNumber,
            r.Vestigingsnummer,
            r.ContentModerationPassed,
            false,
            r.Status.ToString(),
            null,
            r.RequireEmailVerification,
            EngagementReminderTip: null,
            EngagementReminderSentAtUtc: null,
            MinimumReferences: compact ? null : r.MinimumReferences,
            CulturePillars: r.CulturePillars is { Count: > 0 } ? r.CulturePillars.ToList() : null,
            EngagementItems: r.EngagementItems is { Count: > 0 }
                ? r.EngagementItems.Select(e => new VacancyEngagementBadgeDto(e.ItemId, e.Checked)).ToList()
                : null);
    }

    private static VacancyListItemDto MapToDto(
        Core.Entities.Vacancy v,
        bool showWage,
        int? ageYears = null,
        int? travelMinutes = null,
        double? distanceKm = null,
        int impressionCount = 0,
        int clickCount = 0,
        int applicationCount = 0,
        bool includeDescription = true,
        int shareCount = 0,
        int likeCount = 0,
        bool includeCategoryInternals = false,
        DateOnly? freePublishUntil = null,
        string? moderationWarning = null,
        bool isPreview = false)
    {
        decimal? hourly = null;
        IReadOnlyList<WageByAgeDto>? wageByAge = null;
        int? resolvedForAge = null;

        if (showWage)
        {
            var rates = v.SalaryTable is { IsActive: true }
                ? v.SalaryTable.Rates
                : null;

            if (ageYears is int age)
            {
                hourly = VacancyWageResolver.ResolveHourlyWage(v.HourlyWage, rates, age);
                resolvedForAge = age;
            }
            else
            {
                // Without an age filter always expose per-age bands (company table, or
                // a scaled youth scale from the vacancy's flat hourly wage).
                wageByAge = VacancyWageResolver.GetWageBands(v.HourlyWage, rates)
                    .Select(b => new WageByAgeDto(b.AgeYears, b.HourlyRate, b.Label))
                    .ToList();
            }
        }

        var featured = VacancyHighlightRules.IsActive(v.IsHighlighted, v.HighlightedUntil, DateTime.UtcNow);
        var display = IntermediaryVacancyRules.ResolvePublicDisplay(v, v.Company, v.IntermediaryCompany);

        decimal? publishCostTokens = null;
        if (includeCategoryInternals)
        {
            decimal? basePublish = v.Category is null
                ? null
                : (v.Category.IsAlwaysFree ? 0m : v.Category.PublishCostTokens);
            publishCostTokens = basePublish is null
                ? null
                : FreePublishRules.EffectivePublishCost(basePublish.Value, freePublishUntil, DateTime.UtcNow);
        }

        var isIncomplete = v.Status == VacancyStatus.Draft && VacancyDraftCompletenessRules.IsIncomplete(v);
        var displayStatus = v.PublishOnVerification
            && v.Status is VacancyStatus.Draft or VacancyStatus.PendingApproval
            ? "ReadyOnVerification"
            : v.Status == VacancyStatus.Draft && isIncomplete
                ? "DraftIncomplete"
                : v.Status.ToString();
        var barrier = includeDescription ? MapBarrier(v.BarrierRequirementsJson) : default;

        return new VacancyListItemDto(
            v.Id,
            v.Title,
            includeDescription ? v.Description : null,
            hourly,
            v.StartDate,
            v.EndDate,
            v.Status.ToString(),
            v.CompanyId,
            display.DisplayName,
            display.DisplayAddress,
            VacancyImageUrls.Normalize(display.DisplayLogoUrl),
            includeDescription
                ? VacancyImageUrls.Normalize(v.ImageUrl)
                : VacancyImageUrls.ForCard(
                    v.ImageUrl,
                    display.DisplayLogoUrl,
                    v.Id,
                    VacancyImageUrls.FirstSlug(v.WorkTypes)),
            display.Latitude,
            display.Longitude,
            TransportLabels.Expand(v.RequiredTransport),
            showWage,
            travelMinutes,
            distanceKm,
            featured,
            featured ? v.HighlightedUntil : null,
            v.ExtensionCount,
            v.VideoUrl,
            v.SalaryTableId,
            wageByAge,
            resolvedForAge,
            WorkTypeLabels.ResolveLabels(v.WorkTypes, v.WorkTypeLabels) ?? [],
            impressionCount,
            clickCount,
            applicationCount,
            v.RequiredDrivingLicense,
            v.RequiredEducation,
            v.MinimumEmployers,
            v.FulfilledByApplicationId,
            v.CreatedVia.ToString(),
            v.MinHoursPerWeek,
            v.MaxHoursPerWeek,
            v.FlexibleTimes,
            includeDescription ? v.ScheduleJson : null,
            v.LegalWorksAfter19,
            v.LegalNightShift23To06,
            v.LegalAdultSupervisorPresent,
            v.LegalHandlesMoneyOrClosing,
            v.LegalHeavyOrHazardousWork,
            shareCount,
            likeCount,
            display.OfferedByLabel,
            v.ShowClientAddressOnMap,
            v.IntermediaryCompanyId,
            v.Kind.ToString(),
            v.ExclusivitySettingId,
            v.ExclusivitySetting?.Name,
            v.ExclusivitySetting?.IsOpenOption ?? true,
            // Domain needed for apply UX; student-number regex stays server-side only.
            v.ExclusivitySetting?.SchoolDomain,
            ExclusivityStudentNumberPattern: null,
            v.ExclusivitySetting?.Educations?
                .Where(e => e.IsActive)
                .OrderBy(e => e.SortOrder)
                .Select(e => e.Name)
                .ToList(),
            v.CategoryId,
            v.Category?.Name,
            v.Category?.ColorHex,
            includeCategoryInternals
                && (v.Category is null || (v.Category.HighlightAvailable && !v.Category.IsAlwaysFree)),
            includeCategoryInternals
                && (v.Category is null || (v.Category.PushBomAvailable && !v.Category.IsAlwaysFree)),
            publishCostTokens,
            includeCategoryInternals
                ? (v.Category is null ? null : (v.Category.IsAlwaysFree ? 0m : v.Category.HighlightCostTokens))
                : null,
            includeCategoryInternals ? v.Category?.PushBomCostTokens : null,
            includeCategoryInternals
                && (v.Category is null
                    || (v.Category.PushBomAvailable && !v.Category.IsAlwaysFree && v.Category.PushBomCostTokens is null)),
            includeCategoryInternals ? DeserializeCategoryFields(v.CategoryFieldsJson) : null,
            v.SuitableFor65Plus,
            // KB-FALLBACK(A): redact end-client KvK/vestiging on candidate public DTOs in hidden mode.
            Jobsy.Core.Rules.KandidaatBanen.KbHiddenIntermediaryMask.RedactClientPublicPaths(
                v.IntermediaryCompanyId, v.ShowClientAddressOnMap)
                ? null
                : CompanyPublicPaths.NormalizeKvkNumber(v.Company?.KvkNumber),
            Jobsy.Core.Rules.KandidaatBanen.KbHiddenIntermediaryMask.RedactClientPublicPaths(
                v.IntermediaryCompanyId, v.ShowClientAddressOnMap)
                ? null
                : CompanyPublicPaths.TryParseVestigingsnummer(
                    v.Company?.KvkEstablishmentId,
                    CompanyPublicPaths.NormalizeKvkNumber(v.Company?.KvkNumber)),
            v.ContentModerationPassed,
            isIncomplete,
            displayStatus,
            moderationWarning,
            v.RequireEmailVerification,
            includeCategoryInternals ? v.EngagementReminderTip : null,
            includeCategoryInternals ? v.EngagementReminderSentAtUtc : null,
            v.MinimumReferences,
            CulturePillars: CulturePillarCatalog.Deserialize(v.CulturePillarsJson).ToList(),
            BarrierKind: barrier.Kind,
            BarrierDiplomas: barrier.Diplomas,
            BarrierCertifications: barrier.Certs,
            BarrierMinExperienceYears: barrier.Years,
            BarrierMinExperienceHours: barrier.Hours,
            BarrierHardChecks: barrier.HardChecks,
            IsPreview: isPreview,
            PublishOnVerification: v.PublishOnVerification);
    }

    private static (string? Kind, IReadOnlyList<string>? Diplomas, IReadOnlyList<string>? Certs, int? Years, int? Hours, IReadOnlyList<string>? HardChecks)
        MapBarrier(string? json)
    {
        var req = VacancyBarrierCatalog.Deserialize(json);
        if (req.Barrier == VacancyBarrierKind.Low && !VacancyBarrierCatalog.HasFormalRequirements(req))
        {
            return ("Low", null, null, null, null, null);
        }

        return (
            req.Barrier.ToString(),
            req.Diplomas.Count == 0 ? null : req.Diplomas,
            req.Certifications.Count == 0 ? null : req.Certifications,
            req.MinExperienceYears,
            req.MinExperienceHours,
            req.HardChecks.Count == 0 ? null : req.HardChecks);
    }

    private static VacancyBarrierKind? ParseBarrierKind(string? raw)
    {
        if (string.Equals(raw, "high", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, nameof(VacancyBarrierKind.High), StringComparison.OrdinalIgnoreCase))
        {
            return VacancyBarrierKind.High;
        }

        if (string.Equals(raw, "low", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, nameof(VacancyBarrierKind.Low), StringComparison.OrdinalIgnoreCase))
        {
            return VacancyBarrierKind.Low;
        }

        return null;
    }

    private async Task<(Core.Entities.VacancyCategory? Category, string? Error)> ResolveCategoryAsync(
        Guid? categoryId,
        VacancyKind fallbackKind,
        CancellationToken cancellationToken)
    {
        await _categories.EnsureDefaultsAsync(cancellationToken);

        if (categoryId is Guid id)
        {
            var entity = await _categories.GetEntityAsync(id, cancellationToken);
            if (entity is null || !entity.IsActive)
            {
                return (null, "Ongeldige of inactieve vacaturecategorie.");
            }

            return (entity, null);
        }

        var defaultId = VacancyCategoryDefaults.ResolveDefaultId(fallbackKind);
        var fallback = await _categories.GetEntityAsync(defaultId, cancellationToken);
        if (fallback is null || !fallback.IsActive)
        {
            // Prefer any active category with the same placement kind, else any active category.
            var active = await _db.VacancyCategories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.PlacementKind == fallbackKind ? 0 : 1)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken);
            if (active is null)
            {
                return (null, "Geen actieve vacaturecategorie beschikbaar.");
            }

            return (active, null);
        }

        return (fallback, null);
    }

    private static string? SerializeCategoryFields(
        Core.Entities.VacancyCategory category,
        Dictionary<string, string>? values)
    {
        var allowed = VacancyCategoryExtraFields.DeserializeKeys(category.ExtraFieldsJson);
        if (allowed.Count == 0)
        {
            return null;
        }

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (values is not null)
        {
            foreach (var key in allowed)
            {
                if (values.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw))
                {
                    map[key] = raw.Trim();
                }
            }
        }

        return map.Count == 0 ? null : System.Text.Json.JsonSerializer.Serialize(map);
    }

    private static IReadOnlyDictionary<string, string>? DeserializeCategoryFields(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch
        {
            return null;
        }
    }

    private static bool HasCategoryField(string? json, string key)
        => DeserializeCategoryFields(json)?.TryGetValue(key, out var value) == true
            && !string.IsNullOrWhiteSpace(value);

    private async Task<(Guid? SettingId, string? Error)> ResolveExclusivitySettingIdAsync(
        VacancyKind kind,
        Guid? requestedId,
        CancellationToken cancellationToken)
    {
        if (kind != VacancyKind.Internship)
        {
            return (null, null);
        }

        if (!await _db.ExclusivitySettings.AnyAsync(cancellationToken))
        {
            _db.ExclusivitySettings.Add(new Core.Entities.ExclusivitySetting
            {
                Id = ExclusivityRules.DefaultOpenOptionId,
                Name = ExclusivityRules.DefaultOpenName,
                IsActive = true,
                IsOpenOption = true,
                SortOrder = 0,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (requestedId is Guid id)
        {
            var setting = await _db.ExclusivitySettings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive, cancellationToken);
            if (setting is null)
            {
                return (null, "Ongeldige of inactieve exclusiviteitsinstelling.");
            }

            return (setting.Id, null);
        }

        var openId = await _db.ExclusivitySettings.AsNoTracking()
            .Where(s => s.IsOpenOption && s.IsActive)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? ExclusivityRules.DefaultOpenOptionId;
        return (openId, null);
    }

    private async Task<Guid?> ResolveIntermediaryOrganizationIdAsync(
        User? actor,
        CancellationToken cancellationToken)
    {
        if (actor?.CompanyId is Guid primaryId)
        {
            var primary = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == primaryId, cancellationToken);
            if (primary?.Type == CompanyType.Intermediary)
            {
                return primary.ParentCompanyId ?? primary.Id;
            }
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        if (accessible is null || accessible.Count == 0)
        {
            return null;
        }

        return await _db.Companies.AsNoTracking()
            .Where(c => accessible.Contains(c.Id) && c.Type == CompanyType.Intermediary)
            .Select(c => (Guid?)(c.ParentCompanyId ?? c.Id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string? ApplyHoursAndSchedule(Core.Entities.Vacancy vacancy, CreateVacancyRequest request)
    {
        if (request.MinHoursPerWeek is not null || request.MaxHoursPerWeek is not null)
        {
            var min = request.MinHoursPerWeek ?? request.MaxHoursPerWeek!.Value;
            var max = request.MaxHoursPerWeek ?? request.MinHoursPerWeek!.Value;
            var hoursError = HoursRangeRules.Validate(min, max);
            if (hoursError is not null)
            {
                return hoursError;
            }

            vacancy.MinHoursPerWeek = min;
            vacancy.MaxHoursPerWeek = max;
        }

        // Legacy API clients omit schedule → tijden in overleg (FlexibleTimes).
        if (request.FlexibleTimes is null
            && (request.ScheduleSlots is null || request.ScheduleSlots.Count == 0))
        {
            vacancy.FlexibleTimes = true;
            vacancy.FlexibleScheduleSource = FlexibleScheduleSource.ApiEmpty.ToString();
            vacancy.ScheduleJson = null;
            return null;
        }

        var flexible = request.FlexibleTimes == true;
        var schedule = flexible
            ? SchedulePayload.Flexible(FlexibleScheduleSource.Manual)
            : new SchedulePayload { FlexibleTimes = false };

        if (!flexible && request.ScheduleSlots is { Count: > 0 })
        {
            foreach (var (day, parts) in request.ScheduleSlots)
            {
                if (parts is { Length: > 0 })
                {
                    schedule.Slots[day] = parts.ToList();
                }
            }
        }

        schedule = schedule.Normalize();
        var scheduleError = schedule.Validate();
        if (scheduleError is not null)
        {
            return scheduleError;
        }

        vacancy.FlexibleTimes = schedule.FlexibleTimes;
        vacancy.FlexibleScheduleSource = schedule.FlexibleTimes
            ? (schedule.FlexibleSource ?? FlexibleScheduleSource.Manual).ToString()
            : null;
        vacancy.ScheduleJson = schedule.FlexibleTimes
            ? null
            : System.Text.Json.JsonSerializer.Serialize(schedule);

        return null;
    }

    private static void ApplyLegalFlags(Core.Entities.Vacancy vacancy, CreateVacancyRequest request)
    {
        vacancy.LegalWorksAfter19 = request.LegalWorksAfter19;
        vacancy.LegalNightShift23To06 = request.LegalNightShift23To06;
        vacancy.LegalAdultSupervisorPresent = request.LegalAdultSupervisorPresent;
        vacancy.LegalHandlesMoneyOrClosing = request.LegalHandlesMoneyOrClosing;
        vacancy.LegalHeavyOrHazardousWork = request.LegalHeavyOrHazardousWork;
    }

    private async Task<VacancyListItemDto> AttachCandidateMatchAsync(
        VacancyListItemDto dto,
        Core.Entities.Vacancy vacancy,
        int? travelMinutes,
        CancellationToken cancellationToken)
    {
        if (!_companyAuth.IsCandidate(User))
        {
            return dto;
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        var matchContext = await _profileMatch.TryLoadForPrincipalAsync(User, cancellationToken);
        if (user is null || matchContext is null)
        {
            return dto;
        }

        var record = VacancyDiscoveryIndex.ToRecord(vacancy);
        var matches = await _profileMatch.ScoreAsync(matchContext, [(record, travelMinutes)], cancellationToken);
        if (!matches.TryGetValue(vacancy.Id, out var match))
        {
            return dto;
        }

        var cultureStatus = InsightsStatuses.Ready;
        if (match.CultureFit is { } local)
        {
            // Never wait on OpenAI — stored or local + enqueue refine.
            var (resolved, status) = await _cultureFit.ResolveForGetAsync(
                user.Id,
                vacancy.Id,
                record.CulturePillars ?? [],
                matchContext.Competencies,
                matchContext.CultureScores,
                local,
                cancellationToken);
            cultureStatus = status;
            if (resolved is not null)
            {
                match = CloneMatchWithCulture(match, resolved);
            }
        }

        var gate = CandidateFitGate.FromContext(matchContext);
        var dislikeReasons = await LoadDislikeReasonsAsync(user.Id, [vacancy.Id], cancellationToken);
        dislikeReasons.TryGetValue(vacancy.Id, out var rankLower);
        return WithCandidateMatch(dto, match, gate, rankLower, cultureStatus);
    }

    private static ProfileVacancyMatch CloneMatchWithCulture(ProfileVacancyMatch match, CultureFitResult culture)
    {
        var why = match.Why.Where(w => w.Kind != "culture").ToList();
        var gaps = match.Gaps.Where(g => g.Kind != "culture").ToList();
        var point = new ProfileMatchExplainPoint("culture", culture.Band, culture.Why);
        if (culture.Band == "low")
        {
            gaps.Insert(0, point);
        }
        else
        {
            why.Insert(0, point);
        }

        return new ProfileVacancyMatch
        {
            VacancyId = match.VacancyId,
            VacancyTitle = match.VacancyTitle,
            TotalPercent = match.TotalPercent,
            Core = match.Core,
            ExperienceScore01 = match.ExperienceScore01,
            CompetencyScore01 = match.CompetencyScore01,
            CompetencyDim01 = match.CompetencyDim01,
            InterestScore01 = match.InterestScore01,
            CultureDim01 = match.CultureDim01,
            ValuesFit01 = match.ValuesFit01,
            CultureFit = culture,
            EngagementBonus = match.EngagementBonus,
            IsBroadMatch = match.IsBroadMatch,
            MatchRationale = match.MatchRationale,
            Why = why,
            Gaps = gaps,
            ColorBand = match.ColorBand
        };
    }

    private static VacancyListItemDto WithCandidateMatch(
        VacancyListItemDto dto,
        ProfileVacancyMatch match,
        CandidateFitGate gate,
        string? rankLowerReason = null,
        string cultureFitStatus = InsightsStatuses.Ready)
    {
        var applied = CandidateFitApply.Apply(match, gate);
        CandidateFitDimensionsDto? dims = null;
        if (applied.FitDimensions is { } d)
        {
            dims = new CandidateFitDimensionsDto(d.Culture, d.Values, d.Competencies, d.Interests);
        }

        return dto with
        {
            MatchPercent = applied.MatchPercent,
            MatchColorBand = applied.MatchColorBand,
            MatchWhySummary = applied.FitWhyLineNl ?? ProfileVacancyMatchCalculator.SummaryLine(match),
            MatchWhy = PreferRationaleWhy(match),
            MatchGaps = match.Gaps.Select(g => g.Text).ToList(),
            IsBroadMatch = match.IsBroadMatch,
            MatchRationale = match.MatchRationale,
            CultureFitPercent = match.CultureFit?.Percent,
            CultureFitBand = match.CultureFit?.Band,
            CultureFitLabel = match.CultureFit?.Label,
            CultureFitWhy = match.CultureFit?.Why,
            CultureFitStatus = cultureFitStatus,
            FitGate = applied.FitGate,
            FitPercent = applied.FitPercent,
            FitBand = applied.FitBand,
            FitWhyLine = applied.FitWhyLineNl,
            FitWhyKinds = applied.FitWhyKinds.Count > 0 ? applied.FitWhyKinds : null,
            FitDimensions = dims,
            RankLowerReason = rankLowerReason
        };
    }

    /// <summary>
    /// KB-FALLBACK(D): <see cref="IKbDislikeSource"/> returns none until paspoort 06 lands.
    /// </summary>
    private async Task<Dictionary<Guid, string>> LoadDislikeReasonsAsync(
        Guid candidateUserId,
        IEnumerable<Guid> vacancyIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, string>();
        foreach (var vacancyId in vacancyIds)
        {
            var codes = await _dislikeSource.GetMatchingDislikeCodesAsync(
                candidateUserId, vacancyId, cancellationToken);
            if (codes.Count == 0)
            {
                continue;
            }

            // First matching dislike becomes the RankLowerReason localization key.
            result[vacancyId] = $"Kb.Dislike.{codes[0]}";
        }

        return result;
    }

    private static IReadOnlyList<string> PreferRationaleWhy(ProfileVacancyMatch match)
    {
        var lines = match.Why.Select(w => w.Text).ToList();
        if (match.IsBroadMatch
            && !string.IsNullOrWhiteSpace(match.MatchRationale)
            && !lines.Contains(match.MatchRationale, StringComparer.Ordinal))
        {
            lines.Insert(0, match.MatchRationale);
        }

        return lines;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string[] NormalizeBranchLabels(IEnumerable<string>? labels) =>
        (labels ?? [])
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(WorkTypeLabels.MaxPerVacancy)
            .Select(x => x!)
            .ToArray();

    private async Task<bool> AreBranchLabelsAllowedAsync(string[] labels, CancellationToken cancellationToken)
    {
        if (labels.Length == 0)
        {
            return false;
        }

        var allowed = await _db.MasterdataOptions.AsNoTracking()
            .Where(o => o.Category == MasterdataCategories.Branch && o.IsActive && o.ShowOnVacancy)
            .Select(o => o.Value)
            .ToListAsync(cancellationToken);

        if (allowed.Count == 0)
        {
            // Seed not applied yet — fall back to built-in labels.
            allowed = WorkTypeLabels.All.ToList();
        }

        return labels.All(l => allowed.Contains(l, StringComparer.OrdinalIgnoreCase));
    }
}
