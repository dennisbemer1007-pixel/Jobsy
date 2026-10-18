namespace Jobsy.Tests;

public class ChatAssistantRestyleGuardTests
{
    [Fact]
    public void Candidate_and_business_skins_use_brand_assets_and_context_flag()
    {
        var root = FindRepoRoot();
        var coach = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/LobsyCoach.razor"));
        var chat = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/LobsyAssistantChat.razor"));
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        var gate = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Features/ChatContextGreetingGate.cs"));
        var render = File.ReadAllText(Path.Combine(root, "render.yaml"));

        Assert.Contains("Vraag het Lobsy", coach, StringComparison.Ordinal);
        Assert.Contains("lobsy-assistant--skin-warm", chat, StringComparison.Ordinal);
        Assert.Contains("lobsy-assistant-launcher--business", chat, StringComparison.Ordinal);
        Assert.DoesNotContain("UseMascot", chat, StringComparison.Ordinal);
        Assert.Contains("ContextGreetingGate", chat, StringComparison.Ordinal);
        Assert.Contains("CandidateChatContextBuilder", chat, StringComparison.Ordinal);
        Assert.Contains("ChatContextGreetingEnabled", gate, StringComparison.Ordinal);
        Assert.Contains("ChatContextGreetingEnabled", render, StringComparison.Ordinal);
        Assert.Contains("lobsy-assistant__chip", css, StringComparison.Ordinal);
        Assert.Contains("Assistant.HumanLink", chat, StringComparison.Ordinal);

        var admin = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Layout/AdminLayout.razor"));
        Assert.Contains("UiVariant=\"AssistantChatUiVariant.Business\"", admin, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
