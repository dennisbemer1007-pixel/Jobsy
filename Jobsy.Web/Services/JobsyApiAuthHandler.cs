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

    public JobsyApiAuthHandler(
        IHttpContextAccessor httpContextAccessor,
        AuthenticationStateProvider authStateProvider,
        IServiceProvider services)
    {
        _httpContextAccessor = httpContextAccessor;
        _authStateProvider = authStateProvider;
        _services = services;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = await ResolveUserAsync();

        if (user.Identity?.IsAuthenticated == true)
        {
            var email = user.FindFirst(ClaimTypes.Email)?.Value ?? user.Identity.Name;
            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            var name = user.Identity.Name;
            var companyId = user.FindFirst(JobsyClaimTypes.CompanyId)?.Value;
            var companyIds = user.FindFirst(JobsyClaimTypes.CompanyIds)?.Value;

            if (!string.IsNullOrWhiteSpace(email))
            {
                request.Headers.TryAddWithoutValidation("X-Jobsy-Email", email);
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

    private async Task<ClaimsPrincipal> ResolveUserAsync()
    {
        var httpUser = _httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
        {
            return httpUser;
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
