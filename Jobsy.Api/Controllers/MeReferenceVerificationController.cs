using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public class MeReferenceVerificationController : ControllerBase
{
    private readonly IUserLookupService _users;
    private readonly ReferenceVerificationService _verification;

    public MeReferenceVerificationController(IUserLookupService users, ReferenceVerificationService verification)
    {
        _users = users;
        _verification = verification;
    }

    [HttpGet("reference-verification/eligible")]
    public async Task<ActionResult> Eligible(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new { eligible = await _verification.IsEligibleAsync(user.Id, cancellationToken) });
    }

    [HttpGet("reference-verification/{requestId:guid}")]
    public async Task<ActionResult> Detail(Guid requestId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (!await _verification.IsFeatureEnabledAsync(cancellationToken))
        {
            return NotFound();
        }

        var detail = await _verification.GetDetailForCandidateAsync(user.Id, requestId, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("reference-verification")]
    public async Task<ActionResult> List(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (!await _verification.IsFeatureEnabledAsync(cancellationToken))
        {
            return NotFound();
        }

        var rows = await _verification.ListForCandidateAsync(user.Id, cancellationToken);
        return Ok(rows);
    }

    [HttpGet("reference-verification/verified-competences")]
    public async Task<ActionResult> VerifiedCompetences(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (!await _verification.IsFeatureEnabledAsync(cancellationToken))
        {
            return Ok(Array.Empty<object>());
        }

        var prefs = MeController.ParsePreferences(user.PreferencesJson);
        var language = Jobsy.Core.Localization.JobsyLanguages.Normalize(prefs.Language);
        var facts = await _verification.VerifiedCompetencesForCandidateAsync(user.Id, language, cancellationToken);
        return Ok(facts);
    }

    [HttpPost("work-experience/{entryId:guid}/reference-verification")]
    public async Task<ActionResult> SendRequest(
        Guid entryId,
        [FromBody] RequestWorkExperienceReferenceBody? body,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var (ok, error) = await _verification.RequestAsync(
            user.Id,
            entryId,
            body?.ReferentName,
            body?.Relation,
            body?.Email,
            body?.ConsentAccepted == true,
            cancellationToken);
        return ok ? Ok(new { ok = true }) : BadRequest(new { error, message = Message(error) });
    }

    [HttpDelete("reference-verification/{requestId:guid}")]
    public async Task<ActionResult> Revoke(Guid requestId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var (ok, error) = await _verification.RevokeAsync(user.Id, requestId, cancellationToken);
        return ok ? Ok(new { ok = true }) : BadRequest(new { error });
    }

    private static string Message(string? error) => error switch
    {
        "disabled" => "Deze functie staat nog niet aan.",
        "consent" => "Vink de toestemming aan.",
        "referent" => "Vul naam, relatie en een geldig e-mailadres in.",
        "not_found" => "Werkervaring niet gevonden.",
        "not_eligible" => "Je kunt pas referenties vragen als je tests klaar zijn en werkervaring hebt ingevuld.",
        "limit_entry" => "Je kunt voor deze werkervaring hooguit 3 keer per maand vragen.",
        "limit_month" => "Je kunt deze maand hooguit 10 keer vragen.",
        "already" => "Deze werkervaring is al bevestigd.",
        "mail" => "De mail kon niet worden verstuurd. Probeer het later opnieuw.",
        _ => "Er ging iets mis."
    };
}

public sealed record RequestWorkExperienceReferenceBody(
    string? ReferentName,
    string? Relation,
    string? Email,
    bool ConsentAccepted);
