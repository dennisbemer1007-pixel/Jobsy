using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Jobsy.Core;
using Jobsy.Core.Authorization;
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
        services.AddMemoryCache();
        services.AddHttpClient("JobsyAuthProvision");
        services.AddSingleton<IExternalAuthCredentialSource, ExternalAuthCredentialSource>();

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

        // Always register schemes so Integraties credentials can activate login without env vars.
        var entraTenant = string.IsNullOrWhiteSpace(authOptions.Entra.TenantId)
            ? "common"
            : authOptions.Entra.TenantId;
        authBuilder.AddOpenIdConnect(EntraScheme, options =>
        {
            options.Authority = $"https://login.microsoftonline.com/{entraTenant}/v2.0";
            options.ClientId = string.IsNullOrWhiteSpace(authOptions.Entra.ClientId)
                ? "pending"
                : authOptions.Entra.ClientId;
            options.ClientSecret = string.IsNullOrWhiteSpace(authOptions.Entra.ClientSecret)
                ? "pending"
                : authOptions.Entra.ClientSecret;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.CallbackPath = authOptions.Entra.CallbackPath;
            options.SaveTokens = true;
            // Entra ID tokens already carry profile/email claims; UserInfo often 404s and caused 500s.
            options.GetClaimsFromUserInfoEndpoint = false;
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.TokenValidationParameters.NameClaimType = "name";
            options.TokenValidationParameters.IssuerValidator = EntraOidcOptionsApplier.ValidateMicrosoftIssuer;
            options.Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProvider = ApplyEntraCredentialsBeforeChallengeAsync,
                OnMessageReceived = ApplyEntraCredentialsBeforeCallbackAsync,
                OnTokenValidated = async context =>
                {
                    await ApplyExternalJobsyProfileAsync(
                        context.HttpContext,
                        context.Principal,
                        context.Properties);
                },
                OnRemoteFailure = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("Jobsy.Web.Auth.Entra");
                    logger.LogWarning(
                        context.Failure,
                        "Microsoft Entra login mislukt: {Message}",
                        context.Failure?.Message);
                    context.HandleResponse();
                    context.Response.Redirect("/login?error=entra-failed");
                    return Task.CompletedTask;
                }
            };
        });

        authBuilder.AddGoogle(GoogleScheme, options =>
        {
            options.ClientId = string.IsNullOrWhiteSpace(authOptions.Google.ClientId)
                ? "pending"
                : authOptions.Google.ClientId;
            options.ClientSecret = string.IsNullOrWhiteSpace(authOptions.Google.ClientSecret)
                ? "pending"
                : authOptions.Google.ClientSecret;
            options.CallbackPath = authOptions.Google.CallbackPath;
            options.Events.OnRedirectToAuthorizationEndpoint = async context =>
            {
                var source = context.HttpContext.RequestServices.GetRequiredService<IExternalAuthCredentialSource>();
                var google = await source.GetGoogleAsync(context.HttpContext.RequestAborted);
                if (google is null)
                {
                    context.Response.Redirect("/login?error=google-not-configured");
                    return;
                }

                context.Options.ClientId = google.ClientId;
                context.Options.ClientSecret = google.ClientSecret;
                context.Response.Redirect(context.RedirectUri);
            };
            options.Events.OnCreatingTicket = async context =>
            {
                await ApplyExternalJobsyProfileAsync(
                    context.HttpContext,
                    context.Principal,
                    context.Properties);
            };
            options.Events.OnRemoteFailure = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Jobsy.Web.Auth.Google");
                logger.LogWarning(
                    context.Failure,
                    "Google login mislukt: {Message}",
                    context.Failure?.Message);
                context.HandleResponse();
                context.Response.Redirect("/login?error=google-failed");
                return Task.CompletedTask;
            };
        });

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();
        services.AddHttpContextAccessor();
        services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

        return services;
    }

    private static async Task ApplyEntraCredentialsBeforeChallengeAsync(RedirectContext context)
    {
        var source = context.HttpContext.RequestServices.GetRequiredService<IExternalAuthCredentialSource>();
        var entra = await source.GetEntraAsync(context.HttpContext.RequestAborted);
        if (entra is null)
        {
            context.HandleResponse();
            context.Response.Redirect("/login?error=entra-not-configured");
            return;
        }

        EntraOidcOptionsApplier.Apply(context.Options, entra);
        context.ProtocolMessage.ClientId = entra.ClientId;
    }

    private static async Task ApplyEntraCredentialsBeforeCallbackAsync(MessageReceivedContext context)
    {
        var source = context.HttpContext.RequestServices.GetRequiredService<IExternalAuthCredentialSource>();
        var entra = await source.GetEntraAsync(context.HttpContext.RequestAborted);
        if (entra is null)
        {
            context.HandleResponse();
            context.Response.Redirect("/login?error=entra-not-configured");
            return;
        }

        EntraOidcOptionsApplier.Apply(context.Options, entra);
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

            if (principal.HasClaim(c =>
                    c.Type == "show_candidate_how_to" && c.Value == "1"))
            {
                returnUrl = AuthRedirects.CandidateHowToPath;
            }
            else if (principal.IsInRole("Candidate")
                     || principal.HasClaim(ClaimTypes.Role, "Candidate"))
            {
                returnUrl = AuthRedirects.BanenkaartPath;
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

        app.MapGet("/account/external/{provider}", async (
            string provider,
            HttpContext http,
            IExternalAuthCredentialSource credentials,
            IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
            IOptionsMonitor<Microsoft.AspNetCore.Authentication.Google.GoogleOptions> googleOptions,
            string? returnUrl) =>
        {
            var scheme = provider.Equals("entra", StringComparison.OrdinalIgnoreCase) ? EntraScheme
                : provider.Equals("google", StringComparison.OrdinalIgnoreCase) ? GoogleScheme
                : null;

            if (scheme is null)
            {
                return Results.Redirect("/login?error=unknown-provider");
            }

            if (scheme == EntraScheme)
            {
                var entra = await credentials.GetEntraAsync(http.RequestAborted);
                if (entra is null)
                {
                    return Results.Redirect("/login?error=entra-not-configured");
                }

                EntraOidcOptionsApplier.Apply(oidcOptions.Get(EntraScheme), entra);
            }
            else
            {
                var google = await credentials.GetGoogleAsync(http.RequestAborted);
                if (google is null)
                {
                    return Results.Redirect("/login?error=google-not-configured");
                }

                var options = googleOptions.Get(GoogleScheme);
                options.ClientId = google.ClientId;
                options.ClientSecret = google.ClientSecret;
            }

            var props = new AuthenticationProperties
            {
                RedirectUri = AuthRedirects.SafeLocalUrl(AuthRedirects.PostLoginUrl(returnUrl ?? "/home"))
            };

            return Results.Challenge(props, [scheme]);
        });

        app.MapMethods("/account/logout", ["GET", "POST"], async (HttpContext http) =>
        {
            // POST from the header form uses antiforgery; GET covers refresh / Cookie LogoutPath / bookmarks.
            if (HttpMethods.IsPost(http.Request.Method))
            {
                var antiforgery = http.RequestServices.GetRequiredService<IAntiforgery>();
                await antiforgery.ValidateRequestAsync(http);
            }

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
            claims.Add(new Claim(JobsyClaimTypes.CompanyId, user.CompanyId));
        }

        if (!string.IsNullOrWhiteSpace(user.CompanyIds))
        {
            claims.Add(new Claim(JobsyClaimTypes.CompanyIds, user.CompanyIds));
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

            return CreatePrincipalFromProfile(profile, "local-registration");
        }
        catch
        {
            return null;
        }
    }

    private static async Task ApplyExternalJobsyProfileAsync(
        HttpContext http,
        ClaimsPrincipal? principal,
        AuthenticationProperties? properties)
    {
        if (principal?.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return;
        }

        EnsureNameClaim(identity);

        var email = identity.FindFirst(ClaimTypes.Email)?.Value
                    ?? identity.FindFirst("preferred_username")?.Value
                    ?? identity.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            // Fallback: still Candidate so login isn't blocked.
            ReplaceRoleClaim(identity, "Candidate");
            return;
        }

        var fullName = identity.FindFirst(ClaimTypes.Name)?.Value ?? email;
        var config = http.RequestServices.GetRequiredService<IConfiguration>();
        var factory = http.RequestServices.GetRequiredService<IHttpClientFactory>();

        try
        {
            var apiBase = JobsyPublicUrl.NormalizeBaseUrl(
                config["ApiBaseUrl"],
                "http://localhost:5200/");
            var client = factory.CreateClient("JobsyAuthProvision");
            client.BaseAddress = new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(10);

            var secret = config["JobsyAuth:DevelopmentAuthSecret"]
                         ?? config["JobsyAuth:ExternalProvisionSecret"];
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/ensure-external")
            {
                Content = JsonContent.Create(new { email, fullName })
            };
            if (!string.IsNullOrWhiteSpace(secret))
            {
                request.Headers.TryAddWithoutValidation("X-Jobsy-Provision-Secret", secret);
            }

            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                ReplaceRoleClaim(identity, "Candidate");
                return;
            }

            var profile = await response.Content.ReadFromJsonAsync<LocalApiLoginProfile>();
            if (profile is null)
            {
                ReplaceRoleClaim(identity, "Candidate");
                return;
            }

            ApplyProfileClaims(identity, profile, "external");

            if (properties is not null)
            {
                var role = NormalizeRole(profile.Role);
                if (profile.ShowCandidateHowTo)
                {
                    properties.RedirectUri = AuthRedirects.CandidateHowToPath;
                }
                else if (role == "Candidate")
                {
                    properties.RedirectUri = AuthRedirects.BanenkaartPath;
                }
            }
        }
        catch
        {
            ReplaceRoleClaim(identity, "Candidate");
        }
    }

    private static ClaimsPrincipal CreatePrincipalFromProfile(LocalApiLoginProfile profile, string authMethod)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, profile.Email.ToLowerInvariant()));
        identity.AddClaim(new Claim(ClaimTypes.Email, profile.Email));
        identity.AddClaim(new Claim(ClaimTypes.Name, profile.FullName));
        identity.AddClaim(new Claim("auth_method", authMethod));
        ApplyProfileClaims(identity, profile, authMethod);
        return new ClaimsPrincipal(identity);
    }

    private static void ApplyProfileClaims(ClaimsIdentity identity, LocalApiLoginProfile profile, string authMethod)
    {
        ReplaceRoleClaim(identity, NormalizeRole(profile.Role));

        foreach (var existing in identity.FindAll(JobsyClaimTypes.CompanyId).ToList())
        {
            identity.RemoveClaim(existing);
        }

        foreach (var existing in identity.FindAll(JobsyClaimTypes.CompanyIds).ToList())
        {
            identity.RemoveClaim(existing);
        }

        foreach (var existing in identity.FindAll(JobsyClaimTypes.HasCandidateApplications).ToList())
        {
            identity.RemoveClaim(existing);
        }

        foreach (var existing in identity.FindAll("show_candidate_how_to").ToList())
        {
            identity.RemoveClaim(existing);
        }

        if (profile.CompanyId is Guid companyId)
        {
            identity.AddClaim(new Claim(JobsyClaimTypes.CompanyId, companyId.ToString()));
        }

        if (profile.CompanyIds is { Count: > 0 })
        {
            identity.AddClaim(new Claim(
                JobsyClaimTypes.CompanyIds,
                string.Join(',', profile.CompanyIds)));
        }

        if (profile.HasCandidateApplications)
        {
            identity.AddClaim(new Claim(JobsyClaimTypes.HasCandidateApplications, "1"));
        }

        if (profile.ShowCandidateHowTo)
        {
            identity.AddClaim(new Claim("show_candidate_how_to", "1"));
        }

        if (!identity.HasClaim(c => c.Type == "auth_method"))
        {
            identity.AddClaim(new Claim("auth_method", authMethod));
        }
    }

    private static void EnsureNameClaim(ClaimsIdentity identity)
    {
        if (identity.HasClaim(c => c.Type == ClaimTypes.Name))
        {
            return;
        }

        var email = identity.FindFirst(ClaimTypes.Email)?.Value
                    ?? identity.FindFirst("preferred_username")?.Value
                    ?? "Gebruiker";
        identity.AddClaim(new Claim(ClaimTypes.Name, email));
    }

    private static void ReplaceRoleClaim(ClaimsIdentity identity, string role)
    {
        foreach (var existing in identity.FindAll(ClaimTypes.Role).ToList())
        {
            identity.RemoveClaim(existing);
        }

        foreach (var existing in identity.FindAll("roles").ToList())
        {
            identity.RemoveClaim(existing);
        }

        identity.AddClaim(new Claim(ClaimTypes.Role, role));
    }

    private sealed class LocalApiLoginProfile
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Candidate";
        public Guid? CompanyId { get; set; }
        public List<Guid>? CompanyIds { get; set; }
        public bool ShowCandidateHowTo { get; set; }
        public bool HasCandidateApplications { get; set; }
        public bool IsNewUser { get; set; }
    }

    private static string NormalizeRole(string role) => role.Trim().ToLowerInvariant() switch
    {
        "branchmanager" or "manager" or "ondernemer" or "filiaalmanager" => "BranchManager",
        "regionalmanager" or "regiomanager" => "RegionalManager",
        "enterprisemanager" or "bedrijfsmanager" => "EnterpriseManager",
        "intermediary" or "intermediair" => "Intermediary",
        "admin" or "administrator" => "Admin",
        "salesmanager" or "sales" => "SalesManager",
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

        var expected = Encoding.UTF8.GetBytes(user.Password);
        var actual = Encoding.UTF8.GetBytes(password);
        if (expected.Length != actual.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
