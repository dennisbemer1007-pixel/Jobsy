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
[Authorize(Roles = $"{JobsyRoles.Intermediary},{JobsyRoles.EnterpriseManager},{JobsyRoles.Admin}")]
[EnableRateLimiting("public-write")]
[RequestSizeLimit(10 * 1024 * 1024)]
public class VacancyCsvImportController : ControllerBase
{
    public const string PublishHint =
        "Geslaagde rijen zijn opgeslagen als concept. Publiceer ze via Vacatures in Lobsy — daar wordt het tokenverbruik verwerkt. Dit geldt ook voor vacatures via de API.";

    private const int MaxImageEchoChars = 120;

    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IVacancyDraftCreationService _drafts;
    private readonly IUserLookupService _users;
    private readonly ILogger<VacancyCsvImportController> _logger;

    public VacancyCsvImportController(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        IVacancyDraftCreationService drafts,
        IUserLookupService users,
        ILogger<VacancyCsvImportController> logger)
    {
        _db = db;
        _companyAuth = companyAuth;
        _drafts = drafts;
        _users = users;
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
            var result = await ProcessRowAsync(
                request.CompanyId,
                gate.OrgId,
                gate.IsIntermediary,
                gate.IntermediaryCompanyId,
                row,
                cancellationToken);
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

        var result = await ProcessRowAsync(
            request.CompanyId,
            gate.OrgId,
            gate.IsIntermediary,
            gate.IntermediaryCompanyId,
            request.Row,
            cancellationToken);
        if (result.Success)
        {
            var companyId = VacancyCsvParser.TryParseGuid(request.Row.CompanyId) ?? request.CompanyId;
            await TouchCsvImportActivityAsync(companyId, cancellationToken);
        }

        return Ok(result);
    }

    private async Task<(ActionResult? Result, Guid OrgId, bool IsIntermediary, Guid? IntermediaryCompanyId)> EnsureCsvImportAllowedAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _companyAuth.EnsureCanAccessCompanyAsync(User, companyId, cancellationToken);
        }
        catch (Core.Exceptions.ForbiddenCompanyAccessException)
        {
            return (Forbid(), Guid.Empty, false, null);
        }

        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return (NotFound(new { message = "Bedrijf niet gevonden." }), Guid.Empty, false, null);
        }

        var isIntermediary = _companyAuth.GetPrimaryRole(User) == UserRole.Intermediary;
        if (isIntermediary)
        {
            var intermediaryCompanyId = await ResolveIntermediaryOrganizationIdAsync(cancellationToken);
            if (intermediaryCompanyId is null)
            {
                return (BadRequest(new { message = "Intermediair-organisatie niet gevonden voor deze gebruiker." }), Guid.Empty, true, null);
            }

            var intermediaryOrg = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == intermediaryCompanyId.Value, cancellationToken);
            if (intermediaryOrg is null || !intermediaryOrg.CsvBatchImportEnabled)
            {
                return (BadRequest(new
                {
                    message = "CSV Batch Import is niet ingeschakeld voor deze intermediair-organisatie. Schakel dit in bij Bedrijfsgegevens."
                }), Guid.Empty, true, intermediaryCompanyId);
            }

            return (null, intermediaryCompanyId.Value, true, intermediaryCompanyId);
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
            }), Guid.Empty, false, null);
        }

        return (null, orgId, false, null);
    }

    private async Task<CsvImportRowResultDto> ProcessRowAsync(
        Guid defaultCompanyId,
        Guid allowedOrgId,
        bool isIntermediary,
        Guid? intermediaryCompanyId,
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
            if (!isIntermediary && targetOrgId != allowedOrgId)
            {
                return Fail(
                    row.RowNumber,
                    data,
                    "Vestiging hoort niet bij de organisatie waarvoor CSV-import is ingeschakeld.");
            }

            if (isIntermediary)
            {
                var kvkError = IntermediaryVacancyRules.ValidateEndClientKvk(target, callerIsIntermediary: true);
                if (kvkError is not null)
                {
                    return Fail(row.RowNumber, data, kvkError);
                }

                if (!string.IsNullOrWhiteSpace(row.KvkNumber)
                    && !string.Equals(row.KvkNumber.Trim(), target.KvkNumber, StringComparison.OrdinalIgnoreCase))
                {
                    return Fail(row.RowNumber, data, "KVK-nummer komt niet overeen met het gekozen inhurende bedrijf.");
                }

                if (!string.IsNullOrWhiteSpace(row.KvkEstablishmentId)
                    && !string.Equals(row.KvkEstablishmentId.Trim(), target.KvkEstablishmentId, StringComparison.OrdinalIgnoreCase))
                {
                    return Fail(row.RowNumber, data, "Vestigingsnummer komt niet overeen met het gekozen inhurende bedrijf.");
                }
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

            var showClientAddressOnMap = false;
            if (!string.IsNullOrWhiteSpace(row.ShowClientAddressOnMap)
                && !TryParseBoolean(row.ShowClientAddressOnMap, out showClientAddressOnMap))
            {
                return Fail(row.RowNumber, data, "Toon opdrachtgever adres is ongeldig (gebruik true/false, ja/nee of 1/0).");
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
                    minimumEmployers,
                    intermediaryCompanyId,
                    showClientAddressOnMap,
                    isIntermediary),
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
            row.MinimumEmployers,
            row.KvkNumber,
            row.KvkEstablishmentId,
            row.ShowClientAddressOnMap);

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
        new(rowNumber, false, null, message, TruncateHeavyFields(data));

    private static bool TryParseBoolean(string? value, out bool parsed)
    {
        parsed = false;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized is "true" or "1" or "yes" or "y" or "ja" or "j")
        {
            parsed = true;
            return true;
        }

        if (normalized is "false" or "0" or "no" or "n" or "nee")
        {
            return true;
        }

        return false;
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
