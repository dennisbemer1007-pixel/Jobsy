using System.Net.Http.Json;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Infrastructure.Data;
using Jobsy.Core.Security;
using Jobsy.Tests.E2e;
using Jobsy.Tests.TestSupport;
using Jobsy.Web.Auth;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Real Web+API stack: after profile reset, onboarding step 1 autosave shows Bewaard (1440 + 390).
/// Screenshots: <c>/opt/cursor/artifacts/reset/</c>
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CandidateOnboardingAfterResetPlaywrightTests
    : IClassFixture<RoleFunctionalWebAppFactory>, IAsyncLifetime
{
    private const string ArtifactDir = "/opt/cursor/artifacts/reset";

    private readonly RoleFunctionalWebAppFactory _api;
    private OnboardingResetWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public CandidateOnboardingAfterResetPlaywrightTests(RoleFunctionalWebAppFactory api) => _api = api;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(ArtifactDir);
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
        _web?.Dispose();
    }

    [Fact(Timeout = 300_000)]
    public async Task After_reset_onboarding_step1_autosave_shows_bewaard()
    {
        _ = TestContext.Current.CancellationToken;
        await SeedMidOnboardingAsync();
        var client = _api.CreateClient();
        JobsyTestAuth.Authorize(client, _api.CandidateId);
        using var reset = await client.PostAsJsonAsync(
            "api/privacy/reset-candidate-profile",
            new ResetCandidateProfileRequest("VERWIJDER"));
        Assert.Equal(System.Net.HttpStatusCode.OK, reset.StatusCode);

        var baseUrl = EnsureWeb();
        var signIn = $"{baseUrl}/__test/sign-in?return={Uri.EscapeDataString("/candidate/start")}";

        foreach (var (tag, width, height) in new[]
                 {
                     ("desktop-1440", KbE2e.Desktop.Width, KbE2e.Desktop.Height),
                     ("mobile-390", KbE2e.Mobile.Width, KbE2e.Mobile.Height)
                 })
        {
            await using var context = await _browser!.NewContextAsync(KbE2e.ContextFor(width, height));
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(90_000);

            await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForFunctionAsync("() => typeof window.Blazor !== 'undefined'");
            await page.WaitForTimeoutAsync(2_000);

            var transport = page.Locator("#onboarding-field-transport button, #onboarding-field-transport .lobsy-choice-tile").First;
            if (await transport.CountAsync() > 0)
            {
                await transport.ClickAsync();
            }

            await page.GetByText("Bewaard", new() { Exact = false }).First.WaitForAsync(new() { Timeout = 45_000 });
            await KbE2e.AssertNoCircuitErrorAsync(page);
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(ArtifactDir, $"step1-after-reset-{tag}.png"),
                FullPage = false
            });
        }
    }

    private async Task SeedMidOnboardingAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var rows = await db.CandidateOnboardings.Where(o => o.UserId == _api.CandidateId).ToListAsync();
        if (rows.Count > 0)
        {
            db.CandidateOnboardings.RemoveRange(rows);
        }

        db.CandidateOnboardings.Add(new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = _api.CandidateId,
            CurrentStep = 8,
            StartedAtUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private string EnsureWeb()
    {
        if (_web is not null)
        {
            return _web.ServerAddress.TrimEnd('/');
        }

        _web = new OnboardingResetWebFactory(_api.Server.CreateHandler(), _api.CandidateId, _api.CandidateEmail);
        _ = _web.CreateClient();
        return _web.ServerAddress.TrimEnd('/');
    }

    private sealed class OnboardingResetWebFactory : WebWebApplicationFactory
    {
        private readonly HttpMessageHandler _api;
        private readonly Guid _userId;
        private readonly string _email;
        private IHost? _kestrel;

        public string ServerAddress { get; private set; } = "";

        public OnboardingResetWebFactory(HttpMessageHandler api, Guid userId, string email)
        {
            _api = api;
            _userId = userId;
            _email = email;
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
                services.AddSingleton<IStartupFilter>(new CandidateSignInFilter(_userId, _email));
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new PassportOnFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new ForwardFactory(_api));
                services.RemoveAll<JobsyApiClient>();
                services.AddScoped(sp =>
                {
                    var auth = new JobsyApiAuthHandler(
                        sp.GetRequiredService<IHttpContextAccessor>(),
                        sp.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(),
                        sp,
                        sp.GetRequiredService<IConfiguration>(),
                        sp.GetRequiredService<JobsyAccessTokenIssuer>())
                    {
                        InnerHandler = new ForwardHandler(_api)
                    };
                    var http = new HttpClient(auth, disposeHandler: false)
                    {
                        BaseAddress = new Uri("http://api.test/")
                    };
                    return new JobsyApiClient(http);
                });
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

    private sealed class CandidateSignInFilter(Guid userId, string email) : IStartupFilter
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
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString("D")),
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.Role, JobsyRoles.Candidate),
                        new Claim(ClaimTypes.Name, "Dennis Bemer"),
                        new Claim(JobsyClaimTypes.MfaVerified, "1"),
                        new Claim(JobsyClaimTypes.SessionVersion, "0")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/candidate/start";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private sealed class PassportOnFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: true,
                CandidatePassportEnabled: true));

        public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }

    private sealed class ForwardFactory(HttpMessageHandler api) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new ForwardHandler(api), disposeHandler: false);
    }

    private sealed class ForwardHandler : HttpMessageHandler
    {
        private readonly HttpMessageInvoker _api;

        public ForwardHandler(HttpMessageHandler api)
            => _api = new HttpMessageInvoker(api, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.PathAndQuery ?? "/";
            if (!path.StartsWith('/'))
            {
                path = "/" + path;
            }

            using var clone = new HttpRequestMessage(request.Method, new Uri("http://localhost" + path));
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (request.Content is not null)
            {
                var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
                clone.Content = new ByteArrayContent(bytes);
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return await _api.SendAsync(clone, cancellationToken);
        }
    }
}
