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
        ApplyPupilCookie(request, httpContext, user);

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
        CaptureRenewedApiTicket(response, httpContext, user);
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
        ApplyPupilCookie(retry, httpContext, user);
        retry.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var retriedResponse = await base.SendAsync(retry, cancellationToken);
        CaptureRenewedApiTicket(retriedResponse, httpContext, user);
        return retriedResponse;
    }

    /// <summary>
    /// Forwards the API-minted pupil ticket (stored opaquely as
    /// <see cref="PupilApiSessionCookie"/>) to <c>api/pupil/*</c>. The browser's
    /// <c>Lobsy.Leerling</c> cookie is protected with the Web key ring and the
    /// API cannot unprotect it.
    /// </summary>
    private void ApplyPupilCookie(HttpRequestMessage request, HttpContext? httpContext, ClaimsPrincipal user)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        if (!ForwardsPupilCookie(path))
        {
            return;
        }

        var cookie = httpContext is null ? null : PupilApiSessionCookie.Read(httpContext);
        if (string.IsNullOrWhiteSpace(cookie))
        {
            // The circuit has no request cookies. The ticket was stored when /_blazor authenticated.
            var codeId = user.FindFirst(PupilClaimTypes.PupilCodeId)?.Value;
            cookie = _services.GetService<PupilApiTicketStore>()?.Get(codeId);
        }

        if (string.IsNullOrWhiteSpace(cookie))
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
    /// The API may slide its own pupil cookie. Store the new value. Never copy
    /// it onto <c>Lobsy.Leerling</c>: that name is the Web session.
    /// </summary>
    private void CaptureRenewedApiTicket(
        HttpResponseMessage response,
        HttpContext? httpContext,
        ClaimsPrincipal user)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return;
        }

        foreach (var header in values)
        {
            if (!PupilApiSessionCookie.TryReadTicket(header, out var value))
            {
                continue;
            }

            var codeId = user.FindFirst(PupilClaimTypes.PupilCodeId)?.Value;
            _services.GetService<PupilApiTicketStore>()?.Set(codeId, value);
            if (httpContext is not null && !httpContext.Response.HasStarted)
            {
                PupilApiSessionCookie.Set(httpContext, value);
            }

            return;
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

        // A pupil principal must never land on the staff e-mail form.
        if (ShouldRedirectPupilToCodeLogin(httpContext))
        {
            try
            {
                await httpContext.SignOutAsync(PupilAuthDefaults.Scheme);
                PupilApiSessionCookie.Clear(httpContext);
            }
            catch (InvalidOperationException)
            {
                // Headers already went out; the page still must not show staff login.
            }

            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.Redirect("/leerling?error=expired");
            }

            return;
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

    /// <summary>Pupil API 401s go back to the code form, not the staff login.</summary>
    public static bool ShouldRedirectPupilToCodeLogin(HttpContext httpContext)
    {
        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        var path = httpContext.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/leerling", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "/leerling", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/leerling/login", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!IsPupilPrincipal(httpContext.User)
            && !httpContext.Request.Cookies.ContainsKey(PupilAuthDefaults.CookieName)
            && !httpContext.Request.Cookies.ContainsKey(PupilApiSessionCookie.Name))
        {
            return false;
        }

        return httpContext.Request.Headers.Accept.ToString()
            .Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPupilPrincipal(ClaimsPrincipal? user)
    {
        if (user is null)
        {
            return false;
        }

        foreach (var identity in user.Identities)
        {
            if (!identity.IsAuthenticated)
            {
                continue;
            }

            if (string.Equals(identity.AuthenticationType, PupilAuthDefaults.Scheme, StringComparison.Ordinal)
                || identity.HasClaim(c => c.Type == PupilClaimTypes.PupilCodeId))
            {
                return true;
            }
        }

        return false;
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

        // Pupil sessions use the code form. Never the staff login.
        if (IsPupilPrincipal(httpContext.User))
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

        // /_blazor has no staff cookie. The circuit principal is the pupil, not HttpContext.User
        // on later interactive calls (that context is often null or still anonymous).
        try
        {
            var state = await _authStateProvider.GetAuthenticationStateAsync();
            if (IsPupilPrincipal(state.User))
            {
                return state.User;
            }

            if (httpContext is not null
                && !httpContext.Request.Cookies.ContainsKey("Jobsy.Auth"))
            {
                return new ClaimsPrincipal(new ClaimsIdentity());
            }

            if (state.User.Identity?.IsAuthenticated == true)
            {
                return state.User;
            }
        }
        catch (InvalidOperationException)
        {
            // Outside a circuit scope.
        }

        if (httpContext is not null
            && !httpContext.Request.Cookies.ContainsKey("Jobsy.Auth"))
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        return httpUser ?? new ClaimsPrincipal(new ClaimsIdentity());
    }
}
