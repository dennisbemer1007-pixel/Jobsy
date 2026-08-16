using System.Net;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class CursorCloudAgentClientTests
{
    [Fact]
    public async Task Launch_posts_v0_agent_with_prompt_screenshot_branch_and_webhook()
    {
        HttpRequestMessage? captured = null;
        string? capturedBody = null;
        var handler = new StubHandler(async request =>
        {
            captured = request;
            capturedBody = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            return JsonResponse(HttpStatusCode.OK, """{"id":"bc_abc123","status":"CREATING","target":{"url":"https://cursor.com/agents?id=bc_abc123"}}""");
        });

        var sut = CreateSut(handler, new CursorCloudOptions
        {
            ApiKey = "test-key",
            Repository = "https://github.com/lobsy/lobsy",
            Ref = "acc",
            WebhookUrl = "https://api.lobsy.nl/api/feedback/cursor-webhook",
            WebhookSecret = "unit-test-webhook-secret-32chars!!"
        });

        var result = await sut.LaunchAsync(new CursorAgentLaunchRequest(
            "Fix de login op /login",
            "fix/feedback-aabbccdd",
            [new CursorAgentImage("iVBORw0KGgoAAAANSUhEUgAAAAEAAAAB")]));

        Assert.Equal("bc_abc123", result.AgentId);
        Assert.Equal("CREATING", result.Status);
        Assert.Equal("https://cursor.com/agents?id=bc_abc123", result.AgentUrl);
        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("/v0/agents", captured.RequestUri!.AbsolutePath);
        Assert.Equal("Basic", captured.Headers.Authorization?.Scheme);
        Assert.False(string.IsNullOrWhiteSpace(capturedBody));

        using var doc = JsonDocument.Parse(capturedBody!);
        var root = doc.RootElement;
        Assert.Equal("Fix de login op /login", root.GetProperty("prompt").GetProperty("text").GetString());
        Assert.Equal("iVBORw0KGgoAAAANSUhEUgAAAAEAAAAB",
            root.GetProperty("prompt").GetProperty("images")[0].GetProperty("data").GetString());
        Assert.Equal("https://github.com/lobsy/lobsy", root.GetProperty("source").GetProperty("repository").GetString());
        Assert.Equal("acc", root.GetProperty("source").GetProperty("ref").GetString());
        Assert.True(root.GetProperty("target").GetProperty("autoCreatePr").GetBoolean());
        Assert.Equal("fix/feedback-aabbccdd", root.GetProperty("target").GetProperty("branchName").GetString());
        Assert.Equal("https://api.lobsy.nl/api/feedback/cursor-webhook",
            root.GetProperty("webhook").GetProperty("url").GetString());
    }

    [Fact]
    public async Task Get_reads_pr_url_from_finished_agent()
    {
        var handler = new StubHandler(_ => Task.FromResult(JsonResponse(
            HttpStatusCode.OK,
            """{"id":"bc_2","status":"FINISHED","target":{"prUrl":"https://github.com/lobsy/lobsy/pull/9","branchName":"fix/feedback-x"},"summary":"done"}""")));

        var sut = CreateSut(handler, new CursorCloudOptions
        {
            ApiKey = "test-key",
            Repository = "https://github.com/lobsy/lobsy"
        });

        var status = await sut.GetAsync("bc_2");
        Assert.NotNull(status);
        Assert.Equal("FINISHED", status!.Status);
        Assert.Equal("https://github.com/lobsy/lobsy/pull/9", status.PullRequestUrl);
        Assert.Equal("fix/feedback-x", status.BranchName);
    }

    [Fact]
    public async Task Launch_throws_when_cursor_returns_error()
    {
        var handler = new StubHandler(_ => Task.FromResult(JsonResponse(HttpStatusCode.Unauthorized, """{"error":"nope"}""")));
        var sut = CreateSut(handler, new CursorCloudOptions
        {
            ApiKey = "bad",
            Repository = "https://github.com/lobsy/lobsy"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.LaunchAsync(new CursorAgentLaunchRequest("x", "fix/feedback-x")));
    }

    [Fact]
    public void IsConfigured_requires_key_and_repository()
    {
        var handler = new StubHandler(_ => Task.FromResult(JsonResponse(HttpStatusCode.OK, "{}")));
        Assert.False(CreateSut(handler, new CursorCloudOptions()).IsConfigured);
        Assert.False(CreateSut(handler, new CursorCloudOptions { ApiKey = "k" }).IsConfigured);
        Assert.True(CreateSut(handler, new CursorCloudOptions
        {
            ApiKey = "k",
            Repository = "https://github.com/lobsy/lobsy"
        }).IsConfigured);
    }

    private static CursorCloudAgentClient CreateSut(HttpMessageHandler handler, CursorCloudOptions options)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.cursor.com/") };
        return new CursorCloudAgentClient(
            new FixedFactory(client),
            Options.Create(options),
            NullLogger<CursorCloudAgentClient>.Instance);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json)
        => new(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class FixedFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => responder(request);
    }
}
