using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/golf2/westland")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class CandidateGolf2Controller : ControllerBase
{
    private readonly Golf2CandidateService _golf2;
    private readonly IUserLookupService _users;
    private readonly Golf2WestlandOptions _options;

    public CandidateGolf2Controller(
        Golf2CandidateService golf2,
        IUserLookupService users,
        IOptions<Golf2WestlandOptions> options)
    {
        _golf2 = golf2;
        _users = users;
        _options = options.Value;
    }

    [HttpGet("passport-next-step")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<object>> PassportNextStep(CancellationToken cancellationToken)
    {
        if (!TryRequireEnabled(out var disabled))
        {
            return disabled!;
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var title = await _golf2.GetPassportNextStepAsync(user.Id, cancellationToken);
        return Ok(new { nextStepTitle = title });
    }

    [HttpGet("conversation-sheet")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<ConversationSheetDto>> GetConversationSheet(CancellationToken cancellationToken)
    {
        if (!TryRequireEnabled(out var disabled) || !TryRequireFeature(_options.ConversationSheet, out disabled))
        {
            return disabled!;
        }

        var user = await RequireUser(cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var sheet = await _golf2.GetConversationSheetAsync(user.Id, cancellationToken);
        return sheet is null ? NotFound() : Ok(sheet);
    }

    public sealed record SaveConversationSheetRequest(
        string StrengthsText,
        string MotivationText,
        string CustomText,
        Dictionary<string, string?>? ExtraFields);

    [HttpPut("conversation-sheet")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<ConversationSheetDto>> SaveConversationSheet(
        [FromBody] SaveConversationSheetRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryRequireEnabled(out var disabled) || !TryRequireFeature(_options.ConversationSheet, out disabled))
        {
            return disabled!;
        }

        var user = await RequireUser(cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var dto = new ConversationSheetDto(
            request.StrengthsText ?? "",
            request.MotivationText ?? "",
            request.CustomText ?? "",
            "");
        var (ok, errors, sheet) = await _golf2.SaveConversationSheetAsync(
            user.Id,
            dto,
            request.ExtraFields,
            cancellationToken);
        if (!ok)
        {
            return BadRequest(new { errors });
        }

        return Ok(sheet);
    }

    [HttpGet("outside-work")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IReadOnlyList<OutsideWorkDto>>> ListOutsideWork(CancellationToken cancellationToken)
    {
        if (!TryRequireEnabled(out var disabled) || !TryRequireFeature(_options.OutsideWork, out disabled))
        {
            return disabled!;
        }

        var user = await RequireUser(cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(await _golf2.ListOutsideWorkAsync(user.Id, cancellationToken));
    }

    public sealed record UpsertOutsideWorkRequest(
        Guid? Id,
        string ActivityTitle,
        string Description,
        int? HoursPerWeek,
        int SortOrder);

    [HttpPut("outside-work")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<OutsideWorkDto>> UpsertOutsideWork(
        [FromBody] UpsertOutsideWorkRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryRequireEnabled(out var disabled) || !TryRequireFeature(_options.OutsideWork, out disabled))
        {
            return disabled!;
        }

        var user = await RequireUser(cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        try
        {
            var row = await _golf2.UpsertOutsideWorkAsync(
                user.Id,
                request.Id,
                request.ActivityTitle ?? "",
                request.Description ?? "",
                request.HoursPerWeek,
                request.SortOrder,
                cancellationToken);
            return Ok(row);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("outside-work/{id:guid}")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> DeleteOutsideWork(Guid id, CancellationToken cancellationToken)
    {
        if (!TryRequireEnabled(out var disabled) || !TryRequireFeature(_options.OutsideWork, out disabled))
        {
            return disabled!;
        }

        var user = await RequireUser(cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        await _golf2.DeleteOutsideWorkAsync(user.Id, id, cancellationToken);
        return NoContent();
    }

    [HttpGet("four-tests-feedback")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<FourTestsFeedbackDto>> GetFourTestsFeedback(CancellationToken cancellationToken)
    {
        if (!TryRequireEnabled(out var disabled) || !TryRequireFeature(_options.FourTestsFeedback, out disabled))
        {
            return disabled!;
        }

        var user = await RequireUser(cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var row = await _golf2.GetFourTestsFeedbackAsync(user.Id, cancellationToken);
        return row is null ? NotFound() : Ok(row);
    }

    public sealed record SaveFourTestsFeedbackRequest(int HelpfulnessRating, string OpenAnswer, bool ShareWithPilot);

    [HttpPut("four-tests-feedback")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<FourTestsFeedbackDto>> SaveFourTestsFeedback(
        [FromBody] SaveFourTestsFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryRequireEnabled(out var disabled) || !TryRequireFeature(_options.FourTestsFeedback, out disabled))
        {
            return disabled!;
        }

        var user = await RequireUser(cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var row = await _golf2.SaveFourTestsFeedbackAsync(
            user.Id,
            request.HelpfulnessRating,
            request.OpenAnswer ?? "",
            request.ShareWithPilot,
            cancellationToken);
        return Ok(row);
    }

    [HttpGet("tasks")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IReadOnlyList<WestlandTaskDto>>> ListTasks(CancellationToken cancellationToken)
    {
        if (!TryRequireEnabled(out var disabled) || !TryRequireFeature(_options.TaskPicker, out disabled))
        {
            return disabled!;
        }

        return Ok(await _golf2.ListPublishedTasksAsync(cancellationToken));
    }

    [HttpGet("task-choices")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IReadOnlyList<Guid>>> GetTaskChoices(CancellationToken cancellationToken)
    {
        if (!TryRequireEnabled(out var disabled) || !TryRequireFeature(_options.TaskPicker, out disabled))
        {
            return disabled!;
        }

        var user = await RequireUser(cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(await _golf2.GetTaskChoicesAsync(user.Id, cancellationToken));
    }

    public sealed record SetTaskChoicesRequest(IReadOnlyList<Guid> TaskIds);

    [HttpPut("task-choices")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> SetTaskChoices(
        [FromBody] SetTaskChoicesRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryRequireEnabled(out var disabled) || !TryRequireFeature(_options.TaskPicker, out disabled))
        {
            return disabled!;
        }

        var user = await RequireUser(cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        await _golf2.SetTaskChoicesAsync(user.Id, request.TaskIds ?? [], cancellationToken);
        return NoContent();
    }

    private bool TryRequireEnabled(out ActionResult? result)
    {
        if (_options.Enabled)
        {
            result = null;
            return true;
        }

        result = NotFound(new { code = "golf2_westland_disabled" });
        return false;
    }

    private static bool TryRequireFeature(bool enabled, out ActionResult? result)
    {
        if (enabled)
        {
            result = null;
            return true;
        }

        result = new NotFoundObjectResult(new { code = "golf2_westland_feature_disabled" });
        return false;
    }

    private async Task<Core.Entities.User?> RequireUser(CancellationToken cancellationToken)
        => await _users.FindByPrincipalAsync(User, cancellationToken);
}
