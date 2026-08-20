using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Web.Localization;
using Jobsy.Web.Seo;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Tests;

public class PageSeoCatalogTests
{
    [Theory]
    [InlineData("/", true)]
    [InlineData("/login", true)]
    [InlineData("/register", true)]
    [InlineData("/privacy", true)]
    [InlineData("/algemene-voorwaarden", true)]
    [InlineData("/gebruiksvoorwaarden", true)]
    [InlineData("/wie-zijn-wij", true)]
    [InlineData("/westland", true)]
    [InlineData("/lancering", true)]
    [InlineData("/partner", true)]
    [InlineData("/partner/SM-ABCDEF", true)]
    [InlineData("/vacancies/c1000000-0000-0000-0000-000000000010", true)]
    [InlineData("/12345678", true)]
    [InlineData("/12345678/0001", true)]
    [InlineData("/home", false)]
    [InlineData("/admin", false)]
    [InlineData("/admin/users", false)]
    [InlineData("/candidate/profile", false)]
    [InlineData("/employer/tokens", false)]
    [InlineData("/register/activate", false)]
    [InlineData("/privacy/data", false)]
    [InlineData("/banen", false)]
    public void Indexability_matches_public_vs_private_surfaces(string path, bool indexable)
        => Assert.Equal(indexable, PageSeoCatalog.IsIndexable(path));

    [Fact]
    public void Catalog_resolves_every_razor_page_route()
    {
        var pagesDir = Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages");
        var razorFiles = Directory.GetFiles(pagesDir, "*.razor", SearchOption.AllDirectories);
        var routePattern = new Regex(@"@page\s+""([^""]+)""", RegexOptions.CultureInvariant);
        Assert.NotEmpty(razorFiles);

        foreach (var file in razorFiles)
        {
            var text = File.ReadAllText(file);
            var matches = routePattern.Matches(text);
            if (matches.Count == 0)
            {
                continue;
            }

            foreach (Match match in matches)
            {
                var sample = SamplePath(match.Groups[1].Value);
                var entry = PageSeoCatalog.Resolve(sample);
                Assert.False(string.IsNullOrWhiteSpace(entry.TitleKey), $"{file} → {sample}");
                Assert.False(string.IsNullOrWhiteSpace(entry.DescriptionKey), sample);
                Assert.NotEqual(entry.TitleKey, UiStrings.Get(entry.TitleKey, "nl"));
                Assert.NotEqual(entry.DescriptionKey, UiStrings.Get(entry.DescriptionKey, "nl"));
            }
        }
    }

    [Fact]
    public void Layouts_and_shell_emit_crawler_metadata()
    {
        var root = FindRepoRoot();
        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("<html lang=\"nl\">", app);
        Assert.Contains("theme-color", app);
        Assert.Contains("Lobsy — vacatures op reistijd", app);
        Assert.Contains("HeadOutlet", app);

        var layout = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("PageSeoHead", layout);

        var teaser = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "TeaserLayout.razor"));
        Assert.Contains("PageSeoHead", teaser);

        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Program.cs"));
        Assert.Contains("MapSeoEndpoints", program);
        Assert.Contains("PageSeoContext", program);

        var home = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        Assert.Contains("Seo.HomeHeading", home);
        Assert.Contains("visually-hidden", home);
        Assert.Contains("StructuredData.JobList", home);

        var detail = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "VacancyDetail.razor"));
        Assert.Contains("prerender: true", detail);
        Assert.DoesNotContain("prerender: false", detail);
        Assert.Contains("JobPosting", detail);

        var company = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "CompanyPublicPage.razor"));
        Assert.Contains("prerender: true", company);
        Assert.DoesNotContain("prerender: false", company);
    }

    private static string SamplePath(string template)
    {
        if (template.Contains("KvkNumber", StringComparison.Ordinal))
        {
            return template.Contains("Vestigingsnummer", StringComparison.Ordinal)
                ? "/12345678/0001"
                : "/12345678";
        }

        var path = template
            .Replace("{Id:guid}", "c1000000-0000-0000-0000-000000000010", StringComparison.Ordinal)
            .Replace("{TableId:guid}", "c2000000-0000-0000-0000-000000000020", StringComparison.Ordinal)
            .Replace("{CompanyId:guid}", "c3000000-0000-0000-0000-000000000030", StringComparison.Ordinal)
            .Replace("{TrackingCode?}", "", StringComparison.Ordinal)
            .Replace("{TrackingCode}", "SM-ABCDEF", StringComparison.Ordinal)
            .Replace("{Key}", "clicks", StringComparison.Ordinal);

        if (path.Length > 1)
        {
            path = path.TrimEnd('/');
        }

        return string.IsNullOrEmpty(path) ? "/" : path;
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

        throw new InvalidOperationException("Jobsy.sln not found from test base directory.");
    }
}

public class StructuredDataAndSitemapTests
{
    [Fact]
    public void JobPosting_omits_contact_pii_and_hidden_wages()
    {
        var json = StructuredData.JobPosting(
            Guid.Parse("c1000000-0000-0000-0000-000000000010"),
            "Vulploegmedewerker",
            "Omschrijving zonder contactgegevens.",
            "Demo Filiaal",
            null,
            "Veilingweg 1, Naaldwijk",
            52.0,
            4.2,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            "Regular",
            hourlyWage: 14.50m,
            wageVisible: false,
            minHoursPerWeek: 8,
            maxHoursPerWeek: 16,
            "https://lobsy.nl/vacancies/c1000000-0000-0000-0000-000000000010",
            "https://lobsy.nl")!;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("JobPosting", root.GetProperty("@type").GetString());
        Assert.Equal("Vulploegmedewerker", root.GetProperty("title").GetString());
        Assert.Equal("PART_TIME", root.GetProperty("employmentType").GetString());
        Assert.False(root.TryGetProperty("baseSalary", out _));
        Assert.False(root.TryGetProperty("email", out _));
        Assert.False(root.TryGetProperty("telephone", out _));
        Assert.False(root.TryGetProperty("applicant", out _));
        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("telephone", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JobPosting_includes_hourly_wage_when_public_and_escapes_markup()
    {
        var json = StructuredData.JobPosting(
            Guid.NewGuid(),
            "Stage <script>alert(1)</script>",
            "<p>Hallo</p><script>alert(1)</script>",
            "School",
            null,
            "Straat 1",
            52.1,
            4.3,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            "Internship",
            hourlyWage: 8.20m,
            wageVisible: true,
            minHoursPerWeek: 16,
            maxHoursPerWeek: 16,
            "https://lobsy.nl/vacancies/x",
            "https://lobsy.nl")!;

        Assert.Contains("INTERN", json);
        Assert.Contains("baseSalary", json);
        Assert.Contains("\\u003cscript", json);
        Assert.DoesNotContain("<script>", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JobPosting_skips_expired_vacancies()
    {
        var json = StructuredData.JobPosting(
            Guid.NewGuid(),
            "Oud",
            "Oud",
            "Bedrijf",
            null,
            "Adres",
            52,
            4,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            "Regular",
            12m,
            true,
            8,
            12,
            "https://lobsy.nl/vacancies/x",
            "https://lobsy.nl");
        Assert.Null(json);
    }

    [Fact]
    public void Website_json_ld_has_search_action()
    {
        var json = StructuredData.WebsiteAndOrganization("https://lobsy.nl");
        Assert.Contains("WebSite", json);
        Assert.Contains("Organization", json);
        Assert.Contains("/?q={search_term_string}", json);
        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Robots_and_sitemap_list_public_urls_only()
    {
        var robots = SitemapXml.RobotsTxt("https://lobsy.nl");
        Assert.Contains("Sitemap: https://lobsy.nl/sitemap.xml", robots);
        Assert.Contains("Disallow: /admin", robots);
        Assert.Contains("Disallow: /candidate", robots);
        Assert.Contains("Allow: /", robots);

        var xml = SitemapXml.Build("https://lobsy.nl", ["/", "/privacy", "/vacancies/c1000000-0000-0000-0000-000000000010", "/admin", "/home"]);
        Assert.Contains("https://lobsy.nl/", xml);
        Assert.Contains("https://lobsy.nl/privacy", xml);
        Assert.Contains("https://lobsy.nl/vacancies/c1000000-0000-0000-0000-000000000010", xml);
        Assert.DoesNotContain("https://lobsy.nl/admin", xml);
        Assert.DoesNotContain("https://lobsy.nl/home", xml);
    }

    [Fact]
    public void Canonical_uses_https_apex_without_query()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "https://lobsy.nl"
            })
            .Build();

        var canonical = PageSeoResolver.CanonicalUrl(
            "https://www.lobsy.nl/privacy?utm=1",
            "/privacy",
            config);
        Assert.Equal("https://lobsy.nl/privacy", canonical);

        Assert.Equal("Banenkaart · Lobsy", PageSeoResolver.WithBrand("Banenkaart · Lobsy"));
        Assert.Equal("Inloggen · Lobsy", PageSeoResolver.WithBrand("Inloggen"));
    }
}
