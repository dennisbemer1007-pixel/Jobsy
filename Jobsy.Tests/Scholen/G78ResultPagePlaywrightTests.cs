using Bunit;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Playwright;
using System.Security.Claims;

namespace Jobsy.Tests.Scholen;

[Collection("PlaywrightSmoke")]
public sealed class G78ResultPagePlaywrightTests : BunitContext
{
    public G78ResultPagePlaywrightTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IPupilQuestionSetRegistry>(new PupilQuestionSetRegistry());
    }

    public static IEnumerable<object[]> ProfileViewports() =>
        from profile in new[] { "helpt-anderen", "neemt-voortouw", "maker" }
        from vp in new[] { (1440, 900, "desktop"), (390, 844, "mobile") }
        select new object[] { profile, vp.Item1, vp.Item2, vp.Item3 };

    [Theory]
    [MemberData(nameof(ProfileViewports))]
    public async Task G78_result_page_screenshot_per_profile_and_viewport(string profile, int width, int height, string viewportTag)
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var fixture = FixtureFor(profile);
        var html = BuildHtml(fixture);
        Directory.CreateDirectory("/opt/cursor/artifacts");
        var fileName = $"g78-result-{profile}-{viewportTag}.png";

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            DeviceScaleFactor = width < 768 ? 2 : 1,
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(html, new() { WaitUntil = WaitUntilState.Load });
        var tiles = page.Locator("[data-testid=ll-g78-result-tiles]");
        await tiles.ScrollIntoViewIfNeededAsync();
        Assert.True(await tiles.IsVisibleAsync());
        var growth = page.Locator("[data-testid=ll-g78-growth-competencies]");
        if (await growth.CountAsync() > 0)
        {
            await growth.ScrollIntoViewIfNeededAsync();
        }

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine("/opt/cursor/artifacts", fileName),
            FullPage = true,
        });
    }

    [Fact]
    public async Task G78_growth_info_panel_open_desktop_screenshot()
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var fixture = PupilG78RealisticFixtures.HelpsOthers();
        var html = BuildHtml(fixture);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(html, new() { WaitUntil = WaitUntilState.Load });
        var infoBtn = page.Locator(".ll-g78-grow-info").First;
        await infoBtn.ClickAsync();
        await page.Locator(".ll-g78-grow-panel:not([hidden])").First.WaitForAsync();
        Directory.CreateDirectory("/opt/cursor/artifacts");
        await page.ScreenshotAsync(new()
        {
            Path = "/opt/cursor/artifacts/g78-result-growth-panel-open-desktop.png",
            FullPage = true,
        });
    }

  private static (PupilResult Result, PupilProgress Progress) FixtureFor(string profile) => profile switch
  {
      "helpt-anderen" => PupilG78RealisticFixtures.HelpsOthers(),
      "neemt-voortouw" => PupilG78RealisticFixtures.LeadsGroup(),
      "maker" => PupilG78RealisticFixtures.Maker(),
      _ => PupilG78RealisticFixtures.LeadsGroup(),
  };

  private string BuildHtml((PupilResult Result, PupilProgress Progress) fixture)
  {
      var renderer = new PupilStoryRenderer();
      var story = renderer.Render(fixture.Result, fixture.Progress, "nl");
      var culture = Services.GetRequiredService<CultureState>();
      culture.SetLanguageAsync("nl").GetAwaiter().GetResult();
      var dto = new PupilResultPageDto(
          "8B",
          "DEMO-CODE",
          "Demo OBS",
          story,
          ["theater", "lezen"],
          [],
          null,
          null,
          QuestionSet: PupilQuestionSet.Groep78);
      var registry = Services.GetRequiredService<IPupilQuestionSetRegistry>();
      return WrapHtml(PupilDitBenJijSsrHtml.BuildBody(
          dto, culture, groep78: true, "koraalrif", registry, passportV2: true, "/leerling/pdf"));
  }

  private static string WrapHtml(string body)
  {
      var webRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Jobsy.Web", "wwwroot"));
      var cssPath = Path.Combine(webRoot, "css", "features", "leerling-werelden-kies.css");
      var css = File.Exists(cssPath) ? File.ReadAllText(cssPath) : "";
      var jsPath = Path.Combine(webRoot, "js", "leerling-journey-ssr.js");
      var js = File.Exists(jsPath) ? File.ReadAllText(jsPath) : "";
      return $"""
          <!DOCTYPE html>
          <html lang="nl"><head><meta charset="utf-8"/><style>{css}</style></head>
          <body class="ll-g78-shell"><main>{body}</main>
          <script>{js}</script></body></html>
          """;
  }

  private sealed class AnonymousAuth : AuthenticationStateProvider
  {
      public override Task<AuthenticationState> GetAuthenticationStateAsync()
          => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
  }
}
