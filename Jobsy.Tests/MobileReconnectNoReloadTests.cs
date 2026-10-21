namespace Jobsy.Tests;

/// <summary>
/// Mobile tab resume must reconnect in-place: custom handler, no framework reload, session poll defers logout.
/// </summary>
public class MobileReconnectNoReloadTests
{
    [Fact]
    public void Blazor_boot_uses_custom_reconnection_handler_and_never_auto_reloads()
    {
        var boot = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "blazor-boot.js"));
        var extras = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "app-extras.js"));

        Assert.Contains("reconnectionHandler: createLobsyReconnectionHandler()", boot, StringComparison.Ordinal);
        Assert.Contains("Never full-page reload", boot, StringComparison.Ordinal);
        Assert.DoesNotContain("location.reload()", boot, StringComparison.Ordinal);
        Assert.Contains("window.lobsyIsReconnectActive", boot, StringComparison.Ordinal);
        Assert.Contains("blocked unprompted navigation refresh", boot, StringComparison.Ordinal);

        Assert.Contains("reconnectUiActive", extras, StringComparison.Ordinal);
        Assert.Contains("confirmSessionExpiredThenLogout", extras, StringComparison.Ordinal);
    }

    [Fact]
    public void Reconnect_css_keeps_shell_visible_during_circuit_loss()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "reconnect.css"));
        Assert.Contains("html[data-lobsy-reconnect-active] .app-shell", css, StringComparison.Ordinal);
        Assert.Contains("transition: opacity 0.2s ease", css, StringComparison.Ordinal);
        Assert.DoesNotContain("1.5s", css, StringComparison.Ordinal);
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
