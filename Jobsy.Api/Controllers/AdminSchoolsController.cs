using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/admin/schools")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public sealed class AdminSchoolsController : ControllerBase
{
    private static readonly Regex DomainRegex = new(
        @"^[a-z0-9]([a-z0-9\-]*[a-z0-9])?(\.[a-z0-9]([a-z0-9\-]*[a-z0-9])?)+$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly JobsyDbContext _db;
    private readonly ISchoolStaffInviteService _invites;
    private readonly ISchoolAggregateSnapshotter _snapshotter;
    private readonly ISchoolRetentionService _retention;
    private readonly ISchoolReportingService _reporting;

    public AdminSchoolsController(
        JobsyDbContext db,
        ISchoolStaffInviteService invites,
        ISchoolAggregateSnapshotter snapshotter,
        ISchoolRetentionService retention,
        ISchoolReportingService reporting)
    {
        _db = db;
        _invites = invites;
        _snapshotter = snapshotter;
        _retention = retention;
        _reporting = reporting;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SchoolListItemDto>>> List(CancellationToken cancellationToken)
    {
        var schools = await _db.Schools.AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
        var schoolIds = schools.Select(s => s.Id).ToList();
        var classCounts = await _db.SchoolClasses.AsNoTracking()
            .Where(c => schoolIds.Contains(c.SchoolId))
            .GroupBy(c => c.SchoolId)
            .Select(g => new { SchoolId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SchoolId, x => x.Count, cancellationToken);
        var teacherCounts = await _db.Users.AsNoTracking()
            .Where(u => u.SchoolId != null && schoolIds.Contains(u.SchoolId.Value)
                        && (u.Role == UserRole.Teacher || u.Role == UserRole.SchoolAdmin))
            .GroupBy(u => u.SchoolId!.Value)
            .Select(g => new { SchoolId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SchoolId, x => x.Count, cancellationToken);

        return Ok(schools.Select(s => new SchoolListItemDto(
            s.Id,
            s.Name,
            s.City,
            s.BrinCode,
            s.IsActive,
            s.ProcessorAgreementSignedOn,
            classCounts.GetValueOrDefault(s.Id),
            teacherCounts.GetValueOrDefault(s.Id))).ToList());
    }

    [HttpGet("rapportage")]
    public async Task<ActionResult<SchoolReportViewDto>> Rapportage(
        [FromQuery] int? schoolYearStart,
        [FromQuery] Guid? schoolId,
        [FromQuery] SchoolLevel? level,
        [FromQuery] int? year,
        [FromQuery] PupilQuestionSet? questionSet,
        CancellationToken cancellationToken)
    {
        if (questionSet is null)
        {
            return BadRequest(new { error = "question_set_required" });
        }

        var years = await _reporting.ListSchoolYearsAsync(cancellationToken);
        var sy = schoolYearStart ?? (years.Count > 0 ? years[0] : 0);
        if (sy == 0)
        {
            sy = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow));
        }

        var view = await _reporting.GetReportAsync(
            new SchoolReportFilterDto(sy, schoolId, level, year, questionSet.Value),
            cancellationToken);
        return Ok(view);
    }

    [HttpGet("rapportage/years")]
    public async Task<ActionResult<IReadOnlyList<int>>> RapportageYears(CancellationToken cancellationToken)
        => Ok(await _reporting.ListSchoolYearsAsync(cancellationToken));

    [HttpGet("rapportage.csv")]
    public async Task<IActionResult> RapportageCsv(
        [FromQuery] int? schoolYearStart,
        [FromQuery] Guid? schoolId,
        [FromQuery] SchoolLevel? level,
        [FromQuery] int? year,
        [FromQuery] PupilQuestionSet? questionSet,
        CancellationToken cancellationToken)
    {
        if (questionSet is null)
        {
            return BadRequest(new { error = "question_set_required" });
        }

        var years = await _reporting.ListSchoolYearsAsync(cancellationToken);
        var sy = schoolYearStart ?? (years.Count > 0 ? years[0] : 0);
        if (sy == 0)
        {
            sy = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow));
        }

        var (bytes, fileName) = await _reporting.ExportCsvAsync(
            new SchoolReportFilterDto(sy, schoolId, level, year, questionSet.Value),
            cancellationToken);
        Audit("school.rapportage.export", schoolId, new { schoolYearStart = sy, level, year, questionSet, bytes = bytes.Length });
        await _db.SaveChangesAsync(cancellationToken);
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("retention")]
    public async Task<ActionResult<SchoolRetentionStatusDto>> RetentionStatus(CancellationToken cancellationToken)
        => Ok(await _reporting.GetRetentionStatusAsync(cancellationToken));

    [HttpPost("retention/dry-run")]
    public async Task<ActionResult<SchoolRetentionDryRunDto>> RetentionDryRun(CancellationToken cancellationToken)
    {
        var result = await _retention.DryRunAsync(cancellationToken);
        return Ok(new SchoolRetentionDryRunDto(
            result.TodayAmsterdam,
            result.CutoffDate,
            result.ClassesWouldDelete,
            result.CodesWouldDelete,
            result.ResultsWouldDelete,
            result.Schools.Select(s => new SchoolRetentionDryRunSchoolDto(
                s.SchoolId, s.SchoolName, s.Classes, s.Codes, s.Results, s.SchoolYearStarts)).ToList()));
    }

    [HttpGet("retention/impact")]
    public async Task<ActionResult<SchoolRetentionImpactDto>> RetentionImpact(
        [FromQuery] int month,
        [FromQuery] int day,
        CancellationToken cancellationToken)
    {
        try
        {
            SchoolYear.ValidateCutoff(month, day);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var count = await _retention.CountClassesImpactedByCutoffAsync(month, day, cancellationToken);
        var note = count == 0
            ? "Geen klassen worden bij de volgende run verwijderd met deze afkapdatum."
            : $"Dit verwijdert bij de volgende run de gegevens van {count} klassen.";
        return Ok(new SchoolRetentionImpactDto(month, day, count, note));
    }

    [HttpPost("aggregates/refresh")]
    public async Task<ActionResult<SnapshotTotalsResultDto>> RefreshAggregates(
        [FromQuery] int? schoolYearStart,
        CancellationToken cancellationToken)
    {
        var years = await _reporting.ListSchoolYearsAsync(cancellationToken);
        var sy = schoolYearStart
                 ?? (years.Count > 0 ? years[0] : 0);
        if (sy == 0)
        {
            sy = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow));
        }

        var written = await _snapshotter.SnapshotAllSchoolsForYearAsync(sy, cancellationToken);
        Audit("school.aggregates.refresh", null, new { schoolYearStart = sy, written });
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new SnapshotTotalsResultDto(written));
    }

    [HttpGet("{schoolId:guid}")]
    public async Task<ActionResult<SchoolDetailDto>> Get(Guid schoolId, CancellationToken cancellationToken)
    {
        var school = await _db.Schools.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == schoolId, cancellationToken);
        if (school is null)
        {
            return NotFound();
        }

        return Ok(await ToDetailAsync(school, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<SchoolDetailDto>> Create(
        [FromBody] CreateSchoolRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.City))
        {
            return BadRequest(new { message = "Naam en plaats zijn verplicht." });
        }

        var domains = NormalizeDomains(request.AllowedEmailDomains);
        if (domains is null)
        {
            return BadRequest(new { message = "Een of meer e-maildomeinen zijn ongeldig." });
        }

        var actorId = GetUserId() ?? Guid.Empty;
        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            City = request.City.Trim(),
            BrinCode = string.IsNullOrWhiteSpace(request.BrinCode) ? null : request.BrinCode.Trim(),
            AllowedEmailDomains = JsonSerializer.Serialize(domains),
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = actorId
        };
        _db.Schools.Add(school);
        Audit("school.create", school.Id, new { school.Name, school.City });
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { schoolId = school.Id }, await ToDetailAsync(school, cancellationToken));
    }

    [HttpPut("{schoolId:guid}")]
    public async Task<ActionResult<SchoolDetailDto>> Update(
        Guid schoolId,
        [FromBody] UpdateSchoolRequest request,
        CancellationToken cancellationToken)
    {
        var school = await _db.Schools.FirstOrDefaultAsync(s => s.Id == schoolId, cancellationToken);
        if (school is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.City))
        {
            return BadRequest(new { message = "Naam en plaats zijn verplicht." });
        }

        var domains = NormalizeDomains(request.AllowedEmailDomains);
        if (domains is null)
        {
            return BadRequest(new { message = "Een of meer e-maildomeinen zijn ongeldig." });
        }

        school.Name = request.Name.Trim();
        school.City = request.City.Trim();
        school.BrinCode = string.IsNullOrWhiteSpace(request.BrinCode) ? null : request.BrinCode.Trim();
        school.AllowedEmailDomains = JsonSerializer.Serialize(domains);
        if (request.IsActive is bool active)
        {
            school.IsActive = active;
        }

        Audit("school.update", school.Id, new { school.Name, school.IsActive });
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(await ToDetailAsync(school, cancellationToken));
    }

    [HttpPost("{schoolId:guid}/deactivate")]
    public async Task<ActionResult<SchoolDetailDto>> Deactivate(Guid schoolId, CancellationToken cancellationToken)
    {
        var school = await _db.Schools.FirstOrDefaultAsync(s => s.Id == schoolId, cancellationToken);
        if (school is null)
        {
            return NotFound();
        }

        school.IsActive = false;
        Audit("school.deactivate", school.Id, new { school.Name });
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(await ToDetailAsync(school, cancellationToken));
    }

    [HttpPost("{schoolId:guid}/delete")]
    public async Task<ActionResult<SchoolEarlyDeleteResult>> DeleteSchool(
        Guid schoolId,
        [FromBody] ConfirmSchoolNameRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _retention.DeleteSchoolNowAsync(schoolId, request.ConfirmName, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message == "not_found")
        {
            return NotFound();
        }
        catch (InvalidOperationException ex) when (ex.Message == "confirm_name_mismatch")
        {
            return BadRequest(new { message = "Typ de schoolnaam ter bevestiging." });
        }
    }

    [HttpPost("{schoolId:guid}/aggregates/refresh")]
    public async Task<ActionResult<SnapshotTotalsResultDto>> RefreshSchoolAggregates(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Schools.AnyAsync(s => s.Id == schoolId, cancellationToken))
        {
            return NotFound();
        }

        var written = await _snapshotter.SnapshotAllYearsForSchoolAsync(schoolId, cancellationToken);
        Audit("school.aggregates.refresh", schoolId, new { written });
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new SnapshotTotalsResultDto(written));
    }

    [HttpPut("{schoolId:guid}/processor-agreement")]
    public async Task<ActionResult<SchoolDetailDto>> RecordAgreement(
        Guid schoolId,
        [FromBody] RecordProcessorAgreementRequest request,
        CancellationToken cancellationToken)
    {
        var school = await _db.Schools.FirstOrDefaultAsync(s => s.Id == schoolId, cancellationToken);
        if (school is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Version))
        {
            return BadRequest(new { message = "Versie is verplicht." });
        }

        school.ProcessorAgreementSignedOn = request.SignedOn;
        school.ProcessorAgreementVersion = request.Version.Trim();
        Audit("school.processor_agreement", school.Id, new { request.SignedOn, request.Version });
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(await ToDetailAsync(school, cancellationToken));
    }

    [HttpPost("{schoolId:guid}/invite-admin")]
    public async Task<ActionResult<SchoolStaffInviteResultDto>> InviteAdmin(
        Guid schoolId,
        [FromBody] InviteSchoolAdminRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorId = GetUserId() ?? Guid.Empty;
            var result = await _invites.InviteSchoolAdminAsync(
                schoolId,
                request.FullName,
                request.Email,
                actorId,
                cancellationToken);
            return Ok(new SchoolStaffInviteResultDto(
                result.InviteId,
                result.UserId,
                result.Email,
                result.Role,
                result.ExpiresAtUtc));
        }
        catch (InvalidOperationException ex) when (ex.Message == "email_domain_not_allowed")
        {
            return BadRequest(new { error = "email_domain_not_allowed" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<SchoolDetailDto> ToDetailAsync(School school, CancellationToken cancellationToken)
    {
        var classCount = await _db.SchoolClasses.CountAsync(c => c.SchoolId == school.Id, cancellationToken);
        var classIds = await _db.SchoolClasses.AsNoTracking()
            .Where(c => c.SchoolId == school.Id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        var codeCount = classIds.Count == 0
            ? 0
            : await _db.PupilCodes.CountAsync(c => classIds.Contains(c.SchoolClassId), cancellationToken);
        var completedCount = classIds.Count == 0
            ? 0
            : await _db.PupilCodes.CountAsync(
                c => classIds.Contains(c.SchoolClassId) && c.Status == PupilCodeStatus.Completed,
                cancellationToken);

        var admins = await _db.Users.AsNoTracking()
            .Where(u => u.SchoolId == school.Id && u.Role == UserRole.SchoolAdmin)
            .OrderBy(u => u.FullName)
            .Select(u => new SchoolAdminListItemDto(
                u.Id,
                u.FullName,
                u.Email,
                u.AuthenticatorEnabled,
                u.IsActive))
            .ToListAsync(cancellationToken);

        List<string> domains;
        try
        {
            domains = JsonSerializer.Deserialize<List<string>>(school.AllowedEmailDomains) ?? [];
        }
        catch (JsonException)
        {
            domains = [];
        }

        return new SchoolDetailDto(
            school.Id,
            school.Name,
            school.City,
            school.BrinCode,
            domains,
            school.IsActive,
            school.ProcessorAgreementSignedOn,
            school.ProcessorAgreementVersion,
            classCount,
            codeCount,
            completedCount,
            admins);
    }

    private void Audit(string action, Guid? schoolId, object details)
    {
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Scholen.Audit",
            Message = schoolId is Guid id ? $"{action} school={id:D}" : action,
            DetailsJson = JsonSerializer.Serialize(details),
            CreatedAt = DateTime.UtcNow
        });
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static List<string>? NormalizeDomains(IReadOnlyList<string>? raw)
    {
        if (raw is null || raw.Count == 0)
        {
            return [];
        }

        var result = new List<string>();
        foreach (var item in raw)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            var d = item.Trim().TrimStart('@').ToLowerInvariant();
            if (!DomainRegex.IsMatch(d))
            {
                return null;
            }

            if (!result.Contains(d, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(d);
            }
        }

        return result;
    }
}
