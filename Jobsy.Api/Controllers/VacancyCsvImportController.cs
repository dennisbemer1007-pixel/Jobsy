using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

/// <summary>CSV batch import of vacancy drafts for organisations with the feature enabled.</summary>
[ApiController]
[Route("api/vacancies/csv-import")]
[Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.Admin}")]
[EnableRateLimiting("public-write")]
public class VacancyCsvImportController : ControllerBase
{
    public const string PublishHint =
        "Geslaagde rijen zijn opgeslagen als concept. Publiceer ze via Vacatures in Lobsy — daar wordt het tokenverbruik verwerkt. Dit geldt ook voor vacatures via de API.";

    private const int MaxImageEchoChars = 120;

    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IVacancyDraftCreationService _drafts;
    private readonly ILogger<VacancyCsvImportController> _logger;

    public VacancyCsvImportController(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        IVacancyDraftCreationService drafts,
        ILogger<VacancyCsvImportController> logger)
    {
        _db = db;
        _companyAuth = companyAuth;
        _drafts = drafts;
        _logger = logger;
    }

    /// <summary>Import multiple CSV rows. Failed rows are skipped but returned for inline repair.</summary>
    [HttpPost]
    public async Task<ActionResult<CsvImportResultDto>> Import(
        [FromBody] CsvImportRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.Rows is null || request.Rows.Count == 0)
        {
            return BadRequest(new { message = "Geen rijen om te importeren." });
        }

        if (request.Rows.Count > VacancyCsvParser.MaxRows)
        {
            return BadRequest(new { message = $"Maximaal {VacancyCsvParser.MaxRows} rijen per import." });
        }

        var gate = await EnsureCsvImportAllowedAsync(request.CompanyId, cancellationToken);
        if (gate.Result is not null)
        {
            return gate.Result;
        }

        var defaultCompanyId = request.CompanyId;
        var results = new List<CsvImportRowResultDto>(request.Rows.Count);
        var touchedCompanies = new HashSet<Guid>();
        foreach (var row in request.Rows.OrderBy(r => r.RowNumber))
        {
            var result = await ProcessRowAsync(request.CompanyId, gate.OrgId, row, cancellationToken);
            results.Add(result);
            if (result.Success)
            {
                var companyId = VacancyCsvParser.TryParseGuid(row.CompanyId) ?? defaultCompanyId;
                touchedCompanies.Add(companyId);
            }
        }

        foreach (var companyId in touchedCompanies)
        {
            await TouchCsvImportActivityAsync(companyId, cancellationToken);
        }

        return Ok(ToResult(results));
    }

    /// <summary>Re-validate and import a single corrected row.</summary>
    [HttpPost("row")]
    public async Task<ActionResult<CsvImportRowResultDto>> ImportRow(
        [FromBody] CsvImportRetryRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.Row is null)
        {
            return BadRequest(new { message = "Rij ontbreekt." });
        }

        var gate = await EnsureCsvImportAllowedAsync(request.CompanyId, cancellationToken);
        if (gate.Result is not null)
        {
            return gate.Result;
        }

        var result = await ProcessRowAsync(request.CompanyId, gate.OrgId, request.Row, cancellationToken);
        if (result.Success)
        {
            var companyId = VacancyCsvParser.TryParseGuid(request.Row.CompanyId) ?? request.CompanyId;
            await TouchCsvImportActivityAsync(companyId, cancellationToken);
        }

        return Ok(result);
    }

    private async Task<(ActionResult? Result, Guid OrgId)> EnsureCsvImportAllowedAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _companyAuth.EnsureCanAccessCompanyAsync(User, companyId, cancellationToken);
        }
        catch (Core.Exceptions.ForbiddenCompanyAccessException)
        {
            return (Forbid(), Guid.Empty);
        }

        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return (NotFound(new { message = "Bedrijf niet gevonden." }), Guid.Empty);
        }

        var orgId = company.ParentCompanyId ?? company.Id;
        var org = orgId == company.Id
            ? company
            : await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == orgId, cancellationToken);
        if (org is null || !org.CsvBatchImportEnabled)
        {
            return (BadRequest(new
            {
                message = "CSV Batch Import is niet ingeschakeld voor deze organisatie. Schakel dit in bij Bedrijfsgegevens."
            }), Guid.Empty);
        }

        return (null, orgId);
    }

    private async Task<CsvImportRowResultDto> ProcessRowAsync(
        Guid defaultCompanyId,
        Guid allowedOrgId,
        CsvImportRowRequest row,
        CancellationToken cancellationToken)
    {
        var data = NormalizeEcho(row);
        try
        {
            if (string.IsNullOrWhiteSpace(row.Title))
            {
                return Fail(row.RowNumber, data, "Titel is verplicht.");
            }

            if (string.IsNullOrWhiteSpace(row.Description))
            {
                return Fail(row.RowNumber, data, "Omschrijving is verplicht.");
            }

            var start = VacancyCsvParser.TryParseDate(row.StartDate);
            if (start is null)
            {
                return Fail(row.RowNumber, data, "Startdatum ontbreekt of is ongeldig (gebruik yyyy-MM-dd of dd-MM-yyyy).");
            }

            var end = VacancyCsvParser.TryParseDate(row.EndDate);
            if (end is null)
            {
                return Fail(row.RowNumber, data, "Einddatum ontbreekt of is ongeldig (gebruik yyyy-MM-dd of dd-MM-yyyy).");
            }

            if (end < start)
            {
                return Fail(row.RowNumber, data, "Einddatum mag niet vóór de startdatum liggen.");
            }

            var branches = VacancyCsvParser.SplitMultiValue(row.Branches);
            if (branches.Length is < 1 or > WorkTypeLabels.MaxPerVacancy)
            {
                return Fail(
                    row.RowNumber,
                    data,
                    $"Branches verplicht: kies 1 of {WorkTypeLabels.MaxPerVacancy} (scheid met ; of |).");
            }

            var salaryTableId = VacancyCsvParser.TryParseGuid(row.SalaryTableId);
            if (salaryTableId is null)
            {
                return Fail(row.RowNumber, data, "Salaristabel-id ontbreekt of is ongeldig (GUID).");
            }

            Guid companyId = defaultCompanyId;
            if (!string.IsNullOrWhiteSpace(row.CompanyId))
            {
                var parsedCompany = VacancyCsvParser.TryParseGuid(row.CompanyId);
                if (parsedCompany is null)
                {
                    return Fail(row.RowNumber, data, "Vestiging-id is ongeldig (GUID).");
                }

                companyId = parsedCompany.Value;
            }

            try
            {
                await _companyAuth.EnsureCanAccessCompanyAsync(User, companyId, cancellationToken);
            }
            catch (Core.Exceptions.ForbiddenCompanyAccessException)
            {
                return Fail(row.RowNumber, data, "Geen toegang tot deze vestiging.");
            }

            var target = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
            if (target is null)
            {
                return Fail(row.RowNumber, data, "Vestiging niet gevonden.");
            }

            var targetOrgId = target.ParentCompanyId ?? target.Id;
            if (targetOrgId != allowedOrgId)
            {
                return Fail(
                    row.RowNumber,
                    data,
                    "Vestiging hoort niet bij de organisatie waarvoor CSV-import is ingeschakeld.");
            }

            if (!TransportModeParser.TryParseMany(row.Transport, out var transport, out var transportError))
            {
                return Fail(row.RowNumber, data, transportError!);
            }

            decimal hourlyWage = 0;
            if (!string.IsNullOrWhiteSpace(row.HourlyWage))
            {
                var parsedWage = VacancyCsvParser.TryParseDecimal(row.HourlyWage);
                if (parsedWage is null)
                {
                    return Fail(row.RowNumber, data, "Uurloon is ongeldig.");
                }

                hourlyWage = parsedWage.Value;
            }

            int? minimumEmployers = null;
            if (!string.IsNullOrWhiteSpace(row.MinimumEmployers))
            {
                if (!int.TryParse(row.MinimumEmployers.Trim(), out var minEmp) || minEmp is < 0 or > 100)
                {
                    return Fail(row.RowNumber, data, "Minimum werkgevers is ongeldig (0–100).");
                }

                minimumEmployers = minEmp;
            }

            var created = await _drafts.CreateDraftAsync(
                new VacancyDraftInput(
                    companyId,
                    row.Title!,
                    row.Description!,
                    hourlyWage,
                    start.Value,
                    end.Value,
                    transport,
                    branches,
                    salaryTableId.Value,
                    row.Image,
                    row.Video,
                    row.DrivingLicense,
                    row.Education,
                    minimumEmployers),
                VacancySource.Csv,
                cancellationToken);

            if (!created.Succeeded || created.Vacancy is null)
            {
                return Fail(row.RowNumber, data, created.ErrorMessage ?? "Import mislukt.");
            }

            return new CsvImportRowResultDto(
                row.RowNumber,
                true,
                created.Vacancy.Id,
                null,
                TruncateHeavyFields(data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSV import failed for row {RowNumber}", row.RowNumber);
            return Fail(row.RowNumber, data, "Onverwachte fout bij verwerken van deze rij. Controleer de velden en probeer opnieuw.");
        }
    }

    private static CsvImportRowRequest NormalizeEcho(CsvImportRowRequest row) =>
        new(
            row.RowNumber,
            row.Title,
            row.Description,
            row.StartDate,
            row.EndDate,
            row.Branches,
            row.SalaryTableId,
            row.CompanyId,
            row.HourlyWage,
            row.Image,
            row.Video,
            row.Transport,
            row.DrivingLicense,
            row.Education,
            row.MinimumEmployers);

    /// <summary>Shrink Base64 payloads on success only — failed rows keep full data for inline repair.</summary>
    private static CsvImportRowRequest TruncateHeavyFields(CsvImportRowRequest row) =>
        row with { Image = TruncateImageEcho(row.Image) };

    private static string? TruncateImageEcho(string? image)
    {
        if (string.IsNullOrWhiteSpace(image))
        {
            return image;
        }

        var trimmed = image.Trim();
        if (trimmed.Length <= MaxImageEchoChars)
        {
            return trimmed;
        }

        // Keep enough for URL edits; Base64 payloads are summarized for memory/UX.
        if (trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed[..Math.Min(trimmed.Length, 1024)];
        }

        return trimmed[..MaxImageEchoChars] + "…";
    }

    private static CsvImportRowResultDto Fail(int rowNumber, CsvImportRowRequest data, string message) =>
        new(rowNumber, false, null, message, data);

    private static CsvImportResultDto ToResult(IReadOnlyList<CsvImportRowResultDto> rows)
    {
        var success = rows.Count(r => r.Success);
        return new CsvImportResultDto(
            rows.Count,
            success,
            rows.Count - success,
            rows,
            PublishHint);
    }

    private async Task TouchCsvImportActivityAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return;
        }

        company.LastCsvImportAtUtc = DateTime.UtcNow;
        // Also stamp the organisation root for re-engagement inactivity checks.
        if (company.ParentCompanyId is Guid parentId)
        {
            var org = await _db.Companies.FirstOrDefaultAsync(c => c.Id == parentId, cancellationToken);
            if (org is not null)
            {
                org.LastCsvImportAtUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
