using System.Threading.RateLimiting;
using Jobsy.Api;
using Jobsy.Api.Authorization;
using Jobsy.Api.Hosting;
using Jobsy.Api.Jobs;
using Jobsy.Api.Ops;
using Jobsy.Api.Security;
using Jobsy.Api.Swagger;
using Jobsy.Core;
using Jobsy.Core.Options;
using Jobsy.Core.Security;
using Jobsy.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;

if (args.Length > 0 && args[0] == "test-accounts")
{
    Environment.ExitCode = await TestAccountsCommand.RunAsync(args[1..]);
    return;
}

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

var otpPepper = builder.Configuration["VerificationCodes:Pepper"];
if (!builder.Environment.IsDevelopment()
    && string.IsNullOrWhiteSpace(otpPepper))
{
    throw new InvalidOperationException(
        "VerificationCodes:Pepper is required outside Development. " +
        "Set VerificationCodes__Pepper to a long random secret per environment.");
}

Jobsy.Core.Security.VerificationCodes.ConfigurePepper(otpPepper);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Cloudflare → Render: two hops. Loopback-only KnownProxies would leave Scheme=http
    // and break Secure cookies. Client IP is still overridden from CF-Connecting-IP only
    // after CloudflareOriginMiddleware validates the origin secret.
    options.ForwardLimit = 2;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddJobsyApiPerformance();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<MfaChallengeService>();
builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var key = JobsyLocalSessionToken.ResolveSigningKey(
        cfg["JobsyAuth:LocalSessionSigningKey"],
        cfg["JobsyAuth:DevelopmentAuthSecret"]);
    return new UnknownAccountLockoutTracker(key);
});
builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var key = JobsyLocalSessionToken.ResolveSigningKey(
        cfg["JobsyAuth:LocalSessionSigningKey"],
        cfg["JobsyAuth:DevelopmentAuthSecret"]);
    return new PasswordResetRequestLimiter(key);
});
builder.Services.AddScoped<Jobsy.Core.Interfaces.ITotpVerifier, Jobsy.Infrastructure.Security.TotpVerifier>();
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new Jobsy.Core.Hosting.DeploymentEnvironmentLabel(
        Jobsy.Core.Hosting.DeploymentEnvironment.Resolve(
            config["PublicWebBaseUrl"],
            config["Deployment:Label"]));
});
builder.Services.AddSingleton<LoginProtectionRateLimiter>();
builder.Services.AddScoped<Jobsy.Api.Admin.IAdminAuditContext, Jobsy.Api.Admin.AdminAuditContext>();
builder.Services.AddScoped<Jobsy.Api.Admin.AdminAuditFilter>();
builder.Services.AddScoped<Jobsy.Api.Filters.FeatureGateFilter>();
builder.Services.AddScoped<Jobsy.Api.Passport.PassportPdfDownload>();
builder.Services.AddControllers(options =>
    {
        options.Filters.AddService<Jobsy.Api.Admin.AdminAuditFilter>();
        options.Filters.AddService<Jobsy.Api.Filters.FeatureGateFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddOpenApi(ExternalApiOpenApi.DocumentName, ExternalApiOpenApi.Configure);
builder.Services.Configure<Golf2WestlandOptions>(
    builder.Configuration.GetSection(Golf2WestlandOptions.SectionName));
builder.Services.AddScoped<Jobsy.Infrastructure.Services.WestlandPilotReportingService>();
builder.Services.AddScoped<Jobsy.Infrastructure.Services.Golf2CandidateService>();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddJobsyApiAuthorization(builder.Configuration, builder.Environment);
builder.Services.AddHostedService<DatabaseSeedHostedService>();
builder.Services.AddHostedService<MinimumWageUpdateHostedService>();

var allowedOrigins = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5201", "https://localhost:5201"])
    .Select(JobsyPublicUrl.NormalizeOrigin)
    .Where(o => !string.IsNullOrWhiteSpace(o))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("JobsyWeb", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddRateLimiter(options =>
{
    var isProduction = builder.Environment.IsProduction();
    var publicReadLimit = builder.Configuration.GetValue<int?>("RateLimiting:PublicReadPermitLimit")
        ?? (isProduction ? 120 : 10_000);
    var publicTravelLimit = builder.Configuration.GetValue<int?>("RateLimiting:PublicTravelPermitLimit")
        ?? (isProduction ? 30 : 10_000);
    var publicWriteLimit = builder.Configuration.GetValue<int?>("RateLimiting:PublicWritePermitLimit")
        ?? 60;
    var pupilLimit = builder.Configuration.GetValue<int?>("RateLimiting:PupilPermitLimit")
        ?? (isProduction ? 60 : 10_000);
    var kvkSearchLimit = builder.Configuration.GetValue<int?>("RateLimiting:KvkSearchPermitLimit")
        ?? 30;
    var registrationSubmitLimit = builder.Configuration.GetValue<int?>("RateLimiting:RegistrationSubmitPermitLimit")
        ?? 5;
    var verifyStartLimit = builder.Configuration.GetValue<int?>("RateLimiting:VerifyStartPermitLimit")
        ?? 5;
    var accessRequestLimit = builder.Configuration.GetValue<int?>("RateLimiting:AccessRequestPermitLimit")
        ?? 5;
    var reportHourlyLimit = builder.Configuration.GetValue<int?>("RateLimiting:ReportHourlyPermitLimit")
        ?? 5;
    var reportDailyLimit = builder.Configuration.GetValue<int?>("RateLimiting:ReportDailyPermitLimit")
        ?? 20;
    var internalClientIpSecret = builder.Configuration[RateLimitPartitioning.ConfigKey];

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = RateLimitPartitioning.OnRejectedAsync;

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitioning.ResolvePartitionKey(httpContext, internalClientIpSecret),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("public-write", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitioning.ResolvePartitionKey(httpContext, internalClientIpSecret),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = publicWriteLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("kvk-search", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitioning.ResolvePartitionKey(httpContext, internalClientIpSecret),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = kvkSearchLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("registration-submit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitioning.ResolvePartitionKey(httpContext, internalClientIpSecret),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = registrationSubmitLimit,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            }));
    options.AddPolicy("verify-start", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? RateLimitPartitioning.ResolvePartitionKey(httpContext, internalClientIpSecret),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = verifyStartLimit,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            }));
    options.AddPolicy("access-request", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitioning.ResolvePartitionKey(httpContext, internalClientIpSecret),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = accessRequestLimit,
                Window = TimeSpan.FromDays(1),
                QueueLimit = 0
            }));
    // DSA report form: 5 per hour and 20 per day per visitor. The IP is only a partition key,
    // never stored with the report (06).
    options.AddPolicy("report", httpContext =>
        RateLimitPartition.Get(
            RateLimitPartitioning.ResolvePartitionKey(httpContext, internalClientIpSecret),
            _ => new DualWindowRateLimiter(
                reportHourlyLimit,
                TimeSpan.FromHours(1),
                reportDailyLimit,
                TimeSpan.FromDays(1))));
    options.AddPolicy("public-read", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitioning.ResolvePartitionKey(httpContext, internalClientIpSecret),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = publicReadLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("public-travel", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitioning.ResolvePartitionKey(httpContext, internalClientIpSecret),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = publicTravelLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    // Stricter bucket for OTP verification guesses (apply + unsubscribe confirm).
    // Prefer authenticated user id when present so forged X-Forwarded-For cannot bypass limits alone.
    options.AddPolicy("otp-verify", httpContext =>
    {
        // User is always present on HttpContext; capture once for null-flow analysis (CS8602).
        var user = httpContext.User;
        var userKey = user.Identity?.IsAuthenticated == true
            ? user.FindFirst("sub")?.Value
              ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
              ?? user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            : null;
        var ip = user.FindFirst(JobsyAccessToken.ClientIpClaim)?.Value
                 ?? httpContext.Connection.RemoteIpAddress?.ToString()
                 ?? "unknown";
        var partition = string.IsNullOrWhiteSpace(userKey) ? $"ip:{ip}" : $"user:{userKey}|ip:{ip}";
        return RateLimitPartition.GetFixedWindowLimiter(
            partition,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
    options.AddPolicy("ai", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitioning.ResolvePartitionKey(httpContext, internalClientIpSecret),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    // Coach chats are one request per message. 30 messages must not hit the shared AI cap of 10/min.
    options.AddPolicy("assistant", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitioning.ResolvePartitionKey(httpContext, internalClientIpSecret),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 40,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));
    options.AddPolicy("feedback-write", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitioning.ResolvePartitionKey(httpContext, internalClientIpSecret),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    // GET api/privacy/export: per signed-in user, same budget as the web download.
    options.AddPolicy("export", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitioning.ResolvePartitionKey(httpContext, internalClientIpSecret),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            }));
    // Partner PDF flyer generation (QuestPDF) — tighter than generic public-write.
    options.AddPolicy("public-pdf", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitioning.ResolvePartitionKey(httpContext, internalClientIpSecret),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 12,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    // Global pupil API partition: 60 req/min per IP (HMAC partition in production).
    options.AddPolicy("pupil", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitioning.ResolvePartitionKey(httpContext, internalClientIpSecret),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = pupilLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromSeconds(JobsyHsts.MaxAgeSeconds);
    options.IncludeSubDomains = true;
    options.Preload = false;
});

var app = builder.Build();
Jobsy.Core.Careers.OccupationCatalog.Configure(
    app.Configuration.GetValue(Jobsy.Core.Careers.OccupationCatalog.PreviewConfigKey, false),
    app.Environment.IsProduction());
Jobsy.Core.Reports.DeepReportCatalog.Logger =
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Jobsy.DeepReportCatalog");

if (string.IsNullOrWhiteSpace(builder.Configuration[RateLimitPartitioning.ConfigKey])
    && !app.Environment.IsDevelopment())
{
    app.Logger.LogWarning(
        "JobsyAuth:InternalClientIpSecret is empty; auth rate limits fall back to the Web→API hop");
}


app.UseForwardedHeaders();
app.UseResponseCompression();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Short-circuit before auth/HTTPS so Render probes always get 200 once Kestrel listens.
app.Use(async (context, next) =>
{
    if (HttpMethods.IsGet(context.Request.Method)
        && context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync("""{"status":"ok"}""", context.RequestAborted);
        return;
    }

    await next();
});

// After /health short-circuit: Production traffic must come via Cloudflare Transform Rule.
app.UseMiddleware<CloudflareOriginMiddleware>();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    // Scalar/OpenAPI UI needs inline script/style; keep API responses locked down.
    var path = context.Request.Path.Value ?? string.Empty;
    var openApiUi = path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase);
    context.Response.Headers["Content-Security-Policy"] =
        openApiUi
            ? "default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; img-src 'self' data:; frame-ancestors 'none'; base-uri 'self'"
            : "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
else
{
    // Render terminates TLS at the edge; do not redirect internal HTTP probes to https://{Host}/health
    // (Host may be a custom domain that points at the web service, not this API).
    app.UseHsts();
}

app.UseCors("JobsyWeb");
// Outside Development, refuse OpenAPI/Scalar/legacy Swagger with a hard 404 (code-health 08b).
if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next();
    });
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseLoginProtection();
app.UseAuthorization();
// After auth so the admin bypass can read the principal (errors 05).
app.UseMiddleware<Jobsy.Api.Security.MaintenanceApiMiddleware>();
app.UseMiddleware<Jobsy.Api.Security.SchoolsFeatureMiddleware>();
app.UseMiddleware<Jobsy.Api.Middleware.TestAccountScopeMiddleware>();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    commit = Environment.GetEnvironmentVariable("RENDER_GIT_COMMIT")
            ?? Environment.GetEnvironmentVariable("GIT_COMMIT")
            ?? "local"
}))
    .AllowAnonymous();

// Partner OpenAPI for the external vacancy API (X-API-Key). Development only —
// Acceptatie/Production must 404 (code-health 08b). Self-hosted Scalar UI, no CDN.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Lobsy API · OpenAPI");
        options.WithOpenApiRoutePattern($"/openapi/{ExternalApiOpenApi.DocumentName}.json");
    }).AllowAnonymous();
}

app.MapControllers();

app.Run();

// Entry-point type stays compiler-generated. Tests host via Jobsy.Api.ApiAssemblyMarker
// (not bare Program) because Jobsy.Web also emits a public Program on .NET 10+.
