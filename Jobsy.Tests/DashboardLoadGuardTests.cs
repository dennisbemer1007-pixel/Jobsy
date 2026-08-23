using Jobsy.Web.Services;

namespace Jobsy.Tests;

/// <summary>
/// Role dashboards must load once the circuit is interactive. Skipping
/// <c>OnInitializedAsync</c> during prerender without retrying in
/// <c>OnAfterRenderAsync</c> leaves /home on the skeleton forever.
/// </summary>
public class DashboardLoadGuardTests
{
    [Fact]
    public void TryBegin_starts_once_when_interactive()
    {
        var started = false;
        Assert.False(HomeDashboardLoad.TryBegin(ref started, isInteractive: false));
        Assert.False(started);

        Assert.True(HomeDashboardLoad.TryBegin(ref started, isInteractive: true));
        Assert.True(started);
        Assert.False(HomeDashboardLoad.TryBegin(ref started, isInteractive: true));
    }

    [Theory]
    [InlineData("Components/Pages/Admin/AdminHomePanel.razor")]
    [InlineData("Components/Pages/EmployerHomePanel.razor")]
    [InlineData("Components/Pages/Candidate/CandidateHomePanel.razor")]
    [InlineData("Components/Pages/SalesManagerHomePanel.razor")]
    [InlineData("Components/Pages/AmbassadeurHomePanel.razor")]
    public void Role_dashboard_retries_load_after_first_interactive_render(string relativePath)
    {
        var text = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", relativePath));
        Assert.Contains("HomeDashboardLoad.TryBegin", text);
        Assert.Contains("OnAfterRenderAsync(bool firstRender)", text);
        Assert.Contains("TryLoadAsync()", text);
        Assert.Contains("firstRender ? TryLoadAsync()", text);
        Assert.DoesNotContain(
            """
                    if (!RendererInfo.IsInteractive)
                    {
                        return;
                    }
            """,
            text);
    }

    [Fact]
    public void Layout_does_not_block_the_shell_on_branding_http()
    {
        var layout = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "Jobsy.Web", "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("HydrateHostBrandingAsync", layout);
        Assert.Contains("_ = HydrateHostBrandingAsync();", layout);
        Assert.DoesNotContain("await Branding.EnsureInitializedAsync();", layout);
        Assert.DoesNotContain("await RegionHost.EnsureInitializedAsync();", layout);

        var program = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Program.cs"));
        Assert.Contains("PlatformBrandingState.HttpClientName", program);
        Assert.Contains("JobsyPublic", program);
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

        throw new InvalidOperationException("Jobsy.sln not found from test base directory.");
    }
}
