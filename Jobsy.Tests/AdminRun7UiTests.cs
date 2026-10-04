using System.Net;
using System.Security.Claims;
using System.Text;
using Bunit;
using Bunit.TestDoubles;
using Jobsy.Web.Components.Admin.Sections;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class AdminRun7UiTests : BunitContext
{
    private readonly ApiHandler _handler = new();

    public AdminRun7UiTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var admin = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "ffffffff-1111-1111-1111-111111111111"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Name, "Admin")
        ], "test"));
        Services.AddSingleton<AuthenticationStateProvider>(new AuthStub(admin));
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddLogging();
        Services.AddSingleton<UserFacingError>();
        Services.AddAuthorizationCore();
        this.AddAuthorization().SetAuthorized("admin").SetRoles("Admin");
        Services.AddSingleton(new JobsyApiClient(new HttpClient(_handler) { BaseAddress = new Uri("http://localhost") }));
        SetRendererInfo(new RendererInfo("Server", true));
    }

    [Fact]
    public async Task Reset_form_is_hidden_for_real_users_and_shows_confirm_and_api_errors()
    {
        var cut = Render<UsersAdminSection>();
        cut.WaitForAssertion(() => Assert.Contains("Gewone Gebruiker", cut.Markup, StringComparison.Ordinal), TimeSpan.FromSeconds(3));

        await cut.FindAll("tr.admin-user-row").First(r => r.TextContent.Contains("Gewone Gebruiker", StringComparison.Ordinal)).ClickAsync();
        cut.WaitForAssertion(() => Assert.Contains("Gewone Gebruiker", cut.Find(".admin-drawer__title").TextContent, StringComparison.Ordinal));
        Assert.Empty(cut.FindAll(".admin-user-drawer__reset"));

        await cut.FindAll("tr.admin-user-row").First(r => r.TextContent.Contains("Test Persoon", StringComparison.Ordinal)).ClickAsync();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".admin-user-drawer__reset")));

        var input = cut.Find(".admin-user-drawer__reset input");
        Assert.Equal("5", input.GetAttribute("minlength"));
        var submit = cut.Find(".admin-user-drawer__reset button");
        Assert.True(submit.HasAttribute("disabled"));
        Assert.Contains("Uitgebreide tests resetten", submit.TextContent, StringComparison.Ordinal);

        await input.InputAsync("abc");
        Assert.True(cut.Find(".admin-user-drawer__reset button").HasAttribute("disabled"));

        await input.InputAsync("abcde");
        submit = cut.Find(".admin-user-drawer__reset button");
        Assert.False(submit.HasAttribute("disabled"));
        await submit.ClickAsync();

        cut.WaitForAssertion(() =>
            Assert.Contains(
                "Uitgebreide tests van Test Persoon weer op slot zetten? Antwoorden en rapporten worden gewist. Facturen blijven staan.",
                cut.Markup,
                StringComparison.Ordinal));
        Assert.Contains("Annuleren", cut.Markup, StringComparison.Ordinal);

        await cut.Find(".lobsy-dialog .btn-compact--danger").ClickAsync();
        cut.WaitForAssertion(() =>
            Assert.Contains("Geef een reden van 5 tot 500 tekens.", cut.Find(".admin-user-drawer__reset").TextContent, StringComparison.Ordinal));
    }

    [Fact]
    public void Misuse_tab_shows_the_open_count_and_is_the_active_item()
    {
        var cut = Render<BeveiligingTabs>(p => p.Add(x => x.ActiveKey, "misuse"));
        cut.WaitForAssertion(() => Assert.Contains("admin-tabs__count", cut.Markup, StringComparison.Ordinal), TimeSpan.FromSeconds(3));
        var active = cut.Find(".admin-tabs__tab.is-active");
        Assert.Contains("Meldingen referent", active.TextContent, StringComparison.Ordinal);
        Assert.Equal("1", cut.Find(".admin-tabs__count").TextContent.Trim());
        Assert.DoesNotContain("Auditlog", active.TextContent, StringComparison.Ordinal);
    }

    private sealed class AuthStub(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(user));
    }

    private sealed class ApiHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("/test-unlock/reset", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                        """{"message":"Geef een reden van 5 tot 500 tekens."}""",
                        Encoding.UTF8,
                        "application/json")
                });
            }

            if (path.Contains("/reference-misuse", StringComparison.Ordinal))
            {
                return Task.FromResult(Json(
                    """
                    [
                      {"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","createdAtUtc":"2026-10-01T10:00:00Z","candidateMasked":"A. B.","message":"Klopt niet","status":"open"},
                      {"id":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","createdAtUtc":"2026-10-01T11:00:00Z","candidateMasked":"C. D.","message":"Afgehandeld","status":"handled"}
                    ]
                    """));
            }

            if (path.Contains("/sessions", StringComparison.Ordinal))
            {
                return Task.FromResult(Json("[]"));
            }

            if (path.Contains("/api/admin/users", StringComparison.Ordinal))
            {
                const string items =
                    """
                    {"id":"55555555-5555-5555-5555-555555555555","email":"test@jobsy.local","fullName":"Test Persoon","role":"Candidate","isActive":true,"mfaStatus":"not-enrolled","isTestAccount":true},
                    {"id":"66666666-6666-6666-6666-666666666666","email":"echt@jobsy.local","fullName":"Gewone Gebruiker","role":"Candidate","isActive":true,"mfaStatus":"enrolled","isTestAccount":false}
                    """;
                return Task.FromResult(Json(
                    "{\"aggregates\":{\"byRole\":{},\"topCompanies\":[],\"activeCount\":2,\"inactiveCount\":0,\"byRegistrationWeek\":[]},\"items\":["
                    + items
                    + "],\"page\":1,\"pageSize\":50,\"totalCount\":2,\"masked\":true}"));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage Json(string body)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
    }
}
