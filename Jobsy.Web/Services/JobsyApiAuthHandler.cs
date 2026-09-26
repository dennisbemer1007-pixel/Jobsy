using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Web.Auth;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Web.Services;

/// <summary>
/// Forwards the Blazor cookie identity to the API via development auth headers
/// (or later: bearer token from Entra).
/// Mints/refreshes <c>X-Jobsy-Local-Session</c> when missing or near expiry,
/// and on API 401 tries one silent device-session refresh + retry.
/// Must be resolved in the Blazor circuit/component DI scope — not via IHttpClientFactory's root scope.
/// </summary>
public sealed class JobsyApiAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;

    public JobsyApiAuthHandler(
        IHttpContextAccessor httpContextAccessor,
        AuthenticationStateProvider authStateProvider,
        IServiceProvider services,
        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _authStateProvider = authStateProvider;
        _services = services;
        _configuration = configuration;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = await ResolveUserAsync();
        user = await EnsureFreshLocalSessionAsync(httpContext, user, cancellationToken);

        ApplyIdentityHeaders(request, user);

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

        if (httpContext is not null)
        {
            var accessToken = await httpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

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
        ApplyIdentityHeaders(retry, user);
        retry.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return await base.SendAsync(retry, cancellationToken);
    }

    private void ApplyIdentityHeaders(HttpRequestMessage request, ClaimsPrincipal user)
    {
        // Clear previous identity headers when cloning is not used.
        request.Headers.Remove("X-Jobsy-Email");
        request.Headers.Remove("X-Jobsy-Dev-Secret");
        request.Headers.Remove("X-Jobsy-Role");
        request.Headers.Remove("X-Jobsy-Name");
        request.Headers.Remove("X-Jobsy-CompanyId");
        request.Headers.Remove("X-Jobsy-CompanyIds");
        request.Headers.Remove("X-Jobsy-Local-Session");

        if (user.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var email = ResolveEmail(user);
        var role = user.FindFirst(ClaimTypes.Role)?.Value;
        var name = user.FindFirst(ClaimTypes.Name)?.Value ?? user.Identity.Name;
        var companyId = user.FindFirst(JobsyClaimTypes.CompanyId)?.Value;
        var companyIds = user.FindFirst(JobsyClaimTypes.CompanyIds)?.Value;

        if (!string.IsNullOrWhiteSpace(email))
        {
            request.Headers.TryAddWithoutValidation("X-Jobsy-Email", email);

            var developmentAuthSecret = _configuration["JobsyAuth:DevelopmentAuthSecret"];
            if (!string.IsNullOrEmpty(developmentAuthSecret))
            {
                request.Headers.TryAddWithoutValidation("X-Jobsy-Dev-Secret", developmentAuthSecret);
            }
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            request.Headers.TryAddWithoutValidation("X-Jobsy-Role", role);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            request.Headers.TryAddWithoutValidation("X-Jobsy-Name", name);
        }

        if (!string.IsNullOrWhiteSpace(companyId))
        {
            request.Headers.TryAddWithoutValidation("X-Jobsy-CompanyId", companyId);
        }

        if (!string.IsNullOrWhiteSpace(companyIds))
        {
            request.Headers.TryAddWithoutValidation("X-Jobsy-CompanyIds", companyIds);
        }

        var localSession = user.FindFirst(JobsyClaimTypes.LocalSession)?.Value;
        if (!string.IsNullOrWhiteSpace(localSession))
        {
            request.Headers.TryAddWithoutValidation("X-Jobsy-Local-Session", localSession);
        }
    }

    private async Task<ClaimsPrincipal> EnsureFreshLocalSessionAsync(
        HttpContext? httpContext,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (user.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated || httpContext is null)
        {
            return user;
        }

        var key = JobsyLocalSessionToken.ResolveSigningKey(
            _configuration["JobsyAuth:LocalSessionSigningKey"],
            _configuration["JobsyAuth:DevelopmentAuthSecret"]);
        if (string.IsNullOrWhiteSpace(key))
        {
            return user;
        }

        var existing = identity.FindFirst(JobsyClaimTypes.LocalSession)?.Value;
        var needsMint = string.IsNullOrWhiteSpace(existing);
        if (!needsMint
            && JobsyLocalSessionToken.TryReadSignedPayload(
                existing,
                key,
                ignoreExpiry: true,
                out _,
                out _,
                out var expUnix))
        {
            var exp = DateTimeOffset.FromUnixTimeSeconds(expUnix);
            needsMint = exp - DateTimeOffset.UtcNow < DeviceSessionRules.LocalSessionRenewalSkew;
        }
        else if (!string.IsNullOrWhiteSpace(existing))
        {
            // Unreadable token — remint if we can recover email/userId from ignoreExpiry path failed.
            needsMint = true;
        }

        if (!needsMint)
        {
            return user;
        }

        string email;
        Guid userId;
        if (!string.IsNullOrWhiteSpace(existing)
            && JobsyLocalSessionToken.TryReadSignedPayload(
                existing,
                key,
                ignoreExpiry: true,
                out email,
                out userId,
                out _))
        {
            // ok
        }
        else
        {
            return user;
        }

        var fresh = JobsyLocalSessionToken.Create(email, userId, key);
        foreach (var claim in identity.FindAll(JobsyClaimTypes.LocalSession).ToList())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim(JobsyClaimTypes.LocalSession, fresh));
        try
        {
            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                user,
                AuthServiceCollectionExtensions.CreateSessionAuthPropertiesPublic());
        }
        catch
        {
            // Headers still carry the fresh token for this request.
        }

        return user;
    }

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

        // Interactive navigations: send the browser to login. Blazor API calls surface 401.
        if (httpContext.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase)
            && !httpContext.Response.HasStarted)
        {
            var returnUrl = httpContext.Request.Path.Value ?? "/";
            httpContext.Response.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        await Task.CompletedTask;
    }

    private static Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage request,
        byte[]? bodyBytes,
        CancellationToken cancellationToken)
    {
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

    /// <summary>
    /// Never fall back to display name — API DevelopmentAuth looks up users by email.
    /// </summary>
    internal static string? ResolveEmail(ClaimsPrincipal user)
    {
        foreach (var type in new[]
                 {
                     ClaimTypes.Email,
                     "email",
                     "preferred_username",
                     "emails",
                     ClaimTypes.NameIdentifier,
                     "sub"
                 })
        {
            var value = user.FindFirst(type)?.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(value) && value.Contains('@', StringComparison.Ordinal))
            {
                return value;
            }
        }

        return null;
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
