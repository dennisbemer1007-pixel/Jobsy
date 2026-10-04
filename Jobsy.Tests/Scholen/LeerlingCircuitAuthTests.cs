using System.Net;
using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Jobsy.Core.Authorization;
using Jobsy.Web.Components.Layout;
using Jobsy.Web.Localization;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Pupil cookie renewal registers OnStarting. Doing that from the interactive layout
/// throws "OnStarting cannot be set because the response has already started" and kills the circuit.
/// </summary>
public class LeerlingCircuitAuthTests : BunitContext
{
    public LeerlingCircuitAuthTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IAntiforgery>(new FakeAntiforgery());
    }

    [Fact]
    public void Pupil_pages_do_not_authenticate_during_render()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var layout = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "LeerlingLayout.razor"));
        var login = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Leerling", "LeerlingLogin.razor"));
        Assert.DoesNotContain(".AuthenticateAsync", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("OnStarting", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("Response.Headers", layout, StringComparison.Ordinal);
        Assert.DoesNotContain(".AuthenticateAsync", login, StringComparison.Ordinal);
    }

    [Fact]
    public void Interactive_layout_reads_middleware_principal_and_does_not_touch_the_response()
    {
        var throwing = new ThrowingAuth();
        var http = PupilHttpContext(throwing);
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = http });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/leerling/reis");

        var cut = Render<LeerlingLayout>(ps => ps.Add(p => p.Body, b => b.AddMarkupContent(0, "<p>reis</p>")));

        Assert.Contains("1A · ABC-123", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Pauze", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("reis", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(0, throwing.Calls);
    }

    [Fact]
    public void Layout_without_pupil_principal_stays_up()
    {
        var http = new DefaultHttpContext();
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = http });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/leerling");

        var cut = Render<LeerlingLayout>(ps => ps.Add(p => p.Body, b => b.AddMarkupContent(0, "<p>login</p>")));

        Assert.Contains("login", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("ll-topbar__chip", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Pauze", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Middleware_skips_auth_after_the_response_has_started()
    {
        var auth = new RecordingAuth(PupilPrincipal());
        var provider = new ServiceCollection().AddSingleton<IAuthenticationService>(auth).BuildServiceProvider();

        var started = new DefaultHttpContext { RequestServices = provider };
        started.Request.Path = "/leerling";
        MarkResponseStarted(started);
        await new LeerlingNoStoreMiddleware(_ => Task.CompletedTask).InvokeAsync(started);
        Assert.Equal(0, auth.Calls);

        var other = new DefaultHttpContext { RequestServices = provider };
        other.Request.Path = "/school";
        await new LeerlingNoStoreMiddleware(_ => Task.CompletedTask).InvokeAsync(other);
        Assert.Equal(0, auth.Calls);
    }

    [Fact]
    public async Task Middleware_sets_no_store_and_captures_the_pupil_on_the_http_request()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication()
            .AddCookie(PupilAuthDefaults.Scheme, options =>
            {
                options.Cookie.Name = PupilAuthDefaults.CookieName;
                options.SlidingExpiration = true;
            });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseMiddleware<LeerlingNoStoreMiddleware>();
        app.Run(async ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/leerling/sign-in"))
            {
                await ctx.SignInAsync(PupilAuthDefaults.Scheme, PupilPrincipal());
                ctx.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }

            if (ctx.Request.Path.StartsWithSegments("/leerling"))
            {
                var seen = ctx.Items[LeerlingNoStoreMiddleware.PupilPrincipalItemKey] is ClaimsPrincipal;
                ctx.Response.Headers["X-Pupil"] = seen ? "1" : "0";
            }

            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
        });

        await app.StartAsync();
        try
        {
            var client = app.GetTestClient();
            var signIn = await client.GetAsync("/leerling/sign-in");
            Assert.Equal(HttpStatusCode.NoContent, signIn.StatusCode);
            Assert.Equal("no-store", signIn.Headers.CacheControl?.ToString());
            Assert.True(signIn.Headers.TryGetValues("Set-Cookie", out var setCookie));
            var cookie = string.Join("; ", setCookie!.Select(c => c.Split(';', 2)[0]));

            var reis = new HttpRequestMessage(HttpMethod.Get, "/leerling/reis");
            reis.Headers.TryAddWithoutValidation("Cookie", cookie);
            var page = await client.SendAsync(reis);
            Assert.Equal(HttpStatusCode.NoContent, page.StatusCode);
            Assert.Equal("no-store", page.Headers.CacheControl?.ToString());
            Assert.True(page.Headers.TryGetValues("X-Pupil", out var pupil) && pupil.Single() == "1");

            var school = await client.GetAsync("/school");
            Assert.False(school.Headers.Contains("X-Pupil"));
            Assert.Null(school.Headers.CacheControl);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private static void MarkResponseStarted(HttpContext http)
    {
        http.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(new StartedResponseFeature());
        Assert.True(http.Response.HasStarted);
    }

    private sealed class StartedResponseFeature : Microsoft.AspNetCore.Http.Features.IHttpResponseFeature
    {
        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted => true;

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }

        public void OnStarting(Func<object, Task> callback, object state)
            => throw new InvalidOperationException(
                "OnStarting cannot be set because the response has already started");
    }

    private static DefaultHttpContext PupilHttpContext(IAuthenticationService auth)
    {
        var http = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(auth).BuildServiceProvider(),
            Response = { Body = new MemoryStream() }
        };
        http.Items[LeerlingNoStoreMiddleware.PupilPrincipalItemKey] = PupilPrincipal();
        return http;
    }

    private static ClaimsPrincipal PupilPrincipal()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(PupilClaimTypes.ClassLabel, "1A"),
                new Claim(PupilClaimTypes.CodeDisplay, "ABC-123"),
                new Claim(PupilClaimTypes.PupilCodeId, Guid.NewGuid().ToString("D"))
            ],
            PupilAuthDefaults.Scheme);
        return new ClaimsPrincipal(identity);
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class ThrowingAuth : IAuthenticationService
    {
        public int Calls { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        {
            Calls++;
            throw new InvalidOperationException(
                "OnStarting cannot be set because the response has already started");
        }

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => throw new NotSupportedException();

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => throw new NotSupportedException();

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
            => throw new NotSupportedException();

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => throw new NotSupportedException();
    }

    private sealed class RecordingAuth(ClaimsPrincipal principal) : IAuthenticationService
    {
        public int Calls { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        {
            Calls++;
            if (context.Response.HasStarted)
            {
                throw new InvalidOperationException(
                    "OnStarting cannot be set because the response has already started");
            }

            Assert.Equal(PupilAuthDefaults.Scheme, scheme);
            context.Response.OnStarting(() => Task.CompletedTask);
            var ticket = new AuthenticationTicket(principal, PupilAuthDefaults.Scheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
    }

    private sealed class FakeAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
            => new("fake-request-token", "fake-cookie-token", "__RequestVerificationToken", "X-CSRF");

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => GetAndStoreTokens(httpContext);

        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);

        public void SetCookieTokenAndHeader(HttpContext httpContext)
        {
        }

        public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;
    }
}
