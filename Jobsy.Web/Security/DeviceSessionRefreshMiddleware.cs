using System.Net.Http.Json;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Web.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Jobsy.Web.Security;

/// <summary>
/// When <c>Jobsy.Auth</c> is missing/expired but <c>Lobsy.Device</c> is valid,
/// silently rotates the refresh token and restores the interactive cookie session.
/// </summary>
public sealed class DeviceSessionRefreshMiddleware
{
    private readonly RequestDelegate _next;

    public DeviceSessionRefreshMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkip(context))
        {
            await _next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        var refreshToken = DeviceSessionCookie.Read(context);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            await _next(context);
            return;
        }

        var restored = await TryRestoreAsync(context, refreshToken);
        if (!restored)
        {
            DeviceSessionCookie.Clear(context);
        }

        await _next(context);
    }

    private static async Task<bool> TryRestoreAsync(HttpContext http, string refreshToken)
    {
        try
        {
            var config = http.RequestServices.GetRequiredService<IConfiguration>();
            var factory = http.RequestServices.GetRequiredService<IHttpClientFactory>();
            var apiBase = Jobsy.Core.JobsyPublicUrl.NormalizeBaseUrl(
                config["ApiBaseUrl"],
                "http://localhost:5200/");
            var client = factory.CreateClient("JobsyAuthProvision");
            client.BaseAddress = new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(8);

            using var response = await client.PostAsJsonAsync(
                "api/auth/device-sessions/refresh",
                new
                {
                    refreshToken,
                    userAgent = http.Request.Headers.UserAgent.ToString()
                });
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var profile = await response.Content.ReadFromJsonAsync<RefreshProfile>();
            if (profile is null || string.IsNullOrWhiteSpace(profile.Email))
            {
                return false;
            }

            var principal = AuthPrincipalFactory.FromDeviceRefresh(profile);
            await http.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                AuthServiceCollectionExtensions.CreateSessionAuthPropertiesPublic());
            SessionActivityCookie.Stamp(http, DateTimeOffset.UtcNow);
            if (!string.IsNullOrWhiteSpace(profile.RefreshToken) && profile.ExpiresAtUtc is DateTime exp)
            {
                DeviceSessionCookie.Set(http, profile.RefreshToken, exp);
            }

            http.User = principal;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ShouldSkip(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        return path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/images", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/icons", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/service-worker", StringComparison.OrdinalIgnoreCase)
               || string.Equals(path, "/offline.html", StringComparison.OrdinalIgnoreCase)
               || string.Equals(path, "/manifest.webmanifest", StringComparison.OrdinalIgnoreCase)
               || string.Equals(path, "/account/logout", StringComparison.OrdinalIgnoreCase)
               || string.Equals(path, "/account/login", StringComparison.OrdinalIgnoreCase)
               || string.Equals(path, "/account/demo-login", StringComparison.OrdinalIgnoreCase)
               || string.Equals(path, "/account/complete-login", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/account/external", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/signin-", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/login", StringComparison.OrdinalIgnoreCase);
    }

    public sealed class RefreshProfile
    {
        public string? RefreshToken { get; set; }
        public Guid DeviceSessionId { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "Candidate";
        public Guid? CompanyId { get; set; }
        public List<Guid>? CompanyIds { get; set; }
        public bool ShowCandidateHowTo { get; set; }
        public bool HasCandidateApplications { get; set; }
        public bool HasSalesReferral { get; set; }
        public int SessionVersion { get; set; }
        public string? SessionToken { get; set; }
        public Guid? UserId { get; set; }
    }
}

public static class DeviceSessionRefreshMiddlewareExtensions
{
    public static IApplicationBuilder UseDeviceSessionRefresh(this IApplicationBuilder app)
        => app.UseMiddleware<DeviceSessionRefreshMiddleware>();
}

/// <summary>Shared principal construction for login / refresh / handoff exchange.</summary>
public static class AuthPrincipalFactory
{
    public static ClaimsPrincipal FromDeviceRefresh(DeviceSessionRefreshMiddleware.RefreshProfile profile)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        var subject = profile.UserId is Guid uid && uid != Guid.Empty
            ? uid.ToString("D")
            : profile.Email.ToLowerInvariant();
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subject));
        identity.AddClaim(new Claim(ClaimTypes.Email, profile.Email));
        identity.AddClaim(new Claim(ClaimTypes.Name, profile.FullName));
        identity.AddClaim(new Claim("auth_method", "device-refresh"));
        identity.AddClaim(new Claim(ClaimTypes.Role, NormalizeRole(profile.Role)));
        identity.AddClaim(new Claim(JobsyClaimTypes.SessionVersion, profile.SessionVersion.ToString()));
        if (profile.DeviceSessionId != Guid.Empty)
        {
            identity.AddClaim(new Claim(JobsyClaimTypes.DeviceSessionId, profile.DeviceSessionId.ToString()));
            identity.AddClaim(new Claim(JobsyClaimTypes.HasDeviceSession, "1"));
        }

        if (profile.CompanyId is Guid companyId)
        {
            identity.AddClaim(new Claim(JobsyClaimTypes.CompanyId, companyId.ToString()));
        }

        if (profile.CompanyIds is { Count: > 0 })
        {
            identity.AddClaim(new Claim(JobsyClaimTypes.CompanyIds, string.Join(',', profile.CompanyIds)));
        }

        if (profile.HasCandidateApplications)
        {
            identity.AddClaim(new Claim(JobsyClaimTypes.HasCandidateApplications, "1"));
        }

        if (profile.HasSalesReferral)
        {
            identity.AddClaim(new Claim(JobsyClaimTypes.HasSalesReferral, "1"));
        }

        if (!string.IsNullOrWhiteSpace(profile.SessionToken))
        {
            identity.AddClaim(new Claim(JobsyClaimTypes.LocalSession, profile.SessionToken));
        }

        if (profile.ShowCandidateHowTo)
        {
            identity.AddClaim(new Claim("show_candidate_how_to", "1"));
        }

        return new ClaimsPrincipal(identity);
    }

    public static void StampDeviceClaims(ClaimsIdentity identity, Guid deviceSessionId, int sessionVersion)
    {
        foreach (var c in identity.FindAll(JobsyClaimTypes.DeviceSessionId).ToList())
        {
            identity.RemoveClaim(c);
        }

        foreach (var c in identity.FindAll(JobsyClaimTypes.HasDeviceSession).ToList())
        {
            identity.RemoveClaim(c);
        }

        foreach (var c in identity.FindAll(JobsyClaimTypes.SessionVersion).ToList())
        {
            identity.RemoveClaim(c);
        }

        identity.AddClaim(new Claim(JobsyClaimTypes.DeviceSessionId, deviceSessionId.ToString()));
        identity.AddClaim(new Claim(JobsyClaimTypes.HasDeviceSession, "1"));
        identity.AddClaim(new Claim(JobsyClaimTypes.SessionVersion, sessionVersion.ToString()));
    }

    public static void StampSessionVersion(ClaimsIdentity identity, int sessionVersion)
    {
        foreach (var c in identity.FindAll(JobsyClaimTypes.SessionVersion).ToList())
        {
            identity.RemoveClaim(c);
        }

        identity.AddClaim(new Claim(JobsyClaimTypes.SessionVersion, sessionVersion.ToString()));
    }

    private static string NormalizeRole(string role) => role.Trim().ToLowerInvariant() switch
    {
        "branchmanager" or "manager" or "ondernemer" or "filiaalmanager" => "BranchManager",
        "regionalmanager" or "regiomanager" => "RegionalManager",
        "enterprisemanager" or "bedrijfsmanager" => "EnterpriseManager",
        "intermediary" or "intermediair" => "Intermediary",
        "admin" or "administrator" => "Admin",
        "salesmanager" or "sales" => "SalesManager",
        "ambassadeur" or "ambassador" => "Ambassadeur",
        _ => "Candidate"
    };
}
