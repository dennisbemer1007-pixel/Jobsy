using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Json;
using Jobsy.Core;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Jobsy.Web.Auth;

public static class AuthServiceCollectionExtensions
{
    public const string EntraScheme = "Entra";
    public const string GoogleScheme = "Google";

    public static IServiceCollection AddJobsyAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.AddSingleton<DemoUserStore>();

        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        var secureAlways = environment is not null && !environment.IsDevelopment();

        var authBuilder = services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/account/logout";
                options.AccessDeniedPath = "/access-denied";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.Cookie.Name = "Jobsy.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = secureAlways
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;
            });

        if (authOptions.Entra.IsConfigured)
        {
            var tenant = string.IsNullOrWhiteSpace(authOptions.Entra.TenantId)
                ? "common"
                : authOptions.Entra.TenantId;

            authBuilder.AddOpenIdConnect(EntraScheme, options =>
            {
                options.Authority = $"https://login.microsoftonline.com/{tenant}/v2.0";
                options.ClientId = authOptions.Entra.ClientId!;
                options.ClientSecret = authOptions.Entra.ClientSecret!;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.CallbackPath = authOptions.Entra.CallbackPath;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.TokenValidationParameters.NameClaimType = "name";
                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = context =>
                    {
                        EnrichExternalPrincipal(context.Principal);
                        return Task.CompletedTask;
                    }
                };
            });
        }

        if (authOptions.Google.IsConfigured)
        {
            authBuilder.AddGoogle(GoogleScheme, options =>
            {
                options.ClientId = authOptions.Google.ClientId!;
                options.ClientSecret = authOptions.Google.ClientSecret!;
                options.CallbackPath = authOptions.Google.CallbackPath;
                options.Events.OnCreatingTicket = context =>
                {
                    EnrichExternalPrincipal(context.Principal);
                    return Task.CompletedTask;
                };
            });
        }

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();
        services.AddHttpContextAccessor();
        services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

        return services;
    }

    public static void MapJobsyAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/account/login", async (
            HttpContext http,
            DemoUserStore users,
            IConfiguration configuration,
            IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var form = await http.Request.ReadFormAsync();
            var email = form["email"].ToString().Trim();
            var password = form["password"].ToString();
            var returnUrl = string.IsNullOrWhiteSpace(form["returnUrl"]) ? "/home" : form["returnUrl"].ToString();
            returnUrl = AuthRedirects.PostLoginUrl(returnUrl);

            ClaimsPrincipal? principal = null;

            if (users.TryAuthenticate(email, password, out var user) && user is not null)
            {
                principal = CreateLocalPrincipal(user);
            }
            else
            {
                principal = await TryLocalApiLoginAsync(configuration, email, password);
            }

            if (principal is null)
            {
                return Results.Redirect($"/login?error=invalid&returnUrl={Uri.EscapeDataString(AuthRedirects.SafeLocalUrl(returnUrl))}");
            }

            await http.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            return Results.Redirect(AuthRedirects.SafeLocalUrl(returnUrl));
        });

        app.MapGet("/account/external/{provider}", (
            string provider,
            IOptions<AuthOptions> options,
            string? returnUrl) =>
        {
            var auth = options.Value;
            var scheme = provider.Equals("entra", StringComparison.OrdinalIgnoreCase) ? EntraScheme
                : provider.Equals("google", StringComparison.OrdinalIgnoreCase) ? GoogleScheme
                : null;

            if (scheme is null)
            {
                return Results.Redirect("/login?error=unknown-provider");
            }

            if (scheme == EntraScheme && !auth.Entra.IsConfigured)
            {
                return Results.Redirect("/login?error=entra-not-configured");
            }

            if (scheme == GoogleScheme && !auth.Google.IsConfigured)
            {
                return Results.Redirect("/login?error=google-not-configured");
            }

            var props = new AuthenticationProperties
            {
                RedirectUri = AuthRedirects.SafeLocalUrl(AuthRedirects.PostLoginUrl(returnUrl ?? "/home"))
            };

            return Results.Challenge(props, [scheme]);
        });

        app.MapPost("/account/logout", async (HttpContext http, IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/");
        });

        // Keep GET for backwards compatibility but prefer POST with antiforgery.
        app.MapGet("/account/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/");
        });
    }

    private static ClaimsPrincipal CreateLocalPrincipal(DemoUserOptions user)
    {
        var role = NormalizeRole(user.Role);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Email.ToLowerInvariant()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, role),
            new("auth_method", "password")
        };

        if (!string.IsNullOrWhiteSpace(user.CompanyId))
        {
            claims.Add(new Claim(Jobsy.Core.Authorization.JobsyClaimTypes.CompanyId, user.CompanyId));
        }

        if (!string.IsNullOrWhiteSpace(user.CompanyIds))
        {
            claims.Add(new Claim(Jobsy.Core.Authorization.JobsyClaimTypes.CompanyIds, user.CompanyIds));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    private static async Task<ClaimsPrincipal?> TryLocalApiLoginAsync(
        IConfiguration configuration,
        string email,
        string password)
    {
        try
        {
            var apiBase = JobsyPublicUrl.NormalizeBaseUrl(
                configuration["ApiBaseUrl"],
                "http://localhost:5200/");
            using var client = new HttpClient { BaseAddress = new Uri(apiBase), Timeout = TimeSpan.FromSeconds(8) };
            using var response = await client.PostAsJsonAsync("api/auth/local-login", new { email, password });
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var profile = await response.Content.ReadFromJsonAsync<LocalApiLoginProfile>();
            if (profile is null || string.IsNullOrWhiteSpace(profile.Email))
            {
                return null;
            }

            var role = NormalizeRole(profile.Role);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, profile.Email.ToLowerInvariant()),
                new(ClaimTypes.Name, profile.FullName),
                new(ClaimTypes.Email, profile.Email),
                new(ClaimTypes.Role, role),
                new("auth_method", "local-registration")
            };

            if (profile.CompanyId is Guid companyId)
            {
                claims.Add(new Claim(
                    Jobsy.Core.Authorization.JobsyClaimTypes.CompanyId,
                    companyId.ToString()));
            }

            if (profile.CompanyIds is { Count: > 0 })
            {
                claims.Add(new Claim(
                    Jobsy.Core.Authorization.JobsyClaimTypes.CompanyIds,
                    string.Join(',', profile.CompanyIds)));
            }

            return new ClaimsPrincipal(
                new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }
        catch
        {
            return null;
        }
    }

    private sealed class LocalApiLoginProfile
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Candidate";
        public Guid? CompanyId { get; set; }
        public List<Guid>? CompanyIds { get; set; }
    }

    private static void EnrichExternalPrincipal(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return;
        }

        if (!identity.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            // Externe providers krijgen standaard kandidaat; admin/manager via lokale accounts of Entra app roles later.
            identity.AddClaim(new Claim(ClaimTypes.Role, "Candidate"));
        }

        if (!identity.HasClaim(c => c.Type == ClaimTypes.Name))
        {
            var email = identity.FindFirst(ClaimTypes.Email)?.Value
                        ?? identity.FindFirst("preferred_username")?.Value
                        ?? "Gebruiker";
            identity.AddClaim(new Claim(ClaimTypes.Name, email));
        }
    }

    private static string NormalizeRole(string role) => role.Trim().ToLowerInvariant() switch
    {
        "branchmanager" or "manager" or "ondernemer" or "filiaalmanager" => "BranchManager",
        "regionalmanager" or "regiomanager" => "RegionalManager",
        "enterprisemanager" or "bedrijfsmanager" => "EnterpriseManager",
        "intermediary" or "intermediair" => "Intermediary",
        "admin" or "administrator" => "Admin",
        _ => "Candidate"
    };

}

public class DemoUserStore
{
    private readonly IReadOnlyList<DemoUserOptions> _users;

    public DemoUserStore(IOptions<AuthOptions> options)
    {
        _users = options.Value.DemoUsers;
    }

    public bool TryAuthenticate(string email, string password, out DemoUserOptions? user)
    {
        user = _users.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

        if (user is null)
        {
            return false;
        }

        // Constant-time compare for demo passwords (plain in config for local MVP).
        var expected = Encoding.UTF8.GetBytes(user.Password);
        var actual = Encoding.UTF8.GetBytes(password);
        if (expected.Length != actual.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public IReadOnlyList<DemoUserOptions> All => _users;
}
