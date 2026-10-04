using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public class MeReferenceConfirmationController : ControllerBase
{
    private readonly IUserLookupService _users;
    private readonly ReferenceConfirmationService _confirmations;

    public MeReferenceConfirmationController(IUserLookupService users, ReferenceConfirmationService confirmations)
    {
        _users = users;
        _confirmations = confirmations;
    }

    [HttpGet("reference-confirmations")]
    public async Task<ActionResult> List(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var rows = await _confirmations.ListForCandidateAsync(user.Id, cancellationToken);
        return Ok(rows);
    }

    [HttpPost("references/{referenceId:guid}/confirmation")]
    public async Task<ActionResult> RequestConfirmation(
        Guid referenceId,
        [FromBody] RequestReferenceConfirmationBody? body,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var (ok, error) = await _confirmations.RequestAsync(
            user.Id,
            referenceId,
            body?.RoleTitle,
            body?.ConsentAccepted == true,
            cancellationToken);
        return ok ? Ok(new { ok = true }) : BadRequest(new { error, message = Message(error) });
    }

    [HttpPut("references/{referenceId:guid}/confirmation/share")]
    public async Task<ActionResult> Share(
        Guid referenceId,
        [FromBody] ReferenceShareChoice? body,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var choice = body ?? new ReferenceShareChoice(false, false, false, false, false, false);
        var (ok, error) = await _confirmations.SetShareAsync(user.Id, referenceId, choice, cancellationToken);
        return ok ? Ok(new { ok = true }) : BadRequest(new { error, message = Message(error) });
    }

    private static string Message(string? error) => error switch
    {
        "consent" => "Vink de toestemming aan.",
        "role" => "Vul het werk in dat je deed.",
        "limit_reference" => "Je kunt deze persoon hooguit 3 keer vragen.",
        "limit_month" => "Je kunt deze maand hooguit 10 keer vragen.",
        "already" => "Deze persoon heeft al geantwoord.",
        "mail" => "De mail kon niet weg. Probeer het later opnieuw.",
        "not_confirmed" => "Er is nog geen bevestiging.",
        _ => "Deze referent staat niet in je paspoort."
    };
}

public sealed record RequestReferenceConfirmationBody(string? RoleTitle, bool ConsentAccepted);
