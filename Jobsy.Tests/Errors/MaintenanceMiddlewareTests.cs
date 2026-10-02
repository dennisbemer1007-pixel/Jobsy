using System.Net;
using Jobsy.Core.Rules;
using Jobsy.Web.Hosting;

namespace Jobsy.Tests.Errors;

/// <summary>
/// errors 05 §05.3 and §05.9. One switch turns the public site into a real 503 with the calm
/// onderhoudspagina; admins keep working and health checks never go to 503.
/// </summary>
public class MaintenanceMiddlewareTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/vacatures")]
    public async Task Off_leaves_normal_pages_alone(string path)
    {
        await using var factory = new MaintenanceWebFactory { MaintenanceEnabled = false };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync(path);

        Assert.NotEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/vacatures")]
    public async Task On_answers_html_pages_with_the_maintenance_page(string path)
    {
        await using var factory = new MaintenanceWebFactory { MaintenanceEnabled = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(
            MaintenanceRules.DefaultRetryAfterSeconds,
            (int)response.Headers.RetryAfter!.Delta!.Value.TotalSeconds);
        Assert.Contains(
            "noindex",
            response.Headers.GetValues("X-Robots-Tag").First(),
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString(), StringComparison.Ordinal);

        Assert.Contains("We zijn even aan het klussen", html, StringComparison.Ordinal);
        Assert.Contains("Lobsy is zo terug.", html, StringComparison.Ordinal);
        Assert.Contains("noindex", html, StringComparison.OrdinalIgnoreCase);
        // No login button for visitors, only a small admin hint.
        Assert.Contains("Beheerder? Inloggen", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Page_is_in_the_cookie_language()
    {
        await using var factory = new MaintenanceWebFactory { MaintenanceEnabled = true };
        using var client = factory.CreateHtmlClient("pl");

        var html = await (await client.GetAsync("/")).Content.ReadAsStringAsync();

        Assert.Contains("Chwilowo majsterkujemy", html, StringComparison.Ordinal);
        Assert.DoesNotContain("We zijn even aan het klussen", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Page_names_the_expected_end_time_only_when_it_is_set()
    {
        var end = DateTime.UtcNow.AddMinutes(30);
        await using var withEnd = new MaintenanceWebFactory
        {
            MaintenanceEnabled = true,
            ExpectedEndUtc = end
        };
        using var client = withEnd.CreateHtmlClient();

        var html = await (await client.GetAsync("/")).Content.ReadAsStringAsync();
        var expected = Jobsy.Core.Time.AmsterdamTime.ToLocal(end).ToString("HH:mm");

        Assert.Contains("We verwachten terug te zijn om", html, StringComparison.Ordinal);
        Assert.Contains(expected, html, StringComparison.Ordinal);

        await using var withoutEnd = new MaintenanceWebFactory { MaintenanceEnabled = true };
        using var plain = withoutEnd.CreateHtmlClient();
        var plainHtml = await (await plain.GetAsync("/")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("We verwachten terug te zijn om", plainHtml, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/healthz")]
    [InlineData("/login")]
    [InlineData("/css/features/errors.css")]
    [InlineData("/status/404")]
    [InlineData("/robots.txt")]
    public async Task Allow_listed_paths_keep_working(string path)
    {
        await using var factory = new MaintenanceWebFactory { MaintenanceEnabled = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync(path);

        Assert.NotEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Healthz_always_answers_200()
    {
        await using var factory = new MaintenanceWebFactory { MaintenanceEnabled = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Admin_passes_through_and_sees_the_banner()
    {
        await using var factory = new MaintenanceWebFactory { MaintenanceEnabled = true };
        using var client = factory.CreateAdminClient();

        var response = await client.GetAsync("/");

        Assert.NotEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Admin_sees_the_banner_and_a_visitor_never_does()
    {
        await using var factory = new MaintenanceWebFactory { MaintenanceEnabled = true };
        using var admin = factory.CreateAdminClient();
        using var visitor = factory.CreateHtmlClient();

        var adminResponse = await admin.GetAsync(BannerPath);
        var html = await adminResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        Assert.Contains("Onderhoudsmodus staat AAN", html, StringComparison.Ordinal);
        Assert.Contains("/admin/instellingen", html, StringComparison.Ordinal);

        // A protected page keeps redirecting an anonymous visitor to /login (§IA), never the banner.
        var visitorResponse = await visitor.GetAsync(BannerPath);
        var visitorHtml = await visitorResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Redirect, visitorResponse.StatusCode);
        Assert.DoesNotContain("Onderhoudsmodus staat AAN", visitorHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Off_shows_no_banner_to_an_admin()
    {
        await using var factory = new MaintenanceWebFactory { MaintenanceEnabled = false };
        using var admin = factory.CreateAdminClient();

        var html = await (await admin.GetAsync(BannerPath)).Content.ReadAsStringAsync();

        Assert.DoesNotContain("Onderhoudsmodus staat AAN", html, StringComparison.Ordinal);
    }

    /// <summary>Where the switch lives; it renders under <c>AdminLayout</c>, which carries the banner.</summary>
    private const string BannerPath = "/admin/instellingen";

    [Fact]
    public async Task Non_html_requests_get_a_bare_503()
    {
        await using var factory = new MaintenanceWebFactory { MaintenanceEnabled = true };
        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Blazor_circuit_is_reserved_for_admins()
    {
        Assert.False(MaintenanceMiddleware.IsAllowed("/_blazor", isAdmin: false));
        Assert.True(MaintenanceMiddleware.IsAllowed("/_blazor", isAdmin: true));
    }

    [Fact]
    public async Task An_api_outage_keeps_the_last_known_state()
    {
        var state = new MaintenanceState();
        state.Apply(true, null);

        var poller = new MaintenancePoller(
            state,
            new ExplodingHttpClientFactory(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MaintenancePoller>.Instance);

        await poller.PollOnceAsync(CancellationToken.None);

        Assert.True(state.IsEnabled);
    }

    [Fact]
    public void A_cold_start_defaults_to_off()
        => Assert.False(new MaintenanceState().IsEnabled);

    private sealed class ExplodingHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new Handler()) { BaseAddress = new Uri("http://api.test/") };

        private sealed class Handler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
                => throw new HttpRequestException("API is down.");
        }
    }
}

/// <summary>errors 05 §05.9: the four <c>Retry-After</c> cases from decision E8.</summary>
public class MaintenanceRetryAfterTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void End_in_two_minutes_is_the_seconds_until_then()
        => Assert.Equal(120, MaintenanceRules.RetryAfterSeconds(Now.AddMinutes(2), Now));

    [Fact]
    public void An_end_within_a_minute_is_clamped_up_to_the_minimum()
        => Assert.Equal(
            MaintenanceRules.MinRetryAfterSeconds,
            MaintenanceRules.RetryAfterSeconds(Now.AddSeconds(5), Now));

    [Fact]
    public void End_in_three_hours_is_clamped_down_to_an_hour()
        => Assert.Equal(
            MaintenanceRules.MaxRetryAfterSeconds,
            MaintenanceRules.RetryAfterSeconds(Now.AddHours(3), Now));

    [Fact]
    public void No_end_is_five_minutes()
        => Assert.Equal(
            MaintenanceRules.DefaultRetryAfterSeconds,
            MaintenanceRules.RetryAfterSeconds(null, Now));

    [Fact]
    public void An_end_in_the_past_is_five_minutes()
        => Assert.Equal(
            MaintenanceRules.DefaultRetryAfterSeconds,
            MaintenanceRules.RetryAfterSeconds(Now.AddMinutes(-5), Now));

    [Fact]
    public void The_meta_refresh_never_exceeds_five_minutes()
    {
        Assert.Equal(300, MaintenanceMiddleware.MetaRefreshSeconds(3600));
        Assert.Equal(120, MaintenanceMiddleware.MetaRefreshSeconds(120));
    }

    [Fact]
    public void A_note_is_trimmed_to_the_column_width()
    {
        Assert.Null(MaintenanceRules.NormalizeNote("   "));
        Assert.Equal("migratie", MaintenanceRules.NormalizeNote(" migratie "));
        Assert.Equal(
            MaintenanceRules.NoteMaxLength,
            MaintenanceRules.NormalizeNote(new string('x', 500))!.Length);
    }
}
