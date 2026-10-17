namespace Jobsy.Tests;

/// <summary>
/// Guards static SSR auth shells: no Blazor circuit, external providers gated by config.
/// </summary>
public class LoginNoBlazorRuntimeTests
{
    [Theory]
    [InlineData("Jobsy.Web/Components/Pages/Login.razor")]
    [InlineData("Jobsy.Web/Components/Pages/Public/AccountMaken.razor")]
    [InlineData("Jobsy.Web/Components/Pages/Public/AccountMakenCode.razor")]
    public void Static_auth_pages_skip_blazor_runtime(string relativePath)
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), relativePath));
        Assert.Contains("[NoBlazorRuntime]", source, StringComparison.Ordinal);
        Assert.Contains("[ExcludeFromInteractiveRouting]", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Login_renders_microsoft_and_divider_only_when_entra_configured()
    {
        var login = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Login.razor"));
        Assert.Contains("_entraEnabled = await ExternalAuth.IsEntraConfiguredAsync()", login, StringComparison.Ordinal);
        Assert.Contains("var showProviders = _entraEnabled || _googleEnabled", login, StringComparison.Ordinal);
        Assert.Contains("@Culture[\"Login.Microsoft\"]", login, StringComparison.Ordinal);
        Assert.Contains("Login.OrEmail", login, StringComparison.Ordinal);
        Assert.Contains("@if (showProviders)", login, StringComparison.Ordinal);
        Assert.Contains("@if (_entraEnabled)", login, StringComparison.Ordinal);
    }

    [Fact]
    public void NoBlazorRuntime_shells_load_compact_cookie_consent_css()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/App.razor"));
        Assert.Contains("!LoadBlazorRuntime", app, StringComparison.Ordinal);
        Assert.Contains("css/features/cookie-consent.css", app, StringComparison.Ordinal);
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
