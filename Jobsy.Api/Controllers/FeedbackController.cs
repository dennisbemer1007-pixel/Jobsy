using System.Text;
using System.Text.Json;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/feedback")]
public sealed class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedback;
    private readonly IUserLookupService _users;
    private readonly IOptions<CursorCloudOptions> _cursorOptions;
    private readonly IHostEnvironment _environment;

    public FeedbackController(
        IFeedbackService feedback,
        IUserLookupService users,
        IOptions<CursorCloudOptions> cursorOptions,
        IHostEnvironment environment)
    {
        _feedback = feedback;
        _users = users;
        _cursorOptions = cursorOptions;
        _environment = environment;
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("feedback-write")]
    [RequestSizeLimit(4_000_000)]
    public async Task<ActionResult<FeedbackListItemDto>> Submit(
        [FromBody] SubmitFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { message = "Omschrijving is verplicht." });
        }

        var user = User.Identity?.IsAuthenticated == true
            ? await _users.FindByPrincipalAsync(User, cancellationToken)
            : null;

        try
        {
            var row = await _feedback.SubmitAsync(
                new FeedbackSubmitRequest(
                    request.Type,
                    request.Description,
                    request.PageUrl,
                    request.BrowserInfo,
                    request.DeviceInfo,
                    request.ScreenshotDataUrl,
                    user?.Id,
                    user?.Role.ToString(),
                    DisplayName(user)),
                cancellationToken);
            return Ok(ToListItem(row));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<IEnumerable<FeedbackListItemDto>>> List(
        [FromQuery] FeedbackType? type,
        [FromQuery] FeedbackStatus? status,
        [FromQuery] int take = 200,
        CancellationToken cancellationToken = default)
    {
        var rows = await _feedback.ListAsync(new FeedbackListQuery(type, status, take), cancellationToken);
        return Ok(rows.Select(ToListItem));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<FeedbackDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var row = await _feedback.GetAsync(id, cancellationToken);
        return row is null ? NotFound() : Ok(ToDetail(row));
    }

    [HttpGet("{id:guid}/screenshot")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<IActionResult> Screenshot(Guid id, CancellationToken cancellationToken)
    {
        var row = await _feedback.GetAsync(id, cancellationToken);
        if (row?.ScreenshotBytes is not { Length: > 0 })
        {
            return NotFound();
        }

        return File(row.ScreenshotBytes, row.ScreenshotContentType ?? "image/png");
    }

    [HttpPost("{id:guid}/prompt")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    [EnableRateLimiting("ai")]
    public async Task<ActionResult<FeedbackPromptDto>> SavePrompt(
        Guid id,
        [FromBody] FeedbackPromptRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var row = await _feedback.SavePromptAsync(id, request?.Prompt, cancellationToken);
            return Ok(new FeedbackPromptDto(row.Id, row.GeneratedPrompt ?? "", row.BranchName ?? ""));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/automate")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    [EnableRateLimiting("ai")]
    public async Task<ActionResult<FeedbackAutomateResultDto>> Automate(
        Guid id,
        [FromBody] FeedbackAutomateRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _feedback.LaunchAutomationAsync(id, request?.Prompt ?? "", cancellationToken);
            return Ok(new FeedbackAutomateResultDto(ToDetail(result.Feedback), result.Launched, result.Message));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<FeedbackDetailDto>> UpdateStatus(
        Guid id,
        [FromBody] FeedbackStatusRequest request,
        CancellationToken cancellationToken)
    {
        var row = await _feedback.UpdateStatusAsync(id, request.Status, cancellationToken);
        return row is null ? NotFound() : Ok(ToDetail(row));
    }

    [HttpPost("{id:guid}/pull-request")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<FeedbackDetailDto>> AttachPullRequest(
        Guid id,
        [FromBody] FeedbackPullRequestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var row = await _feedback.AttachPullRequestAsync(id, request.PullRequestUrl, cancellationToken);
            return row is null ? NotFound() : Ok(ToDetail(row));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/refresh")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<FeedbackDetailDto>> Refresh(Guid id, CancellationToken cancellationToken)
    {
        var row = await _feedback.RefreshAutomationAsync(id, cancellationToken);
        return row is null ? NotFound() : Ok(ToDetail(row));
    }

    [HttpPost("cursor-webhook")]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> CursorWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var raw = await reader.ReadToEndAsync(cancellationToken);
        var secret = _cursorOptions.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            if (!_environment.IsDevelopment())
            {
                return Unauthorized();
            }
        }
        else
        {
            var signature = Request.Headers["X-Webhook-Signature"].FirstOrDefault();
            if (!FeedbackWebhookSignatures.TryVerify(secret, Encoding.UTF8.GetBytes(raw), signature))
            {
                return Unauthorized();
            }
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return Ok();
        }

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        var agentId = ReadString(root, "id");
        var status = ReadString(root, "status");
        string? prUrl = null;
        string? branch = null;
        if (root.TryGetProperty("target", out var target) && target.ValueKind == JsonValueKind.Object)
        {
            prUrl = ReadString(target, "prUrl") ?? ReadString(target, "prURL");
            branch = ReadString(target, "branchName");
        }

        if (!string.IsNullOrWhiteSpace(agentId))
        {
            await _feedback.ApplyCursorWebhookAsync(
                agentId,
                status ?? "",
                prUrl,
                branch,
                ReadString(root, "summary"),
                cancellationToken);
        }

        return Ok();
    }

    private static string? DisplayName(Jobsy.Core.Entities.User? user)
    {
        if (user is null)
        {
            return "Gast";
        }

        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            return user.FirstName.Trim();
        }

        var name = user.FullName?.Trim();
        return string.IsNullOrWhiteSpace(name) ? "Gebruiker" : name;
    }

    private static FeedbackListItemDto ToListItem(PlatformFeedback row)
        => new(
            row.Id,
            row.CreatedAtUtc,
            row.Type.ToString(),
            row.Status.ToString(),
            row.UserRole,
            row.UserDisplayName,
            row.PageUrl,
            row.PullRequestUrl,
            row.ScreenshotBytes is { Length: > 0 } || row.ScreenshotContentType is not null,
            row.AutomationStatus == FeedbackAutomationStatus.None ? null : row.AutomationStatus.ToString(),
            row.BranchName);

    private static FeedbackDetailDto ToDetail(PlatformFeedback row)
        => new(
            row.Id,
            row.CreatedAtUtc,
            row.Type.ToString(),
            row.Status.ToString(),
            row.Description,
            row.UserRole,
            row.UserDisplayName,
            row.PageUrl,
            row.BrowserInfo,
            row.DeviceInfo,
            row.GeneratedPrompt,
            row.PullRequestUrl,
            row.CursorAgentId,
            row.BranchName,
            row.AutomationStatus == FeedbackAutomationStatus.None ? null : row.AutomationStatus.ToString(),
            row.AutomationError,
            row.ScreenshotBytes is { Length: > 0 });

    private static string? ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
}
