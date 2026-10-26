namespace Jobsy.Tests;

public sealed class AssistantClientEndpointsGuardTests
{
    [Fact]
    public void Empty_chat_returns_static_greeting_without_upstream_assistant_call()
    {
        var src = File.ReadAllText(Path.Combine(TestRepo.FindRoot(), "Jobsy.Web", "Hosting", "AssistantClientEndpoints.cs"));
        var emptyIdx = src.IndexOf("if (messages.Count == 0)", StringComparison.Ordinal);
        var sendIdx = src.IndexOf("SendAssistantChatAsync", StringComparison.Ordinal);
        Assert.True(emptyIdx >= 0);
        Assert.True(sendIdx > emptyIdx);
        var between = src[emptyIdx..sendIdx];
        Assert.DoesNotContain("SendAssistantChatAsync", between, StringComparison.Ordinal);
        Assert.Contains("Reply = culture[\"Assistant.Greeting\"]", between, StringComparison.Ordinal);
    }

    [Fact]
    public void Ssr_coach_warm_boot_fetches_page_context_without_upstream_chat_on_empty()
    {
        var root = TestRepo.FindRoot();
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "lobsy-coach-ssr.js"));
        Assert.Contains("if (!state.started) startChat()", js, StringComparison.Ordinal);
        Assert.Contains("warm: true", js, StringComparison.Ordinal);
        Assert.Contains("pageContextLine", js, StringComparison.Ordinal);
        Assert.Contains("renderChips", js, StringComparison.Ordinal);
        Assert.Contains("renderActionLinks", js, StringComparison.Ordinal);
    }

    [Fact]
    public void Non_empty_chat_forwards_page_context_to_upstream_assistant()
    {
        var src = File.ReadAllText(Path.Combine(TestRepo.FindRoot(), "Jobsy.Web", "Hosting", "AssistantClientEndpoints.cs"));
        Assert.Contains("ResolvePageContextForAssistantAsync", src, StringComparison.Ordinal);
        Assert.Contains("SendAssistantChatAsync(", src, StringComparison.Ordinal);
        Assert.Contains("pageContextLine", src, StringComparison.Ordinal);
    }
}
