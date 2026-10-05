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
        var language = ResolveLanguage(lang);
        var result = await _days.GetAsync(escoId.ToString("D"), language, cancellationToken);
        if (!result.KnownOccupation)
        {
            return NotFound(new { message = "Dit beroep kennen we niet." });
        }

        if (!result.Enabled || result.Day is null)
        {
            var title = OccupationCatalog.Shared.Get(escoId.ToString("D"))?.Nl;
            return Ok(new OccupationDayResponse(result.Enabled, false, escoId.ToString("D"), title, null, null, null, null, [], null, false, language));
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
            day.Closing,
            day.Highlights,
            day.VariesNote,
            day.ThinSource,
            language));
    }

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
        string Language);
}
