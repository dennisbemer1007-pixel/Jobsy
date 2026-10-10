using Jobsy.Core.Authorization;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/admin/westland-pilot")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public sealed class WestlandPilotAdminController : ControllerBase
{
    private readonly WestlandPilotReportingService _reporting;
    private readonly Golf2WestlandOptions _options;

    public WestlandPilotAdminController(
        WestlandPilotReportingService reporting,
        IOptions<Golf2WestlandOptions> options)
    {
        _reporting = reporting;
        _options = options.Value;
    }

    [HttpGet("stats")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<WestlandPilotStatsDto>> Stats(CancellationToken cancellationToken)
    {
        if (!_options.PilotReporting)
        {
            return NotFound(new { code = "golf2_westland_reporting_disabled" });
        }

        return Ok(await _reporting.GetStatsAsync(cancellationToken));
    }

    [HttpGet("export.csv")]
    [EnableRateLimiting("public-read")]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken)
    {
        if (!_options.PilotReporting)
        {
            return NotFound(new { code = "golf2_westland_reporting_disabled" });
        }

        var csv = await _reporting.BuildTaskChoicesCsvAsync(cancellationToken);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv; charset=utf-8", $"westland-pilot-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
