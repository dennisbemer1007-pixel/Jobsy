using System.Security.Claims;
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
    private readonly IAdminAuditContext _audit;

    public OccupationDayInLifeAdminController(
        OccupationDayInLifeReader reader,
        OccupationDayInLifeGenerator generator,
        OccupationDayInLifeBatchRunner runner,
        IAdminAuditContext audit)
    {
        _reader = reader;
        _generator = generator;
        _runner = runner;
        _audit = audit;
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
        [FromQuery] string? ids = null,
        [FromBody] OccupationDayRunRequest? body = null,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(body?.Limit ?? limit, 1, 15);
        if (!TryPick(body?.Ids ?? ids, out var only, out var error))
        {
            return BadRequest(new { message = error });
        }

        try
        {
            var result = await _generator.GenerateMissingAsync(limit, skipEscoIds: null, only, cancellationToken);
            Remember(result);
            return Ok(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var reason = OccupationDayWriteErrors.SafeSnippet(ex.Message);
            _audit.Reason = OccupationDayWriteErrors.Timeout + " " + reason;
            _audit.ResultOverride = AdminAuditKeys.Results.Failed;
            return Ok(new OccupationDayGenerateResult(0, 1, 0, 0, false, [new OccupationDayFailure("", OccupationDayWriteErrors.Timeout)]));
        }
    }

    /// <summary>One occupation through the same checks as a batch, including validation. Nothing is stored.</summary>
    [HttpPost("probe")]
    [EnableRateLimiting("public-write")]
    [AdminAudit(AdminAuditKeys.OccupationDayProbe, TargetType = "setting")]
    public async Task<ActionResult<OccupationDayProbeResult>> Probe(
        [FromQuery] string? q,
        [FromBody] OccupationDayRunRequest? body,
        CancellationToken cancellationToken)
    {
        var raw = string.IsNullOrWhiteSpace(body?.Ids) ? q : body!.Ids;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return BadRequest(new { message = "Kies eerst één beroep." });
        }

        var pick = OccupationDaySelection.Resolve(raw);
        if (pick.Jobs.Count == 0)
        {
            _audit.ResultOverride = AdminAuditKeys.Results.Failed;
            _audit.Reason = "onbekend-beroep";
            return Ok(new OccupationDayProbeResult(false, null, null, "onbekend-beroep", null));
        }

        var result = await _generator.ProbeAsync(pick.Jobs[0].Id, cancellationToken);
        _audit.TargetLabel = result.TitleNl;
        _audit.TargetId = result.EscoId;
        _audit.Reason = result.Ok
            ? result.TitleNl + ": de dag voldoet."
            : OccupationDayWriteErrors.SafeSnippet(result.Error);
        if (!result.Ok)
        {
            _audit.ResultOverride = AdminAuditKeys.Results.Failed;
        }

        return Ok(result);
    }

    /// <summary>Background one-shot. Omit limit to fill every missing occupation. ids limits the pilot.</summary>
    [HttpPost("start")]
    [EnableRateLimiting("public-write")]
    [AdminAudit(AdminAuditKeys.OccupationDayStart, TargetType = "setting")]
    public IActionResult Start(
        [FromQuery] int? limit,
        [FromQuery] string? ids,
        [FromBody] OccupationDayRunRequest? body)
    {
        var chosen = body?.Limit ?? limit;
        if (chosen is < 1)
        {
            return BadRequest(new { message = "Limit moet minstens 1 zijn." });
        }

        if (!TryPick(body?.Ids ?? ids, out var only, out var error))
        {
            return BadRequest(new { message = error });
        }

        Guid? actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : null;
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (!_runner.TryStart(chosen, only, actorId, role))
        {
            return Conflict(new { message = "Er loopt al een vulling." });
        }

        var titles = only is null ? [] : only.Select(id => OccupationCatalog.Shared.Get(id)?.Nl ?? id).ToList();
        _audit.Reason = only is null
            ? "limiet " + (chosen?.ToString() ?? "alles")
            : titles.Count + " beroepen";
        _audit.TargetLabel = titles.Count == 0 ? null : string.Join(", ", titles.Take(8));
        return Ok(new OccupationDayStartResult(true, false, null, titles));
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

    private void Remember(OccupationDayGenerateResult result)
    {
        var first = result.Failures.Count > 0 ? result.Failures[0].Reason : null;
        var reason = result.Generated + " gelukt, " + result.Failed + " mislukt.";
        if (!string.IsNullOrWhiteSpace(first))
        {
            reason = reason + " " + OccupationDayWriteErrors.SafeSnippet(first);
        }

        _audit.Reason = reason;
        if (result.KeyMissing || result.KeyRejected || (result.Failed > 0 && result.Generated == 0))
        {
            _audit.ResultOverride = AdminAuditKeys.Results.Failed;
        }
    }

    private static bool TryPick(string? raw, out IReadOnlySet<string>? only, out string? error)
    {
        only = null;
        error = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        var pick = OccupationDaySelection.Resolve(raw);
        if (pick.Unknown.Count > 0)
        {
            error = "Dit beroep kennen we niet: " + string.Join(", ", pick.Unknown.Take(5));
            return false;
        }

        if (pick.Jobs.Count == 0)
        {
            error = "Kies eerst één beroep.";
            return false;
        }

        only = pick.Jobs.Select(job => job.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return true;
    }
}

public sealed class OccupationDayRunRequest
{
    public int? Limit { get; set; }
    public string? Ids { get; set; }
}

public sealed record OccupationDayStartResult(bool Started, bool Busy, string? Message, IReadOnlyList<string> Titles);

