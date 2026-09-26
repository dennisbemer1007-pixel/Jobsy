using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Jobsy.Core;
using Jobsy.Core.Authorization;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Jobsy.Web.Auth;

public static class AuthServiceCollectionExtensions
{
    public const string EntraScheme = "Entra";
    public const string GoogleScheme = "Google";

    /// <summary>Serializes GoogleOptions ClientId/Secret mutations across concurrent OAuth flows.</summary>
    internal static readonly object GoogleOptionsSync = new();

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
                // Sliding cookie ceiling; fine-grained inactivity is enforced by SessionInactivityMiddleware
                // using the admin-configured SessionInactivityTimeoutMinutes value.
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(SessionSecurityRules.MaxInactivityTimeoutMinutes);
                options.Cookie.Name = "Jobsy.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = secureAlways
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;
                options.Events.OnSigningIn = context =>
                {
                    context.Properties.IsPersistent = true;
                    context.Properties.AllowRefresh = true;
                    if (context.Properties.ExpiresUtc is null)
                    {
                        context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(
                            SessionSecurityRules.MaxInactivityTimeoutMinutes);
                    }

                    StampLastActivity(context.HttpContext);
                    return Task.CompletedTask;
                };
                options.Events.OnValidatePrincipal = ValidatePrincipalSessionVersionAsync;
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
            options.SaveTokens = false;
            options.CorrelationCookie.SecurePolicy = secureAlways
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.NonceCookie.SecurePolicy = secureAlways
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            // OIDC nonce/correlation cookies stay SameSite=None (framework default).
            // Lax/Strict would drop them on the cross-site Entra redirect and break login.
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
                    if (IsEmailUnverified(context.Principal))
                    {
                        context.Fail("E-mailadres is niet geverifieerd bij de identity provider.");
                        context.HandleResponse();
                        context.Response.Redirect(
                            AuthRedirects.AppendReturnUrl("/login?error=email-unverified", context.Properties?.RedirectUri));
                        return;
                    }

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
                    context.Response.Redirect(
                        AuthRedirects.AppendReturnUrl("/login?error=entra-failed", context.Properties?.RedirectUri));
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
            options.CorrelationCookie.SecurePolicy = secureAlways
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.Events.OnRedirectToAuthorizationEndpoint = async context =>
            {
                var source = context.HttpContext.RequestServices.GetRequiredService<IExternalAuthCredentialSource>();
                var google = await source.GetGoogleAsync(context.HttpContext.RequestAborted);
                if (google is null)
                {
                    context.Response.Redirect(
                        AuthRedirects.AppendReturnUrl("/login?error=google-not-configured", context.Properties?.RedirectUri));
                    return;
                }

                lock (GoogleOptionsSync)
                {
                    context.Options.ClientId = google.ClientId;
                    context.Options.ClientSecret = google.ClientSecret;
                }

                context.Response.Redirect(context.RedirectUri);
            };
            options.Events.OnCreatingTicket = async context =>
            {
                if (IsEmailUnverified(context.Principal))
                {
                    context.Fail("E-mailadres is niet geverifieerd bij de identity provider.");
                    return;
                }

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
                context.Response.Redirect(
                    AuthRedirects.AppendReturnUrl("/login?error=google-failed", context.Properties?.RedirectUri));
                return Task.CompletedTask;
            };
        });

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();
        services.AddHttpContextAccessor();
        services.AddAntiforgery(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = secureAlways
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
        });
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
            context.Response.Redirect(
                AuthRedirects.AppendReturnUrl("/login?error=entra-not-configured", context.Properties?.RedirectUri));
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
            context.Response.Redirect(
                AuthRedirects.AppendReturnUrl("/login?error=entra-not-configured", context.Properties?.RedirectUri));
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
            var form = await http.Request.ReadFormAsync();
            var returnUrl = AuthRedirects.ResolveRequestedReturnUrl(
                form["returnUrl"], form["returnTo"], form["redirect"]);
            var safeReturn = AuthRedirects.SafeLocalUrl(returnUrl);

            // Stale tabs / redeployed DP keys must not land on the generic /Error page.
            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return Results.Redirect(
                    $"/login?error=retry&returnUrl={Uri.EscapeDataString(safeReturn)}");
            }

            var email = form["email"].ToString().Trim();
            var password = form["password"].ToString();
            var rememberDevice = string.Equals(
                form["rememberDevice"].ToString(),
                "true",
                StringComparison.OrdinalIgnoreCase);

            ClaimsPrincipal? principal = null;
            LocalApiLoginProfile? apiProfile = null;
            var allowDemoLogin = IsDemoLoginEnabled(http, configuration);

            if (allowDemoLogin && users.TryAuthenticate(email, password, out var user) && user is not null)
            {
                principal = CreateLocalPrincipal(user);
            }
            else
            {
                apiProfile = await TryLocalApiLoginProfileAsync(configuration, email, password, rememberDevice);
                if (apiProfile is not null)
                {
                    principal = CreatePrincipalFromProfile(apiProfile, "local-registration");
                }
            }

            if (principal is null)
            {
                return Results.Redirect($"/login?error=invalid&returnUrl={Uri.EscapeDataString(safeReturn)}");
            }

            var showHowTo = principal.HasClaim(c =>
                c.Type == "show_candidate_how_to" && c.Value == "1");
            if (showHowTo
                || principal.IsInRole("Candidate")
                || principal.HasClaim(ClaimTypes.Role, "Candidate"))
            {
                returnUrl = AuthRedirects.ResolveCandidateReturnUrl(returnUrl, showHowTo);
            }

            if (principal.Identity is ClaimsIdentity identity)
            {
                if (apiProfile?.DeviceSessionId is Guid deviceId
                    && !string.IsNullOrWhiteSpace(apiProfile.DeviceRefreshToken)
                    && apiProfile.DeviceExpiresAtUtc is DateTime deviceExp)
                {
                    AuthPrincipalFactory.StampDeviceClaims(identity, deviceId, apiProfile.SessionVersion);
                    DeviceSessionCookie.Set(http, apiProfile.DeviceRefreshToken, deviceExp);
                }
                else if (rememberDevice)
                {
                    await TryAttachProvisionedDeviceSessionAsync(http, configuration, identity, email);
                }
                else
                {
                    AuthPrincipalFactory.StampSessionVersion(identity, apiProfile?.SessionVersion ?? 0);
                }
            }

            await http.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                CreateSessionAuthProperties());
            StampLastActivity(http);

            return Results.Redirect(AuthRedirects.SafeLocalUrl(returnUrl));
        }).RequireRateLimiting("auth");

        // Demo one-click login resolves password server-side so credentials stay out of HTML.
        app.MapPost("/account/demo-login", async (
            HttpContext http,
            DemoUserStore users,
            IConfiguration configuration,
            IAntiforgery antiforgery) =>
        {
            var form = await http.Request.ReadFormAsync();
            var returnUrl = AuthRedirects.ResolveRequestedReturnUrl(
                form["returnUrl"], form["returnTo"], form["redirect"]);
            var safeReturn = AuthRedirects.SafeLocalUrl(returnUrl);

            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return Results.Redirect(
                    $"/login?error=retry&returnUrl={Uri.EscapeDataString(safeReturn)}");
            }

            var email = form["email"].ToString().Trim();

            if (!IsDemoLoginEnabled(http, configuration)
                || !users.TryFindByEmail(email, out var user)
                || user is null)
            {
                return Results.Redirect($"/login?error=invalid&returnUrl={Uri.EscapeDataString(safeReturn)}");
            }

            var principal = CreateLocalPrincipal(user);
            var showHowTo = principal.HasClaim(c =>
                c.Type == "show_candidate_how_to" && c.Value == "1");
            if (showHowTo
                || principal.IsInRole("Candidate")
                || principal.HasClaim(ClaimTypes.Role, "Candidate"))
            {
                returnUrl = AuthRedirects.ResolveCandidateReturnUrl(returnUrl, showHowTo);
            }

            var rememberDevice = string.Equals(
                form["rememberDevice"].ToString(),
                "true",
                StringComparison.OrdinalIgnoreCase);
            if (rememberDevice && principal.Identity is ClaimsIdentity demoIdentity)
            {
                await TryAttachProvisionedDeviceSessionAsync(http, configuration, demoIdentity, email);
            }

            await http.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                CreateSessionAuthProperties());
            StampLastActivity(http);

            return Results.Redirect(AuthRedirects.SafeLocalUrl(returnUrl));
        }).RequireRateLimiting("auth");

        app.MapGet("/account/external/{provider}", async (
            string provider,
            HttpContext http,
            IExternalAuthCredentialSource credentials,
            IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
            IOptionsMonitor<Microsoft.AspNetCore.Authentication.Google.GoogleOptions> googleOptions,
            string? returnUrl) =>
        {
            var dest = AuthRedirects.ResolveRequestedReturnUrl(
                returnUrl,
                http.Request.Query["returnTo"],
                http.Request.Query["redirect"]);

            var scheme = provider.Equals("entra", StringComparison.OrdinalIgnoreCase) ? EntraScheme
                : provider.Equals("google", StringComparison.OrdinalIgnoreCase) ? GoogleScheme
                : null;

            if (scheme is null)
            {
                return Results.Redirect(AuthRedirects.AppendReturnUrl("/login?error=unknown-provider", dest));
            }

            if (scheme == EntraScheme)
            {
                var entra = await credentials.GetEntraAsync(http.RequestAborted);
                if (entra is null)
                {
                    return Results.Redirect(AuthRedirects.AppendReturnUrl("/login?error=entra-not-configured", dest));
                }

                EntraOidcOptionsApplier.Apply(oidcOptions.Get(EntraScheme), entra);
            }
            else
            {
                var google = await credentials.GetGoogleAsync(http.RequestAborted);
                if (google is null)
                {
                    return Results.Redirect(AuthRedirects.AppendReturnUrl("/login?error=google-not-configured", dest));
                }

                var options = googleOptions.Get(GoogleScheme);
                lock (GoogleOptionsSync)
                {
                    options.ClientId = google.ClientId;
                    options.ClientSecret = google.ClientSecret;
                }
            }

            var props = new AuthenticationProperties
            {
                RedirectUri = dest
            };

            return Results.Challenge(props, [scheme]);
        });

        app.MapMethods("/account/logout", ["GET", "POST"], async (HttpContext http) =>
        {
            // POST from the header form uses antiforgery; GET covers refresh / Cookie LogoutPath / bookmarks.
            // Stale antiforgery must not block logout (GET already signs out) or show /Error.
            if (HttpMethods.IsPost(http.Request.Method))
            {
                var antiforgery = http.RequestServices.GetRequiredService<IAntiforgery>();
                if (!await antiforgery.IsRequestValidAsync(http))
                {
                    var logger = http.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("Jobsy.Web.Auth.Logout");
                    logger.LogInformation("Logout POST without valid antiforgery; signing out anyway.");
                }
            }

            var reason = http.Request.Query["reason"].ToString();
            var deviceToken = DeviceSessionCookie.Read(http);
            var deviceSessionId = http.User.FindFirst(JobsyClaimTypes.DeviceSessionId)?.Value;

            // Best-effort revoke + push unsubscribe before clearing cookies.
            try
            {
                await RevokeCurrentDeviceOnLogoutAsync(http, deviceToken, deviceSessionId);
            }
            catch
            {
                // Always continue with local sign-out.
            }

            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            SessionActivityCookie.Clear(http);
            DeviceSessionCookie.Clear(http);

            if (string.Equals(reason, "session-expired", StringComparison.OrdinalIgnoreCase))
            {
                var returnUrl = AuthRedirects.ResolveSessionReturnUrl(
                    http.Request.Query["returnUrl"],
                    http.Request.Query["returnTo"],
                    http.Request.Query["redirect"]);
                return Results.Redirect(
                    AuthRedirects.AppendReturnUrl(SessionInactivityMiddleware.SessionExpiredPath, returnUrl));
            }

            return Results.Redirect("/");
        });

        // In-scope exchange after Google/Entra (iOS standalone PWA cookie jar).
        app.MapGet("/account/complete-login", async (HttpContext http, string? code, string? returnUrl) =>
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Results.Redirect("/login?error=retry");
            }

            var config = http.RequestServices.GetRequiredService<IConfiguration>();
            var factory = http.RequestServices.GetRequiredService<IHttpClientFactory>();
            var apiBase = JobsyPublicUrl.NormalizeBaseUrl(config["ApiBaseUrl"], "http://localhost:5200/");
            var client = factory.CreateClient("JobsyAuthProvision");
            client.BaseAddress = new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(8);

            using var response = await client.PostAsJsonAsync(
                "api/auth/device-sessions/handoff/exchange",
                new { code, userAgent = http.Request.Headers.UserAgent.ToString() });
            if (!response.IsSuccessStatusCode)
            {
                return Results.Redirect("/login?error=retry");
            }

            var profile = await response.Content.ReadFromJsonAsync<HandoffExchangeProfile>();
            if (profile is null || string.IsNullOrWhiteSpace(profile.Email))
            {
                return Results.Redirect("/login?error=retry");
            }

            var refreshProfile = new DeviceSessionRefreshMiddleware.RefreshProfile
            {
                Email = profile.Email,
                FullName = profile.FullName,
                Role = profile.Role,
                CompanyId = profile.CompanyId,
                CompanyIds = profile.CompanyIds,
                ShowCandidateHowTo = profile.ShowCandidateHowTo,
                HasCandidateApplications = profile.HasCandidateApplications,
                HasSalesReferral = profile.HasSalesReferral,
                SessionVersion = profile.SessionVersion,
                SessionToken = profile.SessionToken,
                DeviceSessionId = profile.DeviceSessionId ?? Guid.Empty,
                RefreshToken = profile.RefreshToken,
                ExpiresAtUtc = profile.DeviceExpiresAtUtc
            };
            var principal = AuthPrincipalFactory.FromDeviceRefresh(refreshProfile);
            if (profile.DeviceSessionId is null && principal.Identity is ClaimsIdentity id)
            {
                foreach (var c in id.FindAll(JobsyClaimTypes.DeviceSessionId).ToList())
                {
                    id.RemoveClaim(c);
                }

                foreach (var c in id.FindAll(JobsyClaimTypes.HasDeviceSession).ToList())
                {
                    id.RemoveClaim(c);
                }

                AuthPrincipalFactory.StampSessionVersion(id, profile.SessionVersion);
            }

            await http.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                CreateSessionAuthProperties());
            StampLastActivity(http);
            if (!string.IsNullOrWhiteSpace(profile.RefreshToken) && profile.DeviceExpiresAtUtc is DateTime exp)
            {
                DeviceSessionCookie.Set(http, profile.RefreshToken, exp);
            }

            var dest = AuthRedirects.SafeLocalUrl(
                profile.ReturnUrl ?? returnUrl ?? "/home");
            if (profile.ShowCandidateHowTo
                || string.Equals(profile.Role, "Candidate", StringComparison.OrdinalIgnoreCase))
            {
                dest = AuthRedirects.SafeLocalUrl(
                    AuthRedirects.ResolveCandidateReturnUrl(dest, profile.ShowCandidateHowTo));
            }

            return Results.Redirect(dest);
        }).AllowAnonymous().DisableAntiforgery();

        // Idle-timer beacon (no antiforgery): refreshes LastActivity for authenticated users only.
        // Also slides the local-session HMAC so API auth stays valid while the cookie is alive.
        // Returns JSON 401 when middleware already expired the session (Accept: application/json).
        app.MapMethods("/account/session-activity", ["GET", "POST"], async (HttpContext http) =>
        {
            if (http.User.Identity?.IsAuthenticated != true)
            {
                return Results.Json(
                    new { ok = false, reason = "session-expired" },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            StampLastActivity(http);
            await RefreshLocalSessionCookieAsync(http);
            return Results.Json(new { ok = true });
        }).AllowAnonymous().DisableAntiforgery();

        // Same-origin proxy for the browser idle timer (avoids cross-origin API CORS issues).
        app.MapGet("/account/session-security", async (HttpContext http, CancellationToken ct) =>
        {
            var timeouts = http.RequestServices.GetRequiredService<ISessionTimeoutProvider>();
            var minutes = await timeouts.GetInactivityTimeoutMinutesAsync(ct);
            return Results.Json(new { inactivityTimeoutMinutes = minutes });
        }).AllowAnonymous().DisableAntiforgery();
    }

    public static AuthenticationProperties CreateSessionAuthPropertiesPublic()
        => CreateSessionAuthProperties();

    private static AuthenticationProperties CreateSessionAuthProperties() =>
        new()
        {
            IsPersistent = true,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(SessionSecurityRules.MaxInactivityTimeoutMinutes)
        };

    private static void StampLastActivity(HttpContext http)
        => SessionActivityCookie.Stamp(http, DateTimeOffset.UtcNow);

    /// <summary>
    /// Re-mints <see cref="JobsyClaimTypes.LocalSession"/> on activity so absolute HMAC
    /// expiry cannot outpace the sliding cookie for non-demo Production users.
    /// </summary>
    private static async Task RefreshLocalSessionCookieAsync(HttpContext http)
    {
        if (http.User.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return;
        }

        var existing = identity.FindFirst(JobsyClaimTypes.LocalSession)?.Value;
        if (string.IsNullOrWhiteSpace(existing))
        {
            return;
        }

        var config = http.RequestServices.GetRequiredService<IConfiguration>();
        var key = JobsyLocalSessionToken.ResolveSigningKey(
            config["JobsyAuth:LocalSessionSigningKey"],
            config["JobsyAuth:DevelopmentAuthSecret"]);
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        // Allow refresh of recently expired tokens while the auth cookie is still valid.
        if (!JobsyLocalSessionToken.TryReadSignedPayload(
                existing,
                key,
                ignoreExpiry: true,
                out var email,
                out var userId,
                out _))
        {
            return;
        }

        var cookieEmail = identity.FindFirst(ClaimTypes.Email)?.Value
                          ?? identity.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(cookieEmail)
            || !string.Equals(cookieEmail, email, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var fresh = JobsyLocalSessionToken.Create(email, userId, key);
        foreach (var claim in identity.FindAll(JobsyClaimTypes.LocalSession).ToList())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim(JobsyClaimTypes.LocalSession, fresh));
        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            http.User,
            CreateSessionAuthProperties());
    }

    private static bool IsDemoLoginEnabled(HttpContext http, IConfiguration configuration)
    {
        var env = http.RequestServices.GetRequiredService<IHostEnvironment>();
        return env.IsDevelopment()
               || configuration.GetValue("JobsyAuth:AllowDevelopmentAuth", false);
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
        var profile = await TryLocalApiLoginProfileAsync(configuration, email, password, rememberDevice: false);
        return profile is null ? null : CreatePrincipalFromProfile(profile, "local-registration");
    }

    private static async Task<LocalApiLoginProfile?> TryLocalApiLoginProfileAsync(
        IConfiguration configuration,
        string email,
        string password,
        bool rememberDevice)
    {
        try
        {
            var apiBase = JobsyPublicUrl.NormalizeBaseUrl(
                configuration["ApiBaseUrl"],
                "http://localhost:5200/");
            using var client = new HttpClient { BaseAddress = new Uri(apiBase), Timeout = TimeSpan.FromSeconds(8) };
            using var response = await client.PostAsJsonAsync(
                "api/auth/local-login",
                new { email, password, rememberDevice });
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var profile = await response.Content.ReadFromJsonAsync<LocalApiLoginProfile>();
            if (profile is null || string.IsNullOrWhiteSpace(profile.Email))
            {
                return null;
            }

            return profile;
        }
        catch
        {
            return null;
        }
    }

    private static async Task TryAttachProvisionedDeviceSessionAsync(
        HttpContext http,
        IConfiguration configuration,
        ClaimsIdentity identity,
        string email)
    {
        try
        {
            var secret = configuration["JobsyAuth:ExternalProvisionSecret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                return;
            }

            var apiBase = JobsyPublicUrl.NormalizeBaseUrl(
                configuration["ApiBaseUrl"],
                "http://localhost:5200/");
            var factory = http.RequestServices.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("JobsyAuthProvision");
            client.BaseAddress = new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(8);
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/device-sessions/for-login")
            {
                Content = JsonContent.Create(new
                {
                    email,
                    rememberDevice = true,
                    userAgent = http.Request.Headers.UserAgent.ToString()
                })
            };
            request.Headers.TryAddWithoutValidation("X-Jobsy-Provision-Secret", secret);
            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            var created = await response.Content.ReadFromJsonAsync<ProvisionedDeviceSession>();
            if (created is null || string.IsNullOrWhiteSpace(created.RefreshToken))
            {
                return;
            }

            AuthPrincipalFactory.StampDeviceClaims(identity, created.DeviceSessionId, created.SessionVersion);
            DeviceSessionCookie.Set(http, created.RefreshToken, created.ExpiresAtUtc);
        }
        catch
        {
            // Device remember is best-effort for demo path.
        }
    }

    private static async Task RevokeCurrentDeviceOnLogoutAsync(
        HttpContext http,
        string? deviceToken,
        string? deviceSessionId)
    {
        var config = http.RequestServices.GetRequiredService<IConfiguration>();
        var factory = http.RequestServices.GetRequiredService<IHttpClientFactory>();
        var apiBase = JobsyPublicUrl.NormalizeBaseUrl(config["ApiBaseUrl"], "http://localhost:5200/");
        var client = factory.CreateClient("JobsyAuthProvision");
        client.BaseAddress = new Uri(apiBase);
        client.Timeout = TimeSpan.FromSeconds(5);

        // Forward local session so the API can authorize the revoke.
        var localSession = http.User.FindFirst(JobsyClaimTypes.LocalSession)?.Value;
        var email = http.User.FindFirst(ClaimTypes.Email)?.Value
                    ?? http.User.FindFirst("email")?.Value;
        if (!string.IsNullOrWhiteSpace(email))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Jobsy-Email", email);
        }

        var role = http.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.IsNullOrWhiteSpace(role))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Jobsy-Role", role);
        }

        var devSecret = config["JobsyAuth:DevelopmentAuthSecret"];
        if (!string.IsNullOrWhiteSpace(devSecret))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Jobsy-Dev-Secret", devSecret);
        }

        if (!string.IsNullOrWhiteSpace(localSession))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Jobsy-Local-Session", localSession);
        }

        if (Guid.TryParse(deviceSessionId, out var id))
        {
            await client.DeleteAsync($"api/auth/device-sessions/{id}");
        }

        // Also ask push unsubscribe for this browser endpoint when possible — handled client-side too.
        _ = deviceToken;
    }

    private static async Task ValidatePrincipalSessionVersionAsync(CookieValidatePrincipalContext context)
    {
        var user = context.Principal;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var claimVersionText = user.FindFirst(JobsyClaimTypes.SessionVersion)?.Value;
        if (!int.TryParse(claimVersionText, out var claimVersion))
        {
            // Legacy cookies without session_version stay valid until next login.
            return;
        }

        var email = user.FindFirst(ClaimTypes.Email)?.Value
                    ?? user.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
        var cacheKey = $"session-validity:{email.Trim().ToLowerInvariant()}";
        if (!cache.TryGetValue(cacheKey, out SessionValidityCacheEntry? entry) || entry is null)
        {
            entry = await FetchSessionValidityAsync(context.HttpContext, email);
            if (entry is not null)
            {
                cache.Set(cacheKey, entry, TimeSpan.FromSeconds(45));
            }
        }

        if (entry is null)
        {
            return;
        }

        if (claimVersion < entry.SessionVersion || claimVersion < entry.MinimumSessionVersion)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            DeviceSessionCookie.Clear(context.HttpContext);
            SessionActivityCookie.Clear(context.HttpContext);
        }
    }

    private static async Task<SessionValidityCacheEntry?> FetchSessionValidityAsync(HttpContext http, string email)
    {
        try
        {
            var config = http.RequestServices.GetRequiredService<IConfiguration>();
            var factory = http.RequestServices.GetRequiredService<IHttpClientFactory>();
            var apiBase = JobsyPublicUrl.NormalizeBaseUrl(config["ApiBaseUrl"], "http://localhost:5200/");
            var client = factory.CreateClient("JobsyAuthProvision");
            client.BaseAddress = new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(3);
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Jobsy-Email", email);
            var role = http.User.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrWhiteSpace(role))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-Jobsy-Role", role);
            }

            var localSession = http.User.FindFirst(JobsyClaimTypes.LocalSession)?.Value;
            if (!string.IsNullOrWhiteSpace(localSession))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-Jobsy-Local-Session", localSession);
            }

            var devSecret = config["JobsyAuth:DevelopmentAuthSecret"];
            if (!string.IsNullOrWhiteSpace(devSecret))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-Jobsy-Dev-Secret", devSecret);
            }

            using var response = await client.GetAsync("api/auth/device-sessions/validity");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var dto = await response.Content.ReadFromJsonAsync<SessionValidityCacheEntry>();
            return dto;
        }
        catch
        {
            return null;
        }
    }

    private sealed class SessionValidityCacheEntry
    {
        public int SessionVersion { get; set; }
        public int MinimumSessionVersion { get; set; }
    }

    private sealed class ProvisionedDeviceSession
    {
        public Guid DeviceSessionId { get; set; }
        public string RefreshToken { get; set; } = "";
        public DateTime ExpiresAtUtc { get; set; }
        public int SessionVersion { get; set; }
    }

    private sealed class HandoffExchangeProfile
    {
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
        public string? ReturnUrl { get; set; }
        public Guid? DeviceSessionId { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? DeviceExpiresAtUtc { get; set; }
    }

    private static bool IsEmailUnverified(ClaimsPrincipal? principal)
    {
        var value = principal?.FindFirst("email_verified")?.Value;
        return string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
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

        // Providers may assert email_verified=false; reject those earlier in OIDC events.
        if (!identity.HasClaim(c => c.Type == "email_verified"))
        {
            identity.AddClaim(new Claim("email_verified", "true"));
        }

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
        // Prefer Entra OID / OIDC sub over e-mail for stable account binding.
        var providerSubject = identity.FindFirst("oid")?.Value
                              ?? identity.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                              ?? identity.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? identity.FindFirst("sub")?.Value;
        var authScheme = identity.AuthenticationType ?? "";
        var provider = authScheme.Contains("Google", StringComparison.OrdinalIgnoreCase)
            ? "google"
            : "entra";
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

            var secret = config["JobsyAuth:ExternalProvisionSecret"];
            string? referralCode = null;
            if (http.Request.Cookies.TryGetValue("lobsy_ambassadeur_ref", out var cookieRef)
                && !string.IsNullOrWhiteSpace(cookieRef))
            {
                referralCode = cookieRef.Trim();
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/ensure-external")
            {
                Content = JsonContent.Create(new
                {
                    email,
                    fullName,
                    provider,
                    providerSubject,
                    referralCode,
                    rememberDevice = true,
                    returnUrl = properties?.RedirectUri,
                    userAgent = http.Request.Headers.UserAgent.ToString()
                })
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

            if (properties is not null && !string.IsNullOrWhiteSpace(profile.HandoffCode))
            {
                // Finish inside the PWA scope via one-time code (iOS standalone cookie jar).
                var returnDest = properties.RedirectUri ?? "/home";
                var role = NormalizeRole(profile.Role);
                if (profile.ShowCandidateHowTo || role == "Candidate")
                {
                    returnDest = AuthRedirects.ResolveCandidateReturnUrl(
                        returnDest,
                        profile.ShowCandidateHowTo);
                }

                properties.RedirectUri =
                    $"/account/complete-login?code={Uri.EscapeDataString(profile.HandoffCode)}" +
                    $"&returnUrl={Uri.EscapeDataString(AuthRedirects.SafeLocalUrl(returnDest))}";
            }
            else if (properties is not null)
            {
                var role = NormalizeRole(profile.Role);
                if (profile.ShowCandidateHowTo || role == "Candidate")
                {
                    properties.RedirectUri = AuthRedirects.SafeLocalUrl(
                        AuthRedirects.ResolveCandidateReturnUrl(
                            properties.RedirectUri ?? "/home",
                            profile.ShowCandidateHowTo));
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

        foreach (var existing in identity.FindAll(JobsyClaimTypes.HasSalesReferral).ToList())
        {
            identity.RemoveClaim(existing);
        }

        foreach (var existing in identity.FindAll(JobsyClaimTypes.LocalSession).ToList())
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

        if (profile.HasSalesReferral)
        {
            identity.AddClaim(new Claim(JobsyClaimTypes.HasSalesReferral, "1"));
        }

        if (!string.IsNullOrWhiteSpace(profile.SessionToken))
        {
            identity.AddClaim(new Claim(JobsyClaimTypes.LocalSession, profile.SessionToken));
        }

        AuthPrincipalFactory.StampSessionVersion(identity, profile.SessionVersion);

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
        public bool HasSalesReferral { get; set; }
        public bool IsNewUser { get; set; }
        public string? SessionToken { get; set; }
        public int SessionVersion { get; set; }
        public Guid? DeviceSessionId { get; set; }
        public string? DeviceRefreshToken { get; set; }
        public DateTime? DeviceExpiresAtUtc { get; set; }
        public string? HandoffCode { get; set; }
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

    public bool TryFindByEmail(string email, out DemoUserOptions? user)
    {
        user = _users.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        return user is not null;
    }
}
