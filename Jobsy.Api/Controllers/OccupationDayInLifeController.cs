using Jobsy.Core.Authorization;
using Jobsy.Core.Careers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

/// <summary>Stored typical workday for one ESCO occupation. Never calls OpenAI.</summary>
[ApiController]
[Route("api/me/occupation-day-in-life")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class OccupationDayInLifeController : ControllerBase
{
    private readonly IOccupationDayInLifeReader _days;

    public OccupationDayInLifeController(IOccupationDayInLifeReader days)
    {
        _days = days;
    }

    [HttpGet("{escoId:guid}")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<OccupationDayResponse>> Get(Guid escoId, CancellationToken cancellationToken)
    {
        var result = await _days.GetAsync(escoId.ToString("D"), cancellationToken);
        if (!result.KnownOccupation)
        {
            return NotFound(new { message = "Dit beroep kennen we niet." });
        }

        if (!result.Enabled || result.Day is null)
        {
            var title = OccupationCatalog.Shared.Get(escoId.ToString("D"))?.Nl;
            return Ok(new OccupationDayResponse(result.Enabled, false, escoId.ToString("D"), title, null, null, null, [], null, false));
        }

        var day = result.Day;
        return Ok(new OccupationDayResponse(
            true,
            true,
            day.EscoId,
            day.TitleNl,
            day.Morning,
            day.Midday,
            day.Afternoon,
            day.Highlights,
            day.VariesNote,
            day.ThinSource));
    }

    public sealed record OccupationDayResponse(
        bool Enabled,
        bool Found,
        string? EscoId,
        string? TitleNl,
        string? Morning,
        string? Midday,
        string? Afternoon,
        IReadOnlyList<string> Highlights,
        string? VariesNote,
        bool ThinSource);
}
