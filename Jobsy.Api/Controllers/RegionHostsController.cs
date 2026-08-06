using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/region-hosts")]
public sealed class RegionHostsController : ControllerBase
{
    private readonly IRegionHostService _hosts;

    public RegionHostsController(IRegionHostService hosts)
    {
        _hosts = hosts;
    }

    /// <summary>Resolve active regional branding for the current (or queried) hostname.</summary>
    [HttpGet("resolve")]
    [AllowAnonymous]
    public async Task<ActionResult<RegionHostDto>> Resolve(
        [FromQuery] string? host,
        CancellationToken cancellationToken)
    {
        var hostname = string.IsNullOrWhiteSpace(host)
            ? Request.Host.Host
            : host;
        var row = await _hosts.FindByHostnameAsync(hostname, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        return Ok(ToDto(row));
    }

    [HttpGet]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<IReadOnlyList<RegionHostDto>>> List(CancellationToken cancellationToken)
    {
        var rows = await _hosts.ListAsync(cancellationToken);
        return Ok(rows.Select(ToDto).ToList());
    }

    [HttpPost]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<RegionHostDto>> Create(
        [FromBody] RegionHostUpsertRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _hosts.CreateAsync(ToUpsert(request), cancellationToken);
            return CreatedAtAction(nameof(List), ToDto(created));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<RegionHostDto>> Update(
        Guid id,
        [FromBody] RegionHostUpsertRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _hosts.UpdateAsync(id, ToUpsert(request), cancellationToken);
            if (updated is null)
            {
                return NotFound();
            }

            return Ok(ToDto(updated));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _hosts.DeleteAsync(id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    private static RegionHostUpsert ToUpsert(RegionHostUpsertRequest request) =>
        new(
            request.Hostname ?? string.Empty,
            request.DisplayName ?? string.Empty,
            request.Slogan,
            request.AddressLabel,
            request.Latitude,
            request.Longitude,
            request.BackgroundImageUrl,
            request.IsActive);

    private static RegionHostDto ToDto(Core.Entities.RegionHost h) =>
        new(
            h.Id,
            h.Hostname,
            h.DisplayName,
            h.Slogan,
            h.AddressLabel,
            h.Latitude,
            h.Longitude,
            h.BackgroundImageUrl,
            h.IsActive,
            h.CreatedAtUtc,
            h.UpdatedAtUtc);
}

public sealed record RegionHostUpsertRequest(
    string? Hostname,
    string? DisplayName,
    string? Slogan,
    string? AddressLabel,
    double? Latitude,
    double? Longitude,
    string? BackgroundImageUrl,
    bool IsActive = true);

public sealed record RegionHostDto(
    Guid Id,
    string Hostname,
    string DisplayName,
    string? Slogan,
    string? AddressLabel,
    double? Latitude,
    double? Longitude,
    string? BackgroundImageUrl,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
