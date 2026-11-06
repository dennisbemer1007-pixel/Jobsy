using Jobsy.Api.Authorization;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/companies/{companyId:guid}/engagement")]
[Authorize(Policy = JobsyPolicies.RequireAdminOrEmployer)]
public sealed class CompanyEngagementController : ControllerBase
{
    private readonly ICompanyEngagementService _engagement;
    private readonly ICompanyAuthorizationService _companyAuth;

    public CompanyEngagementController(
        ICompanyEngagementService engagement,
        ICompanyAuthorizationService companyAuth)
    {
        _engagement = engagement;
        _companyAuth = companyAuth;
    }

    [HttpGet]
    public async Task<ActionResult<CompanyEngagementDto>> Get(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        if (!await _companyAuth.CanAccessCompanyAsync(User, companyId, cancellationToken))
        {
            return NotFound(new { message = "Bedrijf niet gevonden of geen toegang." });
        }

        var dto = await _engagement.GetAsync(companyId, cancellationToken);
        return dto is null
            ? NotFound(new { message = "Bedrijf niet gevonden." })
            : Ok(dto);
    }

    [HttpPut]
    [Authorize(Roles = JobsyRoles.EmployerMutateRoles)]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<CompanyEngagementDto>> Put(
        Guid companyId,
        [FromBody] CompanyEngagementRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _companyAuth.CanAccessCompanyAsync(User, companyId, cancellationToken))
        {
            return NotFound(new { message = "Bedrijf niet gevonden of geen toegang." });
        }

        try
        {
            var claims = (request.Claims ?? [])
                .Select(c => new CompanyEngagementClaimInput(c.ItemId, c.ProofUrl, c.ProofText))
                .ToList();
            var dto = await _engagement.SaveAsync(
                companyId,
                new CompanyEngagementUpdate(claims),
                cancellationToken);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public sealed class CompanyEngagementRequest
{
    public List<CompanyEngagementClaimBody>? Claims { get; set; }
}

public sealed class CompanyEngagementClaimBody
{
    public string ItemId { get; set; } = "";
    public string? ProofUrl { get; set; }
    public string? ProofText { get; set; }
}

[ApiController]
[Route("api/admin/engagement")]
[Authorize(Roles = "Admin")]
public sealed class AdminEngagementController : ControllerBase
{
    private readonly ICompanyEngagementService _engagement;

    public AdminEngagementController(ICompanyEngagementService engagement)
    {
        _engagement = engagement;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminEngagementQueueItem>>> List(
        [FromQuery] string? filter,
        [FromQuery] string? q,
        CancellationToken cancellationToken)
        => Ok(await _engagement.ListAdminQueueAsync(filter, q, cancellationToken));

    [HttpPost("{claimId:guid}/check")]
    public async Task<IActionResult> Check(Guid claimId, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            await _engagement.CheckAsync(claimId, userId.Value, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{claimId:guid}/remove")]
    public async Task<IActionResult> Remove(
        Guid claimId,
        [FromBody] AdminEngagementReasonRequest request,
        CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            await _engagement.RemoveAsync(claimId, userId.Value, request.Reason ?? "", cancellationToken);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{claimId:guid}/reset")]
    public async Task<IActionResult> Reset(Guid claimId, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            await _engagement.ResetToSelfDeclaredAsync(claimId, userId.Value, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private Guid? UserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}

public sealed class AdminEngagementReasonRequest
{
    public string? Reason { get; set; }
}

[ApiController]
[Route("api/public/companies/{companyId:guid}/engagement")]
[AllowAnonymous]
[EnableRateLimiting("public-read")]
public sealed class PublicCompanyEngagementController : ControllerBase
{
    private readonly ICompanyEngagementService _engagement;
    private readonly JobsyDbContext _db;

    public PublicCompanyEngagementController(
        ICompanyEngagementService engagement,
        JobsyDbContext db)
    {
        _engagement = engagement;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PublicEngagementBadgeDto>>> Get(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null || !PublicVisibility.IsCompanyPublic(company))
        {
            return NotFound();
        }

        var dto = await _engagement.GetAsync(companyId, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        var badges = dto.Claims
            .Where(c => CompanyEngagementStatuses.IsPublic(c.Status))
            .Select(c => new PublicEngagementBadgeDto(
                c.ItemId,
                c.Status,
                c.CheckedSource,
                c.ProofUrl))
            .ToList();
        return Ok(badges);
    }

    [HttpPost("report")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> Report(
        Guid companyId,
        [FromBody] EngagementReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _engagement.ReportAsync(
                companyId,
                request.ItemId ?? "",
                request.Message ?? "",
                request.ReporterEmail,
                cancellationToken);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

public sealed class EngagementReportRequest
{
    public string? ItemId { get; set; }
    public string? Message { get; set; }
    public string? ReporterEmail { get; set; }
}
