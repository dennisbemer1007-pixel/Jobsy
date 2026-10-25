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
    public void Ssr_coach_opens_with_local_greeting_without_auto_warm_post()
    {
        var root = TestRepo.FindRoot();
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "lobsy-coach-ssr.js"));
        var razor = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "LobsyCoachSsr.razor"));
        Assert.Contains("showLocalGreeting", js, StringComparison.Ordinal);
        Assert.Contains("data-greeting", razor, StringComparison.Ordinal);
        Assert.DoesNotContain("if (!state.started) startChat()", js, StringComparison.Ordinal);
    }
}
