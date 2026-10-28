using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Jobsy.Web.UwvFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;

namespace Jobsy.Tests;

/// <summary>Kestrel host for illustrated page captures with stubbed API and optional test sign-in.</summary>
public sealed class IllustratedPagesWebFactory : WebWebApplicationFactory
{
    public string ServerAddress { get; private set; } = "";
    private IHost? _kestrel;
    private readonly bool _emptyAppsAndLikes;
    private readonly bool _minimalPassportProfile;
    private readonly bool _companyMapInitiativeEnabled;

    public IllustratedPagesWebFactory(
        bool emptyAppsAndLikes = false,
        bool minimalPassportProfile = false,
        bool companyMapInitiativeEnabled = true)
    {
        _emptyAppsAndLikes = emptyAppsAndLikes;
        _minimalPassportProfile = minimalPassportProfile;
        _companyMapInitiativeEnabled = companyMapInitiativeEnabled;
        _ = CreateClient();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var uwvOn = ResolveUwvFeaturesForIllustratedTests();
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "http://api.test/",
                ["Landing:ForceVariant"] = "on",
                ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem,
                ["Jobsy:CandidateCompactHeroEnabled"] = "true",
                ["Features:UwvFeatures"] = uwvOn ? "true" : "false",
                ["JobsyFeatures:BedrijvenkaartSsrPreview"] = "true"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.AddSingleton<IStartupFilter>(new IllustratedPagesStartupFilter());
            services.RemoveAll<IFeatureFlags>();
            var uwvOn = ResolveUwvFeaturesForIllustratedTests();
            services.AddSingleton<IFeatureFlags>(new PassportOnFlags(uwvOn, _companyMapInitiativeEnabled));
            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(_emptyAppsAndLikes, _minimalPassportProfile));
            services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
            services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                new HttpClient(new StubApiHandler(_emptyAppsAndLikes, _minimalPassportProfile), disposeHandler: false)
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

    /// <summary>Default ON; set <c>JOBSY_UWV_FEATURES=0</c> or <c>JOBSY_UWV_FEATURES_MOCKUP=0</c> to disable in illustrated e2e.</summary>
    private static bool ResolveUwvFeaturesForIllustratedTests()
    {
        if (IsUwvFeaturesEnvDisabled("JOBSY_UWV_FEATURES")
            || IsUwvFeaturesEnvDisabled("JOBSY_UWV_FEATURES_MOCKUP"))
        {
            return false;
        }

        return true;
    }

    private static bool IsUwvFeaturesEnvDisabled(string name)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        return string.Equals(raw, "0", StringComparison.Ordinal)
               || string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class PassportOnFlags(bool uwvFeaturesEnabled, bool companyMapInitiativeEnabled) : IFeatureFlags
    {
        private readonly FeatureFlagSnapshot _snap = new(
            EmployersEnabled: true,
            CandidatePassportEnabled: true,
            CandidateExternalVacanciesEnabled: true,
            ProactiveMatchingEnabled: false,
            CompanyMapInitiativeEnabled: companyMapInitiativeEnabled,
            UwvFeaturesEnabled: uwvFeaturesEnabled);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(_snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(_snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class StubHttpClientFactory(bool emptyAppsAndLikes, bool minimalPassportProfile) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubApiHandler(emptyAppsAndLikes, minimalPassportProfile), disposeHandler: true)
            {
                BaseAddress = new Uri("http://api.test/")
            };
    }

    private sealed class StubApiHandler(bool emptyAppsAndLikes, bool minimalPassportProfile) : HttpMessageHandler
    {
        private static readonly HashSet<Guid> LikedVacancyIds = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var pathAndQuery = path + (request.RequestUri?.Query ?? "");
            if (path.StartsWith("/api/vacancies/", StringComparison.OrdinalIgnoreCase)
                && path.EndsWith("/like", StringComparison.OrdinalIgnoreCase))
            {
                var idPart = path["/api/vacancies/".Length..^"/like".Length].Trim('/');
                _ = Guid.TryParse(idPart, out var vacancyId);
                if (vacancyId != Guid.Empty)
                {
                    if (request.Method == HttpMethod.Post)
                    {
                        LikedVacancyIds.Add(vacancyId);
                    }
                    else if (request.Method == HttpMethod.Delete)
                    {
                        LikedVacancyIds.Remove(vacancyId);
                    }
                }

                var liked = vacancyId != Guid.Empty && LikedVacancyIds.Contains(vacancyId);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        $"{{\"liked\":{(liked ? "true" : "false")},\"Liked\":{(liked ? "true" : "false")}}}",
                        System.Text.Encoding.UTF8,
                        "application/json")
                };
            }
            if (Environment.GetEnvironmentVariable("JOBSY_WALKTHROUGH_SLOW_CORE") == "1"
                && path.Contains("kompas", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(12_000, cancellationToken);
            }

            if (Environment.GetEnvironmentVariable("JOBSY_WALKTHROUGH_SLOW_SECONDARY") == "1"
                && path.Contains("/api/me/", StringComparison.OrdinalIgnoreCase)
                && !path.Contains("kompas", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(20_000, cancellationToken);
            }

            if (path.Contains("/api/uwv/adviseur-share/", StringComparison.OrdinalIgnoreCase)
                && !path.Contains("uwv-share-demo-token", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
            }

            if (path.StartsWith("/api/candidate/external-vacancies/", StringComparison.OrdinalIgnoreCase)
                && request.Method == HttpMethod.Get
                && path.Count(c => c == '/') >= 4)
            {
                var idPart = path["/api/candidate/external-vacancies/".Length..].Trim('/');
                if (Guid.TryParse(idPart, out var extId)
                    && extId == CandidateExternalVacancyDetailHttpTests.PurgedExternalId)
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
                }
            }

            var json = IllustratedPagesApiStub.BuildJson(pathAndQuery, emptyAppsAndLikes, minimalPassportProfile);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class IllustratedPagesStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (ctx, nxt) =>
                {
                    var path = ctx.Request.Path.Value ?? "";
                    if (HttpMethods.IsGet(ctx.Request.Method)
                        && IllustratedPagesApiStub.ShouldStubVacancyCatalog(path))
                    {
                        if (path.Contains("/map-popup-photo", StringComparison.OrdinalIgnoreCase))
                        {
                            var bytes = IllustratedPagesVacancyStub.MapPopupPhotoBytes(
                                path,
                                ctx.Request.QueryString.Value);
                            if (bytes is { Length: > 0 })
                            {
                                ctx.Response.ContentType = "image/webp";
                                ctx.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
                                await ctx.Response.Body.WriteAsync(bytes);
                                return;
                            }

                            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                            return;
                        }

                        if (path.EndsWith("/image", StringComparison.OrdinalIgnoreCase))
                        {
                            var thumb = IllustratedPagesVacancyStub.ThumbnailForImageRequest(path);
                            if (thumb is not null)
                            {
                                ctx.Response.Redirect(thumb);
                                return;
                            }

                            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                            return;
                        }

                        ctx.Response.ContentType = "application/json; charset=utf-8";
                        var pathAndQuery = path + ctx.Request.QueryString;
                        await ctx.Response.WriteAsync(IllustratedPagesVacancyStub.JsonForPath(pathAndQuery));
                        return;
                    }

                    if (path.StartsWith("/api/banenkaart-client", StringComparison.OrdinalIgnoreCase))
                    {
                        // Catalog/pins BFF adds ETag + cache headers; do not short-circuit with raw stub JSON.
                        if (path.Equals("/api/banenkaart-client/all", StringComparison.OrdinalIgnoreCase)
                            || path.Equals("/api/banenkaart-client/pins", StringComparison.OrdinalIgnoreCase))
                        {
                            await nxt();
                            return;
                        }

                        ctx.Response.ContentType = "application/json; charset=utf-8";
                        await ctx.Response.WriteAsync(IllustratedPagesVacancyStub.JsonForPath(path));
                        return;
                    }

                    if (path.StartsWith("/api/travel/isochrones", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Response.ContentType = "application/json; charset=utf-8";
                        var isoPath = path + ctx.Request.QueryString;
                        await ctx.Response.WriteAsync(IllustratedPagesVacancyStub.JsonForPath(isoPath));
                        return;
                    }

                    if (path.Equals("/map/basemap/liberty/style.json", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Response.Redirect("https://tiles.openfreemap.org/styles/liberty");
                        return;
                    }

                    if (!ctx.Request.Path.Equals("/__test/sign-in", StringComparison.OrdinalIgnoreCase))
                    {
                        await nxt();
                        return;
                    }

                    var roleParam = ctx.Request.Query["role"].ToString();
                    var (role, displayName) = roleParam.ToLowerInvariant() switch
                    {
                        "employer" or "werkgever" => ("BranchManager", "Ondernemer Demo"),
                        "admin" => ("Admin", "Admin Demo"),
                        _ => ("Candidate", "Sanne de Vries")
                    };

                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new(ClaimTypes.Role, role),
                        new(ClaimTypes.Name, displayName),
                        new(JobsyClaimTypes.MfaVerified, "1")
                    };

                    if (role == "BranchManager")
                    {
                        claims.Add(new Claim(JobsyClaimTypes.CompanyIds, IllustratedPagesApiStub.SampleCompanyId.ToString("D")));
                    }

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = role == "BranchManager" ? "/werkgever/sollicitaties" : "/candidate/paspoort";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }
}
