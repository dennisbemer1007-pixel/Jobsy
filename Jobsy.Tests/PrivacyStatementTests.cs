using System.Security.Claims;
using System.Text.RegularExpressions;
using Bunit;
using Jobsy.Core.Legal;
using Jobsy.Core.Privacy;
using Jobsy.Web.Components.Layout;
using Jobsy.Web.Components.Legal;
using Jobsy.Web.Components.Legal.Docs;
using Jobsy.Web.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Seo;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

/// <summary>
/// Shared bUnit host for the privacy statement (03.7). The statement is static SSR, so a plain
/// render is exactly what a visitor gets.
/// </summary>
public abstract class PrivacyRenderTestBase : TestContext
{
    protected static readonly string[] Languages = ["nl", "en", "pl", "ro", "ar"];

    private readonly DefaultHttpContext _http = new();
    private readonly AmbientCultureScope _culture = new();

    protected PrivacyRenderTestBase()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddMemoryCache();
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<ICookieConsentTokenService, CookieConsentTokenService>();
        Services.AddSingleton<IEmployersSwitch, AlwaysOnEmployersSwitch>();
        Services.AddSingleton<IHostEnvironment>(new PrivacyTestHostEnvironment());
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = _http });
        Services.AddSingleton<IHttpClientFactory>(new PrivacyOfflineHttpClientFactory());
        Services.AddSingleton<LegalIdentityProvider>();
        Services.AddSingleton<LandingStatsClient>();
        Services.AddSingleton<LandingPriceClient>();
        Services.AddScoped<LandingVariantResolver>();
        Services.AddScoped(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddScoped<PageSeoContext>();
        Services.AddSingleton<NavigationManager>(new PrivacyStaticNavigation("/privacy"));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _culture.Dispose();
        }
    }

    protected void UseLanguage(string language)
    {
        _http.Request.QueryString = new QueryString($"?lang={language}");
        Services.GetRequiredService<CultureState>().InitializeFromRequest(_http);
    }

    protected IRenderedComponent<PrivacyNl> RenderPrivacy() => RenderComponent<PrivacyNl>();

    protected static string PrivacyMarkupSource => File.ReadAllText(Path.Combine(
        RepoRoot(), "Jobsy.Web", "Components", "Legal", "Docs", "PrivacyNl.razor"));

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

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class PrivacyTestHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = Environments.Development;
    }

    private sealed class PrivacyOfflineHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new PrivacyOfflineHandler()) { BaseAddress = new Uri("http://localhost/") };
    }

    private sealed class PrivacyOfflineHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.RequestTimeout));
    }

    private sealed class PrivacyStaticNavigation : NavigationManager
    {
        public PrivacyStaticNavigation(string relativePath)
            => Initialize("http://localhost/", "http://localhost" + relativePath);

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }
}

/// <summary>
/// The processor table of §4 lists every catalog row, including the ones the review found missing
/// (Pingen, OpenStreetMap routing and tiles, web push), and never claims Render is outside the EU.
/// </summary>
public class PrivacyProcessorsTests : PrivacyRenderTestBase
{
    [Fact]
    public void Every_catalog_row_renders_with_its_region_purpose_data_and_transfer_basis()
    {
        var table = RenderPrivacy().Find("#delen .pp-table__grid").TextContent;

        foreach (var row in LegalProcessors.All)
        {
            Assert.Contains(row.Name, table, StringComparison.Ordinal);
            Assert.Contains(row.Region, table, StringComparison.Ordinal);
            Assert.Contains(UiStrings.Get(row.PurposeKey, "nl"), table, StringComparison.Ordinal);
            Assert.Contains(UiStrings.Get(row.DataKey, "nl"), table, StringComparison.Ordinal);
            Assert.Contains(
                UiStrings.Get(row.TransferBasisKey ?? LegalProcessors.InsideEu, "nl"),
                table,
                StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("pingen")]
    [InlineData("routing")]
    [InlineData("maps")]
    [InlineData("push")]
    [InlineData("render")]
    public void The_rows_the_review_found_missing_are_in_the_catalog(string id)
        => Assert.Equal(id, LegalProcessors.ById(id).Id);

    [Fact]
    public void Render_runs_in_frankfurt_and_never_reads_eu_slash_vs()
    {
        var render = LegalProcessors.ById("render");
        Assert.Contains("Frankfurt", render.Region, StringComparison.Ordinal);
        Assert.DoesNotContain("EU/VS", render.Region, StringComparison.Ordinal);
    }

    [Fact]
    public void Pingen_is_a_swiss_row_on_the_eu_adequacy_decision()
    {
        var pingen = LegalProcessors.ById("pingen");
        Assert.Equal("Zwitserland", pingen.Region);
        Assert.Equal(LegalProcessors.AdequacyDecision, pingen.TransferBasisKey);
    }

    [Fact]
    public void Pingen_is_active_because_the_letter_service_ships_in_this_build()
    {
        var hasLetterService = typeof(Jobsy.Infrastructure.DependencyInjection).Assembly
            .GetTypes()
            .Any(t => t.Name.Contains("Pingen", StringComparison.Ordinal));

        Assert.True(hasLetterService, "Expected a Pingen type in Jobsy.Infrastructure (Dependency G).");
        Assert.Equal(ProcessorStatus.Active, LegalProcessors.ById("pingen").Status);
    }

    [Fact]
    public void A_planned_pingen_row_says_from_the_start_of_letter_verification()
    {
        var planned = LegalProcessors.ById("pingen") with { Status = ProcessorStatus.Planned };

        var cut = RenderComponent<ProcessorTable>(p => p.Add(c => c.Rows, [planned]));

        Assert.Contains(
            UiStrings.Get("Legal.Processor.pingen.Planned", "nl"),
            cut.Find(".pp-table__planned").TextContent,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Active_rows_carry_no_planned_note()
        => Assert.Empty(RenderPrivacy().FindAll("#delen .pp-table__planned"));

    [Fact]
    public void Transfers_outside_the_eu_name_a_basis_for_every_row()
    {
        foreach (var row in LegalProcessors.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(row.TransferBasisKey), $"{row.Id} has no transfer basis.");
        }
    }
}

/// <summary>The retention table is generated from <see cref="PrivacyConstants"/>, never hand-typed.</summary>
public class PrivacyRetentionTests : PrivacyRenderTestBase
{
    [Fact]
    public void Every_catalog_row_shows_its_formatted_duration_on_the_page()
    {
        var table = RenderPrivacy().Find("#bewaren .pp-table__grid").TextContent;

        Assert.Equal(13, LegalRetention.Rows.Count);
        foreach (var row in LegalRetention.Rows)
        {
            Assert.Contains(UiStrings.Get(row.LabelKey, "nl"), table, StringComparison.Ordinal);
            foreach (var part in row.Duration().Split(" · ", StringSplitOptions.RemoveEmptyEntries))
            {
                Assert.Contains(part, table, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Changing_a_constant_changes_the_page()
    {
        var patched = new LegalRetentionRow(
            "Legal.Retention.AccessLog",
            () => LegalRetention.FormatDays(1095),
            [nameof(PrivacyConstants.PersonalDataAccessLogRetentionDays)]);

        var cut = RenderComponent<RetentionTable>(p => p.Add(c => c.Rows, [patched]));

        Assert.Contains("3 jaar", cut.Find(".pp-table__grid").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(
            LegalRetention.FormatDays(PrivacyConstants.PersonalDataAccessLogRetentionDays),
            cut.Find(".pp-table__grid").TextContent,
            StringComparison.Ordinal);
    }

    [Fact]
    public void The_constants_the_review_found_missing_are_on_the_page()
    {
        var table = RenderPrivacy().Find("#bewaren .pp-table__grid").TextContent;

        foreach (var duration in new[]
        {
            LegalRetention.FormatDays(PrivacyConstants.PersonalDataAccessLogRetentionDays),
            LegalRetention.FormatDays(PrivacyConstants.UserNotificationRetentionDays),
            LegalRetention.FormatDays(PrivacyConstants.FeedbackScreenshotRetentionDays),
            LegalRetention.FormatDays(PrivacyConstants.CandidateActionTokenRetentionDays),
            LegalRetention.FormatMinutes(PrivacyConstants.UnconfirmedRegistrationRetentionMinutes)
        })
        {
            Assert.Contains(duration, table, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void No_retention_number_is_typed_in_the_privacy_markup()
    {
        foreach (var typed in new[] { "90 dagen", "365 dagen", "730 dagen", "2555 dagen", "48 uur", "25 maanden" })
        {
            Assert.DoesNotContain(typed, PrivacyMarkupSource, StringComparison.Ordinal);
        }
    }
}

/// <summary>The one age sentence of the platform comes from <see cref="CandidateConsentRules"/> (D11).</summary>
public class PrivacyAgeTextTests : PrivacyRenderTestBase
{
    [Fact]
    public void The_shared_component_reads_all_three_constants()
    {
        var text = RenderComponent<AgeRulesText>().Markup;

        Assert.Contains(
            CandidateConsentRules.MinimumCandidateAge.ToString(System.Globalization.CultureInfo.InvariantCulture),
            text,
            StringComparison.Ordinal);
        Assert.Contains(
            CandidateConsentRules.ParentalConsentAge.ToString(System.Globalization.CultureInfo.InvariantCulture),
            text,
            StringComparison.Ordinal);
        Assert.Contains(
            CandidateConsentRules.TalentPoolMinimumAge.ToString(System.Globalization.CultureInfo.InvariantCulture),
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Todays_constants_read_as_13_16_and_18()
    {
        Assert.Equal(13, CandidateConsentRules.MinimumCandidateAge);
        Assert.Equal(16, CandidateConsentRules.ParentalConsentAge);
        Assert.Equal(18, CandidateConsentRules.TalentPoolMinimumAge);
    }

    [Fact]
    public void Section_jonger_renders_the_shared_sentence()
    {
        var section = RenderPrivacy().Find("#jonger").TextContent;

        Assert.Contains("vanaf 13 jaar", section, StringComparison.Ordinal);
        Assert.Contains("jonger dan 16 jaar", section, StringComparison.Ordinal);
        Assert.Contains("vanaf 18 jaar", section, StringComparison.Ordinal);
    }

    [Fact]
    public void The_talent_pool_age_in_the_tests_section_is_not_hand_typed()
    {
        Assert.DoesNotContain("vanaf 18 jaar", PrivacyMarkupSource, StringComparison.Ordinal);
        Assert.DoesNotContain("vanaf 13 jaar", PrivacyMarkupSource, StringComparison.Ordinal);
    }
}

/// <summary>Section ids are stable URLs: they are linked from the footer and from older links.</summary>
public class PrivacyAnchorsTests : PrivacyRenderTestBase
{
    [Theory]
    [InlineData("wie")]
    [InlineData("gegevens")]
    [InlineData("waarom")]
    [InlineData("delen")]
    [InlineData("bewaren")]
    [InlineData("cookies")]
    [InlineData("ai")]
    [InlineData("tests")]
    [InlineData("jonger")]
    [InlineData("rechten")]
    [InlineData("beveiliging")]
    [InlineData("wijzigingen")]
    public void The_section_id_exists(string id)
        => Assert.NotNull(RenderPrivacy().Find($"section.pp-sec#{id}"));

    [Theory]
    [InlineData("verantwoordelijk")]
    [InlineData("grondslag")]
    [InlineData("verwerkers")]
    [InlineData("locatie")]
    [InlineData("jeugdige-arbeid")]
    [InlineData("salesmanager")]
    public void Links_shared_before_the_rewrite_still_land_on_a_section(string oldId)
    {
        var anchor = RenderPrivacy().Find($"#{oldId}");
        Assert.Equal("", anchor.TextContent);
        Assert.NotNull(anchor.Closest("section.pp-sec"));
    }

    [Fact]
    public void The_footer_links_privacy_cookies()
    {
        Assert.Equal("/privacy#cookies", PublicRoutes.PrivacyCookies);

        foreach (var variant in new[] { LandingVariant.On, LandingVariant.Zw })
        {
            Assert.Contains(
                PublicNavCatalog.Footer(variant).SelectMany(c => c.Items),
                item => item.Href == PublicRoutes.PrivacyCookies && item.IsAvailable);
        }
    }

    [Fact]
    public void The_rendered_footer_links_privacy_cookies()
    {
        var cut = RenderComponent<PublicLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(b =>
            {
                b.OpenComponent<PrivacyNl>(0);
                b.CloseComponent();
            })));

        Assert.Contains(
            cut.FindAll("footer a"),
            a => a.GetAttribute("href") == "/privacy#cookies");
    }

    [Fact]
    public void The_table_of_contents_offers_the_cookie_section()
        => Assert.Contains(
            RenderPrivacy().FindAll("nav.pp-toc a"),
            a => a.GetAttribute("href") == "#cookies");
}

/// <summary>A visitor never sees a placeholder, an internal id or a stale region claim (§0).</summary>
public class PrivacyNoPlaceholderTests : PrivacyRenderTestBase
{
    private static readonly Regex Placeholder = new(@"\[[A-Z][A-Z_ ]{1,}\]", RegexOptions.Compiled);

    [Fact]
    public void No_placeholder_and_no_stale_region_in_any_language()
    {
        foreach (var language in Languages)
        {
            UseLanguage(language);
            var markup = RenderPrivacy().Markup;

            var hit = Placeholder.Match(markup);
            Assert.False(hit.Success, $"Placeholder '{hit.Value}' visible in {language}.");
            Assert.DoesNotContain("EU/VS", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("EU/CH", markup, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Every_rendered_key_resolves_in_every_language()
    {
        foreach (var language in Languages)
        {
            UseLanguage(language);
            var markup = RenderPrivacy().Markup;

            Assert.DoesNotContain("Privacy.Sec.", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Legal.Processor.", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Legal.Transfer.", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Legal.Cookies.", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Legal.Retention.", markup, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_outdated_api_credentials_sentence_is_gone()
    {
        var text = RenderPrivacy().Find("#gegevens").TextContent;

        Assert.DoesNotContain("API-credentials kunnen éénmalig per e-mail", text, StringComparison.Ordinal);
        Assert.Contains("Een API-sleutel mailen we nooit.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void The_cookie_section_names_the_consent_rule_and_the_known_storage_keys()
    {
        var section = RenderPrivacy().Find("#cookies").TextContent;

        Assert.Contains("Statistieken alleen na", section, StringComparison.Ordinal);
        foreach (var key in new[]
        {
            "Jobsy.Auth", "Jobsy.LastActivity", "Jobsy.Culture", "Jobsy.CookieConsent",
            "jobsy.gratisDna.v1", "jobsy.anonymousKey", "lobsy_sales_ref"
        })
        {
            Assert.Contains(key, section, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_rights_section_links_my_data_and_the_dutch_authority()
    {
        var cut = RenderPrivacy();
        var links = cut.FindAll("#rechten a").Select(a => a.GetAttribute("href")).ToList();

        Assert.Contains("/privacy/data", links);
        Assert.Contains(links, href => href is not null && href.Contains("autoriteitpersoonsgegevens.nl", StringComparison.Ordinal));
    }
}
