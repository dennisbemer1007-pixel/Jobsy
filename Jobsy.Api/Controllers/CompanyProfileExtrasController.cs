using Jobsy.Api.Authorization;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/companies/{companyId:guid}/profile-extras")]
[Authorize(Policy = JobsyPolicies.RequireAdminOrEmployer)]
public sealed class CompanyProfileExtrasController : ControllerBase
{
    private readonly ICompanyProfileExtrasService _extras;
    private readonly ICompanyAuthorizationService _companyAuth;

    public CompanyProfileExtrasController(
        ICompanyProfileExtrasService extras,
        ICompanyAuthorizationService companyAuth)
    {
        _extras = extras;
        _companyAuth = companyAuth;
    }

    [HttpGet]
    public async Task<ActionResult<CompanyProfileExtrasDto>> Get(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        if (!await _companyAuth.CanAccessCompanyAsync(User, companyId, cancellationToken))
        {
            return NotFound(new { message = "Bedrijf niet gevonden of geen toegang." });
        }

        var dto = await _extras.GetAsync(companyId, cancellationToken);
        return dto is null
            ? NotFound(new { message = "Bedrijf niet gevonden." })
            : Ok(dto);
    }

    [HttpPut]
    [Authorize(Roles = JobsyRoles.EmployerMutateRoles)]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<CompanyProfileExtrasDto>> Put(
        Guid companyId,
        [FromBody] CompanyProfileExtrasRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _companyAuth.CanAccessCompanyAsync(User, companyId, cancellationToken))
        {
            return NotFound(new { message = "Bedrijf niet gevonden of geen toegang." });
        }

        // Unverified companies may fill the profile (03). Mutate roles already cover managers.
        try
        {
            var dto = await _extras.SaveAsync(
                companyId,
                new CompanyProfileExtrasUpdate(
                    WorkTypeLabels: request.WorkTypeLabels,
                    CultureSliders: request.CultureSliders,
                    ValueCardIds: request.ValueCardIds),
                cancellationToken);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public sealed class CompanyProfileExtrasRequest
{
    public string[]? WorkTypeLabels { get; set; }
    public Dictionary<string, int>? CultureSliders { get; set; }
    public string[]? ValueCardIds { get; set; }
}
