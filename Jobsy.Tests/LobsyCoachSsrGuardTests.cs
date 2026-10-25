namespace Jobsy.Tests;

public sealed class LobsyCoachSsrGuardTests
{
    [Fact]
    public void NoBlazor_candidate_layout_renders_ssr_coach_and_client_endpoint()
    {
        var root = FindRepoRoot();
        var layout = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Layout/MainLayout.razor"));
        Assert.Contains("LobsyCoachSsr", layout, StringComparison.Ordinal);
        Assert.Contains("_ssrCoachAvailable", layout, StringComparison.Ordinal);

        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/App.razor"));
        Assert.Contains("lobsy-coach-ssr.js", app, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Program.cs"));
        Assert.Contains("MapAssistantClientEndpoints", program, StringComparison.Ordinal);

        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/lobsy-coach-ssr.js"));
        Assert.Contains("/api/assistant-client/chat", js, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
