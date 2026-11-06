using System.Net;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Security;
using Jobsy.Tests.TestSupport;
using Jobsy.Web.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

/// <summary>
/// Mid-onboarding candidates must reach account deletion via <c>/privacy/data</c> (PublicLayout).
/// </summary>
public sealed class OnboardingPrivacyAccessHttpTests
{
    [Fact]
    public async Task Mid_onboarding_candidate_can_open_privacy_data_page()
    {
        await using var factory = new PrivacyDataWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
        client.DefaultRequestHeaders.Add("X-Test-Sign-In", "candidate-mid-onboarding");

        using var response = await client.GetAsync("/privacy/data");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("mydata-delete-title", html, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class PrivacyDataWebFactory : WebWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new TestSignInFilter());
            });
        }
    }

    private sealed class TestSignInFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (ctx, nxt) =>
                {
                    if (ctx.Request.Headers.ContainsKey("X-Test-Sign-In"))
                    {
                        var identity = new ClaimsIdentity(
                        [
                            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")),
                            new Claim(ClaimTypes.Email, "mid-onboarding@test.local"),
                            new Claim(ClaimTypes.Role, JobsyRoles.Candidate),
                            new Claim(JobsyClaimTypes.MfaVerified, "1")
                        ],
                        CookieAuthenticationDefaults.AuthenticationScheme);
                        await ctx.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(identity));
                    }

                    await nxt();
                });
                next(app);
            };
    }
}
