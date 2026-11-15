using System.Net;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Jobsy.Core.Security;
using Jobsy.Web.Auth;
using Jobsy.Web.Services;
using Jobsy.Tests.TestSupport;
using Microsoft.AspNetCore.Components.Authorization;
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

namespace Jobsy.Tests.WerkgeverApk;

internal sealed class WerkgeverApkPlaywrightWebFactory : WebWebApplicationFactory
{
    private readonly HttpMessageHandler _api;
    private IHost? _kestrel;

    public string ServerAddress { get; private set; } = "";

    public WerkgeverApkPlaywrightWebFactory(HttpMessageHandler api)
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
                ["JobsyAuth:Jwt:PrivateKeyPem"] = JobsyAccessToken.DevelopmentPrivateKeyPem
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.Configure<HostOptions>(o =>
                o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);
            services.AddSingleton<IStartupFilter, WerkgeverApkStaffTestSignInFilter>();
            services.RemoveAll<IFeatureFlags>();
            services.AddSingleton<IFeatureFlags>(new WerkgeverApkTestSupport.FlagsOn());
            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(new WerkgeverApkForwardFactory(_api));
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
                    InnerHandler = new WerkgeverApkForwardHandler(_api)
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

internal sealed class WerkgeverApkStaffTestSignInFilter : IStartupFilter
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
                var role = ctx.Request.Query["role"].ToString();
                if (string.IsNullOrWhiteSpace(role))
                {
                    role = JobsyRoles.EnterpriseManager;
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user),
                    new(ClaimTypes.Role, role),
                    new(JobsyClaimTypes.SessionVersion, "0"),
                    new(JobsyClaimTypes.MfaVerified, "1")
                };
                var company = ctx.Request.Query["company"].ToString();
                if (Guid.TryParse(company, out var companyId))
                {
                    claims.Add(new Claim(JobsyClaimTypes.CompanyId, companyId.ToString("D")));
                }

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await ctx.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));
                var ret = ctx.Request.Query["return"].ToString();
                if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/') || ret.StartsWith("//", StringComparison.Ordinal))
                {
                    ret = "/werkgever/loopbaan-apk";
                }

                ctx.Response.Redirect(ret);
            });
            next(app);
        };
}

internal sealed class WerkgeverApkForwardFactory(HttpMessageHandler api) : IHttpClientFactory, IDisposable
{
    private readonly WerkgeverApkForwardHandler _handler = new(api);

    public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false)
    {
        BaseAddress = new Uri("http://api.test/")
    };

    public void Dispose() => _handler.Dispose();
}

internal sealed class WerkgeverApkForwardHandler(HttpMessageHandler api) : HttpMessageHandler
{
    private readonly HttpMessageInvoker _invoker = new(api, disposeHandler: false);

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
            clone.Content = new ByteArrayContent(await request.Content.ReadAsByteArrayAsync(cancellationToken));
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return await _invoker.SendAsync(clone, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _invoker.Dispose();
        }

        base.Dispose(disposing);
    }
}
