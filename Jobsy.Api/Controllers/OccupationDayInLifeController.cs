using Jobsy.Core.Authorization;
using Jobsy.Core.Careers;
using Jobsy.Core.Localization;
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
    public async Task<ActionResult<OccupationDayResponse>> Get(
        Guid escoId,
        [FromQuery] string? lang,
        CancellationToken cancellationToken)
    {
        var id = escoId.ToString("D");
        if (OccupationCatalog.Shared.Get(id) is null)
        {
            return NotFound(new { message = "Dit beroep kennen we niet." });
        }

        var language = ResolveLanguage(lang);
        var response = await OccupationDayInLifeQuery.ToResponseAsync(_days, id, language, cancellationToken);
        return Ok(ToClient(response));
    }

    [HttpGet("available")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<OccupationDayAvailableResponse>> Available(
        [FromQuery] string? ids,
        CancellationToken cancellationToken)
    {
        var list = (ids ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var stored = await _days.StoredEscoIdsAsync(list, cancellationToken);
        return Ok(new OccupationDayAvailableResponse
        {
            Ids = stored.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList()
        });
    }

    internal static OccupationDayResponse ToClient(OccupationDayPublicResponse response)
        => new(
            response.Enabled,
            response.Found,
            response.EscoId,
            response.TitleNl,
            response.Morning,
            response.Midday,
            response.Afternoon,
            response.Closing,
            response.Highlights,
            response.VariesNote,
            response.ThinSource,
            response.Language,
            response.Blocks ?? [],
            response.Tasks ?? [],
            response.Skills ?? []);

    private string ResolveLanguage(string? lang)
    {
        if (JobsyLanguages.IsSupported(lang))
        {
            return JobsyLanguages.Normalize(lang);
        }

        if (Request.Headers.TryGetValue("X-Jobsy-Language", out var header)
            && JobsyLanguages.IsSupported(header.ToString()))
        {
            return JobsyLanguages.Normalize(header.ToString());
        }

        return JobsyLanguages.Default;
    }

    public sealed record OccupationDayResponse(
        bool Enabled,
        bool Found,
        string? EscoId,
        string? TitleNl,
        string? Morning,
        string? Midday,
        string? Afternoon,
        string? Closing,
        IReadOnlyList<string> Highlights,
        string? VariesNote,
        bool ThinSource,
        string Language,
        IReadOnlyList<OccupationDayBlock>? Blocks = null,
        IReadOnlyList<string>? Tasks = null,
        IReadOnlyList<string>? Skills = null);
}
