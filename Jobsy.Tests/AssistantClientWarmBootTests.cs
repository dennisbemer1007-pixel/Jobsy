using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Security;
using Jobsy.Tests.TestSupport;
using Jobsy.Web.Auth;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobsy.Tests;

public sealed class AssistantClientWarmBootTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _api;

    public AssistantClientWarmBootTests(RoleFunctionalWebAppFactory api) => _api = api;

    [Fact]
    public async Task Warm_post_as_candidate_on_kompas_returns_page_context_and_chip()
    {
        await using var web = new AssistantClientWebFactory(_api);
        var client = web.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Test-Sign-In", "candidate");
        _ = await client.GetAsync("/login");

        using var response = await client.PostAsJsonAsync(
            "/api/assistant-client/chat",
            new
            {
                messages = Array.Empty<object>(),
                pagePath = "/candidate/kompas",
                warm = true
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<WarmBootDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Reply));
        Assert.False(string.IsNullOrWhiteSpace(body.PageContextLine));
        Assert.Contains("Kompas", body.PageContextLine!, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(body.Chips);
        Assert.True(body.Chips!.Count >= 1, "Expected at least one suggestion chip.");
        Assert.True(
            body.Chips!.Any(c => c.Label.Contains("Kompas", StringComparison.OrdinalIgnoreCase)),
            "Expected a Kompas suggestion chip.");
    }

    [Fact]
    public void Base_appsettings_do_not_disable_chat_context_greeting()
    {
        var json = File.ReadAllText(Path.Combine(TestRepo.FindRoot(), "Jobsy.Web", "appsettings.json"));
        Assert.DoesNotContain("\"ChatContextGreetingEnabled\": false", json, StringComparison.Ordinal);
    }

    private sealed class WarmBootDto
    {
        public string? Reply { get; set; }
        public string? PageContextLine { get; set; }
        public List<ChipDto>? Chips { get; set; }
    }

    private sealed class ChipDto
    {
        public string Label { get; set; } = "";
        public string Message { get; set; } = "";
    }

    private sealed class AssistantClientWebFactory : WebWebApplicationFactory
    {
        private readonly RoleFunctionalWebAppFactory _api;

        public AssistantClientWebFactory(RoleFunctionalWebAppFactory api) => _api = api;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            JobsyTestAuth.ApplyStandardAuthSettings(builder);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiBaseUrl"] = "http://api.test/",
                    ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                    ["Jobsy:TestEmployersEnabled"] = "true",
                    ["Seed:Enabled"] = "false",
                    ["ChatContextGreetingEnabled"] = "true"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new CandidateTestSignInFilter(_api.CandidateId, _api.CandidateEmail));
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new ApiForwardClientFactory(_api.Server.CreateHandler()));
                services.RemoveAll<JobsyApiClient>();
                services.AddScoped(sp =>
                {
                    var handler = new JobsyApiAuthHandler(
                        sp.GetRequiredService<IHttpContextAccessor>(),
                        sp.GetRequiredService<AuthenticationStateProvider>(),
                        sp,
                        sp.GetRequiredService<IConfiguration>(),
                        sp.GetRequiredService<JobsyAccessTokenIssuer>())
                    {
                        InnerHandler = new ApiRelayHandler(_api.Server.CreateHandler())
                    };
                    return new JobsyApiClient(new HttpClient(handler, disposeHandler: false)
                    {
                        BaseAddress = new Uri("http://api.test/")
                    });
                });
            });
        }
    }

    private sealed class CandidateTestSignInFilter(Guid userId, string email) : IStartupFilter
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
                            new Claim(ClaimTypes.NameIdentifier, userId.ToString("D")),
                            new Claim(ClaimTypes.Email, email),
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

    private sealed class ApiForwardClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public ApiForwardClientFactory(HttpMessageHandler handler) => _handler = handler;

        public HttpClient CreateClient(string name)
            => new(new ApiRelayHandler(_handler), disposeHandler: false);
    }

    private sealed class ApiRelayHandler : HttpMessageHandler
    {
        private readonly HttpMessageInvoker _api;

        public ApiRelayHandler(HttpMessageHandler api)
            => _api = new HttpMessageInvoker(api, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.PathAndQuery ?? "/";
            if (!path.StartsWith('/'))
            {
                path = "/" + path;
            }

            using var clone = new HttpRequestMessage(request.Method, new Uri("http://localhost" + path));
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (request.Content is not null)
            {
                var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
                clone.Content = new ByteArrayContent(bytes);
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return await _api.SendAsync(clone, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _api.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
