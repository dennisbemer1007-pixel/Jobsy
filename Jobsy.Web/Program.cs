using Jobsy.Web.Auth;
using Jobsy.Web.Components;
using Jobsy.Web.Localization;
using Jobsy.Web.Security;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddJobsyDataProtection(builder.Configuration, builder.Environment);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddJobsyAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddScoped<CultureState>();
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

var app = builder.Build();

app.UseForwardedHeaders();

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

app.UseStaticFiles();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(self)";
    await next();
});

// Apply Integraties ClientId/Secret before OIDC/Google redeem the auth code on callback.
app.UseExternalAuthCallbackCredentials();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapJobsyAuthEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
