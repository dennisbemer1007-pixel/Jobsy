using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/admin/ats")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public sealed class AdminAtsController : ControllerBase
{
    private readonly IAtsVacancyModerationService _moderation;
    private readonly IAtsScrapeService _scrape;
    private readonly IAtsVacancyHealthService _health;

    public AdminAtsController(
        IAtsVacancyModerationService moderation,
        IAtsScrapeService scrape,
        IAtsVacancyHealthService health)
    {
        _moderation = moderation;
        _scrape = scrape;
        _health = health;
    }

    [HttpGet("listings")]
    public async Task<ActionResult<IEnumerable<AtsListingDto>>> List(
        [FromQuery] string? status = null,
        [FromQuery] string? q = null,
        CancellationToken cancellationToken = default)
    {
        AtsListingStatus? parsed = null;
        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<AtsListingStatus>(status, ignoreCase: true, out var s))
        {
            parsed = s;
        }

        var items = await _moderation.ListAsync(parsed, q, cancellationToken);
        return Ok(items.Select(ToDto));
    }

    [HttpGet("listings/{id:guid}")]
    public async Task<ActionResult<AtsListingDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _moderation.GetAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(ToDto(item));
    }

    [HttpPut("listings/{id:guid}")]
    public async Task<ActionResult<AtsListingDto>> Update(
        Guid id,
        [FromBody] AtsListingUpdateRequest body,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _moderation.UpdateFieldsAsync(
                id,
                body.Title,
                body.CompanyName,
                body.LocationLabel,
                body.Description,
                body.SalaryText,
                body.HourlyWage,
                body.HoursText,
                cancellationToken);
            return updated is null ? NotFound() : Ok(ToDto(updated));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("listings/{id:guid}/approve")]
    public async Task<ActionResult<AtsApproveResultDto>> Approve(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var vacancy = await _moderation.ApproveAsync(id, cancellationToken);
            if (vacancy is null)
            {
                return NotFound();
            }

            return Ok(new AtsApproveResultDto(id, vacancy.Id, vacancy.Status.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("listings/{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] AtsRejectRequest? body,
        CancellationToken cancellationToken)
    {
        try
        {
            await _moderation.RejectAsync(id, body?.Reason, cancellationToken);
            return Ok(new { message = "Afgekeurd." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("listings/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _moderation.DeleteAsync(id, cancellationToken);
        return Ok(new { message = "Verwijderd." });
    }

    [HttpPost("scrape")]
    public async Task<ActionResult<AtsScrapeRunReportDto>> ScrapeNow(
        [FromQuery] Guid? sourceId = null,
        CancellationToken cancellationToken = default)
    {
        var report = sourceId is Guid id
            ? await _scrape.ScrapeSourceAsync(id, cancellationToken)
            : await _scrape.ScrapeAllEnabledAsync(cancellationToken);
        return Ok(ToReportDto(report));
    }

    [HttpPost("health")]
    public async Task<ActionResult<object>> HealthNow(CancellationToken cancellationToken)
    {
        var n = await _health.RunHealthPassAsync(cancellationToken);
        return Ok(new { changed = n });
    }

    private static AtsScrapeRunReportDto ToReportDto(AtsScrapeRunReport r) => new(
        r.StartedAtUtc,
        r.FinishedAtUtc,
        r.SourceCount,
        r.Upserted,
        r.Inserted,
        r.Updated,
        r.SkippedDuplicateHash,
        r.SkippedBlacklist,
        r.SkippedParse,
        r.SkippedInvalid,
        r.HttpErrors,
        r.FailedSources,
        r.Sources.Select(s => new AtsScrapeSourceReportDto(
            s.SourceId,
            s.Name,
            s.Domain,
            s.ListUrl,
            s.Status,
            s.ListHttpStatus,
            s.RawAnchorCount,
            s.VacancyLinkCount,
            s.PagesScanned,
            s.PaginationFollowed,
            s.AtsListingsSaved,
            s.DetailPagesFetched,
            s.Upserted,
            s.Inserted,
            s.Updated,
            s.SkippedDuplicateHash,
            s.SkippedBlacklist,
            s.SkippedParse,
            s.SkippedInvalid,
            s.HttpErrors,
            s.Error,
            s.Lines.ToList())).ToList(),
        r.Lines.ToList());

    private static AtsListingDto ToDto(Core.Entities.AtsScrapedListing l) => new(
        l.Id,
        l.SourceId,
        l.Source?.Name ?? string.Empty,
        l.Source?.Domain ?? string.Empty,
        l.SourceUrl,
        l.CompanyName,
        l.Title,
        l.LocationLabel,
        l.PostalCode,
        l.Description,
        l.SalaryText,
        l.HourlyWage,
        l.HoursText,
        l.MinHoursPerWeek,
        l.MaxHoursPerWeek,
        l.TagsJson,
        l.CompletenessScore,
        l.Status.ToString(),
        l.RejectReason,
        l.ScrapedAtUtc,
        l.LastCheckedAtUtc,
        l.ExpiresAtUtc,
        l.ReviewedAtUtc,
        l.LinkedVacancyId);
}

public record AtsListingDto(
    Guid Id,
    Guid SourceId,
    string SourceName,
    string SourceDomain,
    string SourceUrl,
    string CompanyName,
    string Title,
    string? LocationLabel,
    string? PostalCode,
    string Description,
    string? SalaryText,
    decimal? HourlyWage,
    string? HoursText,
    decimal? MinHoursPerWeek,
    decimal? MaxHoursPerWeek,
    string? TagsJson,
    int CompletenessScore,
    string Status,
    string? RejectReason,
    DateTime ScrapedAtUtc,
    DateTime? LastCheckedAtUtc,
    DateTime? ExpiresAtUtc,
    DateTime? ReviewedAtUtc,
    Guid? LinkedVacancyId);

public record AtsListingUpdateRequest(
    string Title,
    string CompanyName,
    string? LocationLabel,
    string Description,
    string? SalaryText,
    decimal? HourlyWage,
    string? HoursText);

public record AtsRejectRequest(string? Reason);

public record AtsApproveResultDto(Guid ListingId, Guid VacancyId, string VacancyStatus);

public record AtsScrapeRunReportDto(
    DateTime StartedAtUtc,
    DateTime FinishedAtUtc,
    int SourceCount,
    int Upserted,
    int Inserted,
    int Updated,
    int SkippedDuplicateHash,
    int SkippedBlacklist,
    int SkippedParse,
    int SkippedInvalid,
    int HttpErrors,
    int FailedSources,
    IReadOnlyList<AtsScrapeSourceReportDto> Sources,
    IReadOnlyList<string> Lines);

public record AtsScrapeSourceReportDto(
    Guid SourceId,
    string Name,
    string Domain,
    string ListUrl,
    string Status,
    int? ListHttpStatus,
    int RawAnchorCount,
    int VacancyLinkCount,
    int PagesScanned,
    int PaginationFollowed,
    int AtsListingsSaved,
    int DetailPagesFetched,
    int Upserted,
    int Inserted,
    int Updated,
    int SkippedDuplicateHash,
    int SkippedBlacklist,
    int SkippedParse,
    int SkippedInvalid,
    int HttpErrors,
    string? Error,
    IReadOnlyList<string> Lines);
