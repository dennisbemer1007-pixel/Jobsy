using System.Net.Http.Json;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Dev;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Privacy;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
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

[Collection("PlaywrightSmoke")]
public sealed class AccountDeleteReregisterPlaywrightTests
    : IClassFixture<RoleFunctionalWebAppFactory>, IAsyncLifetime
{
    private static readonly string StoreDir = "/cursor/stores/self/reset";
    private static readonly string ArtifactDir = "/opt/cursor/artifacts/reset";

    private readonly RoleFunctionalWebAppFactory _api;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public AccountDeleteReregisterPlaywrightTests(RoleFunctionalWebAppFactory api) => _api = api;

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("JOBSY_E2E_CAPTURE_EMAIL_CODES", "1");
        DevEmailCodeCapture.Clear();
        Directory.CreateDirectory(StoreDir);
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
    }

    [Fact(Timeout = 420_000)]
    public async Task Delete_reregister_and_onboarding_bewaard_evidence_at_1440_and_390()
    {
        _ = TestContext.Current.CancellationToken;

        foreach (var (tag, width, height) in new[]
                 {
                     ("desktop-1440", KbE2e.Desktop.Width, KbE2e.Desktop.Height),
                     ("mobile-390", KbE2e.Mobile.Width, KbE2e.Mobile.Height)
                 })
        {
            var userId = Guid.NewGuid();
            var email = $"e2e-reregister-{tag}@test.local";
            await SeedCandidateAsync(userId, email);

            using var web = new ReregisterWebFactory(_api.Server.CreateHandler(), userId, email);
            var baseUrl = web.EnsureServerAddress();
            var api = _api.CreateClient();
            JobsyTestAuth.Authorize(api, userId);

            await using var context = await _browser!.NewContextAsync(KbE2e.ContextFor(width, height));
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(90_000);

            await OnboardingPlaywrightHelpers.WaitForBlazorCircuitAsync(
                page,
                () => page.GotoAsync(
                    $"{baseUrl}/__test/sign-in?userId={userId:D}&return={Uri.EscapeDataString("/candidate/start")}",
                    new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 }));
            await page.Locator(".ob-wizard__loading").WaitForAsync(new()
            {
                State = WaitForSelectorState.Detached,
                Timeout = 60_000
            });
            if (await page.Locator(".ob-wizard__welcome .ob-wizard__cta").CountAsync() > 0)
            {
                await page.Locator(".ob-wizard__welcome .ob-wizard__cta").ClickAsync();
                await page.Locator(".ob-wizard__loading").WaitForAsync(new()
                {
                    State = WaitForSelectorState.Detached,
                    Timeout = 60_000
                });
            }

            var privacyLink = page.Locator(".ob-wizard a[href='/privacy/data']").First;
            await privacyLink.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
            if (width >= 640)
            {
                await privacyLink.ScrollIntoViewIfNeededAsync();
            }

            await SaveShotAsync(page, $"onboarding-privacy-link-{tag}.png");

            await page.GotoAsync($"{baseUrl}/privacy/data", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.Locator("#mydata-delete-title").WaitForAsync(new() { Timeout = 60_000 });
            await SaveShotAsync(page, $"privacy-data-from-onboarding-{tag}.png");

            using (await api.PostAsJsonAsync(
                       "api/privacy/request-unsubscribe",
                       new RequestUnsubscribeRequest(AccountUnsubscribeReasons.Privacy)))
            {
            }

            var deleteCode = await ReadOtpAsync(api, email);
            using (await api.PostAsJsonAsync(
                       "api/privacy/delete-account",
                       new ConfirmUnsubscribeRequest(deleteCode)))
            {
            }

            var anon = _api.CreateClient();
            anon.DefaultRequestHeaders.TryAddWithoutValidation(
                "X-Jobsy-Provision-Secret",
                "local-dev-jobsy-provision-secret");
            using (var start = await anon.PostAsJsonAsync(
                       "api/auth/email-code/start",
                       new Jobsy.Api.Models.EmailCodeStartRequest(email, FirstName: "Nieuw", Culture: "nl")))
            {
                var startBody = await start.Content.ReadFromJsonAsync<Jobsy.Api.Models.EmailCodeStartResponse>();
                var signUpCode = await ReadOtpAsync(anon, email);
                using var verify = await anon.PostAsJsonAsync(
                    "api/auth/email-code/verify",
                    new Jobsy.Api.Models.EmailCodeVerifyRequest(startBody!.ChallengeId, signUpCode, RememberDevice: false));
                Assert.Equal(System.Net.HttpStatusCode.OK, verify.StatusCode);
            }

            var newUserId = await LookupActiveUserIdAsync(email);
            Assert.NotNull(newUserId);
            Assert.NotEqual(userId, newUserId);

            await context.ClearCookiesAsync();
            var onboardingSignIn =
                $"{baseUrl}/__test/sign-in?userId={newUserId:D}&return={Uri.EscapeDataString("/candidate/start")}";
            await OnboardingPlaywrightHelpers.WaitForBlazorCircuitAsync(
                page,
                () => page.GotoAsync(onboardingSignIn, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 }));
            await OnboardingPlaywrightHelpers.EnsureStep1FieldsAsync(page);
            await page.Locator("#onboarding-field-firstname input").FillAsync("Nieuw");
            await page.Locator("#onboarding-field-lastname input").FillAsync("Start");
            await OnboardingPlaywrightHelpers.WaitForSavedIndicatorAsync(page);
            await SaveShotAsync(page, $"step1-after-reregister-{tag}.png");
        }
    }

    private static async Task<string> ReadOtpAsync(HttpClient client, string email)
    {
        using var response = await client.GetAsync($"api/e2e/last-email-code?email={Uri.EscapeDataString(email)}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return json.GetProperty("code").GetString()!;
    }

    private async Task<Guid?> LookupActiveUserIdAsync(string email)
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        return user?.Id;
    }

    private async Task SeedCandidateAsync(Guid userId, string email)
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        if (await db.Users.AnyAsync(u => u.Id == userId))
        {
            return;
        }

        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            FullName = "E2E kandidaat",
            FirstName = "Dennis",
            LastName = "Bemer",
            Role = UserRole.Candidate,
            IsActive = true,
            PreferencesJson = """{"roles":["Winkel"]}""",
            ConsentVersion = PrivacyConstants.CurrentConsentVersion,
            TermsAcceptedAt = DateTime.UtcNow
        });
        db.CandidateOnboardings.Add(new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentStep = 2,
            StartedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static async Task SaveShotAsync(IPage page, string fileName)
    {
        var storePath = Path.Combine(StoreDir, fileName);
        var artifactPath = Path.Combine(ArtifactDir, fileName);
        await page.ScreenshotAsync(new() { Path = storePath, FullPage = false });
        await page.ScreenshotAsync(new() { Path = artifactPath, FullPage = false });
    }

    private sealed class ReregisterWebFactory : WebWebApplicationFactory
    {
        private readonly HttpMessageHandler _api;
        private readonly Guid _userId;
        private readonly string _email;
        private IHost? _kestrel;

        public string ServerAddress { get; private set; } = "";

        public ReregisterWebFactory(HttpMessageHandler api, Guid userId, string email)
        {
            _api = api;
            _userId = userId;
            _email = email;
        }

        public string EnsureServerAddress()
        {
            if (string.IsNullOrEmpty(ServerAddress))
            {
                _ = CreateClient();
            }

            return ServerAddress.TrimEnd('/');
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
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = JobsyAccessToken.DevelopmentPrivateKeyPem
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new SignInFilter(_email, _userId));
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new PassportOnFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(sp =>
                    new OnboardingTestHttpClientFactory(
                        _api,
                        () => ServerAddress,
                        sp.GetRequiredService<IHttpContextAccessor>()));
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
                        InnerHandler = new TestApiForwardHandler(_api)
                    };
                    return new JobsyApiClient(new HttpClient(auth, disposeHandler: false)
                    {
                        BaseAddress = new Uri("http://api.test/")
                    });
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

    private sealed class SignInFilter(string email, Guid fallbackUserId) : IStartupFilter
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

                    var userId = fallbackUserId;
                    if (Guid.TryParse(ctx.Request.Query["userId"], out var fromQuery))
                    {
                        userId = fromQuery;
                    }

                    var identity = new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString("D")),
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.Role, JobsyRoles.Candidate),
                        new Claim(ClaimTypes.Name, "E2E kandidaat"),
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
}
