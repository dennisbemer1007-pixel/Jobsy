using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Jobsy.Core.Authorization;
using Jobsy.Core.Security;
using Jobsy.Web.Auth;
using Jobsy.Web.Services;
using Jobsy.Tests.TestSupport;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobsy.Tests;

/// <summary>
/// Banenkaart heart: same-origin WEB /api/vacancies/*/like proxy (not API-only tests).
/// </summary>
public sealed class VacancyLikeWebProxyTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _api;

    public VacancyLikeWebProxyTests(RoleFunctionalWebAppFactory api) => _api = api;

    [Fact]
    public async Task Lagen_page_token_allows_web_like_get_and_post_json()
    {
        await using var web = new VacancyLikeWebFactory(_api);
        var client = web.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Test-Sign-In", "candidate");

        var page = await client.GetAsync("/banenkaart");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains("jobsy-request-verification-token", html, StringComparison.Ordinal);
        Assert.Contains("kb-filter-form", html, StringComparison.Ordinal);

        var tokenMatch = Regex.Match(
            html,
            "name=\"jobsy-request-verification-token\"\\s+content=\"([^\"]+)\"",
            RegexOptions.IgnoreCase);
        Assert.True(tokenMatch.Success, "Expected antiforgery meta on lagen SSR page.");
        var token = System.Net.WebUtility.HtmlDecode(tokenMatch.Groups[1].Value);

        var vacancyId = _api.VacancyId;
        var get = await client.GetAsync($"/api/vacancies/{vacancyId:D}/like");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("application/json", get.Content.Headers.ContentType?.MediaType);
        var getRaw = await get.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Er ging iets mis", getRaw, StringComparison.Ordinal);

        using var post = new HttpRequestMessage(HttpMethod.Post, $"/api/vacancies/{vacancyId:D}/like");
        post.Headers.TryAddWithoutValidation("RequestVerificationToken", token);
        post.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        var postResp = await client.SendAsync(post);
        Assert.Equal(HttpStatusCode.OK, postResp.StatusCode);
        Assert.Equal("application/json", postResp.Content.Headers.ContentType?.MediaType);
        var postRaw = await postResp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Er ging iets mis", postRaw, StringComparison.Ordinal);
        var body = await postResp.Content.ReadFromJsonAsync<LikeDto>();
        Assert.True(body?.Liked);
    }

    [Fact]
    public void Web_like_proxy_disables_middleware_antiforgery_and_sets_acceptatie_diagnostics()
    {
        var root = FindRepoRoot();
        var proxy = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Hosting", "VacancyMapProxyEndpoints.cs"));
        Assert.Contains("MapPost(\"/api/vacancies/{id:guid}/like\", PostLikeAsync).RequireAuthorization().DisableAntiforgery()", proxy);
        Assert.Contains("MapDelete(\"/api/vacancies/{id:guid}/like\", DeleteLikeAsync).RequireAuthorization().DisableAntiforgery()", proxy);
        Assert.Contains("AcceptatieDiagnosticHeaders.TrySet", proxy);
        Assert.Contains("TryValidateAntiforgeryAsync", proxy);
        Assert.Contains("HasAntiforgeryToken", proxy);
        Assert.Contains("VacancyLikeProxyDto", proxy);

        var shells = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Banenkaart", "BanenkaartSsrShells.cs"));
        Assert.Contains("jobsy-request-verification-token", shells, StringComparison.Ordinal);

        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        Assert.Contains("jobsy-request-verification-token", js, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }

    private sealed class LikeDto
    {
        public bool Liked { get; set; }
    }

    private sealed class VacancyLikeWebFactory : WebWebApplicationFactory
    {
        private readonly RoleFunctionalWebAppFactory _api;
        public VacancyLikeWebFactory(RoleFunctionalWebAppFactory api) => _api = api;

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
                    ["Seed:Enabled"] = "false"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAntiforgery>();
                services.AddSingleton<IAntiforgery>(new AlwaysValidAntiforgery());

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

    private sealed class AlwaysValidAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
            => new("req", "cookie", "form", "header");

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => GetAndStoreTokens(httpContext);

        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);

        public void ValidateRequest(HttpContext httpContext) => _ = httpContext;

        public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;

        public void SetCookieTokenAndHeader(HttpContext httpContext) => _ = httpContext;
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
