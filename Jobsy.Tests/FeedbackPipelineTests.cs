using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Jobsy.Api.Controllers;
using Jobsy.Api.Models;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class FeedbackPipelineTests
{
    [Fact]
    public void Prompt_includes_metadata_branch_and_page_hints()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var feedback = new PlatformFeedback
        {
            Id = id,
            Type = FeedbackType.Bug,
            Status = FeedbackStatus.New,
            Description = "Kaart laadt niet",
            PageUrl = "https://lobsy.nl/banen?q=1",
            UserRole = "Candidate",
            UserDisplayName = "Anna",
            BrowserInfo = "Mozilla/5.0",
            DeviceInfo = "MacIntel · 1440×900",
            CreatedAtUtc = new DateTime(2026, 8, 16, 10, 0, 0, DateTimeKind.Utc),
            ScreenshotBytes = [1, 2, 3]
        };

        var prompt = FeedbackPromptFormatter.Build(feedback, "acc");
        Assert.Contains("Kaart laadt niet", prompt);
        Assert.Contains("/banen", prompt);
        Assert.Contains("Candidate", prompt);
        Assert.Contains("fix/feedback-aaaaaaaa", prompt);
        Assert.Contains("acc", prompt);
        Assert.Contains("Jobsy.Web/Components/Pages/Banen.razor", prompt);
        Assert.Contains("bijgevoegd", prompt);
    }

    [Fact]
    public void Screenshot_codec_rejects_oversize_and_unknown_types()
    {
        Assert.True(FeedbackScreenshotCodec.TryDecodeDataUrl(null, out var empty, out _, out var err));
        Assert.Empty(empty);
        Assert.Null(err);

        var tooBig = "data:image/png;base64," + Convert.ToBase64String(new byte[FeedbackScreenshotCodec.MaxDecodedBytes + 10]);
        Assert.False(FeedbackScreenshotCodec.TryDecodeDataUrl(tooBig, out _, out _, out var sizeError));
        Assert.Contains("groot", sizeError);

        Assert.False(FeedbackScreenshotCodec.TryDecodeDataUrl("data:text/plain;base64,QQ==", out _, out _, out var typeError));
        Assert.Contains("PNG", typeError);
    }

    [Fact]
    public void Webhook_signature_accepts_sha256_header()
    {
        const string secret = "unit-test-webhook-secret-32chars!!";
        var body = Encoding.UTF8.GetBytes("""{"id":"bc_1","status":"FINISHED"}""");
        var header = FeedbackWebhookSignatures.ComputeSha256Header(secret, body);
        Assert.True(FeedbackWebhookSignatures.TryVerify(secret, body, header));
        Assert.False(FeedbackWebhookSignatures.TryVerify(secret, body, "sha256=deadbeef"));
        Assert.False(FeedbackWebhookSignatures.TryVerify(secret, body, null));
    }

    [Fact]
    public async Task Submit_stores_screenshot_without_email_and_list_omits_bytes()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "kandidaat@jobsy.local",
            FullName = "Kees Kandidaat",
            FirstName = "Kees",
            Role = UserRole.Candidate,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var png = Convert.ToBase64String(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        var sut = CreateService(db, new FakeCursor { IsConfigured = false });
        var row = await sut.SubmitAsync(new FeedbackSubmitRequest(
            FeedbackType.Error,
            "Knop doet niets",
            "https://lobsy.nl/candidate/profile",
            "Mozilla/5.0",
            "iPhone",
            "data:image/png;base64," + png,
            userId,
            "Candidate",
            "Kees"));

        Assert.Equal(FeedbackStatus.New, row.Status);
        Assert.Equal("Candidate", row.UserRole);
        Assert.Equal("Kees", row.UserDisplayName);
        Assert.NotNull(row.ScreenshotBytes);
        Assert.StartsWith("fix/feedback-", row.BranchName);

        var listed = await sut.ListAsync(new FeedbackListQuery());
        var item = Assert.Single(listed);
        Assert.Null(item.ScreenshotBytes);
        Assert.Equal("image/png", item.ScreenshotContentType);
        Assert.DoesNotContain("@jobsy.local", item.UserDisplayName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Automate_without_cursor_keeps_prompt_ready()
    {
        await using var db = CreateDb();
        var sut = CreateService(db, new FakeCursor { IsConfigured = false });
        var row = await sut.SubmitAsync(new FeedbackSubmitRequest(
            FeedbackType.Feature,
            "Filter op afstand",
            "https://lobsy.nl/",
            null,
            null,
            null,
            null,
            null,
            "Gast"));

        var result = await sut.LaunchAutomationAsync(row.Id, "");
        Assert.False(result.Launched);
        Assert.Equal(FeedbackAutomationStatus.PromptReady, result.Feedback.AutomationStatus);
        Assert.Equal(FeedbackStatus.InProgress, result.Feedback.Status);
        Assert.Contains("Filter op afstand", result.Feedback.GeneratedPrompt);
    }

    [Fact]
    public async Task Automate_launches_cursor_and_webhook_stores_pr_url()
    {
        await using var db = CreateDb();
        var cursor = new FakeCursor
        {
            IsConfigured = true,
            LaunchResult = new CursorAgentLaunchResult("bc_feedback_1", "CREATING", "https://cursor.com/agents?id=bc_feedback_1")
        };
        var sut = CreateService(db, cursor);
        var row = await sut.SubmitAsync(new FeedbackSubmitRequest(
            FeedbackType.Bug,
            "Login faalt",
            "https://lobsy.nl/login",
            "Chrome",
            "Windows",
            null,
            null,
            "Admin",
            "Beheer"));

        var launched = await sut.LaunchAutomationAsync(row.Id, "Fix de login.");
        Assert.True(launched.Launched);
        Assert.Equal("bc_feedback_1", launched.Feedback.CursorAgentId);
        Assert.Equal(FeedbackAutomationStatus.Launched, launched.Feedback.AutomationStatus);
        Assert.NotNull(cursor.LastRequest);
        Assert.Contains("Fix de login.", cursor.LastRequest!.Prompt);

        await sut.ApplyCursorWebhookAsync(
            "bc_feedback_1",
            "FINISHED",
            "https://github.com/lobsy/lobsy/pull/42",
            "fix/feedback-login",
            "Fixed login",
            CancellationToken.None);

        var updated = await sut.GetAsync(row.Id);
        Assert.Equal("https://github.com/lobsy/lobsy/pull/42", updated!.PullRequestUrl);
        Assert.Equal(FeedbackAutomationStatus.Finished, updated.AutomationStatus);
        Assert.Equal("fix/feedback-login", updated.BranchName);
    }

    [Fact]
    public async Task Refresh_reads_pr_from_cursor_status()
    {
        await using var db = CreateDb();
        var cursor = new FakeCursor
        {
            IsConfigured = true,
            LaunchResult = new CursorAgentLaunchResult("bc_2", "CREATING", null),
            Status = new CursorAgentStatusResult(
                "bc_2",
                "FINISHED",
                "https://github.com/lobsy/lobsy/pull/99",
                "fix/feedback-bc2",
                "done")
        };
        var sut = CreateService(db, cursor);
        var row = await sut.SubmitAsync(new FeedbackSubmitRequest(
            FeedbackType.Bug, "x", "https://lobsy.nl/admin/users", null, null, null, null, "Admin", "A"));
        await sut.LaunchAutomationAsync(row.Id, "prompt");
        var refreshed = await sut.RefreshAutomationAsync(row.Id);
        Assert.Equal("https://github.com/lobsy/lobsy/pull/99", refreshed!.PullRequestUrl);
    }

    [Fact]
    public async Task Attach_pr_rejects_non_https_hosts()
    {
        await using var db = CreateDb();
        var sut = CreateService(db, new FakeCursor());
        var row = await sut.SubmitAsync(new FeedbackSubmitRequest(
            FeedbackType.Bug, "x", "https://lobsy.nl/", null, null, null, null, null, "Gast"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.AttachPullRequestAsync(row.Id, "http://evil.example/pr/1"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.AttachPullRequestAsync(row.Id, "https://evil.example/pr/1"));
    }

    [Fact]
    public async Task Privacy_anonymize_strips_screenshot_and_user_link()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "forget@jobsy.local",
            FullName = "Forget Me",
            FirstName = "Forget",
            Role = UserRole.Candidate,
            IsActive = true
        });
        db.PlatformFeedbacks.Add(new PlatformFeedback
        {
            Id = Guid.NewGuid(),
            Description = "Knop",
            PageUrl = "/home",
            UserId = userId,
            UserDisplayName = "Forget",
            UserRole = "Candidate",
            ScreenshotBytes = [9, 8, 7],
            ScreenshotContentType = "image/png",
            BrowserInfo = "UA"
        });
        await db.SaveChangesAsync();

        var privacy = new PrivacyDataService(
            db,
            new StubUserLookup(db),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, "forget@jobsy.local"),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ], "test"));

        var export = await privacy.ExportAsync(principal);
        var json = JsonSerializer.Serialize(export);
        Assert.Contains("Knop", json);
        Assert.DoesNotContain("iVBORw", json, StringComparison.Ordinal);

        await privacy.DeleteOrAnonymizeAsync(principal);
        var leftover = await db.PlatformFeedbacks.SingleAsync();
        Assert.Null(leftover.UserId);
        Assert.Equal("Verwijderde gebruiker", leftover.UserDisplayName);
        Assert.Null(leftover.ScreenshotBytes);
        Assert.Null(leftover.BrowserInfo);
    }

    [Fact]
    public async Task Controller_list_dto_has_no_screenshot_payload()
    {
        await using var db = CreateDb();
        var sut = CreateService(db, new FakeCursor { IsConfigured = false });
        var row = await sut.SubmitAsync(new FeedbackSubmitRequest(
            FeedbackType.Bug,
            "x",
            "https://lobsy.nl/",
            null,
            null,
            "data:image/png;base64," + Convert.ToBase64String([1, 2, 3, 4]),
            null,
            null,
            "Gast"));

        var controller = new FeedbackController(
            sut,
            new StubUserLookup(db),
            Options.Create(new CursorCloudOptions()),
            new StubHostEnvironment());
        var listed = await controller.List(null, null);
        var ok = Assert.IsType<OkObjectResult>(listed.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<FeedbackListItemDto>>(ok.Value);
        var dto = Assert.Single(items);
        Assert.True(dto.HasScreenshot);
        var serialized = JsonSerializer.Serialize(dto);
        Assert.DoesNotContain("screenshotDataUrl", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ScreenshotBytes", serialized, StringComparison.OrdinalIgnoreCase);

        var shot = await controller.Screenshot(row.Id, CancellationToken.None);
        Assert.IsType<FileContentResult>(shot);
    }

    [Fact]
    public async Task Webhook_rejects_bad_signature_when_secret_configured()
    {
        await using var db = CreateDb();
        var sut = CreateService(db, new FakeCursor());
        var controller = new FeedbackController(
            sut,
            new StubUserLookup(db),
            Options.Create(new CursorCloudOptions { WebhookSecret = "unit-test-webhook-secret-32chars!!" }),
            new StubHostEnvironment { EnvironmentName = Environments.Production });

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Request.Body = new MemoryStream("{}"u8.ToArray());
        controller.HttpContext.Request.Headers["X-Webhook-Signature"] = "sha256=00";

        var result = await controller.CursorWebhook(CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }

    private static FeedbackService CreateService(JobsyDbContext db, FakeCursor cursor)
        => new(
            db,
            cursor,
            Options.Create(new CursorCloudOptions { Ref = "acc" }),
            NullLogger<FeedbackService>.Instance);

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class FakeCursor : ICursorCloudAgentClient
    {
        public bool IsConfigured { get; set; }
        public CursorAgentLaunchResult LaunchResult { get; set; } =
            new("bc_x", "CREATING", null);
        public CursorAgentStatusResult? Status { get; set; }
        public CursorAgentLaunchRequest? LastRequest { get; set; }

        public Task<CursorAgentLaunchResult> LaunchAsync(
            CursorAgentLaunchRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(LaunchResult);
        }

        public Task<CursorAgentStatusResult?> GetAsync(
            string agentId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Status);
    }

    private sealed class StubUserLookup(JobsyDbContext db) : IUserLookupService
    {
        public Task<User?> FindByPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            return db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);
        }
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
