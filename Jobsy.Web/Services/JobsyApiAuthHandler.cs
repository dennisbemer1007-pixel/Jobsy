using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Web.Auth;
using Jobsy.Web.Hosting;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Web.Services;

/// <summary>
/// Forwards the Blazor cookie identity to the API as a short-lived ES256 Bearer token.
/// On API 401 tries one silent device-session refresh + retry.
/// Must be resolved in the Blazor circuit/component DI scope — not via IHttpClientFactory's root scope.
/// </summary>
public sealed class JobsyApiAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly JobsyAccessTokenIssuer _accessTokens;

    public JobsyApiAuthHandler(
        IHttpContextAccessor httpContextAccessor,
        AuthenticationStateProvider authStateProvider,
        IServiceProvider services,
        IConfiguration configuration,
        JobsyAccessTokenIssuer accessTokens)
    {
        _httpContextAccessor = httpContextAccessor;
        _authStateProvider = authStateProvider;
        _services = services;
        _configuration = configuration;
        _accessTokens = accessTokens;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = await ResolveUserAsync();

        ApplyAccessToken(request, user, httpContext);
        ApplyTrustedClientIp(request, httpContext);
        ApplyPupilCookie(request, httpContext);

        try
        {
            var culture = _services.GetService<Jobsy.Web.Localization.CultureState>();
            if (culture is not null && !string.IsNullOrWhiteSpace(culture.Language))
            {
                request.Headers.TryAddWithoutValidation("X-Jobsy-Language", culture.Language);
            }
        }
        catch (InvalidOperationException)
        {
            // Outside of a Blazor circuit scope.
        }

        // Identity is the Jobsy ES256 access token only — do not overwrite with IdP access_token.

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        byte[]? bodyBytes = null;
        if (request.Content is not null
            && request.Method != HttpMethod.Get
            && request.Method != HttpMethod.Head
            && request.Method != HttpMethod.Delete)
        {
            bodyBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            var buffered = new ByteArrayContent(bodyBytes);
            foreach (var header in request.Content.Headers)
            {
                buffered.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            request.Content = buffered;
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized
            || request.Options.TryGetValue(new HttpRequestOptionsKey<bool>("jobsy-retried"), out var retried)
            && retried)
        {
            return response;
        }

        // One silent refresh via device session, then retry once.
        var refreshed = await TrySilentDeviceRefreshAsync(httpContext, cancellationToken);
        if (!refreshed)
        {
            await NotifySessionExpiredAsync(httpContext);
            return response;
        }

        response.Dispose();
        user = await ResolveUserAsync();
        var retry = await CloneRequestAsync(request, bodyBytes, cancellationToken);
        retry.Options.Set(new HttpRequestOptionsKey<bool>("jobsy-retried"), true);
        ApplyAccessToken(retry, user, httpContext);
        ApplyTrustedClientIp(retry, httpContext);
        ApplyPupilCookie(retry, httpContext);
        retry.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return await base.SendAsync(retry, cancellationToken);
    }

    /// <summary>
    /// Forwards the non-persistent <c>Lobsy.Leerling</c> cookie to <c>api/pupil/*</c>
    /// so the API Pupil scheme can authorize progress saves.
    /// </summary>
    private static void ApplyPupilCookie(HttpRequestMessage request, HttpContext? httpContext)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        if (!ForwardsPupilCookie(path))
        {
            return;
        }

        if (httpContext?.Request.Cookies.TryGetValue(PupilAuthDefaults.CookieName, out var cookie) != true
            || string.IsNullOrWhiteSpace(cookie))
        {
            return;
        }

        if (request.Headers.TryGetValues("Cookie", out var existing))
        {
            request.Headers.Remove("Cookie");
            request.Headers.TryAddWithoutValidation(
                "Cookie",
                string.Join("; ", existing.Append($"{PupilAuthDefaults.CookieName}={cookie}")));
        }
        else
        {
            request.Headers.TryAddWithoutValidation("Cookie", $"{PupilAuthDefaults.CookieName}={cookie}");
        }
    }

    /// <summary>
    /// Pupil progress lives under <c>/api/pupil</c>. The story PDF is
    /// <c>GET /leerling/pdf</c> on the API (not under <c>/api/pupil</c>), so the
    /// web proxy must forward the pupil cookie there too.
    /// </summary>
    internal static bool ForwardsPupilCookie(string absolutePath)
        => absolutePath.Contains("/api/pupil", StringComparison.OrdinalIgnoreCase)
           || absolutePath.Contains("/leerling/", StringComparison.OrdinalIgnoreCase);

    private void ApplyAccessToken(HttpRequestMessage request, ClaimsPrincipal user, HttpContext? httpContext)
    {
        // Never forward identity/role headers — API trusts only the signed JWT.
        request.Headers.Remove("X-Jobsy-Email");
        request.Headers.Remove("X-Jobsy-Dev-Secret");
        request.Headers.Remove("X-Jobsy-Role");
        request.Headers.Remove("X-Jobsy-Name");
        request.Headers.Remove("X-Jobsy-CompanyId");
        request.Headers.Remove("X-Jobsy-CompanyIds");
        request.Headers.Remove("X-Jobsy-Local-Session");
        request.Headers.Remove("Authorization");

        if (user.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var clientIp = VacancyMapApiForwarder.ResolveVisitorIp(httpContext);
        var jwt = _accessTokens.TryCreate(user, clientIp);
        if (!string.IsNullOrWhiteSpace(jwt))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        }
    }

    /// <summary>
    /// Forwards the browser client IP (CF-Connecting-IP when present) to the API
    /// behind a shared internal secret so anonymous public-* rate limits partition
    /// per visitor, not the shared Web→API hop.
    /// </summary>
    private void ApplyTrustedClientIp(HttpRequestMessage request, HttpContext? httpContext)
        => Jobsy.Web.Auth.TrustedClientIpHandler.ApplyTrustedClientIp(request, httpContext, _configuration);

    private async Task<bool> TrySilentDeviceRefreshAsync(HttpContext? httpContext, CancellationToken cancellationToken)
    {
        if (httpContext is null)
        {
            return false;
        }

        var refreshToken = DeviceSessionCookie.Read(httpContext);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        try
        {
            var factory = _services.GetRequiredService<IHttpClientFactory>();
            var apiBase = Jobsy.Core.JobsyPublicUrl.NormalizeBaseUrl(
                _configuration["ApiBaseUrl"],
                "http://localhost:5200/");
            var client = factory.CreateClient("JobsyAuthProvision");
            client.BaseAddress = new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(8);

            using var response = await client.PostAsJsonAsync(
                "api/auth/device-sessions/refresh",
                new
                {
                    refreshToken,
                    userAgent = httpContext.Request.Headers.UserAgent.ToString()
                },
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var profile = await response.Content.ReadFromJsonAsync<DeviceSessionRefreshMiddleware.RefreshProfile>(
                cancellationToken: cancellationToken);
            if (profile is null || string.IsNullOrWhiteSpace(profile.Email))
            {
                return false;
            }

            var principal = AuthPrincipalFactory.FromDeviceRefresh(profile);
            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                AuthServiceCollectionExtensions.CreateSessionAuthPropertiesPublic());
            SessionActivityCookie.Stamp(httpContext, DateTimeOffset.UtcNow);
            if (!string.IsNullOrWhiteSpace(profile.RefreshToken) && profile.ExpiresAtUtc is DateTime exp)
            {
                DeviceSessionCookie.Set(httpContext, profile.RefreshToken, exp);
            }

            httpContext.User = principal;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task NotifySessionExpiredAsync(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return;
        }

        try
        {
            var cache = _services.GetService<IMemoryCache>();
            cache?.Set("jobsy:session-expired:" + httpContext.TraceIdentifier, true, TimeSpan.FromMinutes(1));
        }
        catch
        {
            // ignore
        }

        // Interactive navigations for an authenticated session: send the browser to login.
        // Anonymous SSR routinely gets 401 from optional layout chips (notifications, sales
        // dashboard) — never hijack those into a /login redirect loop.
        if (!ShouldRedirectHtmlNavigationToLogin(httpContext))
        {
            return;
        }

        var returnUrl = AuthRedirects.SafeLocalUrl(httpContext.Request.Path.Value);
        httpContext.Response.Redirect(
            AuthRedirects.AppendReturnUrl("/login?error=session-expired", returnUrl));

        await Task.CompletedTask;
    }

    /// <summary>
    /// Whether an API 401 during an HTML document request should bounce the browser to login.
    /// </summary>
    public static bool ShouldRedirectHtmlNavigationToLogin(HttpContext httpContext)
    {
        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var path = httpContext.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/login", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/account/login", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/account/demo-login", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/account/external", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/register", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/signin-", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return httpContext.Request.Headers.Accept.ToString()
            .Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private static Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage request,
        byte[]? bodyBytes,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (bodyBytes is not null)
        {
            clone.Content = new ByteArrayContent(bodyBytes);
            if (request.Content is not null)
            {
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        return Task.FromResult(clone);
    }

    private async Task<ClaimsPrincipal> ResolveUserAsync()
    {
        var user = await ResolveUserCoreAsync();
        if (user.Identity?.IsAuthenticated == true)
        {
            return user;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null || !httpContext.Request.Cookies.ContainsKey("Jobsy.Auth"))
        {
            return user;
        }

        for (var i = 0; i < 6; i++)
        {
            await Task.Delay(75);
            user = await ResolveUserCoreAsync();
            if (user.Identity?.IsAuthenticated == true)
            {
                return user;
            }
        }

        return user;
    }

    private async Task<ClaimsPrincipal> ResolveUserCoreAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var httpUser = httpContext?.User;

        if (httpUser?.Identity?.IsAuthenticated == true)
        {
            return httpUser;
        }

        if (httpContext is not null
            && !httpContext.Request.Cookies.ContainsKey("Jobsy.Auth"))
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        try
        {
            var state = await _authStateProvider.GetAuthenticationStateAsync();
            return state.User;
        }
        catch (InvalidOperationException)
        {
            return httpUser ?? new ClaimsPrincipal(new ClaimsIdentity());
        }
    }
}
