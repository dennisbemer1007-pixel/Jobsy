using Jobsy.Api.Authorization;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
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

    public VacanciesController(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        IVacancyProductService products,
        IRoutingService routing,
        IUserLookupService users,
        ISalaryService salary,
        IVacancyContentModerationService moderation,
        ITranslationService translation)
    {
        _db = db;
        _companyAuth = companyAuth;
        _products = products;
        _routing = routing;
        _users = users;
        _salary = salary;
        _moderation = moderation;
        _translation = translation;
    }

    /// <summary>
    /// Public Funda feed: all currently active vacancies.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<VacancyListItemDto>>> GetActive(
        CancellationToken cancellationToken)
    {
        var showWage = await CanViewerSeeWageAsync(cancellationToken);
        var targetLanguage = await ResolveTargetLanguageAsync(cancellationToken);
        var vacancies = await LoadActiveVacanciesAsync(cancellationToken);
        var mapped = new List<VacancyListItemDto>(vacancies.Count);
        foreach (var v in vacancies)
        {
            mapped.Add(await MapToDtoAsync(v, showWage, targetLanguage, cancellationToken: cancellationToken));
        }

        return Ok(mapped);
    }

    /// <summary>
    /// Banenkaart discover: without origin returns all active vacancies (optional workType/wage).
    /// With origin, filters by transport, travel time and optional radius via IRoutingService.
    /// Optional ageYears resolves salary-table wages; min/max hourly filters apply when age is set.
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
        [FromQuery] string? workType = null,
        CancellationToken cancellationToken = default)
    {
        maxMinutes = Math.Clamp(maxMinutes, 5, 90);
        int? age = ageYears is int a ? Math.Clamp(a, 15, 67) : null;
        var mode = TransportLabels.Parse(transport);
        var showWage = age is not null || await CanViewerSeeWageAsync(cancellationToken);
        var targetLanguage = await ResolveTargetLanguageAsync(cancellationToken);
        var vacancies = await LoadActiveVacanciesAsync(cancellationToken);

        var workTypeFiltered = vacancies
            .Where(v => WorkTypeLabels.MatchesFilter(v.WorkTypes, workType))
            .ToList();

        List<VacancyListItemDto> results;
        if (originLat is null || originLng is null)
        {
            // No origin: show all matching work-type vacancies (transport is only a routing preference once located).
            results = new List<VacancyListItemDto>(workTypeFiltered.Count);
            foreach (var v in workTypeFiltered.OrderBy(v => v.Title))
            {
                results.Add(await MapToDtoAsync(v, showWage, targetLanguage, age, cancellationToken: cancellationToken));
            }
        }
        else
        {
            var transportFiltered = workTypeFiltered
                .Where(v => TransportLabels.MatchesRequired(TransportLabels.Expand(v.RequiredTransport), transport))
                .ToList();

            var lat = originLat.Value;
            var lng = originLng.Value;

            var routeTasks = transportFiltered.Select(async vacancy =>
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
            var filtered = routed
                .Where(r => !(radiusKm is > 0 && r.distanceKm > radiusKm.Value))
                .Where(r => r.travelMinutes <= maxMinutes)
                .OrderBy(r => r.travelMinutes)
                .ThenBy(r => r.vacancy.Title)
                .ToList();

            results = new List<VacancyListItemDto>(filtered.Count);
            foreach (var r in filtered)
            {
                results.Add(await MapToDtoAsync(
                    r.vacancy,
                    showWage,
                    targetLanguage,
                    age,
                    r.travelMinutes,
                    Math.Round(r.distanceKm, 2),
                    cancellationToken));
            }
        }

        if (age is not null && (minHourlyWage is not null || maxHourlyWage is not null))
        {
            results = results
                .Where(v => v.HourlyWage is decimal wage
                    && (minHourlyWage is null || wage >= minHourlyWage)
                    && (maxHourlyWage is null || wage <= maxHourlyWage))
                .ToList();
        }

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
        if (User.Identity?.IsAuthenticated == true
            && (_companyAuth.IsAdmin(User) || _companyAuth.IsEmployer(User))
            && await _companyAuth.CanAccessCompanyAsync(User, vacancy.CompanyId, cancellationToken))
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
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    public async Task<ActionResult<IEnumerable<VacancyListItemDto>>> GetManaged(
        CancellationToken cancellationToken)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        var query = _db.Vacancies.AsNoTracking().Include(v => v.Company).AsQueryable();

        if (accessible is not null)
        {
            query = query.Where(v => accessible.Contains(v.CompanyId));
        }

        var vacancies = await query.OrderBy(v => v.Title).ToListAsync(cancellationToken);
        return Ok(vacancies.Select(v => MapToDto(v, showWage: true)));
    }

    /// <summary>
    /// Create a vacancy for a company the employer is allowed to manage.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    [RequireCompanyAccess]
    public async Task<ActionResult<VacancyListItemDto>> Create(
        [FromBody] CreateVacancyRequest request,
        CancellationToken cancellationToken)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);
        if (company is null)
        {
            return NotFound(new { message = "Bedrijf niet gevonden." });
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

        // Prefer explicit wage only when no table adult rate could be resolved; otherwise table wins.
        var hourlyWage = adultRate > 0 ? adultRate : request.HourlyWage;

        if (!_salary.MeetsMinimumWage(hourlyWage, ageYears: 21))
        {
            return BadRequest(new { message = "Uurloon ligt onder het wettelijk minimumloon (21+)." });
        }

        if (!WorkTypeLabels.IsValidSelection(request.WorkTypes))
        {
            return BadRequest(new { message = $"Kies 1 of {WorkTypeLabels.MaxPerVacancy} branches." });
        }

        var imageUrl = HtmlSanitize.NormalizeMediaUrl(request.ImageUrl);
        var videoUrl = HtmlSanitize.NormalizeMediaUrl(request.VideoUrl);
        if (request.ImageUrl is not null && imageUrl is null)
        {
            return BadRequest(new { message = "Ongeldige afbeelding-URL (alleen http/https)." });
        }

        if (request.VideoUrl is not null && videoUrl is null)
        {
            return BadRequest(new { message = "Ongeldige video-URL (alleen http/https)." });
        }

        var moderation = await _moderation.CheckAsync(request.Title, request.Description, cancellationToken);
        if (!moderation.IsAllowed)
        {
            return UnprocessableEntity(new
            {
                code = VacancyModerationCodes.ContentModeration,
                message = moderation.Warning,
                suggestion = moderation.Suggestion
            });
        }

        var vacancy = new Core.Entities.Vacancy
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            HourlyWage = hourlyWage,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = VacancyStatus.Draft,
            CompanyId = request.CompanyId,
            Location = company.Location,
            RequiredTransport = request.RequiredTransport,
            WorkTypes = request.WorkTypes,
            ImageUrl = imageUrl,
            VideoUrl = videoUrl,
            SalaryTableId = tableId
        };

        _db.Vacancies.Add(vacancy);
        await _db.SaveChangesAsync(cancellationToken);

        vacancy.Company = company;
        return CreatedAtAction(nameof(GetById), new { id = vacancy.Id }, MapToDto(vacancy, showWage: true));
    }

    [HttpPost("publish")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    public async Task<ActionResult<VacancyProductActionResultDto>> Publish(
        [FromBody] PublishVacancyRequest request,
        CancellationToken cancellationToken)
    {
        var vacancy = await LoadManagedVacancyAsync(request.VacancyId, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var access = await EnsureCompanyAccessAsync(vacancy.CompanyId, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        var result = await _products.PublishAsync(
            vacancy,
            new VacancyPublishOptions(request.Highlight, request.PushBom, request.Extend),
            actor?.Id,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(ToProductResult(result));
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

        var access = await EnsureCompanyAccessAsync(vacancy.CompanyId, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        var result = await _products.ApprovePublishAsync(vacancy, actor?.Id, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(ToProductResult(result));
    }

    [HttpPost("{id:guid}/highlight")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    public async Task<ActionResult<VacancyProductActionResultDto>> Highlight(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await RunProductAsync(id, (v, actorId, ct) => _products.HighlightAsync(v, actorId, ct), cancellationToken);
    }

    [HttpGet("{id:guid}/pushbom/preview")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    public async Task<ActionResult<PushBomPreviewDto>> PreviewPushBom(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vacancy = await LoadManagedVacancyAsync(id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var access = await EnsureCompanyAccessAsync(vacancy.CompanyId, cancellationToken);
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
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    public async Task<ActionResult<VacancyProductActionResultDto>> PushBom(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await RunProductAsync(id, (v, actorId, ct) => _products.PushBomAsync(v, actorId, ct), cancellationToken);
    }

    [HttpPost("{id:guid}/extend")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    public async Task<ActionResult<VacancyProductActionResultDto>> Extend(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await RunProductAsync(id, (v, actorId, ct) => _products.ExtendAsync(v, actorId, ct), cancellationToken);
    }

    [HttpPost("{id:guid}/inactive")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    public async Task<ActionResult<VacancyProductActionResultDto>> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vacancy = await LoadManagedVacancyAsync(id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var access = await EnsureCompanyAccessAsync(vacancy.CompanyId, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        var result = await _products.DeactivateAsync(vacancy, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(ToProductResult(result));
    }

    [HttpPost("batch")]
    [Authorize(Roles = $"{JobsyRoles.Intermediary},{JobsyRoles.EnterpriseManager},{JobsyRoles.RegionalManager},{JobsyRoles.Admin}")]
    public async Task<ActionResult<IEnumerable<VacancyListItemDto>>> CreateBatch(
        [FromBody] BatchVacancyRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CompanyIds is null || request.CompanyIds.Length == 0)
        {
            return BadRequest(new { message = "Selecteer minstens één locatie." });
        }

        var moderation = await _moderation.CheckAsync(request.Title, request.Description, cancellationToken);
        if (!moderation.IsAllowed)
        {
            return UnprocessableEntity(new
            {
                code = VacancyModerationCodes.ContentModeration,
                message = moderation.Warning,
                suggestion = moderation.Suggestion
            });
        }

        if (!WorkTypeLabels.IsValidSelection(request.WorkTypes))
        {
            return BadRequest(new { message = $"Kies 1 of {WorkTypeLabels.MaxPerVacancy} branches." });
        }

        var created = new List<Core.Entities.Vacancy>();
        foreach (var companyId in request.CompanyIds.Distinct())
        {
            try
            {
                await _companyAuth.EnsureCanAccessCompanyAsync(User, companyId, cancellationToken);
            }
            catch (ForbiddenCompanyAccessException)
            {
                return Forbid();
            }

            var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
            if (company is null)
            {
                continue;
            }

            var organizationId = company.ParentCompanyId ?? company.Id;
            var salaryTable = await _db.CompanySalaryTables
                .Include(t => t.Rates)
                .FirstOrDefaultAsync(
                    t => t.IsActive
                         && t.IsSystemWml
                         && t.CompanyId == organizationId,
                    cancellationToken);
            if (salaryTable is null || salaryTable.Rates.Count == 0)
            {
                return BadRequest(new { message = $"Geen actief Wettelijk Minimumloon voor vestiging {company.Name}." });
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
                return BadRequest(new { message = $"Uurloon ligt onder het wettelijk minimumloon voor vestiging {company.Name}." });
            }

            var vacancy = new Core.Entities.Vacancy
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                HourlyWage = hourlyWage,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = VacancyStatus.Draft,
                CompanyId = companyId,
                Location = company.Location,
                RequiredTransport = request.RequiredTransport,
                WorkTypes = request.WorkTypes,
                SalaryTableId = salaryTable.Id
            };
            _db.Vacancies.Add(vacancy);
            vacancy.Company = company;
            created.Add(vacancy);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(created.Select(v => MapToDto(v, showWage: true)));
    }

    private async Task<ActionResult<VacancyProductActionResultDto>> RunProductAsync(
        Guid vacancyId,
        Func<Core.Entities.Vacancy, Guid?, CancellationToken, Task<VacancyProductOutcome>> action,
        CancellationToken cancellationToken)
    {
        var vacancy = await LoadManagedVacancyAsync(vacancyId, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var access = await EnsureCompanyAccessAsync(vacancy.CompanyId, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        var result = await action(vacancy, actor?.Id, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(ToProductResult(result));
    }

    private async Task<Core.Entities.Vacancy?> LoadManagedVacancyAsync(Guid id, CancellationToken cancellationToken)
        => await _db.Vacancies
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    private async Task<ActionResult?> EnsureCompanyAccessAsync(Guid companyId, CancellationToken cancellationToken)
    {
        try
        {
            await _companyAuth.EnsureCanAccessCompanyAsync(User, companyId, cancellationToken);
            return null;
        }
        catch (ForbiddenCompanyAccessException)
        {
            return Forbid();
        }
    }

    private VacancyProductActionResultDto ToProductResult(VacancyProductOutcome result) => new(
        MapToDto(result.Vacancy, showWage: true),
        result.PendingApproval,
        result.ErrorMessage,
        result.PushBomRecipientCount);

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

    private async Task<List<Core.Entities.Vacancy>> LoadActiveVacanciesAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _db.Vacancies
            .AsNoTracking()
            .Include(v => v.Company)
            .Include(v => v.SalaryTable!)
                .ThenInclude(t => t.Rates)
            .Where(v =>
                v.Status == VacancyStatus.Active
                && v.StartDate <= today
                && v.EndDate >= today)
            .OrderBy(v => v.Title)
            .ToListAsync(cancellationToken);
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
        CancellationToken cancellationToken = default)
    {
        var dto = MapToDto(v, showWage, ageYears, travelMinutes, distanceKm);
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

    private static VacancyListItemDto MapToDto(
        Core.Entities.Vacancy v,
        bool showWage,
        int? ageYears = null,
        int? travelMinutes = null,
        double? distanceKm = null)
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

        return new VacancyListItemDto(
            v.Id,
            v.Title,
            v.Description,
            hourly,
            v.StartDate,
            v.EndDate,
            v.Status.ToString(),
            v.CompanyId,
            v.Company.Name,
            v.Company.Address,
            v.Company.LogoUrl,
            v.ImageUrl,
            v.Location.Latitude,
            v.Location.Longitude,
            TransportLabels.Expand(v.RequiredTransport),
            showWage,
            travelMinutes,
            distanceKm,
            v.IsHighlighted,
            v.ExtensionCount,
            v.VideoUrl,
            v.SalaryTableId,
            wageByAge,
            resolvedForAge,
            WorkTypeLabels.Expand(v.WorkTypes));
    }
}
