using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/company/culture")]
[Authorize(Policy = JobsyPolicies.RequireEmployer)]
public sealed class CompanyCultureController : ControllerBase
{
    private readonly ICompanyCultureService _culture;
    private readonly IUserLookupService _users;
    private readonly ICompanyAuthorizationService _companyAuth;

    public CompanyCultureController(
        ICompanyCultureService culture,
        IUserLookupService users,
        ICompanyAuthorizationService companyAuth)
    {
        _culture = culture;
        _users = users;
        _companyAuth = companyAuth;
    }

    [HttpGet]
    public async Task<ActionResult<CompanyCultureStateDto>> Get(
        [FromQuery] Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var company = await ResolveCompanyIdAsync(companyId, cancellationToken);
        if (company is null)
        {
            return NotFound(new { message = "Bedrijf niet gevonden of geen toegang." });
        }

        return Ok(await _culture.GetAsync(company.Value, cancellationToken));
    }

    [HttpPut]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<CompanyCultureStateDto>> Save(
        [FromQuery] Guid? companyId,
        [FromBody] SaveCandidateCompetenciesRequest request,
        CancellationToken cancellationToken)
    {
        var company = await ResolveCompanyIdAsync(companyId, cancellationToken);
        if (company is null)
        {
            return NotFound(new { message = "Bedrijf niet gevonden of geen toegang." });
        }

        var answers = new Dictionary<int, int>();
        if (request.Answers is not null)
        {
            foreach (var (key, value) in request.Answers)
            {
                if (int.TryParse(key, out var id))
                {
                    answers[id] = value;
                }
                else
                {
                    return BadRequest(new { message = "Onbekend vraagnummer in de cultuurscan." });
                }
            }
        }

        try
        {
            return Ok(await _culture.SaveAsync(company.Value, answers, request.Complete, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<Guid?> ResolveCompanyIdAsync(Guid? companyId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var target = companyId ?? user.CompanyId;
        if (target is not Guid id)
        {
            return null;
        }

        try
        {
            await _companyAuth.EnsureCanAccessCompanyAsync(User, id, cancellationToken);
            return id;
        }
        catch
        {
            return null;
        }
    }
}
