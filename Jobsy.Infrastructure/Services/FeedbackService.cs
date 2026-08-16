using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class FeedbackService : IFeedbackService
{
    private readonly JobsyDbContext _db;
    private readonly ICursorCloudAgentClient _cursor;
    private readonly IOptions<CursorCloudOptions> _cursorOptions;
    private readonly ILogger<FeedbackService> _logger;

    public FeedbackService(
        JobsyDbContext db,
        ICursorCloudAgentClient cursor,
        IOptions<CursorCloudOptions> cursorOptions,
        ILogger<FeedbackService> logger)
    {
        _db = db;
        _cursor = cursor;
        _cursorOptions = cursorOptions;
        _logger = logger;
    }

    public async Task<PlatformFeedback> SubmitAsync(
        FeedbackSubmitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var description = FeedbackScreenshotCodec.Truncate(request.Description, FeedbackScreenshotCodec.MaxDescriptionLength);
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new InvalidOperationException("Omschrijving is verplicht.");
        }

        var pageUrl = FeedbackScreenshotCodec.Truncate(request.PageUrl, FeedbackScreenshotCodec.MaxPageUrlLength);
        if (string.IsNullOrWhiteSpace(pageUrl))
        {
            throw new InvalidOperationException("Pagina-URL is verplicht.");
        }

        if (!FeedbackScreenshotCodec.TryDecodeDataUrl(
                request.ScreenshotDataUrl,
                out var screenshot,
                out var contentType,
                out var screenshotError))
        {
            throw new InvalidOperationException(screenshotError ?? "Ongeldige screenshot.");
        }

        var row = new PlatformFeedback
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            Type = request.Type,
            Status = FeedbackStatus.New,
            Description = description,
            PageUrl = pageUrl,
            UserId = request.UserId,
            UserRole = EmptyToNull(FeedbackScreenshotCodec.Truncate(request.UserRole, 64)),
            UserDisplayName = string.IsNullOrWhiteSpace(request.UserDisplayName)
                ? (request.UserId is null ? "Gast" : "Gebruiker")
                : FeedbackScreenshotCodec.Truncate(request.UserDisplayName, 128),
            BrowserInfo = EmptyToNull(FeedbackScreenshotCodec.Truncate(request.BrowserInfo, FeedbackScreenshotCodec.MaxBrowserInfoLength)),
            DeviceInfo = EmptyToNull(FeedbackScreenshotCodec.Truncate(request.DeviceInfo, FeedbackScreenshotCodec.MaxDeviceInfoLength)),
            ScreenshotBytes = screenshot.Length == 0 ? null : screenshot,
            ScreenshotContentType = screenshot.Length == 0 ? null : contentType
        };

        row.BranchName = FeedbackPromptFormatter.BranchNameFor(row.Id);

        _db.PlatformFeedbacks.Add(row);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Feedback submitted {FeedbackId} type={Type} role={Role} page={Page}",
            row.Id,
            row.Type,
            row.UserRole ?? "guest",
            TruncateForLog(row.PageUrl));

        return row;
    }

    public async Task<IReadOnlyList<PlatformFeedback>> ListAsync(
        FeedbackListQuery query,
        CancellationToken cancellationToken = default)
    {
        var take = query.Take <= 0 ? 200 : Math.Min(query.Take, 500);
        var rows = _db.PlatformFeedbacks.AsNoTracking().AsQueryable();
        if (query.Type is FeedbackType type)
        {
            rows = rows.Where(f => f.Type == type);
        }

        if (query.Status is FeedbackStatus status)
        {
            rows = rows.Where(f => f.Status == status);
        }

        return await rows
            .OrderByDescending(f => f.CreatedAtUtc)
            .Take(take)
            .Select(f => new PlatformFeedback
            {
                Id = f.Id,
                CreatedAtUtc = f.CreatedAtUtc,
                Type = f.Type,
                Status = f.Status,
                Description = f.Description,
                PageUrl = f.PageUrl,
                UserRole = f.UserRole,
                UserId = f.UserId,
                UserDisplayName = f.UserDisplayName,
                BrowserInfo = f.BrowserInfo,
                DeviceInfo = f.DeviceInfo,
                GeneratedPrompt = f.GeneratedPrompt,
                PromptEditedAtUtc = f.PromptEditedAtUtc,
                CursorAgentId = f.CursorAgentId,
                BranchName = f.BranchName,
                PullRequestUrl = f.PullRequestUrl,
                AutomationStatus = f.AutomationStatus,
                AutomationError = f.AutomationError,
                AutomationLaunchedAtUtc = f.AutomationLaunchedAtUtc,
                AutomationFinishedAtUtc = f.AutomationFinishedAtUtc,
                ScreenshotContentType = f.ScreenshotBytes != null ? f.ScreenshotContentType : null
            })
            .ToListAsync(cancellationToken);
    }

    public Task<PlatformFeedback?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.PlatformFeedbacks.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public string BuildPrompt(PlatformFeedback feedback)
        => FeedbackPromptFormatter.Build(feedback, TargetRef());

    public async Task<PlatformFeedback> SavePromptAsync(
        Guid id,
        string? editedPrompt,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.PlatformFeedbacks.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
                  ?? throw new KeyNotFoundException("Feedback niet gevonden.");

        var prompt = string.IsNullOrWhiteSpace(editedPrompt)
            ? FeedbackPromptFormatter.Build(row, TargetRef())
            : editedPrompt.Trim();

        row.GeneratedPrompt = prompt;
        row.PromptEditedAtUtc = DateTime.UtcNow;
        if (row.AutomationStatus == FeedbackAutomationStatus.None)
        {
            row.AutomationStatus = FeedbackAutomationStatus.PromptReady;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }

    public async Task<FeedbackAutomationResult> LaunchAutomationAsync(
        Guid id,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.PlatformFeedbacks.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
                  ?? throw new KeyNotFoundException("Feedback niet gevonden.");

        var text = string.IsNullOrWhiteSpace(prompt)
            ? FeedbackPromptFormatter.Build(row, TargetRef())
            : prompt.Trim();

        row.GeneratedPrompt = text;
        row.PromptEditedAtUtc = DateTime.UtcNow;
        row.BranchName ??= FeedbackPromptFormatter.BranchNameFor(row.Id);
        row.Status = FeedbackStatus.InProgress;
        row.AutomationError = null;

        if (!_cursor.IsConfigured)
        {
            row.AutomationStatus = FeedbackAutomationStatus.PromptReady;
            await _db.SaveChangesAsync(cancellationToken);
            return new FeedbackAutomationResult(
                row,
                false,
                "Prompt opgeslagen. Configureer CursorCloud:ApiKey en Repository om automatisch een PR te openen, of plak de prompt in Cursor.");
        }

        var images = new List<CursorAgentImage>();
        if (row.ScreenshotBytes is { Length: > 0 })
        {
            images.Add(new CursorAgentImage(FeedbackScreenshotCodec.ToBase64(row.ScreenshotBytes)));
        }

        try
        {
            var launched = await _cursor.LaunchAsync(
                new CursorAgentLaunchRequest(text, row.BranchName, images),
                cancellationToken);
            row.CursorAgentId = launched.AgentId;
            row.AutomationStatus = FeedbackAutomationStatus.Launched;
            row.AutomationLaunchedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Feedback {FeedbackId} launched Cursor agent {AgentId}",
                row.Id,
                launched.AgentId);

            return new FeedbackAutomationResult(
                row,
                true,
                "Cursor-taak gestart. De PR-link verschijnt hier zodra de agent klaar is.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            row.AutomationStatus = FeedbackAutomationStatus.Error;
            row.AutomationError = TruncateForLog(ex.Message);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogWarning(ex, "Feedback {FeedbackId} Cursor launch failed", row.Id);
            return new FeedbackAutomationResult(
                row,
                false,
                "Cursor-taak starten mislukt. De prompt is bewaard; je kunt hem handmatig in Cursor plakken.");
        }
    }

    public async Task<PlatformFeedback?> UpdateStatusAsync(
        Guid id,
        FeedbackStatus status,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.PlatformFeedbacks.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (row is null)
        {
            return null;
        }

        row.Status = status;
        if (status == FeedbackStatus.Resolved && row.AutomationStatus == FeedbackAutomationStatus.Finished)
        {
            row.AutomationFinishedAtUtc ??= DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }

    public async Task<PlatformFeedback?> AttachPullRequestAsync(
        Guid id,
        string pullRequestUrl,
        CancellationToken cancellationToken = default)
    {
        if (!IsAllowedPullRequestUrl(pullRequestUrl, out var normalized))
        {
            throw new InvalidOperationException("Ongeldige PR-URL (alleen https GitHub/GitLab/Cursor).");
        }

        var row = await _db.PlatformFeedbacks.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (row is null)
        {
            return null;
        }

        ApplyPullRequest(row, normalized, branchName: null, finished: true);
        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }

    public async Task<PlatformFeedback?> RefreshAutomationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.PlatformFeedbacks.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (row is null)
        {
            return null;
        }

        await TryRefreshRowAsync(row, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }

    public async Task ApplyCursorWebhookAsync(
        string agentId,
        string status,
        string? pullRequestUrl,
        string? branchName,
        string? summary,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return;
        }

        var row = await _db.PlatformFeedbacks
            .FirstOrDefaultAsync(f => f.CursorAgentId == agentId, cancellationToken);
        if (row is null)
        {
            _logger.LogInformation("Cursor webhook for unknown agent {AgentId}", agentId);
            return;
        }

        ApplyAgentStatus(row, status, pullRequestUrl, branchName, summary);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> RefreshPendingAutomationsAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _db.PlatformFeedbacks
            .Where(f => f.CursorAgentId != null
                        && f.PullRequestUrl == null
                        && (f.AutomationStatus == FeedbackAutomationStatus.Launched
                            || f.AutomationStatus == FeedbackAutomationStatus.Error))
            .OrderBy(f => f.AutomationLaunchedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        var updated = 0;
        foreach (var row in pending)
        {
            if (await TryRefreshRowAsync(row, cancellationToken))
            {
                updated++;
            }
        }

        if (updated > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return updated;
    }

    private async Task<bool> TryRefreshRowAsync(PlatformFeedback row, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(row.CursorAgentId) || !_cursor.IsConfigured)
        {
            return false;
        }

        var status = await _cursor.GetAsync(row.CursorAgentId, cancellationToken);
        if (status is null)
        {
            return false;
        }

        ApplyAgentStatus(row, status.Status, status.PullRequestUrl, status.BranchName, status.Summary);
        return true;
    }

    private static void ApplyAgentStatus(
        PlatformFeedback row,
        string? status,
        string? pullRequestUrl,
        string? branchName,
        string? summary)
    {
        if (!string.IsNullOrWhiteSpace(branchName))
        {
            row.BranchName = branchName.Trim();
        }

        var normalized = status?.Trim().ToUpperInvariant();
        if (normalized is "FINISHED" or "COMPLETED")
        {
            if (IsAllowedPullRequestUrl(pullRequestUrl, out var url))
            {
                ApplyPullRequest(row, url, branchName, finished: true);
            }
            else
            {
                row.AutomationStatus = FeedbackAutomationStatus.Finished;
                row.AutomationFinishedAtUtc = DateTime.UtcNow;
                row.Status = FeedbackStatus.InProgress;
            }
        }
        else if (normalized is "ERROR" or "FAILED")
        {
            row.AutomationStatus = FeedbackAutomationStatus.Error;
            row.AutomationError = string.IsNullOrWhiteSpace(summary)
                ? "Cursor-taak mislukt."
                : TruncateForLog(summary);
        }

        if (IsAllowedPullRequestUrl(pullRequestUrl, out var pr) && row.PullRequestUrl is null)
        {
            ApplyPullRequest(row, pr, branchName, finished: normalized is "FINISHED" or "COMPLETED");
        }
    }

    private static void ApplyPullRequest(PlatformFeedback row, string url, string? branchName, bool finished)
    {
        row.PullRequestUrl = url;
        if (!string.IsNullOrWhiteSpace(branchName))
        {
            row.BranchName = branchName.Trim();
        }

        if (finished)
        {
            row.AutomationStatus = FeedbackAutomationStatus.Finished;
            row.AutomationFinishedAtUtc = DateTime.UtcNow;
            if (row.Status == FeedbackStatus.New)
            {
                row.Status = FeedbackStatus.InProgress;
            }
        }
    }

    internal static bool IsAllowedPullRequestUrl(string? value, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(value)
            || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var host = uri.Host;
        if (!host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
            && !host.Equals("gitlab.com", StringComparison.OrdinalIgnoreCase)
            && !host.EndsWith(".github.com", StringComparison.OrdinalIgnoreCase)
            && !host.EndsWith(".gitlab.com", StringComparison.OrdinalIgnoreCase)
            && !host.Equals("cursor.com", StringComparison.OrdinalIgnoreCase)
            && !host.EndsWith(".cursor.com", StringComparison.OrdinalIgnoreCase)
            && !host.Equals("cursor.sh", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        normalized = uri.GetLeftPart(UriPartial.Query);
        return true;
    }

    private string TargetRef()
        => string.IsNullOrWhiteSpace(_cursorOptions.Value.Ref) ? "main" : _cursorOptions.Value.Ref.Trim();

    private static string TruncateForLog(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 180 ? trimmed : trimmed[..180];
    }

    private static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
