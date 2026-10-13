using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using System.Security.Claims;

namespace Jobsy.Tests;

/// <summary>
/// Logged-out one-click unsubscribe, and the reminder-email switch in account settings.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class ReminderEmailOptOutPlaywrightTests : IAsyncLifetime
{
    private OptOutWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        _web = new OptOutWebFactory();
        _ = _web.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
    }

    [Fact]
    public async Task Logged_out_unsubscribe_confirms_without_login()
    {
        var page = await _browser!.NewPageAsync();
        page.SetDefaultTimeout(30_000);
        await page.GotoAsync(_web!.ServerAddress + "/mail/afmelden?t=signed-token", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60_000
        });

        var confirm = page.Locator("#mail-unsub-once button[type=submit]");
        await confirm.WaitForAsync();
        Assert.DoesNotContain("requestSubmit", await page.ContentAsync(), StringComparison.Ordinal);
        await confirm.ClickAsync();
        await page.Locator("[data-mail-unsub='done']").WaitForAsync();
        var text = await page.Locator("[data-mail-unsub='done']").InnerTextAsync();
        Assert.Contains("Je krijgt geen herinneringen meer per e-mail", text, StringComparison.Ordinal);
        Assert.Contains("instellingen", text, StringComparison.Ordinal);
        var href = await page.Locator("a[href='/account/mail-instellingen']").GetAttributeAsync("href");
        Assert.Equal("/account/mail-instellingen", href);
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.True(_web.State.UnsubscribePosts > 0);
    }

    [Fact]
    public async Task Settings_toggle_turns_reminder_email_off()
    {
        var page = await _browser!.NewPageAsync();
        page.SetDefaultTimeout(30_000);
        await page.GotoAsync(
            _web!.ServerAddress + "/__test/sign-in?return=/account/mail-instellingen",
            new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });

        var box = page.Locator("[data-reminder-emails='1']");
        await box.WaitForAsync();
        Assert.Contains(
            "Herinneringen per e-mail",
            await page.Locator("body").InnerTextAsync(),
            StringComparison.Ordinal);
        await box.UncheckAsync();
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForURLAsync("**/account/mail-instellingen?saved=1**");
        Assert.Equal(false, _web.State.LastReminderEmailsEnabled);
    }

    private sealed class OptOutWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
    {
        private IHost? _kestrel;

        public Probe State { get; } = new();

        public string ServerAddress { get; private set; } = "";

        public OptOutWebFactory()
        {
            _ = CreateClient();
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
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new StubFactory(State));
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new StubHandler(State), disposeHandler: false)
                    {
                        BaseAddress = new Uri("http://api.test/")
                    }));
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

    private sealed class Probe
    {
        public int UnsubscribePosts { get; set; }
        public bool? LastReminderEmailsEnabled { get; set; }
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
                        ret = "/account/mail-instellingen";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private sealed class StubFactory(Probe state) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubHandler(state), disposeHandler: true)
            {
                BaseAddress = new Uri("http://api.test/")
            };
    }

    private sealed class StubHandler(Probe state) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("unsubscribe/preview", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { valid = true, category = "ComebackReminder" });
            }

            if (path.Contains("unsubscribe", StringComparison.OrdinalIgnoreCase)
                && request.Method == HttpMethod.Post)
            {
                state.UnsubscribePosts++;
                return Json(new { ok = true, category = "ComebackReminder" });
            }

            if (path.Contains("email-preferences", StringComparison.OrdinalIgnoreCase)
                && request.Method == HttpMethod.Put)
            {
                var body = request.Content is null
                    ? ""
                    : await request.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
                if (doc.RootElement.TryGetProperty("reminderEmailsEnabled", out var flag)
                    && flag.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    state.LastReminderEmailsEnabled = flag.GetBoolean();
                }

                return Json(new
                {
                    optional = new[]
                    {
                        new { key = "PushBom", label = "Tips", enabled = false },
                        new { key = "ComebackReminder", label = "Herinnering", enabled = false }
                    },
                    always = new[] { "Codes" },
                    reminderEmailsEnabled = state.LastReminderEmailsEnabled ?? false
                });
            }

            if (path.Contains("email-preferences", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new
                {
                    optional = new[]
                    {
                        new { key = "PushBom", label = "Tips", enabled = true },
                        new { key = "ComebackReminder", label = "Herinnering", enabled = false }
                    },
                    always = new[] { "Codes" },
                    reminderEmailsEnabled = true
                });
            }

            if (path.Contains("reminder-whatsapp", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { available = false, optedIn = false, phone = (string?)null });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json(object body)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
    }
}
