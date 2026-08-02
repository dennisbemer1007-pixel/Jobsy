using System.Threading.RateLimiting;
using Jobsy.Api;
using Jobsy.Api.Authorization;
using Jobsy.Api.Jobs;
using Jobsy.Api.Swagger;
using Jobsy.Core;
using Jobsy.Infrastructure;
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

Jobsy.Core.Security.VerificationCodes.ConfigurePepper(
    builder.Configuration["VerificationCodes:Pepper"]);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    // Render / reverse proxies; trust edge headers for HTTPS cookies and redirects.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(ExternalApiSwagger.Configure);
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
    options.AddPolicy("public-write", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    // Stricter bucket for OTP verification guesses (apply + unsubscribe confirm).
    // Prefer authenticated user id when present so forged X-Forwarded-For cannot bypass limits alone.
    options.AddPolicy("otp-verify", httpContext =>
    {
        var userKey = httpContext.User?.Identity?.IsAuthenticated == true
            ? httpContext.User.FindFirst("sub")?.Value
              ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
              ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            : null;
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
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
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    // Partner PDF flyer generation (QuestPDF) — tighter than generic public-write.
    options.AddPolicy("public-pdf", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 12,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

app.UseForwardedHeaders();
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

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    // Swagger UI needs inline script/style; keep API responses locked down.
    var path = context.Request.Path.Value ?? string.Empty;
    context.Response.Headers["Content-Security-Policy"] =
        path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            ? "default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; img-src 'self' data:; frame-ancestors 'none'; base-uri 'self'"
            : "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
    await next();
});

// Partner docs for the external vacancy API (X-API-Key). Scoped to /api/external/vacancies.
// Default: on in Development, off elsewhere. Override with Swagger:Enabled=true|false.
var swaggerEnabled = builder.Configuration.GetValue<bool?>("Swagger:Enabled")
    ?? builder.Environment.IsDevelopment();
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint($"/swagger/{ExternalApiSwagger.DocumentName}/swagger.json", "Lobsy externe vacature-API");
        options.DocumentTitle = "Lobsy API · Swagger";
        options.RoutePrefix = "swagger";
        if (!app.Environment.IsDevelopment())
        {
            // Reduce abuse of live "Try it out" against production.
            options.SupportedSubmitMethods();
        }
    });
}

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
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

app.MapControllers();

app.Run();

public partial class Program;
