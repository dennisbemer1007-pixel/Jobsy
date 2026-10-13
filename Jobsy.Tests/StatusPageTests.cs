using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Jobsy.Core.Features;
using Jobsy.Web.Features;
using Jobsy.Web.Hosting;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobsy.Tests;

public class StatusPageTests
{
    [Fact]
    public async Task Unknown_html_path_returns_404_status_page_with_noindex()
    {
        await using var factory = new StatusPageWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await client.GetAsync("/bestaat-niet");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Deze pagina bestaat niet", html, StringComparison.Ordinal);
        Assert.Contains("noindex", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Direct_status_404_keeps_404_status()
    {
        await using var factory = new StatusPageWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        var response = await client.GetAsync("/status/404");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Deze pagina bestaat niet", html, StringComparison.Ordinal);
        Assert.Contains("noindex", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Api_unknown_stays_non_html_404()
    {
        await using var factory = new StatusPageWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await client.GetAsync("/api/unknown");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Deze pagina bestaat niet", body, StringComparison.Ordinal);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Missing_static_css_is_404_without_status_html()
    {
        await using var factory = new StatusPageWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/css/missing.css");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Deze pagina bestaat niet", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_vacancy_returns_404_with_noindex()
    {
        await using var factory = new StatusPageWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        var id = Guid.NewGuid();
        var response = await client.GetAsync($"/vacancies/{id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("noindex", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Vacancy_detail_marks_not_found_status()
    {
        var root = FindRepoRoot();
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "VacancyDetail.razor"));
        Assert.Contains("MarkVacancyNotFound", page, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status404NotFound", page, StringComparison.Ordinal);
        Assert.Contains("Index=\"@(_vacancy is not null)\"", page, StringComparison.Ordinal);
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
}

file sealed class StatusPageWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "http://api.test/",
                ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IVacancyMapApiForwarder>();
            services.AddSingleton<IVacancyMapApiForwarder>(_ => new NoopForwarder());
            services.RemoveAll<IFeatureFlags>();
            services.AddSingleton<IFeatureFlags>(_ => new AlwaysOnFeatureFlags());
            services.RemoveAll<JobsyApiClient>();
            services.AddScoped(_ =>
            {
                var http = new HttpClient(new StubApiHandler())
                {
                    BaseAddress = new Uri("http://api.test/")
                };
                return new JobsyApiClient(http);
            });
        });
    }

    private sealed class NoopForwarder : IVacancyMapApiForwarder
    {
        public Task ForwardAsync(HttpContext http, string apiPath, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class AlwaysOnFeatureFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(EmployersEnabled: true, CandidatePassportEnabled: false));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);

        public void Invalidate()
        {
        }
    }

    private sealed class StubApiHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("/api/vacancies/", StringComparison.OrdinalIgnoreCase)
                && !path.Contains("/discover", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("""{"code":"not_found"}""", Encoding.UTF8, "application/json")
                });
            }

            if (path.Contains("feature-flags", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"employersEnabled":true,"candidatePassportEnabled":false}""",
                        Encoding.UTF8,
                        "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
        }
    }
}
