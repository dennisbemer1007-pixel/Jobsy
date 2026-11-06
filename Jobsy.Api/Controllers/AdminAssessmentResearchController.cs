using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/admin/research")]
[Authorize(Roles = JobsyRoles.Admin)]
public sealed class AdminAssessmentResearchController : ControllerBase
{
    private static readonly Guid PlatformSettingsId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private readonly JobsyDbContext _db;
    private readonly IAssessmentResearchService _research;
    private readonly IUserLookupService _users;

    public AdminAssessmentResearchController(
        JobsyDbContext db,
        IAssessmentResearchService research,
        IUserLookupService users)
    {
        _db = db;
        _research = research;
        _users = users;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ResearchDashboardDto>> Dashboard(CancellationToken cancellationToken)
        => Ok(await _research.GetDashboardAsync(cancellationToken));

    [HttpGet("export.csv")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] string? testKey,
        [FromQuery] int? testVersion,
        CancellationToken cancellationToken)
    {
        var admin = await _users.FindByPrincipalAsync(User, cancellationToken);
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Category = "ResearchExport",
            Level = PlatformLogLevel.Info,
            Message = $"Admin research CSV export actor={admin?.Id} testKey={testKey ?? "*"} version={testVersion?.ToString() ?? "*"}",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        var bytes = await _research.ExportCsvAsync(testKey, testVersion, cancellationToken);
        var fileName = $"lobsy_research_{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("pilots")]
    public async Task<ActionResult<IReadOnlyList<ResearchPilotCampaignDto>>> ListPilots(CancellationToken cancellationToken)
        => Ok(await _research.ListPilotCampaignsAsync(cancellationToken));

    [HttpPost("pilots")]
    public async Task<ActionResult<ResearchPilotCampaignDto>> CreatePilot(
        [FromBody] CreatePilotCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var admin = await _users.FindByPrincipalAsync(User, cancellationToken);
        var dto = await _research.CreatePilotCampaignAsync(
            request.Title ?? "Pilot",
            admin?.Id ?? Guid.Empty,
            cancellationToken);
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Category = "ResearchPilot",
            Level = PlatformLogLevel.Info,
            Message = $"Pilot campaign created code={dto.Code}",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(dto);
    }

    [HttpGet("settings")]
    public async Task<ActionResult<ResearchAdminSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var row = await _db.PlatformFeatureSettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        var enabled = row?.ResearchConsentEnabled == true;
        return Ok(new ResearchAdminSettingsDto(enabled));
    }

    [HttpPut("settings")]
    public async Task<ActionResult<ResearchAdminSettingsDto>> UpdateSettings(
        [FromBody] ResearchAdminSettingsDto request,
        CancellationToken cancellationToken)
    {
        var row = await _db.PlatformFeatureSettings.FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            row = new PlatformFeatureSettings { Id = PlatformSettingsId };
            _db.PlatformFeatureSettings.Add(row);
        }

        row.ResearchConsentEnabled = request.ResearchConsentEnabled;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new ResearchAdminSettingsDto(row.ResearchConsentEnabled));
    }
}
