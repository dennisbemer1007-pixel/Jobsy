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

builder.Services.AddTransient<Jobsy.Web.Auth.TrustedClientIpHandler>();
builder.Services.AddJobsyAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<JobsyAccessTokenIssuer>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<LoginProtectionRateLimiter>();
builder.Services.AddHttpClient("JobsySessionSecurity");
builder.Services.AddHttpClient(Jobsy.Web.Auth.AuthApiClient.HttpClientName, client =>
{
    client.BaseAddress = new Uri(Jobsy.Core.JobsyPublicUrl.NormalizeBaseUrl(
        builder.Configuration["ApiBaseUrl"],
        "http://localhost:5200/"));
    client.Timeout = TimeSpan.FromSeconds(8);
}).AddHttpMessageHandler<Jobsy.Web.Auth.TrustedClientIpHandler>();
builder.Services.AddSingleton<Jobsy.Web.Auth.AuthApiClient>();
builder.Services.AddSingleton<Jobsy.Web.Security.ISessionTimeoutProvider, Jobsy.Web.Security.SessionTimeoutProvider>();
builder.Services.AddSingleton<Jobsy.Core.Features.IFeatureFlags, Jobsy.Web.Features.WebFeatureFlags>();
builder.Services.AddScoped<CultureState>();
builder.Services.AddScoped<Jobsy.Web.Werkgever.EmployerScopeState>();
builder.Services.AddScoped<Jobsy.Web.Werkgever.EmployerScopeBootstrap>();
builder.Services.AddScoped<Jobsy.Web.Werkgever.WerkgeverCountsState>();
builder.Services.AddSingleton<Jobsy.Core.Rules.KandidaatBanen.IKbDislikeSource>(
    Jobsy.Core.Rules.KandidaatBanen.KbNoDislikeSource.Instance); // KB-FALLBACK(D)
builder.Services.AddScoped<PageSeoContext>();
builder.Services.AddSingleton<Jobsy.Web.Features.IEmployersSwitch, Jobsy.Web.Features.AlwaysOnEmployersSwitch>();
builder.Services.AddScoped<Jobsy.Web.Features.LandingVariantResolver>();
builder.Services.AddSingleton<Jobsy.Web.Services.LandingStatsClient>();
builder.Services.AddSingleton<Jobsy.Web.Services.LandingPriceClient>();
builder.Services.AddSingleton<Jobsy.Web.Services.LegalIdentityProvider>();
builder.Services.AddHttpClient(Jobsy.Web.Services.LegalIdentityProvider.HttpClientName, client =>
{
    var apiBaseUrl = JobsyPublicUrl.NormalizeBaseUrl(
        builder.Configuration["ApiBaseUrl"],
        "http://localhost:5200/");
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(3);
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "LobsyWeb/1.0");
});
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
builder.Services.AddScoped<Jobsy.Web.Components.Candidate.ProfileSections.CandidateProfileEditor>();
builder.Services.AddScoped<Jobsy.Web.Services.GratisDnaStorage>();
builder.Services.AddScoped<Jobsy.Web.Services.GratisDnaMergeService>();
// Anonymous POST relay for the static /melden form; forwards the visitor IP so the
// API partitions the "report" rate limit per visitor instead of per Web instance.
builder.Services.AddHttpClient(Jobsy.Web.Hosting.ContentReportEndpoints.HttpClientName, client =>
{
    var apiBaseUrl = JobsyPublicUrl.NormalizeBaseUrl(
        builder.Configuration["ApiBaseUrl"],
        "http://localhost:5200/");
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(8);
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "LobsyWeb/1.0");
}).AddHttpMessageHandler<Jobsy.Web.Auth.TrustedClientIpHandler>();
// GET /privacy/data/export forwards to the API with a minted bearer token (07) — this is a
// plain HttpClient, not the circuit-scoped JobsyApiClient, so the download works outside Blazor.
builder.Services.AddHttpClient(Jobsy.Web.Hosting.PrivacyDataExportEndpoints.HttpClientName, client =>
{
    var apiBaseUrl = JobsyPublicUrl.NormalizeBaseUrl(
        builder.Configuration["ApiBaseUrl"],
        "http://localhost:5200/");
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "LobsyWeb/1.0");
});
// GET /partner/flyer.pdf forwards the anonymous API flyer so the share link needs no JavaScript (09).
builder.Services.AddHttpClient(Jobsy.Web.Hosting.PartnerFlyerEndpoints.HttpClientName, client =>
{
    var apiBaseUrl = JobsyPublicUrl.NormalizeBaseUrl(
        builder.Configuration["ApiBaseUrl"],
        "http://localhost:5200/");
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "LobsyWeb/1.0");
});
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

builder.Services.AddHttpClient<NominatimGeocodingClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
    client.DefaultRequestHeaders.TryAddWithoutValidation(
        "User-Agent",
        "Lobsy/1.0 (demo; contact@jobsy.local)");
    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "nl");
});
builder.Services.AddHttpClient<PdokGeocodingClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
    client.DefaultRequestHeaders.TryAddWithoutValidation(
        "User-Agent",
        "Lobsy/1.0 (demo; contact@jobsy.local)");
    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "nl");
});
builder.Services.AddScoped<IGeocodingClient, CompositeGeocodingClient>();

// Scoped (circuit) registration — do not use IHttpClientFactory + message handler here.
// That resolves AuthenticationStateProvider outside the Razor component scope.
// JobsyApiClient is IAsyncDisposable so the circuit scope disposes the HttpClient.
builder.Services.AddScoped(sp =>
    new JobsyApiClient(
        JobsyApiClientFactory.Create(sp, builder.Configuration),
        sp.GetRequiredService<Jobsy.Web.Services.MeGetCache>()));
builder.Services.AddScoped<Jobsy.Web.Services.KvkSearchClient>();
builder.Services.AddScoped<Jobsy.Web.Components.Registration.RegistrationWizardState>();
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
    options.OnRejected = async (context, token) =>
    {
        var http = context.HttpContext;
        var path = http.Request.Path.Value ?? string.Empty;
        var isAuthForm = HttpMethods.IsPost(http.Request.Method)
            && (path.Equals("/account/login", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/account/mfa/verify", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/account/mfa/herstelcodes-vernieuwen", StringComparison.OrdinalIgnoreCase));
        if (isAuthForm)
        {
            var retryAfterSeconds = 60;
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            }

            var until = DateTimeOffset.UtcNow.AddSeconds(retryAfterSeconds).ToUnixTimeSeconds();
            var target = path.Equals("/account/mfa/verify", StringComparison.OrdinalIgnoreCase)
                ? $"/account/mfa?error=too-many&until={until}"
                : path.Equals("/account/mfa/herstelcodes-vernieuwen", StringComparison.OrdinalIgnoreCase)
                    ? $"/account/mfa/herstelcodes-vernieuwen?error=too-many&until={until}"
                    : $"/login?error=too-many&until={until}";
            http.Response.StatusCode = StatusCodes.Status303SeeOther;
            http.Response.Headers.Location = target;
            return;
        }

        // The /melden form works without JS, so keep the visitor on the page.
        if (HttpMethods.IsPost(http.Request.Method)
            && path.Equals("/melden", StringComparison.OrdinalIgnoreCase))
        {
            var form = await http.Request.ReadFormAsync(token);
            var target = ContentReportEndpoints.BuildFormUrl(
                ContentReportEndpoints.NormalizeType(form["type"].ToString()),
                form["id"].ToString().Trim());
            http.Response.StatusCode = StatusCodes.Status303SeeOther;
            http.Response.Headers.Location = target + "&fout=teveel";
            return;
        }

        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await http.Response.WriteAsJsonAsync(
            new { code = "rate_limited", message = "Te veel verzoeken." },
            token);
    };
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            Jobsy.Web.Security.TrustedClientIp.PartitionKey(
                Jobsy.Web.Security.TrustedClientIp.Resolve(httpContext)),
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
    // RFC 8058 one-click unsubscribe (30/min per IP).
    options.AddPolicy("mail-unsubscribe", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    // Static /melden form POST; the API applies the stricter per-visitor "report" limit.
    options.AddPolicy("report-form", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            Jobsy.Web.Security.TrustedClientIp.PartitionKey(
                Jobsy.Web.Security.TrustedClientIp.Resolve(httpContext)),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    // GET /partner/flyer.pdf (09): PDF rendering is costly, so keep it tighter than public-read.
    options.AddPolicy("partner-flyer", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            Jobsy.Web.Security.TrustedClientIp.PartitionKey(
                Jobsy.Web.Security.TrustedClientIp.Resolve(httpContext)),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    // GET /privacy/data/export (07): per signed-in user, not per IP (shared Web→API hop).
    options.AddPolicy("export", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

if (string.IsNullOrWhiteSpace(builder.Configuration[Jobsy.Core.Security.InternalClientIpHeaders.ConfigKey])
    && !app.Environment.IsDevelopment())
{
    app.Logger.LogWarning(
        "JobsyAuth:InternalClientIpSecret is empty; auth rate limits fall back to the Web→API hop");
}


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

// HTML-only status pages (404 etc.); leave API/static/Blazor circuits alone.
app.UseWhen(
    ctx => ShouldReExecuteStatusPages(ctx),
    branch => branch.UseStatusCodePagesWithReExecute("/status/{0}"));

// Routing must run *after* the re-execute so the rewritten /status/{code} request still matches an
// endpoint. With the implicit UseRouting (at the top of the pipeline) a 404 re-executed into an
// empty body, because the cleared endpoint was never resolved again.
app.UseRouting();

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
app.UseLegacyAuthRouteRedirects();
app.UseLoginProtection();
app.UseDeviceSessionRefresh();
app.UseSessionInactivity();
app.UseAdminProviderGuard();
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
app.MapPublicTokenEndpoints();
app.MapMailUnsubscribeEndpoints();
app.MapContentReportEndpoints();
app.MapPrivacyDataExportEndpoints();
app.MapPartnerFlyerEndpoints();
app.MapMailSettingsEndpoints();
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

static bool ShouldReExecuteStatusPages(HttpContext ctx)
{
    var path = ctx.Request.Path.Value ?? string.Empty;
    if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/healthz", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    var lastSlash = path.LastIndexOf('/');
    var file = lastSlash >= 0 ? path[(lastSlash + 1)..] : path;
    if (file.Contains('.', StringComparison.Ordinal))
    {
        return false;
    }

    var accept = ctx.Request.Headers.Accept.ToString();
    return string.IsNullOrEmpty(accept)
           || accept.Contains("text/html", StringComparison.OrdinalIgnoreCase)
           || accept.Contains("*/*", StringComparison.OrdinalIgnoreCase);
}
