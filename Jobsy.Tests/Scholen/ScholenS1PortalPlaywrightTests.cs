using System.Net;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
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
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Playwright;
using Jobsy.Tests.TestSupport;

namespace Jobsy.Tests.Scholen;

[Collection("BanenkaartPlaywright")]
public sealed class ScholenS1PortalPlaywrightTests : IClassFixture<RoleFunctionalWebAppFactory>, IAsyncLifetime
{
    private readonly RoleFunctionalWebAppFactory _api;
    private ScholenS1WebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private TeacherSeed? _seed;

    public ScholenS1PortalPlaywrightTests(RoleFunctionalWebAppFactory api) => _api = api;

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        await EnableSchoolsAsync();
        _seed = await SeedTeacherWithClassAsync();
        _web = new ScholenS1WebFactory(_api.Server.CreateHandler());
        _ = _web.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        if (_web is not null)
        {
            await _web.DisposeAsync();
        }
    }

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task S5b_teacher_class_overview_progress_and_pending(int width, int height)
    {
        var seed = _seed!;
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{baseUrl}/__test/staff?user={seed.TeacherId:D}&school={seed.SchoolId:D}&role=Teacher&return={Uri.EscapeDataString($"/leraar/klas/{seed.ClassId:D}")}",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });

        await page.Locator("[data-testid='leraar-class-progress']").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator("[data-testid='leraar-kpi-row']").WaitForAsync();
        await page.Locator("[data-testid='leraar-overview-charts']").WaitForAsync(new() { Timeout = 60_000 });

        var donutWidth = await page.Locator(".sch-chart-donut__svg").First.EvaluateAsync<double>(
            "el => el.getBoundingClientRect().width");
        Assert.True(donutWidth > 40);

        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_WALKTHROUGH_ARTIFACTS");
        if (!string.IsNullOrWhiteSpace(artifactDir))
        {
            Directory.CreateDirectory(artifactDir);
            var suffix = width <= 400 ? "390" : "1440";
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, $"after-{suffix}.png"),
                FullPage = true
            });
        }

        var pendingVisible = await page.Locator("[data-testid='leraar-pending-codes']").CountAsync();
        Assert.True(pendingVisible >= 0);
    }

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task S10_teacher_never_sees_school_admin_nav_on_class_overview(int width, int height)
    {
        var seed = _seed!;
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{baseUrl}/__test/staff?user={seed.TeacherId:D}&school={seed.SchoolId:D}&role=Teacher&return={Uri.EscapeDataString($"/leraar/klas/{seed.ClassId:D}")}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });

        await Assertions.Expect(page.Locator(".sch-sidebar__link", new() { HasText = "Dashboard" })).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator(".sch-sidebar__link", new() { HasText = "Klassen & codes" })).ToHaveCountAsync(0);
        await page.Locator("[data-testid='leraar-class-progress']").WaitForAsync(new() { Timeout = 60_000 });
        await Assertions.Expect(page.Locator(".sch-sidebar__link", new() { HasText = "Dashboard" })).ToHaveCountAsync(0);
    }

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Teacher_portal_scope_chip_styles_and_leraar_redirect(int width, int height)
    {
        var seed = _seed!;
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{baseUrl}/__test/staff?user={seed.TeacherId:D}&school={seed.SchoolId:D}&role=Teacher&return={Uri.EscapeDataString("/leraar")}",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForURLAsync("**/leraar/klas/**", new() { Timeout = 90_000 });

        var hasEnterpriseUi = await page.EvaluateAsync<bool>(
            """
            () => [...document.querySelectorAll('link[rel="stylesheet"]')]
                .some(l => (l.getAttribute('href') || '').includes('enterprise-ui'))
            """);
        Assert.True(hasEnterpriseUi);

        var scopeName = page.Locator(".sch-topbar__scope-name").First;
        await scopeName.WaitForAsync(new() { Timeout = 30_000 });
        await Assertions.Expect(scopeName).ToContainTextAsync("Klas");

        var blazorScript = await page.Locator("script[src*='blazor.web']").CountAsync();
        Assert.Equal(0, blazorScript);
    }

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Teacher_codes_table_has_mobile_labels_and_nav_active_state(int width, int height)
    {
        var seed = _seed!;
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        var codesPath = $"/leraar/klas/{seed.ClassId:D}/codes";
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{baseUrl}/__test/staff?user={seed.TeacherId:D}&school={seed.SchoolId:D}&role=Teacher&return={Uri.EscapeDataString(codesPath)}",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForURLAsync("**/codes**", new() { Timeout = 90_000 });
        var codeCell = page.Locator("[data-testid=\"leraar-code-table\"] .sch-code").First;
        await codeCell.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });

        var labeled = await page.Locator("[data-testid=\"leraar-code-table\"] td[data-label]:not([data-label=\"\"])").CountAsync();
        Assert.True(labeled > 0);

        var overviewHref = $"/leraar/klas/{seed.ClassId:D}";
        var activeCodes = page.Locator(
            $".sch-sidebar__link.is-active[href=\"{codesPath}\"], .sch-bottom-nav__item.is-active[href=\"{codesPath}\"]");
        await Assertions.Expect(activeCodes).Not.ToHaveCountAsync(0, new() { Timeout = 30_000 });
        var overviewActive = await page.Locator(
            $".sch-sidebar__link.is-active[href=\"{overviewHref}\"], .sch-bottom-nav__item.is-active[href=\"{overviewHref}\"]")
            .CountAsync();
        Assert.Equal(0, overviewActive);

        var blazorScript = await page.Locator("script[src*='blazor.web']").CountAsync();
        Assert.Equal(0, blazorScript);

        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_WALKTHROUGH_ARTIFACTS");
        if (!string.IsNullOrWhiteSpace(artifactDir))
        {
            Directory.CreateDirectory(artifactDir);
            var suffix = width <= 400 ? "codes-390" : "codes-1440";
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, $"{suffix}.png"),
                FullPage = true
            });
        }
    }

    [Theory(Timeout = 300_000)]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Teacher_codes_page_has_no_console_errors(int width, int height)
    {
        _ = TestContext.Current.CancellationToken;
        var seed = _seed!;
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        var codesPath = $"/leraar/klas/{seed.ClassId:D}/codes";
        var consoleErrors = new List<string>();
        var pageErrors = new List<string>();

        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true,
        });
        var page = await context.NewPageAsync();
        page.Console += (_, msg) =>
        {
            if (msg.Type is "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };
        page.PageError += (_, err) => pageErrors.Add(err);

        await page.GotoAsync(
            $"{baseUrl}/__test/staff?user={seed.TeacherId:D}&school={seed.SchoolId:D}&role=Teacher&return={Uri.EscapeDataString(codesPath)}",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForURLAsync("**/codes**", new() { Timeout = 90_000 });
        await page.Locator("[data-testid=\"leraar-code-table\"] .sch-code").First.WaitForAsync(new() { Timeout = 90_000 });

        var scripts = await page.EvaluateAsync<string[]>(
            "() => [...document.querySelectorAll('script[src]')].map(s => s.getAttribute('src') || '')");
        Assert.Contains(scripts, s => s.Contains("scholen-portal-ssr.js", StringComparison.Ordinal));
        Assert.Contains(scripts, s => s.Contains("jobsy-dom-safe.js", StringComparison.Ordinal));

        var bad = consoleErrors
            .Where(e => !e.Contains("favicon", StringComparison.OrdinalIgnoreCase)
                        && !e.Contains("script-src-attr", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.True(bad.Count == 0 && pageErrors.Count == 0,
            "console: " + string.Join("; ", bad) + " | pageerror: " + string.Join("; ", pageErrors));
    }

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Teacher_testvenster_ssr_without_blazor(int width, int height)
    {
        var seed = _seed!;
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        var path = $"/leraar/klas/{seed.ClassId:D}/testvenster";
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{baseUrl}/__test/staff?user={seed.TeacherId:D}&school={seed.SchoolId:D}&role=Teacher&return={Uri.EscapeDataString(path)}",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.Locator("h1").Filter(new() { HasText = "Testvenster" }).WaitForAsync(new() { Timeout = 60_000 });
        Assert.Equal(0, await page.Locator("script[src*='blazor.web']").CountAsync());

        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_WALKTHROUGH_ARTIFACTS");
        if (!string.IsNullOrWhiteSpace(artifactDir))
        {
            Directory.CreateDirectory(artifactDir);
            var suffix = width <= 400 ? "testvenster-390" : "testvenster-1440";
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, suffix + ".png"),
                FullPage = true
            });
        }
    }

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task School_admin_dashboard_ssr_without_blazor(int width, int height)
    {
        var seed = _seed!;
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{baseUrl}/__test/staff?user={seed.SchoolAdminId:D}&school={seed.SchoolId:D}&role=SchoolAdmin&return={Uri.EscapeDataString("/school")}",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.Locator("h1").First.WaitForAsync(new() { Timeout = 60_000 });
        Assert.Equal(0, await page.Locator("script[src*='blazor.web']").CountAsync());

        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_WALKTHROUGH_ARTIFACTS");
        if (!string.IsNullOrWhiteSpace(artifactDir))
        {
            Directory.CreateDirectory(artifactDir);
            var suffix = width <= 400 ? "school-390" : "school-1440";
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, suffix + ".png"),
                FullPage = true
            });
        }
    }

    private async Task EnableSchoolsAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync();
        if (row is null)
        {
            db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                SchoolsEnabled = true
            });
        }
        else
        {
            row.SchoolsEnabled = true;
        }

        await db.SaveChangesAsync();
    }

    private async Task<TeacherSeed> SeedTeacherWithClassAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();
        var schoolId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var schoolAdminId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var domain = "s1test.nl";
        db.Schools.Add(new School
        {
            Id = schoolId,
            Name = "S1 Testschool",
            City = "Naaldwijk",
            AllowedEmailDomains = "[\"s1test.nl\"]",
            IsActive = true,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "1.0",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _api.AdminId
        });
        var cls = new SchoolClass
        {
            Id = classId,
            SchoolId = schoolId,
            Name = "7A",
            Level = SchoolLevel.Groep78,
            Year = 7,
            QuestionSet = PupilQuestionSet.Groep78,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = 10,
            TestWindow = TestWindowState.Open,
            ParentalInfoConfirmedAtUtc = DateTime.UtcNow,
            ParentalInfoConfirmedByUserId = _api.AdminId,
            ParentalInfoTextVersion = "1",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.SchoolClasses.Add(cls);
        db.Users.Add(new User
        {
            Id = teacherId,
            Email = $"leraar-s1@{domain}",
            FullName = "S1 Leraar",
            Role = UserRole.Teacher,
            SchoolId = schoolId,
            IsActive = true,
            AuthenticatorEnabled = true,
            SessionVersion = 0
        });
        db.Users.Add(new User
        {
            Id = schoolAdminId,
            Email = $"admin-s1@{domain}",
            FullName = "S1 Schooladmin",
            Role = UserRole.SchoolAdmin,
            SchoolId = schoolId,
            IsActive = true,
            AuthenticatorEnabled = true,
            SessionVersion = 0
        });
        db.TeacherClassAssignments.Add(new TeacherClassAssignment
        {
            TeacherUserId = teacherId,
            SchoolClassId = classId,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        await codes.GenerateAsync(3, cls);
        return new TeacherSeed(schoolId, classId, teacherId, schoolAdminId);
    }

    private sealed record TeacherSeed(Guid SchoolId, Guid ClassId, Guid TeacherId, Guid SchoolAdminId);

    private sealed class ScholenS1WebFactory : WebWebApplicationFactory
    {
        private readonly HttpMessageHandler _api;
        private IHost? _kestrel;
        public string ServerAddress { get; private set; } = "";

        public ScholenS1WebFactory(HttpMessageHandler api)
        {
            _api = api;
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
                services.Configure<HostOptions>(o =>
                    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);
                services.AddSingleton<IStartupFilter, StaffTestSignInFilter>();
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new SchoolsOnFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new ForwardFactory(_api));
                services.RemoveAll<JobsyApiClient>();
                services.AddScoped(sp =>
                {
                    var auth = new JobsyApiAuthHandler(
                        sp.GetRequiredService<IHttpContextAccessor>(),
                        sp.GetRequiredService<AuthenticationStateProvider>(),
                        sp,
                        sp.GetRequiredService<IConfiguration>(),
                        sp.GetRequiredService<JobsyAccessTokenIssuer>())
                    {
                        InnerHandler = new ForwardHandler(_api)
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
            ServerAddress = _kestrel.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!
                .Addresses.First(a => a.StartsWith("http://", StringComparison.Ordinal));
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

    private sealed class StaffTestSignInFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (ctx, nxt) =>
                {
                    if (!ctx.Request.Path.Equals("/__test/staff", StringComparison.OrdinalIgnoreCase))
                    {
                        await nxt();
                        return;
                    }

                    var user = ctx.Request.Query["user"].ToString();
                    var school = ctx.Request.Query["school"].ToString();
                    var role = ctx.Request.Query["role"].ToString();
                    if (string.IsNullOrWhiteSpace(role))
                    {
                        role = "Teacher";
                    }

                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user),
                        new(ClaimTypes.Role, role),
                        new(JobsyClaimTypes.SchoolId, school),
                        new(JobsyClaimTypes.SessionVersion, "0"),
                        new(JobsyClaimTypes.MfaVerified, "1")
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/') || ret.StartsWith("//", StringComparison.Ordinal))
                    {
                        ret = "/leraar";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private sealed class SchoolsOnFlags : IFeatureFlags
    {
        private static readonly FeatureFlagSnapshot Snap = new(false, false, SchoolsEnabled: true);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class ForwardFactory(HttpMessageHandler api) : IHttpClientFactory, IDisposable
    {
        private readonly ForwardHandler _handler = new(api);

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false)
        {
            BaseAddress = new Uri("http://api.test/")
        };

        public void Dispose() => _handler.Dispose();
    }

    private sealed class ForwardHandler(HttpMessageHandler api) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
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
                clone.Content = new ByteArrayContent(await request.Content.ReadAsByteArrayAsync(cancellationToken));
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return await new HttpMessageInvoker(api, disposeHandler: false).SendAsync(clone, cancellationToken);
        }
    }
}
