using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Web.Models;
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
/// Styled paspoort PNGs via Kestrel Blazor Web + stubbed kompas API (Draft vs Completed).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class PassportDeepOnboardingScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/opt/cursor/artifacts/screenshots";
    private PaspoortScreenshotWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new PaspoortScreenshotWebFactory();
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
    public async Task Capture_styled_paspoort_before_and_after_deep_onboarding()
    {
        Directory.CreateDirectory(ArtifactsDir);
        await CaptureAsync(completed: false, "paspoort-deep-onboarding-before");
        await CaptureAsync(completed: true, "paspoort-deep-onboarding-after");
    }

    private async Task CaptureAsync(bool completed, string namePrefix)
    {
        _web!.Probe.DeepTestsCompleted = completed;

        foreach (var (width, height, suffix) in new[] { (1280, 900, "desktop"), (375, 812, "mobile") })
        {
            await using var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                Locale = "nl-NL"
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(90_000);

            await page.GotoAsync(
                $"{_web.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString("/candidate/paspoort?tab=dna")}",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded });

            var overview = page.Locator(".passport-overview");
            await overview.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Assertions.Expect(page.Locator(".passport-overview__title")).ToHaveTextAsync("Dit ben jij");

            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    await overview.ScreenshotAsync(new()
                    {
                        Path = Path.Combine(ArtifactsDir, $"{namePrefix}-{suffix}.png")
                    });
                    break;
                }
                catch (PlaywrightException) when (attempt < 2)
                {
                    await page.WaitForTimeoutAsync(800);
                }
            }

            var text = await overview.InnerTextAsync();
            if (completed)
            {
                Assert.Contains("fase 4 van 4", text, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Volledig", text, StringComparison.Ordinal);
            }
            else
            {
                Assert.True(
                    text.Contains("nog in het ei", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Eerste indruk", StringComparison.OrdinalIgnoreCase),
                    text);
            }
        }
    }

    internal static CandidateKompasState BuildKompas(bool completed)
    {
        var status = completed ? "Completed" : "Draft";
        var scores = new CompetencyScoreSet
        {
            Samenwerken = 78,
            Resultaatgerichtheid = 82,
            Stressbestendigheid = 71,
            Innovatie = 69,
            Extraversie = 74
        };
        var riasec = new RiasecScoreSet
        {
            Realistic = 55,
            Investigative = 62,
            Artistic = 48,
            Social = 80,
            Enterprising = 58,
            Conventional = 52
        };

        return new CandidateKompasState
        {
            Profile = new MeProfile
            {
                Id = Guid.Parse("d2a20000-0000-0000-0000-000000000042"),
                FullName = "Samira Demo",
                Preferences = new CandidatePreferences { AboutMe = "Ik werk graag in teamverband." }
            },
            ProfileCompletenessPercent = completed ? 92 : 55,
            WhoAmI = completed
                ? new WhoAmIStorySummary
                {
                    Story = "Samira pakt taken concreet op en houdt het overzicht, ook als het druk wordt.",
                    GeneratedAtUtc = DateTime.UtcNow,
                    Status = "Ready"
                }
                : null,
            Competencies = CompetenceState(25, status, scores),
            CareerInterests = CareerState(25, status, riasec),
            Culture = CultureState(18, status),
            Values = ValuesState(25, status)
        };
    }

    private static CandidateCompetencyState CompetenceState(int n, string status, CompetencyScoreSet scores)
        => new()
        {
            Status = status,
            AnsweredCount = n,
            QuestionCount = 25,
            Answers = Enumerable.Range(1, n).ToDictionary(i => i.ToString(), _ => 4),
            Scores = status == "Completed" ? scores : null,
            PreviewScores = status == "Draft" ? scores : null,
            CompletedAtUtc = status == "Completed" ? DateTime.UtcNow : null
        };

    private static CandidateCareerInterestState CareerState(int n, string status, RiasecScoreSet scores)
        => new()
        {
            Status = status,
            AnsweredCount = n,
            QuestionCount = 25,
            Answers = Enumerable.Range(1, n).ToDictionary(i => i.ToString(), _ => 4),
            Scores = status == "Completed" ? scores : null,
            PreviewScores = status == "Draft" ? scores : null,
            CompletedAtUtc = status == "Completed" ? DateTime.UtcNow : null
        };

    private static CandidateCultureState CultureState(int n, string status)
        => new()
        {
            Status = status,
            Answers = Enumerable.Range(1, n).ToDictionary(i => i.ToString(), _ => 4),
            Scores = new CulturePersonalityScoreSet
            {
                Autonomy = 70,
                Informal = 65,
                Collaboration = 72,
                Flexibility = 68,
                Innovation = 60,
                PeopleFirst = 75
            },
            PreviewScores = status == "Draft"
                ? new CulturePersonalityScoreSet { Collaboration = 72, PeopleFirst = 75, Autonomy = 70 }
                : null,
            CompletedAtUtc = status == "Completed" ? DateTime.UtcNow : null
        };

    private static CandidateValuesState ValuesState(int n, string status)
        => new()
        {
            Status = status,
            Answers = Enumerable.Range(1, n).ToDictionary(i => i.ToString(), _ => 4),
            Scores = new SchwartzValuesScoreSet
            {
                Autonomy = 70,
                Connection = 82,
                Achievement = 65,
                Stability = 58,
                Impact = 72
            },
            PreviewScores = status == "Draft"
                ? new SchwartzValuesScoreSet { Connection = 82, Impact = 72 }
                : null,
            CompletedAtUtc = status == "Completed" ? DateTime.UtcNow : null
        };

    internal sealed class PaspoortScreenshotWebFactory : WebWebApplicationFactory
    {
        public PaspoortProbe Probe { get; } = new();
        public string ServerAddress { get; private set; } = "";
        private IHost? _kestrel;

        public PaspoortScreenshotWebFactory()
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
                services.RemoveAll<IHostedService>();
                services.AddSingleton<IStartupFilter>(new CandidateSignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new PassportOnFlags());
                services.AddSingleton(Probe);
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new StubFactory(Probe));
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new StubHandler(Probe), disposeHandler: false)
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

    internal sealed class PaspoortProbe
    {
        public bool DeepTestsCompleted { get; set; }
    }

    private sealed class CandidateSignInFilter : IStartupFilter
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
                        new Claim(ClaimTypes.Name, "Samira Demo")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/candidate/paspoort";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private sealed class PassportOnFlags : IFeatureFlags
    {
        private static readonly FeatureFlagSnapshot Snap = new(EmployersEnabled: false, CandidatePassportEnabled: true);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class StubFactory(PaspoortProbe probe) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubHandler(probe), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class StubHandler(PaspoortProbe probe) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var kompas = BuildKompas(probe.DeepTestsCompleted);
            var json = path.Contains("/kompas/dna", StringComparison.OrdinalIgnoreCase)
                ? JsonSerializer.Serialize(MapDna(kompas), Json)
                : path.Contains("/kompas", StringComparison.OrdinalIgnoreCase)
                    ? JsonSerializer.Serialize(kompas, Json)
                    : path.Contains("/site/status", StringComparison.OrdinalIgnoreCase)
                        ? """{"maintenance":false,"expectedEndUtc":null}"""
                        : path.Contains("/applications", StringComparison.OrdinalIgnoreCase)
                            ? "[]"
                            : "{}";
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        private static CandidateDnaSummary MapDna(CandidateKompasState kompas)
            => new()
            {
                ProfileCompletenessPercent = kompas.ProfileCompletenessPercent,
                Competencies = kompas.Competencies is null
                    ? null
                    : new CandidateDnaCompetencySummary
                    {
                        Status = kompas.Competencies.Status,
                        AnsweredCount = kompas.Competencies.AnsweredCount,
                        QuestionCount = kompas.Competencies.QuestionCount,
                        Scores = kompas.Competencies.Scores,
                        PreviewScores = kompas.Competencies.PreviewScores
                    },
                CareerInterests = kompas.CareerInterests is null
                    ? null
                    : new CandidateDnaCareerSummary
                    {
                        Status = kompas.CareerInterests.Status,
                        AnsweredCount = kompas.CareerInterests.AnsweredCount,
                        QuestionCount = kompas.CareerInterests.QuestionCount,
                        Scores = kompas.CareerInterests.Scores,
                        PreviewScores = kompas.CareerInterests.PreviewScores
                    },
                Culture = kompas.Culture is null
                    ? null
                    : new CandidateDnaCultureSummary
                    {
                        Status = kompas.Culture.Status,
                        AnsweredCount = kompas.Culture.AnsweredCount,
                        QuestionCount = kompas.Culture.QuestionCount,
                        Scores = kompas.Culture.Scores,
                        PreviewScores = kompas.Culture.PreviewScores
                    },
                Values = kompas.Values is null
                    ? null
                    : new CandidateDnaValuesSummary
                    {
                        Status = kompas.Values.Status,
                        AnsweredCount = kompas.Values.AnsweredCount,
                        QuestionCount = kompas.Values.QuestionCount,
                        Scores = kompas.Values.Scores,
                        PreviewScores = kompas.Values.PreviewScores
                    }
            };
    }
}
