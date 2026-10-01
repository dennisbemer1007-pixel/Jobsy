using System.Net;
using System.Text.RegularExpressions;

namespace Jobsy.Tests.Errors;

/// <summary>
/// Hosts for the 06.3 HTTP guards. One fixture for the whole table so the matrix does not boot a
/// web host per row.
/// </summary>
public sealed class StatusPagesHttpFixture : IAsyncLifetime
{
    /// <summary>Signed-in / anonymous HTML + JSON requests; every API call throws.</summary>
    public ForbiddenWebFactory Web { get; } = new();

    /// <summary>Adds the always-throwing path, so the 500 page can be requested.</summary>
    public ErrorPagesWebFactory Throwing { get; } = new();

    /// <summary>Web + a real in-process API with closed and public vacancies.</summary>
    public ClosedVacancyWebFactory Vacancies { get; } = new();

    /// <summary>Maintenance on, to prove the health checks are untouched.</summary>
    public MaintenanceWebFactory Maintenance { get; } = new() { MaintenanceEnabled = true };

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await Web.DisposeAsync();
        await Throwing.DisposeAsync();
        await Vacancies.DisposeAsync();
        await Maintenance.DisposeAsync();
    }
}

/// <summary>
/// errors 06 §06.3: the status pages as an HTTP contract, without a browser. Every row asserts the
/// real status code, the content type, <c>Cache-Control: no-store</c>, noindex and — on 429/503 —
/// <c>Retry-After</c>. API, framework and static-file paths never get an HTML page, and the health
/// checks answer 200 even during maintenance.
/// </summary>
public class StatusPagesHttpTests : IClassFixture<StatusPagesHttpFixture>
{
    private const string Html = "text/html,application/xhtml+xml";
    private const string Json = "application/json";

    /// <summary>Text a visitor may never see on a status page (06.1 / 06.3).</summary>
    private static readonly string[] ForbiddenFragments =
    [
        "Exception",
        "at Jobsy.",
        "System.",
        "[PLACEHOLDER]",
        "Secret boom detail"
    ];

    private readonly StatusPagesHttpFixture _hosts;

    public StatusPagesHttpTests(StatusPagesHttpFixture hosts)
    {
        _hosts = hosts;
    }

    public static TheoryData<string, bool, int> HtmlMatrix()
    {
        var data = new TheoryData<string, bool, int>();
        foreach (var signedIn in new[] { false, true })
        {
            data.Add("/bestaat-niet", signedIn, 404);
            data.Add("/status/404", signedIn, 404);
            data.Add("/status/410", signedIn, 410);
            data.Add("/status/429", signedIn, 429);
            data.Add("/status/503", signedIn, 503);
            // Not one of ErrorResponse.DirectStatusCodes: falls back to the 404 page.
            data.Add("/status/999", signedIn, 404);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(HtmlMatrix))]
    public async Task Html_status_pages_answer_the_real_code_with_no_store_and_noindex(
        string path,
        bool signedIn,
        int expected)
    {
        using var client = CreateClient(signedIn, Html);

        var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(expected, (int)response.StatusCode);
        AssertStatusPage(response, body, expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Access_denied_is_a_403_page_for_anonymous_and_signed_in_visitors(bool signedIn)
    {
        using var client = CreateClient(signedIn, Html);

        var response = await client.GetAsync("/access-denied?returnUrl=%2Fadmin%2Fsettings");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(403, (int)response.StatusCode);
        AssertStatusPage(response, body, 403);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unhandled_exception_answers_500_with_a_support_code_and_no_detail(bool signedIn)
    {
        using var client = _hosts.Throwing.CreateHtmlClient();
        if (signedIn)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, Jobsy.Core.Authorization.JobsyRoles.Candidate);
        }

        var response = await client.GetAsync(ErrorPagesWebFactory.ThrowPath);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(500, (int)response.StatusCode);
        AssertStatusPage(response, body, 500);
        Assert.Matches(@"LB-[2-9A-HJKMNP-TV-Z]{4}", body);
    }

    [Theory]
    [InlineData("/bestaat-niet")]
    [InlineData("/api/bestaat-niet")]
    [InlineData("/api/vacancies/onbekend")]
    public async Task Json_requests_never_get_an_html_status_page(string path)
    {
        using var client = CreateClient(signedIn: false, accept: Json);

        var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertNotAnHtmlPage(response, body);
    }

    [Theory]
    [InlineData("/_framework/x.js")]
    [InlineData("/css/missing.css")]
    [InlineData("/img/missing.png")]
    public async Task Framework_and_static_paths_stay_a_plain_404(string path)
    {
        using var client = CreateClient(signedIn: false, accept: Html);

        var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertNotAnHtmlPage(response, body);
    }

    [Fact]
    public async Task Rate_limited_and_maintenance_pages_carry_retry_after()
    {
        using var client = CreateClient(signedIn: false, accept: Html);

        foreach (var path in new[] { "/status/429", "/status/503" })
        {
            using var response = await client.GetAsync(path);
            Assert.NotNull(response.Headers.RetryAfter);
            var seconds = response.Headers.RetryAfter!.Delta?.TotalSeconds
                          ?? throw new InvalidOperationException("Retry-After must be a delta in seconds.");
            Assert.InRange(seconds, 1, 3600);
        }
    }

    [Fact]
    public async Task Unknown_vacancy_is_404_and_a_closed_one_is_410_in_html()
    {
        using var client = _hosts.Vacancies.CreateHtmlClient();

        var unknown = await client.GetAsync($"/vacancies/{Guid.NewGuid():D}");
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        AssertStatusPage(unknown, await unknown.Content.ReadAsStringAsync(), 404);

        var closed = await client.GetAsync($"/vacancies/{_hosts.Vacancies.Api.ArchivedId:D}");
        Assert.Equal(410, (int)closed.StatusCode);
        AssertStatusPage(closed, await closed.Content.ReadAsStringAsync(), 410);
    }

    [Fact]
    public async Task Closed_vacancy_api_answers_410_with_a_machine_readable_code()
    {
        using var client = _hosts.Vacancies.Api.CreateClient();

        var response = await client.GetAsync($"api/vacancies/{_hosts.Vacancies.Api.ArchivedId:D}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(410, (int)response.StatusCode);
        Assert.Contains("\"code\":\"vacancy_closed\"", body.Replace(" ", "", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Health_checks_answer_200_during_maintenance()
    {
        using var client = _hosts.Maintenance.CreateHtmlClient();

        var blocked = await client.GetAsync("/");
        Assert.Equal(503, (int)blocked.StatusCode);

        var health = await client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    [Fact]
    public void Sitemap_never_lists_status_pages_or_access_denied()
    {
        var xml = Jobsy.Web.Seo.SitemapXml.Build(
            "https://lobsy.nl",
            ["/", "/banenkaart", "/status/404", "/status/500", "/status/503", "/access-denied", "/Error"]);

        Assert.Contains("<loc>https://lobsy.nl/banenkaart</loc>", xml, StringComparison.Ordinal);
        Assert.DoesNotContain("/status/", xml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/access-denied", xml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/Error", xml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Crawl_index_never_lists_a_closed_vacancy()
    {
        using var client = _hosts.Vacancies.Api.CreateClient();

        var body = await (await client.GetAsync("api/site/crawl-index")).Content.ReadAsStringAsync();

        Assert.Contains(_hosts.Vacancies.Api.PublicId.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
        foreach (var closed in new[]
                 {
                     _hosts.Vacancies.Api.ArchivedId,
                     _hosts.Vacancies.Api.FulfilledId,
                     _hosts.Vacancies.Api.ExpiredId
                 })
        {
            Assert.DoesNotContain(closed.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>06.3 source guard: nothing navigates to the 403 page with a full page load any more.</summary>
    [Fact]
    public void No_force_load_navigation_to_access_denied_is_left()
    {
        var offenders = new List<string>();
        foreach (var file in EnumerateWebSources())
        {
            var text = File.ReadAllText(file);
            foreach (var match in Regex.Matches(text, @"NavigateTo\(\s*""[^""]*access-denied[^""]*""[^)]*\)").Cast<Match>())
            {
                if (match.Value.Contains("true", StringComparison.OrdinalIgnoreCase)
                    || match.Value.Contains("forceLoad", StringComparison.OrdinalIgnoreCase))
                {
                    offenders.Add(Path.GetFileName(file) + ": " + match.Value);
                }
            }
        }

        Assert.Empty(offenders);
    }

    /// <summary>06.3 source guard: the reconnect toast is catalog text, never hard-coded Dutch.</summary>
    [Fact]
    public void App_razor_reconnect_markup_has_no_hard_coded_dutch()
    {
        var app = File.ReadAllText(Path.Combine(RepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        var start = app.IndexOf("components-reconnect-modal", StringComparison.Ordinal);
        Assert.True(start > 0, "The reconnect modal markup moved; update this guard.");
        var markup = app[start..Math.Min(app.Length, start + 2_000)];

        foreach (var dutch in new[] { "Verbinding", "verbinding", "Opnieuw laden", "sessie", "Even iets" })
        {
            Assert.DoesNotContain(dutch, markup, StringComparison.Ordinal);
        }

        Assert.Contains("Status.Reconnect.", markup, StringComparison.Ordinal);
    }

    private HttpClient CreateClient(bool signedIn, string accept)
    {
        var client = signedIn
            ? _hosts.Web.CreateSignedInClient(Jobsy.Core.Authorization.JobsyRoles.Candidate)
            : _hosts.Web.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        client.DefaultRequestHeaders.Remove("Accept");
        client.DefaultRequestHeaders.Add("Accept", accept);
        return client;
    }

    private static void AssertStatusPage(HttpResponseMessage response, string body, int expected)
    {
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        var cacheControl = response.Headers.CacheControl?.ToString() ?? "";
        Assert.Contains("no-store", cacheControl, StringComparison.OrdinalIgnoreCase);

        Assert.True(
            response.Headers.TryGetValues("X-Robots-Tag", out var robots),
            $"{expected} is missing X-Robots-Tag.");
        Assert.Contains("noindex", string.Join(",", robots!), StringComparison.OrdinalIgnoreCase);
        Assert.Matches(@"<meta\s+name=""robots""\s+content=""noindex", body);

        Assert.Equal(1, Regex.Matches(body, "<h1", RegexOptions.IgnoreCase).Count);

        foreach (var fragment in ForbiddenFragments)
        {
            Assert.DoesNotContain(fragment, body, StringComparison.Ordinal);
        }
    }

    private static void AssertNotAnHtmlPage(HttpResponseMessage response, string body)
    {
        Assert.NotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Deze pagina bestaat niet", body, StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumerateWebSources()
    {
        var web = Path.Combine(RepoRoot(), "Jobsy.Web");
        foreach (var pattern in new[] { "*.razor", "*.cs" })
        {
            foreach (var file in Directory.EnumerateFiles(web, pattern, SearchOption.AllDirectories))
            {
                if (!file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    yield return file;
                }
            }
        }
    }

    private static string RepoRoot()
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
