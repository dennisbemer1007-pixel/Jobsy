using System.Threading.RateLimiting;
using Jobsy.Core;
using Jobsy.Core.Security;
using Jobsy.Web.Auth;
using Jobsy.Web.Components;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Security;
using Jobsy.Web.Seo;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var sentryDsn = builder.Configuration["Sentry:Dsn"];
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.SendDefaultPii = false;
        options.TracesSampleRate = 0;
        options.Environment = builder.Environment.EnvironmentName;
    });
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Cloudflare → Render is two hops; default KnownProxies (loopback-only) ignores both.
    // Clear so X-Forwarded-Proto reaches Kestrel (Secure cookies / no redirect loops).
    // Client IP still comes from CF-Connecting-IP only after CloudflareOriginMiddleware
    // validates the origin secret — do not treat X-Forwarded-For as authoritative alone.
    options.ForwardLimit = 2;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddJobsyDataProtection(builder.Configuration, builder.Environment);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        // Page screenshots travel through JS interop (data URL). Default 32 KB is too small.
        options.MaximumReceiveMessageSize = 2 * 1024 * 1024;
        // Mobile networks / brief background pauses: keep the circuit warmer than defaults.
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    });

var circuitDetailedErrors = builder.Environment.IsDevelopment()
    || builder.Configuration.GetValue<bool>("Circuit:DetailedErrors");
builder.Services.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(options =>
{
    // DetailedErrors only in Development and Acceptatie (Circuit__DetailedErrors=true).
    options.DetailedErrors = circuitDetailedErrors;
    // Acc 27-09 §4: keep backgrounded mobile tabs longer; cap retained circuits on Starter RAM.
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(15);
    options.DisconnectedCircuitMaxRetained = 100;
});
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler, Jobsy.Web.Hosting.CircuitExceptionLogger>();

builder.Services.AddJobsyAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<JobsyAccessTokenIssuer>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<LoginProtectionRateLimiter>();
builder.Services.AddHttpClient("JobsySessionSecurity");
builder.Services.AddSingleton<Jobsy.Web.Security.ISessionTimeoutProvider, Jobsy.Web.Security.SessionTimeoutProvider>();
builder.Services.AddScoped<CultureState>();
builder.Services.AddScoped<Jobsy.Web.Werkgever.EmployerScopeState>();
builder.Services.AddScoped<Jobsy.Web.Werkgever.EmployerScopeBootstrap>();
builder.Services.AddScoped<Jobsy.Web.Werkgever.WerkgeverCountsState>();
builder.Services.AddScoped<PageSeoContext>();
builder.Services.AddSingleton<Jobsy.Web.Features.IEmployersSwitch, Jobsy.Web.Features.AlwaysOnEmployersSwitch>();
builder.Services.AddScoped<Jobsy.Web.Features.LandingVariantResolver>();
builder.Services.AddSingleton<Jobsy.Web.Services.LandingStatsClient>();
builder.Services.AddSingleton<Jobsy.Web.Services.LandingPriceClient>();
builder.Services.AddHttpClient(Jobsy.Web.Services.LandingStatsClient.HttpClientName, client =>
{
    var apiBaseUrl = JobsyPublicUrl.NormalizeBaseUrl(
        builder.Configuration["ApiBaseUrl"],
        "http://localhost:5200/");
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMilliseconds(400);
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "LobsyLanding/1.0");
});
builder.Services.AddHttpClient(Jobsy.Web.Services.LandingPriceClient.HttpClientName, client =>
{
    var apiBaseUrl = JobsyPublicUrl.NormalizeBaseUrl(
        builder.Configuration["ApiBaseUrl"],
        "http://localhost:5200/");
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMilliseconds(400);
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "LobsyLanding/1.0");
});
builder.Services.AddSingleton<Jobsy.Web.Services.ICookieConsentTokenService, Jobsy.Web.Services.CookieConsentTokenService>();
builder.Services.AddScoped<Jobsy.Web.RegionHosting.RegionHostState>();
builder.Services.AddScoped<Jobsy.Web.Branding.PlatformBrandingState>();
builder.Services.AddScoped<TokenBalanceCache>();
builder.Services.AddScoped<Jobsy.Web.Services.MeGetCache>();
builder.Services.AddScoped<Jobsy.Web.Services.ApiCallTracker>();
builder.Services.AddScoped<Jobsy.Web.Services.NotificationUnreadStore>();
builder.Services.AddScoped<Jobsy.Web.Navigation.BottomNavRefreshService>();
builder.Services.AddScoped<Jobsy.Web.Navigation.AssistantChatHost>();
builder.Services.AddScoped<Jobsy.Web.Navigation.FeedbackHost>();
builder.Services.AddScoped<Jobsy.Web.Components.Admin.Shell.AdminSidebarState>();
builder.Services.AddScoped<Jobsy.Web.Components.Admin.Shell.AdminTodoCountsStore>();
builder.Services.AddScoped<Jobsy.Web.Components.Admin.Shell.AdminTodoChanged>();
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new Jobsy.Core.Hosting.DeploymentEnvironmentLabel(
        Jobsy.Core.Hosting.DeploymentEnvironment.Resolve(
            config["PublicWebBaseUrl"],
            config["Deployment:Label"]));
});
builder.Services.AddScoped<Jobsy.Web.Services.CandidateMatchProfileService>();
builder.Services.AddScoped<Jobsy.Web.Services.MatchVacancyService>();
builder.Services.AddScoped<Jobsy.Web.Services.CareerPathService>();
builder.Services.AddScoped<Jobsy.Web.Services.CandidateProfileService>();
builder.Services.AddScoped<Jobsy.Web.Services.GratisDnaStorage>();
builder.Services.AddScoped<Jobsy.Web.Services.GratisDnaMergeService>();
builder.Services.AddHttpClient("JobsySeo", client =>
{
    var apiBaseUrl = JobsyPublicUrl.NormalizeBaseUrl(
        builder.Configuration["ApiBaseUrl"],
        "http://localhost:5200/");
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(8);
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "LobsySeo/1.0");
});

// Anonymous, no auth handler — header slogan must not share the circuit API client.
builder.Services.AddHttpClient(Jobsy.Web.Branding.PlatformBrandingState.HttpClientName, client =>
{
    var apiBaseUrl = JobsyPublicUrl.NormalizeBaseUrl(
        builder.Configuration["ApiBaseUrl"],
        "http://localhost:5200/");
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(3);
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "LobsyWeb/1.0");
});

builder.Services.AddHttpClient<IGeocodingClient, NominatimGeocodingClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
    client.DefaultRequestHeaders.TryAddWithoutValidation(
        "User-Agent",
        "Lobsy/1.0 (demo; contact@jobsy.local)");
    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "nl");
});

// Scoped (circuit) registration — do not use IHttpClientFactory + message handler here.
// That resolves AuthenticationStateProvider outside the Razor component scope.
// JobsyApiClient is IAsyncDisposable so the circuit scope disposes the HttpClient.
builder.Services.AddScoped(sp =>
    new JobsyApiClient(
        JobsyApiClientFactory.Create(sp, builder.Configuration),
        sp.GetRequiredService<Jobsy.Web.Services.MeGetCache>()));
builder.Services.AddScoped<IVacancyMapApiForwarder, VacancyMapApiForwarder>();

builder.Services.AddJobsyWebPerformance();

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromSeconds(JobsyHsts.MaxAgeSeconds);
    options.IncludeSubDomains = true;
    options.Preload = false;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("pupil-login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    // Short referral redirects (/p/{code}) — same envelope as API public-write.
    options.AddPolicy("public-redirect", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// Rewrite HEAD→GET before routing so MapRazorComponents (GET-only) does not 405.
app.UseMiddleware<HeadAsGetMiddleware>();
app.UseForwardedHeaders();
app.UseMiddleware<CloudflareOriginMiddleware>();
app.UseWebSockets(new WebSocketOptions
{
    // Keep the Blazor circuit alive through Cloudflare/Render idle proxies.
    KeepAliveInterval = TimeSpan.FromSeconds(15)
});
app.UseMiddleware<WwwCanonicalMiddleware>();
app.UseAdminLegacyRedirects();
app.UseResponseCompression();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Render terminates TLS at the edge; keep local HTTPS redirect for Development only.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles(WebPerformanceExtensions.JobsyStaticFiles());
app.UseMiddleware<VersionedAssetCacheMiddleware>();

// MapLibre is self-hosted — OpenFreeMap tiles/styles load over HTTPS; no unpkg.
// Scripts use a per-request nonce (no script-src 'unsafe-inline').
app.UseMiddleware<SecurityHeadersMiddleware>();

// Apply Integraties ClientId/Secret before OIDC/Google redeem the auth code on callback.
app.UseExternalAuthCallbackCredentials();

app.UseAuthentication();
app.UseRateLimiter();
app.UseLoginProtection();
app.UseDeviceSessionRefresh();
app.UseSessionInactivity();
app.UseAuthorization();
app.UseMiddleware<SchoolsFeatureMiddleware>();
app.UseMiddleware<SalesLegacyRoutesMiddleware>();
app.UseMiddleware<AmbassadorsFeatureMiddleware>();
app.UseMfaEnforcement();
app.UseAntiforgery();
app.UseRegisterOntdekRedirect();
app.UseBanenRedirect();
app.UseLandingRedirect();

// Legacy /employer|/branch|/regional → /werkgever (GET/HEAD 301). Needs auth for /home.
app.UseMiddleware<Jobsy.Web.Middleware.WerkgeverLegacyRedirectMiddleware>();

app.MapJobsyAuthEndpoints();
app.MapPupilAuthEndpoints();
app.MapLanguageEndpoints();
app.MapCookieConsentEndpoints();
app.MapSeoEndpoints();
app.MapSalesReferralEndpoints();
// Lightweight probe for Render — no auth, no prerender, no API client.
app.MapGet("/healthz", () => Results.Text("ok"));
// Banenkaart same-origin API proxies — must be before MapRazorComponents.
app.MapVacancyMapProxyEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(o =>
    {
        // CSP is issued once by SecurityHeadersMiddleware (frame-ancestors 'none').
        // A second Blazor CSP header makes Observatory treat script-src as unrestricted.
        o.ContentSecurityFrameAncestorsPolicy = null;
    });

app.Run();
