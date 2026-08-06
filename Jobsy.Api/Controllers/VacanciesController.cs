using Jobsy.Api.Authorization;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Exceptions;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VacanciesController : ControllerBase
{
    /// <summary>Vacancy content is authored in Dutch unless a source language is stored later.</summary>
    private const string VacancySourceLanguage = JobsyLanguages.Default;

    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IVacancyProductService _products;
    private readonly IRoutingService _routing;
    private readonly IUserLookupService _users;
    private readonly ISalaryService _salary;
    private readonly IVacancyContentModerationService _moderation;
    private readonly ITranslationService _translation;
    private readonly IVacancyCategoryService _categories;
    private readonly IPlatformFeatureService _features;

    public VacanciesController(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        IVacancyProductService products,
        IRoutingService routing,
        IUserLookupService users,
        ISalaryService salary,
        IVacancyContentModerationService moderation,
        ITranslationService translation,
        IVacancyCategoryService categories,
        IPlatformFeatureService features)
    {
        _db = db;
        _companyAuth = companyAuth;
        _products = products;
        _routing = routing;
        _users = users;
        _salary = salary;
        _moderation = moderation;
        _translation = translation;
        _categories = categories;
        _features = features;
    }

    /// <summary>
    /// Public Funda feed: all currently active vacancies.
    /// List payloads omit full descriptions (detail endpoint keeps them).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<VacancyListItemDto>>> GetActive(
        CancellationToken cancellationToken)
    {
        var showWage = await CanViewerSeeWageAsync(cancellationToken);
        var targetLanguage = await ResolveTargetLanguageAsync(cancellationToken);
        var vacancies = await LoadActiveVacanciesAsync(origin: null, reachKm: null, cancellationToken);
        var mapped = await MapManyToDtoAsync(
            vacancies,
            showWage,
            targetLanguage,
            ageYears: null,
            includeDescription: false,
            cancellationToken);
        return Ok(mapped);
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
        var targetLanguage = await ResolveTargetLanguageAsync(cancellationToken);

        double? reachKm = null;
        Core.ValueObjects.GeoPoint? origin = null;
        if (originLat is not null && originLng is not null)
        {
            origin = new Core.ValueObjects.GeoPoint(originLat.Value, originLng.Value);
            reachKm = TravelReach.MaxCrowFliesKm(mode, maxMinutes, radiusKm);
        }

        var vacancies = await LoadActiveVacanciesAsync(
            origin,
            reachKm,
            cancellationToken,
            includeExclusivityEducations: false,
            includeSalaryRates: showWage);

        var companyFilter = companyId?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToHashSet();
        if (companyFilter is { Count: > 0 })
        {
            vacancies = vacancies.Where(v => companyFilter.Contains(v.CompanyId)).ToList();
        }

        var categoryFilter = categoryId?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToHashSet();

        var workTypeFiltered = vacancies
            .Where(v => VacancyCategoryDefaults.MatchesSelectedCategories(
                v.CategoryId,
                v.SuitableFor65Plus,
                categoryFilter))
            .Where(v => suitableFor65Plus != true
                || VacancyCategoryDefaults.MatchesSuitableFor65PlusFilter(v.CategoryId, v.SuitableFor65Plus))
            .Where(v => WorkTypeLabels.MatchesFilter(v.WorkTypes, v.WorkTypeLabels, workType))
            .Where(v => VacancyTextSearch.Matches(v, q))
            .Where(v => HoursRangeRules.MatchesFilter(
                v.MinHoursPerWeek,
                v.MaxHoursPerWeek,
                filterMinHours,
                filterMaxHours))
            .ToList();

        List<(Core.Entities.Vacancy Vacancy, int? TravelMinutes, double? DistanceKm)> candidates;
        if (originLat is null || originLng is null)
        {
            // No origin: show all matching work-type vacancies (transport is only a routing preference once located).
            candidates = workTypeFiltered
                .OrderBy(v => v.Title)
                .Select(v => (v, (int?)null, (double?)null))
                .ToList();
        }
        else
        {
            var transportFiltered = workTypeFiltered
                .Where(v => TransportLabels.MatchesRequired(TransportLabels.Expand(v.RequiredTransport), transport))
                .ToList();

            var lat = originLat.Value;
            var lng = originLng.Value;
            // Crow-flies shortlist already applied in LoadActiveVacanciesAsync when origin/reach set.
            var nearby = transportFiltered;

            var routeTasks = nearby.Select(async vacancy =>
            {
                var route = await _routing.GetRouteAsync(
                    lat,
                    lng,
                    vacancy.Location.Latitude,
                    vacancy.Location.Longitude,
                    mode,
                    cancellationToken);

                var travelMinutes = (int)Math.Ceiling(route.DurationSeconds / 60.0);
                var distanceKm = route.DistanceMeters / 1000.0;
                return (vacancy, travelMinutes, distanceKm);
            });

            var routed = await Task.WhenAll(routeTasks);
            candidates = routed
                .Where(r => !(radiusKm is > 0 && r.distanceKm > radiusKm.Value))
                .Where(r => r.travelMinutes <= maxMinutes)
                .OrderBy(r => r.travelMinutes)
                .ThenBy(r => r.vacancy.Title)
                .Select(r => (r.vacancy, (int?)r.travelMinutes, (double?)Math.Round(r.distanceKm, 2)))
                .ToList();
        }

        // Resolve wages before translation so min/max filters avoid OpenAI work on discarded rows.
        var wageReady = new List<(Core.Entities.Vacancy Vacancy, int? TravelMinutes, double? DistanceKm, VacancyListItemDto Dto)>(candidates.Count);
        foreach (var c in candidates)
        {
            var dto = MapToDto(c.Vacancy, showWage, age, c.TravelMinutes, c.DistanceKm, includeDescription: false);
            if (age is not null && (minHourlyWage is not null || maxHourlyWage is not null))
            {
                if (dto.HourlyWage is not decimal wage
                    || (minHourlyWage is not null && wage < minHourlyWage)
                    || (maxHourlyWage is not null && wage > maxHourlyWage))
                {
                    continue;
                }
            }

            wageReady.Add((c.Vacancy, c.TravelMinutes, c.DistanceKm, dto));
        }

        var results = await TranslateManyAsync(wageReady.Select(x => x.Dto).ToList(), targetLanguage, cancellationToken);
        return Ok(results);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
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
            return Ok(await MapWithOptionalRouteAsync(vacancy, originLat, originLng, transport, showWage, age, cancellationToken));
        }

        // Drafts / pending / archived: only for authenticated employers with company access (or admin).
        // Intermediaries may also access via IntermediaryCompanyId (end-client CompanyId alone is insufficient).
        if (User.Identity?.IsAuthenticated == true
            && (_companyAuth.IsAdmin(User) || _companyAuth.IsEmployer(User))
            && await CanManageVacancyAsync(vacancy, cancellationToken))
        {
            // Employers always see wage on managed vacancies.
            return Ok(await MapWithOptionalRouteAsync(vacancy, originLat, originLng, transport, showWage: true, age, cancellationToken));
        }

        return NotFound();
    }

    /// <summary>
    /// Employer-managed vacancies, scoped to companies the caller may access.
    /// Branch managers only see their own company.
    /// </summary>
    [HttpGet("manage")]
    [Authorize(Policy = JobsyPolicies.RequireAdminOrEmployer)]
    public async Task<ActionResult<IEnumerable<VacancyListItemDto>>> GetManaged(
        CancellationToken cancellationToken)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        if (accessible is { Count: 0 })
        {
            return Ok(Array.Empty<VacancyListItemDto>());
        }

        // Defense-in-depth: enable EF tenant filter for this manage request.
        CompanyTenantScope.Enforce(_db, accessible);

        var query = _db.Vacancies
            .AsNoTracking()
            .AsSplitQuery()
            .Include(v => v.Company)
            .Include(v => v.IntermediaryCompany)
            .Include(v => v.Category)
            .Include(v => v.ExclusivitySetting!)
                .ThenInclude(s => s.Educations)
            .AsQueryable();

        if (accessible is not null)
        {
            // End-client company OR intermediary org that posted the vacancy.
            query = query.Where(v =>
                accessible.Contains(v.CompanyId)
                || (v.IntermediaryCompanyId != null && accessible.Contains(v.IntermediaryCompanyId.Value)));
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
        var likeCounts = await _db.VacancyLikes.AsNoTracking()
            .Where(l => ids.Contains(l.VacancyId))
            .GroupBy(l => l.VacancyId)
            .Select(g => new { VacancyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VacancyId, x => x.Count, cancellationToken);

        var freePublishUntil = (await _features.GetAsync(cancellationToken)).FreePublishUntil;
        var mapped = new List<VacancyListItemDto>(vacancies.Count);
        foreach (var v in vacancies)
        {
            try
            {
                mapped.Add(MapToDto(
                    v,
                    showWage: true,
                    impressionCount: impressionCounts.GetValueOrDefault(v.Id),
                    clickCount: clickCounts.GetValueOrDefault(v.Id),
                    applicationCount: applicationCounts.GetValueOrDefault(v.Id),
                    shareCount: shareCounts.GetValueOrDefault(v.Id),
                    likeCount: likeCounts.GetValueOrDefault(v.Id),
                    includeDescription: false,
                    includeCategoryInternals: true,
                    freePublishUntil: freePublishUntil));
            }
            catch
            {
                // Skip corrupt rows so one bad vacancy cannot 500 the whole manage page.
            }
        }

        return Ok(mapped);
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

        if (vacancy.Status != VacancyStatus.Draft)
        {
            return BadRequest(new { message = "Alleen conceptvacatures kunnen worden bewerkt." });
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
            cancellationToken,
            allowPendingApproval: !canPurchase);

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
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(await ToProductResultAsync(result, cancellationToken));
    }

    [HttpPost("{id:guid}/highlight")]
    [Authorize(Roles = JobsyRoles.VacancyLifecycleRoles)]
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

    private async Task<Guid?> ResolveIntermediaryOrganizationIdAsync(CancellationToken cancellationToken)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor?.CompanyId is not Guid companyId)
        {
            return null;
        }

        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return null;
        }

        if (company.Type == CompanyType.Intermediary)
        {
            return company.Id;
        }

        if (company.ParentCompanyId is Guid parentId)
        {
            var parent = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == parentId, cancellationToken);
            if (parent?.Type == CompanyType.Intermediary)
            {
                return parent.Id;
            }
        }

        return null;
    }

    private async Task<VacancyProductActionResultDto> ToProductResultAsync(
        VacancyProductOutcome result,
        CancellationToken cancellationToken)
    {
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

    private async Task<List<Core.Entities.Vacancy>> LoadActiveVacanciesAsync(
        Core.ValueObjects.GeoPoint? origin,
        double? reachKm,
        CancellationToken cancellationToken,
        bool includeExclusivityEducations = true,
        bool includeSalaryRates = true)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // When origin is known, shortlist via PostGIS GIST (geometry degrees) before Includes.
        if (origin is not null && reachKm is > 0 && _db.Database.IsNpgsql())
        {
            var degrees = reachKm.Value / 111.32;
            var ids = await _db.Database
                .SqlQueryRaw<Guid>(
                    """
                    SELECT v."Id" AS "Value"
                    FROM "Vacancies" v
                    WHERE v."Status" = {0}
                      AND v."StartDate" <= {1}
                      AND v."EndDate" >= {1}
                      AND v."Location" IS NOT NULL
                      AND ST_DWithin(
                        v."Location",
                        ST_SetSRID(ST_MakePoint({2}, {3}), 4326),
                        {4})
                    """,
                    (int)VacancyStatus.Active,
                    today,
                    origin.Longitude,
                    origin.Latitude,
                    degrees)
                .ToListAsync(cancellationToken);

            if (ids.Count == 0)
            {
                return [];
            }

            return await BuildActiveVacancyQuery(includeExclusivityEducations, includeSalaryRates)
                .Where(v => ids.Contains(v.Id))
                .OrderBy(v => v.Title)
                .ToListAsync(cancellationToken);
        }

        var all = await BuildActiveVacancyQuery(includeExclusivityEducations, includeSalaryRates)
            .Where(v =>
                v.Status == VacancyStatus.Active
                && v.StartDate <= today
                && v.EndDate >= today)
            .OrderBy(v => v.Title)
            .ToListAsync(cancellationToken);

        if (origin is not null && reachKm is > 0)
        {
            return all.Where(v => GeoDistance.IsWithinKm(origin, v.Location, reachKm.Value)).ToList();
        }

        return all;
    }

    private IQueryable<Core.Entities.Vacancy> BuildActiveVacancyQuery(
        bool includeExclusivityEducations,
        bool includeSalaryRates)
    {
        IQueryable<Core.Entities.Vacancy> query = _db.Vacancies
            .AsNoTracking()
            .AsSplitQuery()
            .Include(v => v.Company)
            .Include(v => v.IntermediaryCompany)
            .Include(v => v.Category)
            .Include(v => v.ExclusivitySetting!);

        if (includeExclusivityEducations)
        {
            query = query.Include(v => v.ExclusivitySetting!)
                .ThenInclude(s => s.Educations);
        }

        if (includeSalaryRates)
        {
            query = query.Include(v => v.SalaryTable!)
                .ThenInclude(t => t.Rates);
        }

        return query;
    }

    private async Task<List<VacancyListItemDto>> MapManyToDtoAsync(
        IReadOnlyList<Core.Entities.Vacancy> vacancies,
        bool showWage,
        string targetLanguage,
        int? ageYears,
        bool includeDescription,
        CancellationToken cancellationToken,
        bool includeCategoryInternals = false)
    {
        var mapped = new List<VacancyListItemDto>(vacancies.Count);
        DateOnly? freePublishUntil = null;
        if (includeCategoryInternals)
        {
            freePublishUntil = (await _features.GetAsync(cancellationToken)).FreePublishUntil;
        }

        foreach (var v in vacancies)
        {
            mapped.Add(MapToDto(
                v,
                showWage,
                ageYears,
                includeDescription: includeDescription,
                includeCategoryInternals: includeCategoryInternals,
                freePublishUntil: freePublishUntil));
        }

        return await TranslateManyAsync(mapped, targetLanguage, cancellationToken);
    }

    private async Task<List<VacancyListItemDto>> TranslateManyAsync(
        List<VacancyListItemDto> items,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0 || JobsyLanguages.AreSame(VacancySourceLanguage, targetLanguage))
        {
            return items;
        }

        // Bound OpenAI concurrency on cold caches; warm cache stays cheap.
        using var gate = new SemaphoreSlim(4);
        var tasks = items.Select(async dto =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                return await TranslateDtoAsync(dto, targetLanguage, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        });

        var translated = await Task.WhenAll(tasks);
        return translated.ToList();
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
            dto.Description,
            VacancySourceLanguage,
            targetLanguage,
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
        CancellationToken cancellationToken = default)
    {
        var targetLanguage = await ResolveTargetLanguageAsync(cancellationToken);

        if (originLat is null || originLng is null)
        {
            return await MapToDtoAsync(vacancy, showWage, targetLanguage, ageYears, cancellationToken: cancellationToken);
        }

        var mode = TransportLabels.Parse(transport);
        var route = await _routing.GetRouteAsync(
            originLat.Value,
            originLng.Value,
            vacancy.Location.Latitude,
            vacancy.Location.Longitude,
            mode,
            cancellationToken);

        var travelMinutes = (int)Math.Ceiling(route.DurationSeconds / 60.0);
        var distanceKm = Math.Round(route.DistanceMeters / 1000.0, 2);
        return await MapToDtoAsync(
            vacancy,
            showWage,
            targetLanguage,
            ageYears,
            travelMinutes: travelMinutes,
            distanceKm: distanceKm,
            cancellationToken: cancellationToken);
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
        CancellationToken cancellationToken = default)
    {
        var dto = MapToDto(v, showWage, ageYears, travelMinutes, distanceKm, includeDescription: includeDescription);
        return await TranslateDtoAsync(dto, targetLanguage, cancellationToken);
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
        string? moderationWarning = null)
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
        var displayStatus = v.Status == VacancyStatus.Draft && isIncomplete
            ? "DraftIncomplete"
            : v.Status.ToString();

        return new VacancyListItemDto(
            v.Id,
            v.Title,
            includeDescription ? v.Description : string.Empty,
            hourly,
            v.StartDate,
            v.EndDate,
            v.Status.ToString(),
            v.CompanyId,
            display.DisplayName,
            display.DisplayAddress,
            display.DisplayLogoUrl,
            v.ImageUrl,
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
            v.ScheduleJson,
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
            CompanyPublicPaths.NormalizeKvkNumber(v.Company?.KvkNumber),
            CompanyPublicPaths.TryParseVestigingsnummer(
                v.Company?.KvkEstablishmentId,
                CompanyPublicPaths.NormalizeKvkNumber(v.Company?.KvkNumber)),
            v.ContentModerationPassed,
            isIncomplete,
            displayStatus,
            moderationWarning);
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
