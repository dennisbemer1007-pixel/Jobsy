namespace Jobsy.Tests;

/// <summary>
/// Guard: banenkaart map must stay flush with bottom-nav while the cookie banner is open.
/// </summary>
public class BanenkaartCookiePaddingTests
{
    [Fact]
    public void App_css_overrides_cookie_padding_for_discovery_on_mobile()
    {
        var root = FindRepoRoot();
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "app.css"));

        const string generalCookieRule =
            "html:not(.cookie-consent-known) .app-shell.has-bottom-nav .app-main";
        const string discoveryOverride =
            "html:not(.cookie-consent-known) .app-shell:has(.jobsy-discovery) .app-main";

        var generalIdx = css.IndexOf(generalCookieRule, StringComparison.Ordinal);
        var overrideIdx = css.IndexOf(discoveryOverride, StringComparison.Ordinal);
        Assert.True(generalIdx >= 0, "general cookie padding rule missing");
        Assert.True(overrideIdx >= 0, "discovery cookie padding override missing");
        Assert.True(overrideIdx > generalIdx, "discovery override must appear AFTER the general cookie padding rule");

        var overrideSlice = css.Substring(overrideIdx, Math.Min(280, css.Length - overrideIdx));
        Assert.Contains("padding-bottom: calc(4.75rem + env(safe-area-inset-bottom, 0px));", overrideSlice, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 1024px)", css.Substring(Math.Max(0, overrideIdx - 80), 80), StringComparison.Ordinal);

        // General cookie rule for other pages must remain.
        var generalSlice = css.Substring(generalIdx, Math.Min(260, css.Length - generalIdx));
        Assert.Contains("var(--bottom-nav-clearance) + 5.5rem", generalSlice, StringComparison.Ordinal);

        var min = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "app.min.css"));
        Assert.Contains(
            "html:not(.cookie-consent-known) .app-shell:has(.jobsy-discovery) .app-main",
            min,
            StringComparison.Ordinal);
        Assert.Contains("4.75rem", min, StringComparison.Ordinal);
    }

    [Fact]
    public void Cookie_consent_banner_only_hides_when_set_succeeds()
    {
        var banner = File.ReadAllText(
            Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "CookieConsentBanner.razor"));
        var saveIdx = banner.IndexOf("private async Task SaveAsync", StringComparison.Ordinal);
        Assert.True(saveIdx >= 0);
        var saveFn = banner.Substring(saveIdx, Math.Min(700, banner.Length - saveIdx));
        Assert.Contains("InvokeAsync<bool>", saveFn, StringComparison.Ordinal);
        Assert.Contains("jobsyCookieConsent.set", saveFn, StringComparison.Ordinal);
        Assert.Contains("if (!saved)", saveFn, StringComparison.Ordinal);
        Assert.DoesNotContain("InvokeVoidAsync", saveFn, StringComparison.Ordinal);
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
