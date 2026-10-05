using System.Text;
using System.Text.Json;
using Jobsy.Api.Admin;
using Jobsy.Core.Admin;
using Jobsy.Core.Authorization;
using Jobsy.Core.Careers;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

/// <summary>
/// Admin fill and export for typical occupation days. OpenAI runs here, not on the candidate page.
/// Existing rows are left as they are. Import replaces a row only when the content hash differs.
/// </summary>
[ApiController]
[Route("api/admin/occupation-day-in-life")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public sealed class OccupationDayInLifeAdminController : ControllerBase
{
    private readonly OccupationDayInLifeReader _reader;
    private readonly OccupationDayInLifeGenerator _generator;
    private readonly OccupationDayInLifeBatchRunner _runner;

    public OccupationDayInLifeAdminController(
        OccupationDayInLifeReader reader,
        OccupationDayInLifeGenerator generator,
        OccupationDayInLifeBatchRunner runner)
    {
        _reader = reader;
        _generator = generator;
        _runner = runner;
    }

    [HttpGet("status")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<OccupationDayAdminStatus>> Status(CancellationToken cancellationToken)
    {
        var run = _runner.Current;
        return Ok(await _reader.StatusAsync(run.Running, run.Generated, run.Failed, run.LastError, run.LastEscoId, cancellationToken));
    }

    /// <summary>Synchronous chunk for a small resume step or a script. Max 15 occupations per call.</summary>
    [HttpPost("generate")]
    [EnableRateLimiting("public-write")]
    [AdminAudit(AdminAuditKeys.OccupationDayGenerate, TargetType = "setting")]
    public async Task<ActionResult<OccupationDayGenerateResult>> Generate(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 15);
        return Ok(await _generator.GenerateMissingAsync(limit, cancellationToken));
    }

    /// <summary>Background one-shot. Omit <paramref name="limit"/> to fill every missing occupation.</summary>
    [HttpPost("start")]
    [EnableRateLimiting("public-write")]
    [AdminAudit(AdminAuditKeys.OccupationDayStart, TargetType = "setting")]
    public IActionResult Start([FromQuery] int? limit)
    {
        if (limit is < 1)
        {
            return BadRequest(new { message = "Limit moet minstens 1 zijn." });
        }

        if (!_runner.TryStart(limit))
        {
            return Conflict(new { message = "Er loopt al een vulling." });
        }

        return Accepted();
    }

    [HttpPost("stop")]
    [EnableRateLimiting("public-write")]
    [AdminAudit(AdminAuditKeys.OccupationDayStop, TargetType = "setting")]
    public IActionResult Stop()
    {
        _runner.Stop();
        return NoContent();
    }

    [HttpGet("export")]
    [EnableRateLimiting("public-read")]
    [AdminAudit(AdminAuditKeys.OccupationDayExport, TargetType = "setting")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var document = await _generator.ExportAsync(cancellationToken);
        var json = JsonSerializer.Serialize(document, OccupationDayJson.Options);
        return File(Encoding.UTF8.GetBytes(json), "application/json", "occupation-day-in-life.nl.json");
    }

    [HttpPost("import")]
    [EnableRateLimiting("public-write")]
    [RequestSizeLimit(20_000_000)]
    [AdminAudit(AdminAuditKeys.OccupationDayImport, TargetType = "setting")]
    public async Task<ActionResult<OccupationDayImportResult>> Import(CancellationToken cancellationToken)
    {
        OccupationDayExportDocument? document;
        try
        {
            document = await JsonSerializer.DeserializeAsync<OccupationDayExportDocument>(
                Request.Body,
                OccupationDayJson.Options,
                cancellationToken);
        }
        catch (JsonException)
        {
            return BadRequest(new { message = "De JSON is niet leesbaar." });
        }

        if (document?.Rows is null)
        {
            return BadRequest(new { message = "De JSON heeft geen rijen." });
        }

        if (document.Rows.Count > OccupationDayInLifeGenerator.MaxImportRows)
        {
            return BadRequest(new { message = "Te veel rijen in één import." });
        }

        return Ok(await _generator.ImportAsync(document, cancellationToken));
    }
}
