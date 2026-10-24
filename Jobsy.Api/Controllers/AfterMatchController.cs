using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Authorize]
[RequiresFeature(PlatformFeature.AfterMatch)]
public sealed class AfterMatchController : ControllerBase
{
    private readonly IAfterMatchService _afterMatch;
    private readonly IUserLookupService _users;
    private readonly ICompanyAuthorizationService _companies;

    public AfterMatchController(
        IAfterMatchService afterMatch,
        IUserLookupService users,
        ICompanyAuthorizationService companies)
    {
        _afterMatch = afterMatch;
        _users = users;
        _companies = companies;
    }

    [HttpGet("api/me/applications/{applicationId:guid}/after-match")]
    [Authorize(Roles = JobsyRoles.Candidate)]
    public async Task<ActionResult<AfterMatchThreadDto>> GetForCandidate(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var thread = await _afterMatch.GetThreadForCandidateAsync(applicationId, user.Id, cancellationToken);
        return thread is null ? NotFound() : Ok(thread);
    }

    [HttpGet("api/werkgever/applications/{applicationId:guid}/after-match")]
    [Authorize(Roles = $"{JobsyRoles.BranchManager},{JobsyRoles.RegionalManager},{JobsyRoles.EnterpriseManager},{JobsyRoles.Intermediary},{JobsyRoles.Admin}")]
    public async Task<ActionResult<AfterMatchThreadDto>> GetForEmployer(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var thread = await _afterMatch.GetThreadForEmployerAsync(applicationId, user.Id, cancellationToken);
        return thread is null ? NotFound() : Ok(thread);
    }

    [HttpPost("api/werkgever/applications/{applicationId:guid}/after-match/slots")]
    [Authorize(Roles = $"{JobsyRoles.BranchManager},{JobsyRoles.RegionalManager},{JobsyRoles.EnterpriseManager},{JobsyRoles.Intermediary},{JobsyRoles.Admin}")]
    public async Task<ActionResult<AfterMatchThreadDto>> ProposeEmployerSlots(
        Guid applicationId,
        [FromBody] ProposeEmployerSlotsRequest request,
        CancellationToken cancellationToken)
    {
        var user = await RequireEmployerUserAsync(cancellationToken);
        var thread = await _afterMatch.ProposeEmployerSlotsAsync(applicationId, user.Id, request, cancellationToken);
        return Ok(thread);
    }

    [HttpPost("api/me/applications/{applicationId:guid}/after-match/slots/propose")]
    [Authorize(Roles = JobsyRoles.Candidate)]
    public async Task<ActionResult<AfterMatchThreadDto>> ProposeCandidateSlot(
        Guid applicationId,
        [FromBody] ProposeCandidateSlotRequest request,
        CancellationToken cancellationToken)
    {
        var user = await RequireCandidateUserAsync(cancellationToken);
        var thread = await _afterMatch.ProposeCandidateSlotAsync(applicationId, user.Id, request, cancellationToken);
        return Ok(thread);
    }

    [HttpPost("api/applications/{applicationId:guid}/after-match/select-slot")]
    public async Task<ActionResult<AfterMatchThreadDto>> SelectSlot(
        Guid applicationId,
        [FromBody] SelectSlotRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var isEmployer = _companies.IsEmployer(User);
        var thread = await _afterMatch.SelectSlotAsync(applicationId, user.Id, isEmployer, request, cancellationToken);
        return Ok(thread);
    }

    [HttpPost("api/applications/{applicationId:guid}/after-match/reschedule")]
    public async Task<ActionResult<AfterMatchThreadDto>> Reschedule(
        Guid applicationId,
        [FromBody] RescheduleRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var isEmployer = _companies.IsEmployer(User);
        var thread = await _afterMatch.RescheduleAsync(applicationId, user.Id, isEmployer, request, cancellationToken);
        return Ok(thread);
    }

    [HttpPost("api/applications/{applicationId:guid}/after-match/cancel")]
    public async Task<ActionResult<AfterMatchThreadDto>> Cancel(
        Guid applicationId,
        [FromBody] RescheduleRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var isEmployer = _companies.IsEmployer(User);
        var thread = await _afterMatch.CancelInterviewAsync(applicationId, user.Id, isEmployer, request, cancellationToken);
        return Ok(thread);
    }

    [HttpPost("api/applications/{applicationId:guid}/after-match/messages/preview")]
    public async Task<ActionResult<SensitiveMessagePreview>> PreviewMessage(
        Guid applicationId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var isEmployer = _companies.IsEmployer(User);
        var preview = await _afterMatch.PreviewMessageAsync(applicationId, user.Id, isEmployer, request.Body, cancellationToken);
        return Ok(preview);
    }

    [HttpPost("api/me/applications/{applicationId:guid}/after-match/messages/read")]
    [Authorize(Roles = JobsyRoles.Candidate)]
    public async Task<IActionResult> MarkCandidateMessagesRead(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var user = await RequireCandidateUserAsync(cancellationToken);
        await _afterMatch.MarkCandidateMessagesReadAsync(applicationId, user.Id, cancellationToken);
        return NoContent();
    }

    [HttpPost("api/applications/{applicationId:guid}/after-match/messages")]
    public async Task<ActionResult<AfterMatchThreadDto>> SendMessage(
        Guid applicationId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var isEmployer = _companies.IsEmployer(User);
        var thread = await _afterMatch.SendMessageAsync(applicationId, user.Id, isEmployer, request, cancellationToken);
        return Ok(thread);
    }

    [HttpPost("api/werkgever/applications/{applicationId:guid}/after-match/outcome")]
    [Authorize(Roles = $"{JobsyRoles.BranchManager},{JobsyRoles.RegionalManager},{JobsyRoles.EnterpriseManager},{JobsyRoles.Intermediary},{JobsyRoles.Admin}")]
    public async Task<ActionResult<AfterMatchThreadDto>> RecordOutcome(
        Guid applicationId,
        [FromBody] RecordOutcomeRequest request,
        CancellationToken cancellationToken)
    {
        var user = await RequireEmployerUserAsync(cancellationToken);
        var thread = await _afterMatch.RecordOutcomeAsync(applicationId, user.Id, request, cancellationToken);
        return Ok(thread);
    }

    [HttpPut("api/me/after-match/reminder-preferences")]
    [Authorize(Roles = JobsyRoles.Candidate)]
    public async Task<ActionResult<AfterMatchReminderPreferenceDto>> UpdateReminderPreferences(
        [FromBody] UpdateReminderPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var user = await RequireCandidateUserAsync(cancellationToken);
        var prefs = await _afterMatch.UpdateReminderPreferenceAsync(user.Id, request, cancellationToken);
        return Ok(prefs);
    }

    [HttpGet("api/applications/{applicationId:guid}/after-match/calendar.ics")]
    public async Task<IActionResult> DownloadCalendar(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var bytes = await _afterMatch.BuildCalendarInviteAsync(applicationId, user.Id, cancellationToken);
        return File(bytes, "text/calendar", "lobsy-gesprek.ics");
    }

    private async Task<Jobsy.Core.Entities.User> RequireCandidateUserAsync(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null || !_companies.IsCandidate(User))
        {
            throw new UnauthorizedAccessException();
        }

        return user;
    }

    private async Task<Jobsy.Core.Entities.User> RequireEmployerUserAsync(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null || !_companies.IsEmployer(User))
        {
            throw new UnauthorizedAccessException();
        }

        return user;
    }
}
