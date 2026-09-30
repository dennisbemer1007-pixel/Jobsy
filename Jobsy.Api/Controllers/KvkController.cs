using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Jobsy.Core.Features;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/kvk")]
[EnableRateLimiting("public-write")]
[RequiresFeature(PlatformFeature.Employers)]
public class KvkController : ControllerBase
{
    private readonly IKvkService _kvk;

    public KvkController(IKvkService kvk)
    {
        _kvk = kvk;
    }

    /// <summary>
    /// Public name or KvK-number search. Anonymous, dedicated <c>kvk-search</c> rate limit.
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [EnableRateLimiting("kvk-search")]
    public async Task<ActionResult<KvkSearchResponse>> Search(
        [FromQuery] string? q,
        [FromQuery] string? plaats,
        [FromQuery] int? pagina,
        CancellationToken cancellationToken)
    {
        var text = (q ?? string.Empty).Trim();
        var digitOnly = new string(text.Where(char.IsDigit).ToArray());
        var isNumber = digitOnly.Length == 8
                       && text.All(c => char.IsDigit(c) || char.IsWhiteSpace(c) || c is '.' or '-');
        if (!isNumber && text.Length < 3)
        {
            return BadRequest(new { message = "Zoekterm moet minimaal 3 tekens zijn." });
        }

        try
        {
            var result = await _kvk.SearchAsync(
                new KvkSearchQuery(text, plaats, pagina is null or < 1 ? 1 : pagina.Value),
                cancellationToken);

            return Ok(new KvkSearchResponse(
                result.Status.ToString(),
                result.Total,
                result.Hits.Select(h => new KvkSearchHitDto(
                    h.KvkNumber,
                    h.Name,
                    h.Place,
                    h.Type,
                    h.VestigingCount,
                    h.IsOnLobsy)).ToList(),
                result.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Public lookup for registration flows. Establishment occupancy (IsInUse) is only
    /// returned to authenticated callers to avoid leaking registration state anonymously.
    /// </summary>
    [HttpGet("{kvkNumber}")]
    [AllowAnonymous]
    public async Task<ActionResult<KvkCompanyResult>> GetCompany(
        string kvkNumber,
        CancellationToken cancellationToken)
    {
        var company = await _kvk.GetByKvkNumberAsync(kvkNumber, cancellationToken);
        return company is null ? NotFound() : Ok(company);
    }

    [HttpGet("{kvkNumber}/establishments")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<KvkEstablishmentResult>>> GetEstablishments(
        string kvkNumber,
        CancellationToken cancellationToken)
    {
        var items = await _kvk.GetEstablishmentsAsync(kvkNumber, cancellationToken);
        if (User.Identity?.IsAuthenticated == true)
        {
            return Ok(items);
        }

        // Anonymous callers see establishments but not whether they are already registered.
        return Ok(items.Select(i => i with { IsInUse = false }));
    }
}

public sealed record KvkSearchResponse(
    string Status,
    int Total,
    IReadOnlyList<KvkSearchHitDto> Hits,
    string? Message = null);

public sealed record KvkSearchHitDto(
    string KvkNumber,
    string Name,
    string Place,
    string Type,
    int? VestigingCount,
    bool IsOnLobsy);
