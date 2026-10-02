using System.Security.Claims;
using Bunit;
using Jobsy.Web.Components.KandidaatBanen;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class KbListModeAndDetailBunitTests : BunitContext
{
    public KbListModeAndDetailBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Discovery_markup_has_view_toggle_list_rows_and_map_fab()
    {
        var root = FindRepoRoot();
        var discovery = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        Assert.Contains("kb-view-toggle", discovery, StringComparison.Ordinal);
        Assert.Contains("role=\"radiogroup\"", discovery, StringComparison.Ordinal);
        Assert.Contains("weergave", discovery, StringComparison.Ordinal);
        Assert.Contains("KbListRow", discovery, StringComparison.Ordinal);
        Assert.Contains("kb-list-rail", discovery, StringComparison.Ordinal);
        Assert.Contains("kb-map-fab", discovery, StringComparison.Ordinal);
        Assert.Contains("jobsy-discovery--list", discovery, StringComparison.Ordinal);
        Assert.Contains("SetDesktopViewAsync", discovery, StringComparison.Ordinal);
    }

    [Fact]
    public void List_row_source_has_travel_fit_and_view_action()
    {
        var root = FindRepoRoot();
        var row = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "KandidaatBanen", "KbListRow.razor"));
        Assert.Contains("kb-list-row", row, StringComparison.Ordinal);
        Assert.Contains("KbTravelTime", row, StringComparison.Ordinal);
        Assert.Contains("Large=\"true\"", row, StringComparison.Ordinal);
        Assert.Contains("KbBadgeRow", row, StringComparison.Ordinal);
        Assert.Contains("Kb.List.View", row, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-list-row\"", row, StringComparison.Ordinal);
    }

    [Fact]
    public void List_rail_css_hides_below_1024()
    {
        var root = FindRepoRoot();
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "features", "kandidaat-banen.css"));
        Assert.Contains("@media (min-width: 900px) and (max-width: 1023px)", css, StringComparison.Ordinal);
        Assert.Contains(".jobsy-discovery.jobsy-discovery--list .kb-list-rail", css, StringComparison.Ordinal);
        Assert.Contains("display: none", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Detail_markup_has_one_primary_pattern_travel_card_and_sticky()
    {
        var root = FindRepoRoot();
        var detail = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "VacancyDetail.razor"));
        Assert.Contains("kb-travel-card", detail, StringComparison.Ordinal);
        Assert.Contains("kb-fit-panel", detail, StringComparison.Ordinal);
        Assert.Contains("btn--primary", detail, StringComparison.Ordinal);
        Assert.Contains("kb-detail__sticky-apply", detail, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-travel-modes\"", detail, StringComparison.Ordinal);
        Assert.Contains("HideRouteAndStreetView", detail, StringComparison.Ordinal);
        Assert.Contains("Kb.Travel.Approx", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("kb-employer-branche", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("kb-employer-values", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("kb-employer-engagement", detail, StringComparison.Ordinal);

        // Primary CTA is gated by _detailWide so only one `.btn--primary` mounts at a time.
        Assert.Contains("if (_detailWide)", detail, StringComparison.Ordinal);
        Assert.Contains("if (!_detailWide)", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Travel_time_large_variant_renders_minutes()
    {
        var cut = Render<KbTravelTime>(p => p
            .Add(x => x.Minutes, 8)
            .Add(x => x.Transport, "Fiets")
            .Add(x => x.Large, true)
            .Add(x => x.Approx, true));

        Assert.Contains("kb-travel--large", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("kb-travel__minutes", cut.Markup, StringComparison.Ordinal);
        Assert.Contains(">8<", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("kb-travel__approx", cut.Markup, StringComparison.Ordinal);
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

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}

[Collection("PlaywrightSmoke")]
public class KandidaatBanenListDetailPlaywrightTests
{
    [Fact]
    public async Task Desktop_list_mode_full_width_no_map_and_query_survives_reload()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/?weergave=lijst", new() { WaitUntil = Microsoft.Playwright.WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForSelectorAsync("[data-testid=kb-view-toggle], .kb-list-rows, .jobsy-discovery", new() { Timeout = 60_000 });

        var listMode = page.Locator(".jobsy-discovery--list");
        if (await listMode.CountAsync() > 0)
        {
            await Microsoft.Playwright.Assertions.Expect(listMode).ToBeVisibleAsync();
            var mapCanvas = page.Locator("#job-map canvas");
            Assert.Equal(0, await mapCanvas.CountAsync());
        }

        await page.ReloadAsync(new() { WaitUntil = Microsoft.Playwright.WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        Assert.Contains("weergave=lijst", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Mobile_kaart_fab_does_not_overlap_bottom_nav()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = Microsoft.Playwright.WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        var toggle = page.Locator("[data-testid=kb-mobile-view-toggle], .kb-chip--toggle").First;
        if (await toggle.CountAsync() > 0 && await toggle.IsVisibleAsync())
        {
            await toggle.ClickAsync();
        }

        var fab = page.Locator("[data-testid=kb-map-fab]");
        if (await fab.CountAsync() == 0)
        {
            return;
        }

        await Microsoft.Playwright.Assertions.Expect(fab).ToBeVisibleAsync();
        var overlap = await page.EvaluateAsync<bool>("""
            () => {
              const fab = document.querySelector('[data-testid=kb-map-fab]');
              const nav = document.querySelector('.bottom-nav');
              if (!fab || !nav) return false;
              const a = fab.getBoundingClientRect();
              const b = nav.getBoundingClientRect();
              return !(a.bottom <= b.top || a.top >= b.bottom || a.right <= b.left || a.left >= b.right);
            }
            """);
        Assert.False(overlap);
    }

    private static string? BaseUrl()
    {
        var raw = Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL");
        return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim().TrimEnd('/');
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await client.GetAsync(baseUrl, cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<Microsoft.Playwright.IBrowser> LaunchAsync()
    {
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        return await playwright.Chromium.LaunchAsync(new() { Headless = true });
    }
}
