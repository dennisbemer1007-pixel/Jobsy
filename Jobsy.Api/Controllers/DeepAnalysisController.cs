using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/deep-analysis")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class DeepAnalysisController : ControllerBase
{
    private readonly IDeepAnalysisService _deep;
    private readonly IUserLookupService _users;

    public DeepAnalysisController(IDeepAnalysisService deep, IUserLookupService users)
    {
        _deep = deep;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<DeepAnalysisStateDto>> Get(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(await _deep.GetStateAsync(user.Id, cancellationToken));
    }

    [HttpPost("checkout")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<DeepAnalysisCheckoutResult>> Checkout(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        try
        {
            return Ok(await _deep.StartCheckoutAsync(user.Id, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("checkout/{paymentId}/complete")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> CompleteCheckout(string paymentId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var ok = await _deep.TryFulfillPaidCheckoutAsync(paymentId, cancellationToken);
        if (!ok)
        {
            return BadRequest(new { message = "Checkout niet gevonden of al verwerkt." });
        }

        return Ok(await _deep.GetStateAsync(user.Id, cancellationToken));
    }

    [HttpPut]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<DeepAnalysisStateDto>> Save(
        [FromBody] SaveDeepAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var answers = new Dictionary<int, int>();
        if (request.Answers is not null)
        {
            foreach (var (key, value) in request.Answers)
            {
                if (!int.TryParse(key, out var id))
                {
                    return BadRequest(new { message = "Onbekend vraagnummer in de diepte-analyse." });
                }

                answers[id] = value;
            }
        }

        try
        {
            return Ok(await _deep.SaveAsync(user.Id, answers, request.Complete, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public sealed record SaveDeepAnalysisRequest(
    Dictionary<string, int>? Answers,
    bool Complete);
