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
    options.ForwardLimit = 1;
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
    });

builder.Services.AddJobsyAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("JobsySessionSecurity");
builder.Services.AddSingleton<Jobsy.Web.Security.ISessionTimeoutProvider, Jobsy.Web.Security.SessionTimeoutProvider>();
builder.Services.AddScoped<CultureState>();
builder.Services.AddScoped<PageSeoContext>();
builder.Services.AddScoped<Jobsy.Web.RegionHosting.RegionHostState>();
builder.Services.AddScoped<Jobsy.Web.Branding.PlatformBrandingState>();
builder.Services.AddScoped<TokenBalanceCache>();
builder.Services.AddScoped<Jobsy.Web.Navigation.BottomNavRefreshService>();
builder.Services.AddHttpClient("JobsySeo", client =>
{
    var apiBaseUrl = JobsyPublicUrl.NormalizeBaseUrl(
        builder.Configuration["ApiBaseUrl"],
        "http://localhost:5200/");
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(8);
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "LobsySeo/1.0");
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
    new JobsyApiClient(JobsyApiClientFactory.Create(sp, builder.Configuration)));

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
});

var app = builder.Build();

// Rewrite HEAD→GET before routing so MapRazorComponents (GET-only) does not 405.
app.UseMiddleware<HeadAsGetMiddleware>();
app.UseForwardedHeaders();
app.UseWebSockets(new WebSocketOptions
{
    // Keep the Blazor circuit alive through Cloudflare/Render idle proxies.
    KeepAliveInterval = TimeSpan.FromSeconds(15)
});
app.UseMiddleware<WwwCanonicalMiddleware>();
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

// MapLibre is self-hosted — OpenFreeMap tiles/styles load over HTTPS; no unpkg.
// Scripts use a per-request nonce (no script-src 'unsafe-inline').
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseRateLimiter();

// Apply Integraties ClientId/Secret before OIDC/Google redeem the auth code on callback.
app.UseExternalAuthCallbackCredentials();

app.UseAuthentication();
app.UseSessionInactivity();
app.UseAuthorization();
app.UseAntiforgery();

app.MapJobsyAuthEndpoints();
app.MapSeoEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(o =>
    {
        // CSP is issued once by SecurityHeadersMiddleware (frame-ancestors 'none').
        // A second Blazor CSP header makes Observatory treat script-src as unrestricted.
        o.ContentSecurityFrameAncestorsPolicy = null;
    });

app.Run();
