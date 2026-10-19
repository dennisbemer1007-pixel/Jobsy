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

    public IllustratedPagesWebFactory(bool emptyAppsAndLikes = false)
    {
        _emptyAppsAndLikes = emptyAppsAndLikes;
        _ = CreateClient();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var uwvOn = string.Equals(Environment.GetEnvironmentVariable("JOBSY_UWV_FEATURES"), "1", StringComparison.Ordinal)
                        || string.Equals(Environment.GetEnvironmentVariable("JOBSY_UWV_FEATURES_MOCKUP"), "1", StringComparison.Ordinal);
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "http://api.test/",
                ["Landing:ForceVariant"] = "on",
                ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem,
                ["Features:UwvFeatures"] = uwvOn ? "true" : "false"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.AddSingleton<IStartupFilter>(new IllustratedPagesStartupFilter());
            services.RemoveAll<IFeatureFlags>();
            var uwvOn = string.Equals(Environment.GetEnvironmentVariable("JOBSY_UWV_FEATURES"), "1", StringComparison.Ordinal)
                        || string.Equals(Environment.GetEnvironmentVariable("JOBSY_UWV_FEATURES_MOCKUP"), "1", StringComparison.Ordinal);
            services.AddSingleton<IFeatureFlags>(new PassportOnFlags(uwvOn));
            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(_emptyAppsAndLikes));
            services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
            services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                new HttpClient(new StubApiHandler(_emptyAppsAndLikes), disposeHandler: false)
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

    private sealed class PassportOnFlags(bool uwvFeaturesEnabled) : IFeatureFlags
    {
        private readonly FeatureFlagSnapshot _snap = new(
            EmployersEnabled: true,
            CandidatePassportEnabled: true,
            ProactiveMatchingEnabled: false,
            UwvFeaturesEnabled: uwvFeaturesEnabled);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(_snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(_snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class StubHttpClientFactory(bool emptyAppsAndLikes) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubApiHandler(emptyAppsAndLikes), disposeHandler: true)
            {
                BaseAddress = new Uri("http://api.test/")
            };
    }

    private sealed class StubApiHandler(bool emptyAppsAndLikes) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var json = IllustratedPagesApiStub.BuildJson(path, emptyAppsAndLikes);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
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
                        await ctx.Response.WriteAsync(IllustratedPagesVacancyStub.JsonForPath(path));
                        return;
                    }

                    if (path.StartsWith("/api/travel/isochrones", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Response.ContentType = "application/json; charset=utf-8";
                        await ctx.Response.WriteAsync(IllustratedPagesVacancyStub.JsonForPath(path));
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
