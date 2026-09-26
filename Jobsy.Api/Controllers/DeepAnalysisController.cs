using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
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
    private readonly IAssessmentReportPdfService _reports;
    private readonly IUserLookupService _users;

    public DeepAnalysisController(
        IDeepAnalysisService deep,
        IAssessmentReportPdfService reports,
        IUserLookupService users)
    {
        _deep = deep;
        _reports = reports;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<DeepAnalysisStateDto>> Get(
        [FromQuery] string? kind,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(await _deep.GetStateAsync(user.Id, ParseKind(kind), cancellationToken));
    }

    [HttpPost("checkout")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<DeepAnalysisCheckoutResult>> Checkout(
        [FromQuery] string? kind,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }
        if (!CanUseTests(user, out var message))
        {
            return BadRequest(new { message });
        }

        try
        {
            return Ok(await _deep.StartCheckoutAsync(user.Id, ParseKind(kind), cancellationToken));
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
        if (!CanUseTests(user, out var message))
        {
            return BadRequest(new { message });
        }

        var ok = await _deep.TryFulfillPaidCheckoutAsync(
            paymentId,
            expectedUserId: user.Id,
            allowDevStubMarkPaid: true,
            cancellationToken);
        if (!ok)
        {
            return BadRequest(new { message = "Checkout niet gevonden, niet van jou, of betaling nog niet afgerond." });
        }

        var checkoutKind =
            paymentId.Contains("_career_", StringComparison.OrdinalIgnoreCase) ? AssessmentKind.Career
            : paymentId.Contains("_values_", StringComparison.OrdinalIgnoreCase) ? AssessmentKind.Values
            : paymentId.Contains("_culture_", StringComparison.OrdinalIgnoreCase) ? AssessmentKind.Culture
            : AssessmentKind.Competence;

        return Ok(await _deep.GetStateAsync(user.Id, checkoutKind, cancellationToken));
    }

    [HttpPut]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<DeepAnalysisStateDto>> Save(
        [FromQuery] string? kind,
        [FromBody] SaveDeepAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }
        if (!CanUseTests(user, out var message))
        {
            return BadRequest(new { message });
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
            return Ok(await _deep.SaveAsync(user.Id, ParseKind(kind), answers, request.Complete, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("report")]
    [EnableRateLimiting("public-read")]
    public async Task<IActionResult> Report(
        [FromQuery] string? kind,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var pdf = await _reports.TryRenderAsync(user.Id, ParseKind(kind), cancellationToken);
        if (pdf is null)
        {
            return NotFound(new { message = "Rapport is nog niet beschikbaar. Rond de diepte-analyse eerst af." });
        }

        return File(pdf.Content, "application/pdf", pdf.FileName);
    }

    private static AssessmentKind ParseKind(string? kind)
        => AssessmentKindLabels.ParseOrDefault(kind);

    private static bool CanUseTests(Core.Entities.User user, out string message)
    {
        if (!CandidateConsentRules.CanUseCandidateFeatures(user))
        {
            message = CandidateConsentRules.ParentalConsentRequiredMessage;
            return false;
        }

        if (!CandidateConsentRules.HasCurrentTestAiConsent(user))
        {
            message = CandidateConsentRules.TestConsentRequiredMessage;
            return false;
        }

        message = string.Empty;
        return true;
    }
}

public sealed record SaveDeepAnalysisRequest(
    Dictionary<string, int>? Answers,
    bool Complete);
