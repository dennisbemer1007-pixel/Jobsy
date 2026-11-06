using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Jobsy.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using Jobsy.Core.Features;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class CandidatePwaInstallPlaywrightTests : IAsyncLifetime
{
    private const string ScreenshotDir = "/cursor/stores/self/pwa-install/v2";

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(ScreenshotDir);
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    [Fact]
    public async Task Top_bar_install_button_desktop_and_mobile_with_deferred_prompt()
    {
        using var web = new PwaInstallWebFactory();
        var signIn = web.EnsureServerAddress() + "/__test/sign-in?return=/candidate/applications";

        await using var desktopCtx = await NewContextAsync(1440, 900, mobile: false, samsung: false, ios: false, standalone: false);
        await desktopCtx.AddInitScriptAsync(BeforeInstallPromptInitScript);
        var desktop = await desktopCtx.NewPageAsync();
        await desktop.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(desktop);
        await desktop.Locator("[data-testid=pwa-install-entry-btn]").WaitForAsync(new() { Timeout = 15_000 });
        await SaveJpegAsync(desktop, "top-bar-desktop-1440.jpg", clipHeader: true);

        await using var mobileCtx = await NewContextAsync(390, 844, mobile: true, samsung: false, ios: false, standalone: false, deviceScaleFactor: 2);
        await mobileCtx.AddInitScriptAsync(BeforeInstallPromptInitScript);
        var mobile = await mobileCtx.NewPageAsync();
        await mobile.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(mobile);
        await mobile.Locator("[data-testid=pwa-install-entry-btn]").WaitForAsync(new() { Timeout = 15_000 });
        await SaveJpegAsync(mobile, "top-bar-mobile-390.jpg", clipHeader: true);
    }

    [Fact]
    public async Task Standalone_mode_hides_install_button()
    {
        using var web = new PwaInstallWebFactory();
        var signIn = web.EnsureServerAddress() + "/__test/sign-in?return=/candidate/applications";

        await using var ctx = await NewContextAsync(390, 844, mobile: true, samsung: false, ios: false, standalone: true, deviceScaleFactor: 2);
        await ctx.AddInitScriptAsync(BeforeInstallPromptInitScript);
        var page = await ctx.NewPageAsync();
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForTimeoutAsync(800);
        Assert.Equal(0, await page.Locator("[data-testid=pwa-install-entry-btn]").CountAsync());
        await SaveJpegAsync(page, "top-bar-standalone-hidden-390.jpg", clipHeader: true);
    }

    [Fact]
    public async Task Samsung_and_ios_tip_sheets_on_user_tap()
    {
        using var web = new PwaInstallWebFactory();
        var signIn = web.EnsureServerAddress() + "/__test/sign-in?return=/candidate/applications";

        await using var samsungCtx = await NewContextAsync(
            390,
            844,
            mobile: true,
            samsung: true,
            ios: false,
            standalone: false,
            deviceScaleFactor: 2);
        var samsung = await samsungCtx.NewPageAsync();
        await samsung.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(samsung);
        await samsung.Locator("[data-testid=pwa-install-entry-btn]").WaitForAsync(new() { Timeout = 15_000 });
        await samsung.Locator("[data-testid=pwa-install-entry-btn]").ClickAsync();
        var samsungSheet = samsung.Locator("[data-testid=pwa-install-sheet]");
        await samsungSheet.WaitForAsync();
        await samsung.Locator("[data-testid=pwa-open-chrome]").WaitForAsync();
        await samsung.GetByRole(AriaRole.Button, new() { Name = "Link kopiëren" }).WaitForAsync();
        Assert.Equal(0, await samsung.Locator(".lobsy-assistant-tab:visible").CountAsync());
        await SaveJpegAsync(samsung, "sheet-samsung-390.jpg");

        await using var iosCtx = await NewContextAsync(
            390,
            844,
            mobile: true,
            samsung: false,
            ios: true,
            standalone: false,
            deviceScaleFactor: 2);
        var ios = await iosCtx.NewPageAsync();
        await ios.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(ios);
        await ios.Locator("[data-testid=pwa-install-entry-btn]").WaitForAsync(new() { Timeout = 15_000 });
        await ios.Locator("[data-testid=pwa-install-entry-btn]").ClickAsync();
        await ios.Locator("[data-testid=pwa-install-sheet]").WaitForAsync();
        await ios.Locator(".pwa-entry-sheet__steps li").Nth(2).WaitForAsync();
        await SaveJpegAsync(ios, "sheet-ios-390.jpg");
    }

    [Fact]
    public async Task Chrome_sheet_v3_screenshots_with_copy_confirmation()
    {
        using var web = new PwaInstallWebFactory();
        var signIn = web.EnsureServerAddress() + "/__test/sign-in?return=/candidate/applications";
        const string v3 = "/cursor/stores/self/pwa-install/v3";
        Directory.CreateDirectory(v3);

        await using var samsungCtx = await NewContextAsync(
            390, 844, mobile: true, samsung: true, ios: false, standalone: false, deviceScaleFactor: 2);
        await samsungCtx.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
        var samsung = await samsungCtx.NewPageAsync();
        await samsung.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(samsung);
        await samsung.Locator("[data-testid=pwa-install-entry-btn]").ClickAsync();
        await samsung.Locator("[data-testid=pwa-install-sheet]").WaitForAsync();
        await samsung.GetByRole(AriaRole.Button, new() { Name = "Link kopiëren" }).ClickAsync();
        await samsung.Locator("[data-testid=pwa-copy-confirm]").WaitForAsync();
        await SaveJpegToDirAsync(samsung, v3, "sheet-samsung-390.jpg");

        await using var desktopCtx = await NewContextAsync(1440, 900, mobile: false, samsung: true, ios: false, standalone: false);
        await desktopCtx.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
        var desktop = await desktopCtx.NewPageAsync();
        await desktop.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(desktop);
        await desktop.Locator("[data-testid=pwa-install-entry-btn]").ClickAsync();
        await desktop.Locator("[data-testid=pwa-install-sheet]").WaitForAsync();
        await desktop.GetByRole(AriaRole.Button, new() { Name = "Link kopiëren" }).ClickAsync();
        await desktop.Locator("[data-testid=pwa-copy-confirm]").WaitForAsync();
        await SaveJpegToDirAsync(desktop, v3, "sheet-desktop-1440.jpg");
    }

    [Fact]
    public async Task Desktop_sheet_centered_modal_at_1440()
    {
        using var web = new PwaInstallWebFactory();
        var signIn = web.EnsureServerAddress() + "/__test/sign-in?return=/candidate/applications";

        await using var ctx = await NewContextAsync(1440, 900, mobile: false, samsung: true, ios: false, standalone: false);
        var page = await ctx.NewPageAsync();
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.Locator("[data-testid=pwa-install-entry-btn]").WaitForAsync(new() { Timeout = 15_000 });
        await page.Locator("[data-testid=pwa-install-entry-btn]").ClickAsync();
        await page.Locator("[data-testid=pwa-install-sheet]").WaitForAsync();
        await SaveJpegAsync(page, "sheet-desktop-1440.jpg", clipSelector: null);
    }

    [Fact]
    public async Task Post_application_card_in_vacancy_success_context()
    {
        using var web = new PwaInstallWebFactory();
        var signIn = web.EnsureServerAddress() + "/__test/sign-in?return=/__test/pwa-vacancy-apply-success";

        foreach (var (name, width, height, mobile, scale) in new (string, int, int, bool, int)[]
                 {
                     ("390", 390, 844, true, 2),
                     ("1440", 1440, 900, false, 1)
                 })
        {
            await using var ctx = await NewContextAsync(width, height, mobile, samsung: false, ios: false, standalone: false, deviceScaleFactor: scale);
            await ctx.AddInitScriptAsync(BeforeInstallPromptInitScript);
            await ctx.AddInitScriptAsync(
                """
                (() => {
                  try {
                    localStorage.setItem('Jobsy.PwaPostApplyCardVisible', '1');
                  } catch (_) {}
                })();
                """);
            var page = await ctx.NewPageAsync();
            await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
            await PlaywrightCookieConsent.AcceptOnPageAsync(page);
            await page.Locator("[data-testid=pwa-vacancy-apply-preview]").WaitForAsync(new() { Timeout = 20_000 });
            await page.Locator("[data-testid=pwa-install-post-apply-card]").WaitForAsync(new() { Timeout = 20_000 });
            await SaveJpegAsync(page, $"post-apply-card-{name}.jpg", clipSelector: "[data-testid=pwa-vacancy-apply-preview]");
        }
    }

    private async Task<IBrowserContext> NewContextAsync(
        int width,
        int height,
        bool mobile,
        bool samsung,
        bool ios,
        bool standalone,
        int deviceScaleFactor = 1)
    {
        var ua = ios
            ? "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1"
            : samsung
                ? "Mozilla/5.0 (Linux; Android 14; SM-S928B) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/25.0 Chrome/120.0.0.0 Mobile Safari/537.36"
                : "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36";

        var ctx = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IsMobile = mobile,
            HasTouch = mobile,
            UserAgent = ua,
            DeviceScaleFactor = deviceScaleFactor
        });

        if (standalone)
        {
            await ctx.AddInitScriptAsync(
                """
                (() => {
                  const orig = window.matchMedia.bind(window);
                  window.matchMedia = (query) => {
                    if (typeof query === 'string' && query.includes('display-mode')) {
                      return { matches: true, media: query, addListener: () => {}, removeListener: () => {}, addEventListener: () => {}, removeEventListener: () => {}, dispatchEvent: () => false };
                    }
                    return orig(query);
                  };
                  try { Object.defineProperty(navigator, 'standalone', { get: () => true, configurable: true }); } catch (_) {}
                })();
                """);
        }

        return ctx;
    }

    private static Task SaveJpegToDirAsync(IPage page, string dir, string fileName, bool clipHeader = false, string? clipSelector = null)
        => SaveJpegAsync(page, fileName, clipHeader, clipSelector, dir);

    private static async Task SaveJpegAsync(IPage page, string fileName, bool clipHeader = false, string? clipSelector = null, string? dir = null)
    {
        dir ??= ScreenshotDir;
        var pngPath = Path.Combine(dir, Path.ChangeExtension(fileName, ".png"));
        var jpgPath = Path.Combine(dir, fileName);
        if (clipHeader)
        {
            var header = page.Locator(".app-header");
            await header.WaitForAsync();
            await header.ScreenshotAsync(new() { Path = pngPath });
        }
        else if (!string.IsNullOrWhiteSpace(clipSelector))
        {
            var target = page.Locator(clipSelector);
            await target.WaitForAsync();
            await target.ScreenshotAsync(new() { Path = pngPath });
        }
        else
        {
            await page.ScreenshotAsync(new() { Path = pngPath, FullPage = false });
        }

        await ConvertPngToJpegAsync(pngPath, jpgPath);
        try
        {
            File.Delete(pngPath);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private static async Task ConvertPngToJpegAsync(string pngPath, string jpgPath)
    {
        var ffmpeg = await Shell.RunAsync($"ffmpeg -y -i \"{pngPath}\" -q:v 2 \"{jpgPath}\" 2>/dev/null");
        if (ffmpeg.ExitCode == 0 && File.Exists(jpgPath))
        {
            return;
        }

        File.Copy(pngPath, jpgPath, overwrite: true);
    }

    private const string BeforeInstallPromptInitScript =
        """
        (() => {
          function fireDeferredPrompt() {
            const e = new Event('beforeinstallprompt', { cancelable: true });
            e.preventDefault = () => {};
            e.prompt = () => {};
            e.userChoice = Promise.resolve({ outcome: 'dismissed' });
            window.dispatchEvent(e);
          }
          if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => setTimeout(fireDeferredPrompt, 50));
          } else {
            setTimeout(fireDeferredPrompt, 50);
          }
        })();
        """;

    private sealed class PwaInstallWebFactory : WebWebApplicationFactory
    {
        private IHost? _kestrel;
        public string ServerAddress { get; private set; } = "";

        public string EnsureServerAddress()
        {
            if (string.IsNullOrEmpty(ServerAddress))
            {
                _ = CreateClient();
            }

            return ServerAddress;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiBaseUrl"] = "http://api.test/",
                    ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new SignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new StubFlags());
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var testHost = builder.Build();
            builder.ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseKestrel();
                webHostBuilder.UseUrls("http://127.0.0.1:0");
            });
            _kestrel = builder.Build();
            _kestrel.Start();
            var addresses = _kestrel.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
            ServerAddress = addresses!.Addresses.First(a => a.StartsWith("http://", StringComparison.Ordinal));
            testHost.Start();
            return testHost;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _kestrel?.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class StubFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: true,
                CandidatePassportEnabled: true,
                ProactiveMatchingEnabled: false));

        public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }

    private sealed class SignInFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (ctx, nxt) =>
                {
                    if (!ctx.Request.Path.Equals("/__test/sign-in", StringComparison.OrdinalIgnoreCase))
                    {
                        await nxt();
                        return;
                    }

                    var identity = new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Candidate"),
                        new Claim(ClaimTypes.Name, "Sanne")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/candidate/applications";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private static class Shell
    {
        public static async Task<(int ExitCode, string Output)> RunAsync(string command)
        {
            var psi = new System.Diagnostics.ProcessStartInfo("/bin/bash", $"-lc \"{command.Replace("\"", "\\\"")}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = System.Diagnostics.Process.Start(psi)!;
            var output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            return (proc.ExitCode, output);
        }
    }
}
