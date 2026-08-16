using System.Net.Http.Headers;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

namespace Jobsy.Web.Services;

/// <summary>
/// Forwards the Blazor cookie identity to the API via development auth headers
/// (or later: bearer token from Entra).
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

        if (user.Identity?.IsAuthenticated == true)
        {
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

        // Prefer cookie/OIDC access_token as Bearer when present (Entra); keep X-Jobsy for local DevelopmentAuth.
        if (httpContext is not null)
        {
            var accessToken = await httpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return await base.SendAsync(request, cancellationToken);
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

        // Cookie is present but the circuit principal is still catching up after login.
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

        // Prefer the live HTTP cookie principal. If this request already lost auth
        // (session inactivity middleware signed out), do not resurrect credentials
        // from a stale Blazor circuit AuthenticationStateProvider cache.
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
            // Handler resolved outside a Razor circuit (e.g. startup). Treat as anonymous.
            return httpUser ?? new ClaimsPrincipal(new ClaimsIdentity());
        }
    }
}
