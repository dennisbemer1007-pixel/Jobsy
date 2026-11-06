using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reports.TestReflection;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/test-reflection")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class CandidateTestReflectionController : ControllerBase
{
    private readonly ITestReflectionService _reflection;
    private readonly IUserLookupService _users;

    public CandidateTestReflectionController(ITestReflectionService reflection, IUserLookupService users)
    {
        _reflection = reflection;
        _users = users;
    }

    [HttpGet("{kind}")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<TestReflectionState>> Get(
        string kind,
        [FromQuery] string? lang,
        CancellationToken cancellationToken)
    {
        if (!AssessmentKindLabels.TryParse(kind, out var assessmentKind))
        {
            return NotFound();
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        return Ok(await _reflection.GetFullMirrorAsync(user.Id, assessmentKind, lang ?? "nl", cancellationToken));
    }

    [HttpPut("{kind}/feedback")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> SaveFeedback(
        string kind,
        [FromBody] SaveTestReflectionFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (!AssessmentKindLabels.TryParse(kind, out var assessmentKind))
        {
            return NotFound();
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        if (!Enum.TryParse<TestReflectionFeedback>(request.Feedback, ignoreCase: true, out var feedback))
        {
            return BadRequest(new { message = "Onbekende feedback." });
        }

        var level = request.Level ?? TestDepthLevel.Full;
        await _reflection.SaveFeedbackAsync(user.Id, assessmentKind, level, feedback, cancellationToken);
        return NoContent();
    }
}

public sealed class SaveTestReflectionFeedbackRequest
{
    public string Feedback { get; set; } = "";

    public TestDepthLevel? Level { get; set; }
}
