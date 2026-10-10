using Jobsy.Api.Authorization;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/candidate/external-vacancies")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
[RequiresFeature(PlatformFeature.CandidateExternalVacancies)]
public sealed class CandidateExternalVacanciesController : ControllerBase
{
    private readonly ICandidateExternalVacancyService _service;
    private readonly IUserLookupService _users;

    public CandidateExternalVacanciesController(
        ICandidateExternalVacancyService service,
        IUserLookupService users)
    {
        _service = service;
        _users = users;
    }

    [HttpPost("import")]
    public async Task<ActionResult<ExternalVacancyDetailDto>> Import(
        [FromBody] ExternalVacancyImportRequest request,
        CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        try
        {
            var dto = await _service.ImportAsync(user.Id, request.Url, cancellationToken);
            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "rate_limit")
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new { code = "rate_limit" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "fetch_failed")
        {
            return BadRequest(new { code = "fetch_failed", message = "De pagina kon niet worden gelezen." });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExternalVacancyListItemDto>>> List(CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(await _service.ListAsync(user.Id, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExternalVacancyDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var dto = await _service.GetDetailAsync(user.Id, id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("{id:guid}/apply")]
    public async Task<ActionResult<ExternalVacancyApplyResultDto>> Apply(
        Guid id,
        [FromBody] ExternalVacancyApplyRequest request,
        CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var result = await _service.ApplyAsync(user.Id, id, request, cancellationToken);
        if (!result.Succeeded && result.ErrorCode is "not_found")
        {
            return NotFound(result);
        }

        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}/application-letter.pdf")]
    public async Task<IActionResult> ApplicationLetter(Guid id, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var pdf = await _service.BuildApplicationLetterPdfAsync(user.Id, id, cancellationToken);
        if (pdf is null)
        {
            return NotFound();
        }

        return File(pdf, "application/pdf", "sollicitatiebrief.pdf");
    }

    private async Task<Core.Entities.User?> RequireUserAsync(CancellationToken cancellationToken)
        => await _users.FindByPrincipalAsync(User, cancellationToken);
}
