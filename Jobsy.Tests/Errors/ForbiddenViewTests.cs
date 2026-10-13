using System.Security.Claims;
using Bunit;
using Jobsy.Web.Components;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Errors;

/// <summary>
/// Errors 02 (bUnit): the account card comes from claims only — no API client is even
/// registered, so a bug that tried to call one would throw instead of silently rendering.
/// </summary>
public class ForbiddenViewTests : BunitContext
{
    public ForbiddenViewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuthStateProvider());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<IAntiforgery>(new FakeAntiforgery());
    }

    private static DefaultHttpContext SignedInHttpContext(string role, string email, string name)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name)
            },
            authenticationType: "Test");
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    [Fact]
    public void Account_card_shows_name_masked_email_and_role_from_claims_only()
    {
        var http = SignedInHttpContext("Candidate", "kim.kandidaat@example.nl", "Kim Kandidaat");
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = http });

        var cut = Render<AccessDeniedView>(p => p
            .Add(x => x.ReturnUrl, "/admin/users")
            .Add(x => x.Reason, "role"));

        var markup = cut.Markup;
        Assert.Contains("Kim Kandidaat", markup, StringComparison.Ordinal);
        Assert.Contains("k***@example.nl", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("kim.kandidaat@example.nl", markup, StringComparison.Ordinal);
        Assert.Contains("Kandidaat", markup, StringComparison.Ordinal);
        Assert.Equal(StatusCodes.Status403Forbidden, http.Response.StatusCode);
    }

    [Fact]
    public void No_name_claim_shows_only_the_masked_email()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Role, "Candidate"), new Claim(ClaimTypes.Email, "ab@b.nl") },
            authenticationType: "Test");
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = http });

        var cut = Render<AccessDeniedView>(p => p.Add(x => x.ReturnUrl, "/admin"));

        Assert.Contains("a***@b.nl", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("err-account__role", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Switch_account_form_posts_to_logout_with_reason_switch_and_the_return_url()
    {
        var http = SignedInHttpContext("Candidate", "kim@example.nl", "Kim");
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = http });

        var cut = Render<AccessDeniedView>(p => p
            .Add(x => x.ReturnUrl, "/admin/users")
            .Add(x => x.Reason, "role"));

        var form = cut.Find("form");
        Assert.Equal("post", form.GetAttribute("method"));
        Assert.Equal("/account/logout", form.GetAttribute("action"));

        var hidden = cut.FindAll("form input[type=hidden]")
            .ToDictionary(e => e.GetAttribute("name")!, e => e.GetAttribute("value"));
        Assert.Equal("switch", hidden["reason"]);
        Assert.Equal("/admin/users", hidden["returnUrl"]);
    }

    [Fact]
    public void Employers_off_reason_hides_the_switch_account_button()
    {
        var http = SignedInHttpContext("Candidate", "kim@example.nl", "Kim");
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = http });

        var cut = Render<AccessDeniedView>(p => p
            .Add(x => x.ReturnUrl, "/banenkaart")
            .Add(x => x.Reason, "employers-off"));

        Assert.Empty(cut.FindAll("form"));
        Assert.Contains(
            "Lobsy is nu eerst voor kandidaten",
            cut.Markup,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_reason_falls_back_to_role_copy()
    {
        var http = SignedInHttpContext("Candidate", "kim@example.nl", "Kim");
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = http });

        var cut = Render<AccessDeniedView>(p => p
            .Add(x => x.ReturnUrl, "/admin")
            .Add(x => x.Reason, "something-else"));

        Assert.Contains("Deze pagina is niet voor jouw account", cut.Markup, StringComparison.Ordinal);
        Assert.NotEmpty(cut.FindAll("form"));
    }

    private sealed class FakeAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
            => new("fake-request-token", "fake-cookie-token", "__RequestVerificationToken", "X-CSRF");

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext)
            => GetAndStoreTokens(httpContext);

        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);

        public void SetCookieTokenAndHeader(HttpContext httpContext)
        {
        }

        public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;
    }

    private sealed class AnonymousAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
