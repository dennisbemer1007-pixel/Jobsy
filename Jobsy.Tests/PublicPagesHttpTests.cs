using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Web.Hosting;
using Jobsy.Web.Seo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobsy.Tests;

/// <summary>
/// Public-pages 10.3 — API half of the HTTP guards: only a KvK-verified company with a publicly
/// visible vacancy has a page, and its JSON carries no internal id, address or coordinate.
/// </summary>
public class PublicPagesApiGuardTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    /// <summary>A GUID anywhere in a public body would leak an internal id (§0 security rules).</summary>
    private static readonly Regex GuidShaped = new(
        @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
        RegexOptions.CultureInvariant);

    private readonly RoleFunctionalWebAppFactory _factory;

    public PublicPagesApiGuardTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Verified_company_json_has_no_ids_address_or_coordinates()
    {
        var kvk = await SeedCompanyAsync(KvkVerificationStatus.Verified, VacancySeed.Public);
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"api/public/companies/{kvk}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotMatch(GuidShaped, json);
        foreach (var forbidden in new[] { "companyIds", "companyId", "address", "latitude", "longitude" })
        {
            Assert.DoesNotContain($"\"{forbidden}\"", json, StringComparison.OrdinalIgnoreCase);
        }

        // The street of the seeded address never reaches the page; only the city does.
        Assert.DoesNotContain("Voorstraat", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Naaldwijk", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(KvkVerificationStatus.Pending, VacancySeed.Public)]
    [InlineData(KvkVerificationStatus.Failed, VacancySeed.Public)]
    [InlineData(KvkVerificationStatus.Verified, VacancySeed.DraftOnly)]
    [InlineData(KvkVerificationStatus.Verified, VacancySeed.None)]
    public async Task Company_without_a_public_page_answers_404(KvkVerificationStatus kvkStatus, VacancySeed vacancies)
    {
        var kvk = await SeedCompanyAsync(kvkStatus, vacancies);
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"api/public/companies/{kvk}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_kvk_answers_404_without_an_oracle()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("api/public/companies/00000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("Exception", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sitemap_crawl_index_lists_the_verified_company_and_not_the_others()
    {
        var verified = await SeedCompanyAsync(KvkVerificationStatus.Verified, VacancySeed.Public);
        var pending = await SeedCompanyAsync(KvkVerificationStatus.Pending, VacancySeed.Public);
        var failed = await SeedCompanyAsync(KvkVerificationStatus.Failed, VacancySeed.Public);
        var draftOnly = await SeedCompanyAsync(KvkVerificationStatus.Verified, VacancySeed.DraftOnly);
        var client = _factory.CreateClient();

        var index = await client.GetFromJsonElementAsync("api/site/crawl-index");
        var paths = index.GetProperty("companyPaths")
            .EnumerateArray()
            .Select(p => p.GetString() ?? "")
            .ToList();

        Assert.Contains($"/{verified}", paths);
        Assert.DoesNotContain($"/{pending}", paths);
        Assert.DoesNotContain($"/{failed}", paths);
        Assert.DoesNotContain($"/{draftOnly}", paths);
    }

    public enum VacancySeed
    {
        None,
        Public,
        DraftOnly
    }

    private async Task<string> SeedCompanyAsync(KvkVerificationStatus kvkStatus, VacancySeed vacancies)
    {
        var kvk = Random.Shared.NextInt64(10_000_000, 99_999_999).ToString();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Public Pages BV",
            KvkNumber = kvk,
            KvkEstablishmentId = $"{kvk}_0001",
            KvkVerificationStatus = kvkStatus,
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.Manual,
            Address = "Voorstraat 1, 2671 AB Naaldwijk",
            Location = new GeoPoint(52.0, 4.2),
            Type = CompanyType.Employer
        });

        if (vacancies is not VacancySeed.None)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            db.Vacancies.Add(new Vacancy
            {
                Id = Guid.NewGuid(),
                Title = "Kassamedewerker",
                Description = "Test",
                CompanyId = companyId,
                Status = vacancies is VacancySeed.Public ? VacancyStatus.Active : VacancyStatus.Draft,
                StartDate = today.AddDays(-1),
                EndDate = today.AddDays(14),
                HourlyWage = 14m,
                Location = new GeoPoint(52.0, 4.2),
                RequiredTransport = TransportMode.Bike,
                WorkTypes = WorkType.Winkel
            });
        }

        await db.SaveChangesAsync();

        var discovery = scope.ServiceProvider.GetService<Jobsy.Core.Interfaces.IVacancyDiscoveryIndex>();
        if (discovery is not null)
        {
            await discovery.RefreshAsync();
        }

        return kvk;
    }
}

/// <summary>
/// Public-pages 10.3 — Web half of the HTTP guards: real 404 statuses, noindex, the render mode of
/// the static pages and a sitemap that only carries what a visitor may actually open.
/// </summary>
public class PublicPagesWebGuardTests
{
    private static readonly string[] StaticPublicPaths =
    [
        "/privacy",
        "/algemene-voorwaarden",
        "/gebruiksvoorwaarden",
        "/hoe-werkt-lobsy",
        "/wie-zijn-wij",
        "/partner"
    ];

    /// <summary>Unfilled copy like "[ADRES]" must never reach a visitor (§0).</summary>
    private static readonly Regex Placeholder = new(@"\[[A-Z][A-Z \-]+\]", RegexOptions.CultureInvariant);

    [Fact]
    public async Task Unknown_path_answers_404_html_with_noindex()
    {
        await using var factory = new PublicPagesWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/bestaat-niet");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("noindex", html, StringComparison.OrdinalIgnoreCase);
        AssertNoLeakedInternals(html);
    }

    [Fact]
    public async Task Unknown_vacancy_answers_404()
    {
        await using var factory = new PublicPagesWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync($"/vacancies/{Guid.NewGuid():D}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertNoLeakedInternals(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Company_page_without_api_data_answers_404()
    {
        await using var factory = new PublicPagesWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/00000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertNoLeakedInternals(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Partner_with_a_sales_code_is_noindex_and_canonical_to_partner()
    {
        Assert.False(PageSeoCatalog.IsIndexable("/partner/ABC"));
        Assert.Equal("/partner", PageSeoCatalog.Resolve("/partner/ABC").CanonicalPath);
        Assert.True(PageSeoCatalog.IsIndexable("/partner"));

        await using var factory = new PublicPagesWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var html = await client.GetStringAsync("/partner/ABC");

        Assert.Contains("noindex", html, StringComparison.OrdinalIgnoreCase);
        Assert.Matches(new Regex("<link[^>]+rel=\"canonical\"[^>]+href=\"[^\"]*/partner\""), html);
    }

    [Theory]
    [InlineData("/privacy")]
    [InlineData("/algemene-voorwaarden")]
    [InlineData("/gebruiksvoorwaarden")]
    [InlineData("/hoe-werkt-lobsy")]
    [InlineData("/wie-zijn-wij")]
    [InlineData("/partner")]
    public async Task Static_public_pages_are_complete_html_without_an_interactive_marker(string path)
    {
        await using var factory = new PublicPagesWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(1, CountOccurrences(html, "<h1"));
        Assert.DoesNotContain("\"type\":\"server\"", html, StringComparison.Ordinal);
        Assert.DoesNotMatch(Placeholder, StripScripts(html));
        AssertNoLeakedInternals(html);
    }

    [Fact]
    public void Static_public_pages_are_excluded_from_interactive_routing()
    {
        var root = FindRepoRoot();
        var pages = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["/privacy"] = "Components/Pages/Legal/Privacy.razor",
            ["/algemene-voorwaarden"] = "Components/Pages/Legal/AlgemeneVoorwaarden.razor",
            ["/gebruiksvoorwaarden"] = "Components/Pages/Legal/Gebruiksvoorwaarden.razor",
            ["/hoe-werkt-lobsy"] = "Components/Pages/HowLobsyWorks.razor",
            ["/wie-zijn-wij"] = "Components/Pages/Legal/WieZijnWij.razor",
            ["/partner"] = "Components/Pages/Partner/PartnerSales.razor",
            ["/melden"] = "Components/Pages/Public/Melden.razor"
        };

        foreach (var (route, relative) in pages)
        {
            var source = File.ReadAllText(Path.Combine(root, "Jobsy.Web", relative));
            Assert.True(
                source.Contains("ExcludeFromInteractiveRouting", StringComparison.Ordinal),
                $"{route} ({relative}) must be static SSR.");
        }
    }

    [Fact]
    public async Task Sitemap_carries_the_public_pages_and_the_verified_company_only()
    {
        await using var factory = new PublicPagesWebFactory(crawlIndexJson:
            """
            {
              "vacancies": [],
              "companyPaths": ["/12345678", "/12345678/0001"]
            }
            """);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var xml = await client.GetStringAsync("/sitemap.xml");

        foreach (var path in StaticPublicPaths)
        {
            Assert.Contains($"<loc>http://localhost{path}</loc>", xml, StringComparison.Ordinal);
        }

        Assert.Contains("<loc>http://localhost/12345678</loc>", xml, StringComparison.Ordinal);
        Assert.DoesNotContain("/privacy/data", xml, StringComparison.Ordinal);
        Assert.DoesNotContain("/melden", xml, StringComparison.Ordinal);
    }

    private static void AssertNoLeakedInternals(string html)
    {
        Assert.DoesNotContain("at Jobsy.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Exception", html, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", html, StringComparison.Ordinal);
    }

    /// <summary>Inline JSON (Blazor component state) legitimately contains bracketed upper-case tokens.</summary>
    private static string StripScripts(string html)
        => Regex.Replace(html, "<script.*?</script>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            i += needle.Length;
        }

        return count;
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

/// <summary>
/// Public-pages 10.3 — source guards: the pages this stack touched never show <c>ex.Message</c>,
/// and the types 01/08 deleted stay deleted.
/// </summary>
public class PublicPagesSourceGuardTests
{
    /// <summary>Every page/component the public-pages stack owns (files 01–09).</summary>
    private static readonly string[] OwnedSources =
    [
        "Jobsy.Web/Components/Pages/Legal/Privacy.razor",
        "Jobsy.Web/Components/Pages/Legal/PrivacyData.razor",
        "Jobsy.Web/Components/Pages/Legal/AlgemeneVoorwaarden.razor",
        "Jobsy.Web/Components/Pages/Legal/Gebruiksvoorwaarden.razor",
        "Jobsy.Web/Components/Pages/Legal/WieZijnWij.razor",
        "Jobsy.Web/Components/Pages/HowLobsyWorks.razor",
        "Jobsy.Web/Components/Pages/Partner/PartnerSales.razor",
        "Jobsy.Web/Components/Pages/CompanyPublicPage.razor",
        "Jobsy.Web/Components/Pages/Public/Melden.razor",
        "Jobsy.Web/Components/Pages/Status/StatusPage.razor",
        "Jobsy.Web/Components/Legal/LegalDocument.razor",
        "Jobsy.Web/Components/Legal/TermsPage.razor",
        "Jobsy.Web/Components/Legal/LegalIdentityCard.razor"
    ];

    [Fact]
    public void Public_pages_never_render_an_exception_message()
    {
        var root = FindRepoRoot();
        foreach (var relative in OwnedSources)
        {
            var code = StripComments(File.ReadAllText(Path.Combine(root, relative)));
            Assert.DoesNotContain("ex.Message", code, StringComparison.Ordinal);
            Assert.DoesNotContain("Exception.Message", code, StringComparison.Ordinal);
        }
    }

    /// <summary>A comment may name <c>ex.Message</c> as the thing we deliberately do not render.</summary>
    private static string StripComments(string source)
    {
        source = Regex.Replace(source, "<!--.*?-->", " ", RegexOptions.Singleline);
        source = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(source, @"^\s*//.*$", " ", RegexOptions.Multiline);
    }

    [Fact]
    public void Deleted_types_of_01_and_08_stay_deleted()
    {
        var root = FindRepoRoot();
        var hits = new List<string>();
        foreach (var project in new[] { "Jobsy.Core", "Jobsy.Api", "Jobsy.Infrastructure", "Jobsy.Web" })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, project), "*.*", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || Path.GetExtension(file) is not (".cs" or ".razor"))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                if (text.Contains("PlatformLegalIdentity", StringComparison.Ordinal)
                    || text.Contains("AboutPageSettingsService", StringComparison.Ordinal))
                {
                    hits.Add(Path.GetRelativePath(root, file));
                }
            }
        }

        Assert.Empty(hits);
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

/// <summary>Web host for the 10.3 guards: no API behind it unless a crawl-index stub is given.</summary>
file sealed class PublicPagesWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
{
    private readonly string? _crawlIndexJson;

    public PublicPagesWebFactory(string? crawlIndexJson = null) => _crawlIndexJson = crawlIndexJson;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "http://api.test/",
                ["PublicWebBaseUrl"] = "http://localhost",
                ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IVacancyMapApiForwarder>();
            services.AddSingleton<IVacancyMapApiForwarder>(_ => new NoopForwarder());
            services.RemoveAll<IFeatureFlags>();
            services.AddSingleton<IFeatureFlags>(new AlwaysOnFeatureFlags());

            // An API that answers 404 stands in for "this thing is not public": the page must turn
            // that into a real 404 instead of an error screen.
            services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
            services.AddScoped(sp => new Jobsy.Web.Services.JobsyApiClient(
                new HttpClient(new NotFoundHandler()) { BaseAddress = new Uri("http://api.test/") },
                sp.GetRequiredService<Jobsy.Web.Services.MeGetCache>()));

            if (_crawlIndexJson is not null)
            {
                services.AddHttpClient("JobsySeo")
                    .ConfigurePrimaryHttpMessageHandler(() => new CrawlIndexHandler(_crawlIndexJson));
            }
        });
    }

    private sealed class NoopForwarder : IVacancyMapApiForwarder
    {
        public Task ForwardAsync(HttpContext http, string apiPath, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class NotFoundHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"code\":\"not_found\"}", System.Text.Encoding.UTF8, "application/json")
            });
    }

    private sealed class CrawlIndexHandler : HttpMessageHandler
    {
        private readonly string _json;

        public CrawlIndexHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json")
            });
    }
}

internal static class PublicPagesJsonExtensions
{
    internal static async Task<JsonElement> GetFromJsonElementAsync(this HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
