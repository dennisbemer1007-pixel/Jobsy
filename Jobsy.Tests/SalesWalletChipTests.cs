using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Bunit;
using Bunit.TestDoubles;
using Jobsy.Web.Components.Layout;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class SalesWalletChipTests : TestContext
{
    [Fact]
    public void Anonymous_makes_zero_dashboard_calls()
    {
        var counter = Arrange(CreateAnonymous());
        var cut = RenderComponent<SalesWalletChip>();
        cut.WaitForState(() => cut.Instance is not null, TimeSpan.FromSeconds(1));

        Assert.Equal(0, counter.DashboardCalls);
        Assert.DoesNotContain("sales-wallet-chip", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Candidate_makes_zero_dashboard_calls()
    {
        var counter = Arrange(CreateUser("Candidate"));
        var cut = RenderComponent<SalesWalletChip>();
        cut.WaitForState(() => cut.Instance is not null, TimeSpan.FromSeconds(1));

        Assert.Equal(0, counter.DashboardCalls);
        Assert.DoesNotContain("sales-wallet-chip", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void SalesManager_makes_exactly_one_dashboard_call()
    {
        var counter = Arrange(CreateUser("SalesManager"), dashboardOk: true);
        var cut = RenderComponent<SalesWalletChip>();

        cut.WaitForAssertion(
            () => Assert.Contains("sales-wallet-chip", cut.Markup, StringComparison.Ordinal),
            TimeSpan.FromSeconds(2));
        Assert.Equal(1, counter.DashboardCalls);
        Assert.Contains("€ 12.50", cut.Markup, StringComparison.Ordinal);
    }

    private CountingHandler Arrange(ClaimsPrincipal user, bool dashboardOk = false)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        JSInterop.Mode = JSRuntimeMode.Loose;

        var auth = new FakeAuthStateProvider(user);
        Services.AddSingleton<AuthenticationStateProvider>(auth);
        Services.AddAuthorizationCore();
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        var authCtx = this.AddTestAuthorization();
        if (user.Identity?.IsAuthenticated == true)
        {
            authCtx.SetAuthorized(user.Identity.Name ?? "user");
            if (roles.Length > 0)
            {
                authCtx.SetRoles(roles);
            }
        }
        else
        {
            authCtx.SetNotAuthorized();
        }

        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));

        var counter = new CountingHandler(dashboardOk);
        var http = new HttpClient(counter) { BaseAddress = new Uri("http://wallet.test/") };
        Services.AddSingleton(new JobsyApiClient(http));
        Services.AddCascadingAuthenticationState();

        return counter;
    }

    private static ClaimsPrincipal CreateAnonymous()
        => new(new ClaimsIdentity());

    private static ClaimsPrincipal CreateUser(params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "user@jobsy.local"),
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }

    private sealed class FakeAuthStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(user));
    }

    private sealed class CountingHandler(bool dashboardOk) : HttpMessageHandler
    {
        public int DashboardCalls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("sales-managers/me/dashboard", StringComparison.OrdinalIgnoreCase))
            {
                DashboardCalls++;
                if (!dashboardOk)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                }

                var json = JsonSerializer.Serialize(new
                {
                    UninvoicedExVat = 12.5m,
                    BalanceExVat = 12.5m
                });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
