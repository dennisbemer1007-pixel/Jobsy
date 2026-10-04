using System.Net;
using System.Security.Claims;
using Bunit;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Web.Auth;
using Jobsy.Web.Components;
using Jobsy.Web.Components.Layout;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Scholen;

public class PupilCircuitAuthorizationTests
{
    private static readonly Guid CodeId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public async Task Pupil_principal_passes_PupilSession_and_fails_staff_policies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(AuthServiceCollectionExtensions.ConfigureAuthorization);
        await using var provider = services.BuildServiceProvider();
        var auth = provider.GetRequiredService<IAuthorizationService>();
        var options = new AuthorizationOptions();
        AuthServiceCollectionExtensions.ConfigureAuthorization(options);

        var pupil = PupilPrincipal();
        Assert.True((await auth.AuthorizeAsync(pupil, JobsyPolicies.PupilSession)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(pupil, null, options.DefaultPolicy)).Succeeded);

        var admin = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole("Admin")
            .Build();
        Assert.False((await auth.AuthorizeAsync(pupil, admin)).Succeeded);

        var teacher = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole("Teacher")
            .Build();
        Assert.False((await auth.AuthorizeAsync(pupil, teacher)).Succeeded);

        var staff = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Teacher")],
            CookieAuthenticationDefaults.AuthenticationScheme));
        Assert.True((await auth.AuthorizeAsync(staff, null, options.DefaultPolicy)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(staff, JobsyPolicies.PupilSession)).Succeeded);
    }

    [Fact]
    public async Task Blazor_circuit_uses_the_pupil_scheme_and_staff_pages_do_not()
    {
        var store = new PupilApiTicketStore();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(store);
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => options.Cookie.Name = "Jobsy.Auth")
            .AddCookie(PupilAuthDefaults.Scheme, options => options.Cookie.Name = PupilAuthDefaults.CookieName);
        builder.Services.AddAuthorization(AuthServiceCollectionExtensions.ConfigureAuthorization);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<PupilCircuitAuthenticationMiddleware>();
        app.Run(async ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/leerling/sign-in"))
            {
                await ctx.SignInAsync(PupilAuthDefaults.Scheme, PupilPrincipal());
                PupilApiSessionCookie.Set(ctx, "api-ticket-1");
                ctx.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }

            if (ctx.Request.Path.StartsWithSegments("/staff/sign-in"))
            {
                var staff = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Role, "Teacher"), new Claim(ClaimTypes.Name, "leraar")],
                    CookieAuthenticationDefaults.AuthenticationScheme));
                await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, staff);
                ctx.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }

            ctx.Response.Headers["X-Auth"] = ctx.User.Identity?.AuthenticationType ?? "none";
            ctx.Response.Headers["X-Pupil"] = JobsyApiAuthHandler.IsPupilPrincipal(ctx.User) ? "1" : "0";
            var code = ctx.User.FindFirst(PupilClaimTypes.PupilCodeId)?.Value;
            ctx.Response.Headers["X-Ticket"] = store.Get(code) ?? "";
            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
        });

        await app.StartAsync();
        try
        {
            var client = app.GetTestClient();
            var signIn = await client.GetAsync("/leerling/sign-in");
            var pupilCookie = CookiePair(signIn, PupilAuthDefaults.CookieName);
            var apiCookie = CookiePair(signIn, PupilApiSessionCookie.Name);

            var circuit = await client.SendAsync(WithCookies("/_blazor", pupilCookie, apiCookie));
            Assert.Equal(HttpStatusCode.NoContent, circuit.StatusCode);
            Assert.Equal(PupilAuthDefaults.Scheme, Header(circuit, "X-Auth"));
            Assert.Equal("1", Header(circuit, "X-Pupil"));
            Assert.Equal("api-ticket-1", Header(circuit, "X-Ticket"));

            var school = await client.SendAsync(WithCookies("/school", pupilCookie, apiCookie));
            Assert.Equal("none", Header(school, "X-Auth"));
            Assert.Equal("0", Header(school, "X-Pupil"));
            Assert.Equal("", Header(school, "X-Ticket"));

            var staffIn = await client.GetAsync("/staff/sign-in");
            var staffCookie = CookiePair(staffIn, "Jobsy.Auth");
            var mixed = await client.SendAsync(WithCookies("/_blazor", staffCookie, pupilCookie, apiCookie));
            Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, Header(mixed, "X-Auth"));
            Assert.Equal("0", Header(mixed, "X-Pupil"));
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("https://lobsy.test/leerling/start", true)]
    [InlineData("https://lobsy.test/leerling", true)]
    [InlineData("https://lobsy.test/login?returnUrl=/leerling/start", false)]
    [InlineData("https://lobsy.test/school", false)]
    public void Pupil_routes_are_not_the_staff_login(string uri, bool pupil)
        => Assert.Equal(pupil, LeerlingCircuitNavigation.IsPupilPath(uri));

    [Fact]
    public void Logged_in_without_answers_is_not_labelled_not_started()
    {
        Assert.Equal("Ingelogd", UiStrings.Get("School.Status.LoggedIn", "nl"));
        Assert.Equal("Nog niet gestart", UiStrings.Get("School.Status.NotStarted", "nl"));
        Assert.False(PupilPuzzleRoutes.IsBuilt("schelpenrij"));
        Assert.False(PupilPuzzleRoutes.IsBuilt(null));
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var reis = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Leerling", "LeerlingReis.razor"));
        Assert.Contains("PupilPuzzleRoutes.IsBuilt", reis, StringComparison.Ordinal);
        Assert.DoesNotContain("3 puzzelpauzes", File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Localization", "UiStringsScholen.cs")), StringComparison.Ordinal);
    }

    private static ClaimsPrincipal PupilPrincipal()
    {
        var claims = new List<Claim>
        {
            new(PupilClaimTypes.PupilCodeId, CodeId.ToString("D")),
            new(PupilClaimTypes.ClassId, Guid.NewGuid().ToString("D")),
            new(PupilClaimTypes.SchoolId, Guid.NewGuid().ToString("D")),
            new(PupilClaimTypes.SessionVersion, "1"),
            new(PupilClaimTypes.IssuedAt, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new(PupilClaimTypes.ClassLabel, "1A"),
            new(PupilClaimTypes.CodeDisplay, "ABC-123")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, PupilAuthDefaults.Scheme));
    }

    private static HttpRequestMessage WithCookies(string path, params string[] cookies)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, path);
        message.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies));
        return message;
    }

    private static string CookiePair(HttpResponseMessage response, string name)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var values));
        var match = values!.First(c => c.StartsWith(name + "=", StringComparison.Ordinal));
        return match.Split(';', 2)[0];
    }

    private static string Header(HttpResponseMessage response, string name)
        => response.Headers.TryGetValues(name, out var values) ? values.Single() : "";
}

public class PupilAuthorizeRouteViewTests : BunitContext
{
    public PupilAuthorizeRouteViewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        // bUnit registers a placeholder that throws until its own AddAuthorization runs.
        // The real policy must decide, so the placeholder is replaced.
        foreach (var descriptor in Services.Where(d =>
                     d.ServiceType == typeof(IAuthorizationService)
                     || d.ServiceType == typeof(IAuthorizationPolicyProvider)
                     || d.ServiceType == typeof(IAuthorizationHandlerProvider)).ToList())
        {
            Services.Remove(descriptor);
        }

        Services.AddAuthorizationCore(AuthServiceCollectionExtensions.ConfigureAuthorization);
        Services.AddSingleton<AuthenticationStateProvider>(new FixedAuth(PupilPrincipal()));
        Services.AddCascadingAuthenticationState();
    }

    [Fact]
    public void Pupil_principal_renders_the_authorized_page()
    {
        var cut = Render<AuthorizeRouteView>(ps => ps
            .Add(p => p.RouteData, new RouteData(typeof(PupilProbe), new Dictionary<string, object?>())));

        Assert.Contains("pupil-ok", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Circuit_layout_keeps_pause_from_the_authentication_state()
    {
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IAntiforgery>(new QuietAntiforgery());
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/leerling/reis");

        var cut = Render<LeerlingLayout>(ps => ps.Add(p => p.Body, b => b.AddMarkupContent(0, "<p>reis</p>")));

        Assert.Contains("1A · ABC-123", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Pauze", cut.Markup, StringComparison.Ordinal);
    }

    private static ClaimsPrincipal PupilPrincipal()
    {
        var claims = new List<Claim>
        {
            new(PupilClaimTypes.PupilCodeId, Guid.NewGuid().ToString("D")),
            new(PupilClaimTypes.ClassId, Guid.NewGuid().ToString("D")),
            new(PupilClaimTypes.SchoolId, Guid.NewGuid().ToString("D")),
            new(PupilClaimTypes.SessionVersion, "1"),
            new(PupilClaimTypes.IssuedAt, "1"),
            new(PupilClaimTypes.ClassLabel, "1A"),
            new(PupilClaimTypes.CodeDisplay, "ABC-123")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, PupilAuthDefaults.Scheme));
    }

    [Authorize(Policy = JobsyPolicies.PupilSession)]
    private sealed class PupilProbe : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => builder.AddMarkupContent(0, "<p>pupil-ok</p>");
    }

    private sealed class FixedAuth(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(principal));
    }

    private sealed class QuietAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
        {
            _ = httpContext;
            return new AntiforgeryTokenSet("req", "cookie", "form", "header");
        }

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => GetAndStoreTokens(httpContext);

        public Task<bool> IsRequestValidAsync(HttpContext httpContext)
        {
            _ = httpContext;
            return Task.FromResult(true);
        }

        public void ValidateRequest(HttpContext httpContext) => _ = httpContext;

        public Task ValidateRequestAsync(HttpContext httpContext)
        {
            _ = httpContext;
            return Task.CompletedTask;
        }

        public void SetCookieTokenAndHeader(HttpContext httpContext) => _ = httpContext;
    }
}
