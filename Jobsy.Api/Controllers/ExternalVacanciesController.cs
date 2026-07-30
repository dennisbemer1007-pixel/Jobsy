using Jobsy.Api.Authorization;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

/// <summary>
/// External vacancy API for ATS/partner integrations. Authenticated solely via <c>X-API-Key</c>.
/// Tenancy is derived from the key's company — callers cannot access other companies.
/// </summary>
[ApiController]
[Route("api/external/vacancies")]
[Authorize(Policy = JobsyPolicies.RequireApiKey)]
[EnableRateLimiting("public-write")]
public class ExternalVacanciesController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly ISalaryService _salary;
    private readonly IVacancyContentModerationService _moderation;

    public ExternalVacanciesController(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        ISalaryService salary,
        IVacancyContentModerationService moderation)
    {
        _db = db;
        _companyAuth = companyAuth;
        _salary = salary;
        _moderation = moderation;
    }

    /// <summary>Create a vacancy for a company owned by this API key.</summary>
    [HttpPost]
    [RequireCompanyAccess]
    public async Task<ActionResult<ExternalVacancyStatusDto>> Create(
        [FromBody] CreateVacancyRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null
            || string.IsNullOrWhiteSpace(request.Title)
            || string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { message = "Titel en omschrijving zijn verplicht." });
        }

        if (request.EndDate < request.StartDate)
        {
            return BadRequest(new { message = "Einddatum mag niet vóór de startdatum liggen." });
        }

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
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            HourlyWage = hourlyWage,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = VacancyStatus.Draft,
            CompanyId = request.CompanyId,
            CreatedVia = VacancySource.Api,
            CreatedAtUtc = DateTime.UtcNow,
            Location = company.Location,
            RequiredTransport = request.RequiredTransport,
            WorkTypes = WorkTypeLabels.Combine(branchLabels),
            WorkTypeLabels = WorkTypeLabels.CombineStored(branchLabels),
            ImageUrl = imageUrl,
            VideoUrl = videoUrl,
            SalaryTableId = tableId,
            RequiredDrivingLicense = string.IsNullOrWhiteSpace(request.RequiredDrivingLicense)
                ? null
                : request.RequiredDrivingLicense.Trim(),
            RequiredEducation = string.IsNullOrWhiteSpace(request.RequiredEducation)
                ? null
                : request.RequiredEducation.Trim(),
            MinimumEmployers = request.MinimumEmployers is > 0 ? request.MinimumEmployers : null
        };

        _db.Vacancies.Add(vacancy);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetStatus), new { id = vacancy.Id }, ToStatusDto(vacancy));
    }

    /// <summary>Update a vacancy that belongs to this API key's company scope.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExternalVacancyStatusDto>> Update(
        Guid id,
        [FromBody] UpdateExternalVacancyRequest request,
        CancellationToken cancellationToken)
    {
        var vacancy = await _db.Vacancies.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vacancy is null
            || !await _companyAuth.CanAccessCompanyAsync(User, vacancy.CompanyId, cancellationToken))
        {
            // Hide cross-tenant existence from other API keys.
            return NotFound(new { message = "Vacature niet gevonden." });
        }

        if (request.CompanyId is Guid bodyCompanyId && bodyCompanyId != vacancy.CompanyId)
        {
            return BadRequest(new { message = "CompanyId mag niet worden gewijzigd via de API." });
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            vacancy.Title = request.Title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            vacancy.Description = request.Description.Trim();
        }

        if (request.StartDate is DateOnly start)
        {
            vacancy.StartDate = start;
        }

        if (request.EndDate is DateOnly end)
        {
            vacancy.EndDate = end;
        }

        if (vacancy.EndDate < vacancy.StartDate)
        {
            return BadRequest(new { message = "Einddatum mag niet vóór de startdatum liggen." });
        }

        if (request.RequiredTransport is TransportMode transport)
        {
            vacancy.RequiredTransport = transport;
        }

        if (request.WorkTypes is { Length: > 0 })
        {
            var branchLabels = NormalizeBranchLabels(request.WorkTypes);
            if (branchLabels.Length is < 1 or > WorkTypeLabels.MaxPerVacancy)
            {
                return BadRequest(new { message = $"Kies 1 of {WorkTypeLabels.MaxPerVacancy} branches." });
            }

            if (!await AreBranchLabelsAllowedAsync(branchLabels, cancellationToken))
            {
                return BadRequest(new { message = "Een of meer branches zijn ongeldig of niet actief." });
            }

            vacancy.WorkTypes = WorkTypeLabels.Combine(branchLabels);
            vacancy.WorkTypeLabels = WorkTypeLabels.CombineStored(branchLabels);
        }

        if (request.ImageUrl is not null)
        {
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

            vacancy.ImageUrl = imageUrl;
        }

        if (request.VideoUrl is not null)
        {
            var videoUrl = HtmlSanitize.NormalizeMediaUrl(request.VideoUrl);
            if (videoUrl is null && !string.IsNullOrWhiteSpace(request.VideoUrl))
            {
                return BadRequest(new { message = "Ongeldige video-URL (alleen http/https)." });
            }

            vacancy.VideoUrl = videoUrl;
        }

        if (request.RequiredDrivingLicense is not null)
        {
            vacancy.RequiredDrivingLicense = string.IsNullOrWhiteSpace(request.RequiredDrivingLicense)
                ? null
                : request.RequiredDrivingLicense.Trim();
        }

        if (request.RequiredEducation is not null)
        {
            vacancy.RequiredEducation = string.IsNullOrWhiteSpace(request.RequiredEducation)
                ? null
                : request.RequiredEducation.Trim();
        }

        if (request.MinimumEmployers is not null)
        {
            vacancy.MinimumEmployers = request.MinimumEmployers is > 0 ? request.MinimumEmployers : null;
        }

        if (request.Status is string statusRaw)
        {
            if (!Enum.TryParse<VacancyStatus>(statusRaw, ignoreCase: true, out var status)
                || status is not (VacancyStatus.Draft or VacancyStatus.Archived))
            {
                return BadRequest(new
                {
                    message = "Status via API mag alleen Draft of Archived zijn (publiceren gebeurt in Lobsy)."
                });
            }

            // Do not silently demote Active → Draft; only archive (or keep draft edits).
            if (vacancy.Status == VacancyStatus.Active && status == VacancyStatus.Draft)
            {
                return BadRequest(new
                {
                    message = "Actieve vacatures kunnen via API alleen op Archived worden gezet."
                });
            }

            if (vacancy.Status is VacancyStatus.PendingApproval or VacancyStatus.Fulfilled)
            {
                return BadRequest(new
                {
                    message = $"Vacature met status {vacancy.Status} kan niet via de API worden gewijzigd."
                });
            }

            vacancy.Status = status;
        }

        var moderation = await _moderation.CheckAsync(vacancy.Title, vacancy.Description, cancellationToken);
        if (!moderation.IsAllowed)
        {
            return UnprocessableEntity(new
            {
                code = VacancyModerationCodes.ContentModeration,
                message = moderation.Warning,
                suggestion = moderation.Suggestion
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToStatusDto(vacancy));
    }

    /// <summary>Status check for a vacancy owned by this API key's company.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExternalVacancyStatusDto>> GetStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vacancy = await _db.Vacancies.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vacancy is null
            || !await _companyAuth.CanAccessCompanyAsync(User, vacancy.CompanyId, cancellationToken))
        {
            return NotFound(new { message = "Vacature niet gevonden." });
        }

        return Ok(ToStatusDto(vacancy));
    }

    private static ExternalVacancyStatusDto ToStatusDto(Core.Entities.Vacancy v) =>
        new(v.Id, v.CompanyId, v.Title, v.Status.ToString(), v.CreatedVia.ToString(), v.StartDate, v.EndDate);

    private static string[] NormalizeBranchLabels(IEnumerable<string>? labels) =>
        (labels ?? [])
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(WorkTypeLabels.MaxPerVacancy)
            .Cast<string>()
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
            allowed = WorkTypeLabels.All.ToList();
        }

        return labels.All(l => allowed.Contains(l, StringComparer.OrdinalIgnoreCase));
    }
}
