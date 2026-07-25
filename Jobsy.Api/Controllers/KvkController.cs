using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/kvk")]
public class KvkController : ControllerBase
{
    private readonly IKvkService _kvk;

    public KvkController(IKvkService kvk)
    {
        _kvk = kvk;
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
