using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Bunit;
using Jobsy.Core.Legal;
using Jobsy.Core.Privacy;
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
/// Shared bUnit host for the two terms documents (04.9). Both routes are static SSR, so a plain
/// render of <see cref="TermsPage"/> is exactly what a visitor gets.
/// </summary>
public abstract class TermsRenderTestBase : BunitContext
{
    protected static readonly string[] Languages = ["nl", "en", "pl", "ro", "ar"];

    private readonly DefaultHttpContext _http = new();
    private readonly AmbientCultureScope _culture = new();

    /// <summary>Response of <c>GET api/site/legal</c>; tests override it to drop identity rows.</summary>
    protected string LegalJson { get; set; } =
        """
        {"name":"Lobsy B.V.","street":"Teststraat 1","postalCode":"1234 AB","city":"Testdorp",
         "kvkNumber":"12345678","vatNumber":"NL001234567B01","supportEmail":"support@lobsy.nl"}
        """;

    protected TermsRenderTestBase()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddMemoryCache();
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<ICookieConsentTokenService, CookieConsentTokenService>();
        Services.AddSingleton<IEmployersSwitch, AlwaysOnEmployersSwitch>();
        Services.AddSingleton<IHostEnvironment>(new TermsTestHostEnvironment());
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = _http });
        Services.AddSingleton<IHttpClientFactory>(new TermsJsonHttpClientFactory(() => LegalJson));
        Services.AddSingleton<LegalIdentityProvider>();
        Services.AddSingleton<LandingStatsClient>();
        Services.AddSingleton<LandingPriceClient>();
        Services.AddScoped<LandingVariantResolver>();
        Services.AddScoped(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddScoped<PageSeoContext>();
        Services.AddSingleton<NavigationManager>(new TermsStaticNavigation(PublicRoutes.Terms));
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

    protected IRenderedComponent<TermsPage> Render(TermsAudience audience)
        => Render<TermsPage>(p => p.Add(c => c.Audience, audience));

    protected IRenderedComponent<TermsPage> Employer() => Render(TermsAudience.Employer);

    protected IRenderedComponent<TermsPage> Candidate() => Render(TermsAudience.Candidate);

    protected static string EmployerMarkupSource => ReadDoc("AlgemeneVoorwaardenNl.razor");

    protected static string CandidateMarkupSource => ReadDoc("GebruiksvoorwaardenNl.razor");

    private static string ReadDoc(string file) => File.ReadAllText(Path.Combine(
        RepoRoot(), "Jobsy.Web", "Components", "Legal", "Docs", file));

    protected static string RepoRoot()
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

    private sealed class TermsTestHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = Environments.Development;
    }

    private sealed class TermsJsonHttpClientFactory(Func<string> json) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new TermsJsonHandler(json)) { BaseAddress = new Uri("http://localhost/") };
    }

    private sealed class TermsJsonHandler(Func<string> json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json(), Encoding.UTF8, "application/json")
            });
    }

    private sealed class TermsStaticNavigation : NavigationManager
    {
        public TermsStaticNavigation(string relativePath)
            => Initialize("http://localhost/", "http://localhost" + relativePath);

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }
}

/// <summary>Both routes render one component with the right audience and switch state (04.2).</summary>
public class TermsPageTests : TermsRenderTestBase
{
    [Fact]
    public void Both_routes_render_the_shared_page_component()
    {
        foreach (var page in new[]
        {
            typeof(Jobsy.Web.Components.Pages.Legal.AlgemeneVoorwaarden),
            typeof(Jobsy.Web.Components.Pages.Legal.Gebruiksvoorwaarden)
        })
        {
            var source = File.ReadAllText(Path.Combine(
                RepoRoot(), "Jobsy.Web", "Components", "Pages", "Legal", page.Name + ".razor"));
            Assert.Contains("<TermsPage Audience=", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Employer_route_renders_the_employer_document()
    {
        var cut = Employer();
        Assert.Equal(
            UiStrings.Get("Legal.Terms"),
            cut.Find("h1.pp-doc__title").TextContent.Trim());
        Assert.NotNull(cut.Find("section.pp-sec#tokens"));
        Assert.Empty(cut.FindAll("section.pp-sec#bedenktijd"));
    }

    [Fact]
    public void Candidate_route_renders_the_candidate_document()
    {
        var cut = Candidate();
        Assert.Equal(
            UiStrings.Get("Legal.Usage"),
            cut.Find("h1.pp-doc__title").TextContent.Trim());
        Assert.NotNull(cut.Find("section.pp-sec#bedenktijd"));
        Assert.Empty(cut.FindAll("section.pp-sec#tokens"));
    }

    [Theory]
    [InlineData(TermsAudience.Employer)]
    [InlineData(TermsAudience.Candidate)]
    public void The_switch_offers_both_audiences_as_real_links(TermsAudience audience)
    {
        var links = Render(audience).FindAll("nav.pp-switch a").ToList();

        Assert.Equal(2, links.Count);
        Assert.Equal(PublicRoutes.Terms, links[0].GetAttribute("href"));
        Assert.Equal(PublicRoutes.UsageTerms, links[1].GetAttribute("href"));
        Assert.All(links, a => Assert.Null(a.GetAttribute("onclick")));
    }

    [Theory]
    [InlineData(TermsAudience.Employer, PublicRoutes.Terms)]
    [InlineData(TermsAudience.Candidate, PublicRoutes.UsageTerms)]
    public void Only_the_active_pill_is_aria_current_page(TermsAudience audience, string activeHref)
    {
        var current = Render(audience)
            .FindAll("nav.pp-switch a")
            .Where(a => a.GetAttribute("aria-current") == "page")
            .ToList();

        Assert.Single(current);
        Assert.Equal(activeHref, current[0].GetAttribute("href"));
        Assert.Contains("pp-switch__link--active", current[0].GetAttribute("class"));
    }

    [Theory]
    [InlineData("nl")]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("ro")]
    [InlineData("ar")]
    public void The_switch_label_is_translated(string language)
    {
        UseLanguage(language);
        var nav = Candidate().Find("nav.pp-switch");

        Assert.Equal(UiStrings.Get("Terms.Switch.Label", language), nav.GetAttribute("aria-label"));
        Assert.Contains(UiStrings.Get("Terms.Switch.Employers", language), nav.TextContent, StringComparison.Ordinal);
        Assert.Contains(UiStrings.Get("Terms.Switch.Candidates", language), nav.TextContent, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("wie")]
    [InlineData("toepassing")]
    [InlineData("dienst")]
    [InlineData("account")]
    [InlineData("tokens")]
    [InlineData("betalen")]
    [InlineData("vacatures")]
    [InlineData("melden")]
    [InlineData("sollicitaties")]
    [InlineData("beschikbaar")]
    [InlineData("aansprakelijkheid")]
    [InlineData("einde")]
    [InlineData("recht")]
    public void Employer_section_id_exists(string id)
        => Assert.NotNull(Employer().Find($"section.pp-sec#{id}"));

    [Theory]
    [InlineData("wie")]
    [InlineData("voor-wie")]
    [InlineData("wat")]
    [InlineData("account")]
    [InlineData("solliciteren")]
    [InlineData("bedenktijd")]
    [InlineData("ai")]
    [InlineData("melden")]
    [InlineData("gebruik")]
    [InlineData("beschikbaar")]
    [InlineData("aansprakelijkheid")]
    [InlineData("einde")]
    [InlineData("wijzigingen")]
    public void Candidate_section_id_exists(string id)
        => Assert.NotNull(Candidate().Find($"section.pp-sec#{id}"));

    [Theory]
    [InlineData(TermsAudience.Employer, "wie-is-lobsy")]
    [InlineData(TermsAudience.Employer, "toepasselijkheid")]
    [InlineData(TermsAudience.Employer, "account-kvk")]
    [InlineData(TermsAudience.Employer, "betalen-btw")]
    [InlineData(TermsAudience.Employer, "matching")]
    [InlineData(TermsAudience.Employer, "kandidaatgegevens")]
    [InlineData(TermsAudience.Employer, "beschikbaarheid")]
    [InlineData(TermsAudience.Employer, "beeindiging")]
    [InlineData(TermsAudience.Candidate, "wie-is-lobsy")]
    [InlineData(TermsAudience.Candidate, "dienst")]
    [InlineData(TermsAudience.Candidate, "matchscores")]
    [InlineData(TermsAudience.Candidate, "beschikbaarheid")]
    [InlineData(TermsAudience.Candidate, "beeindiging")]
    public void Links_shared_before_the_rewrite_still_land_on_a_section(TermsAudience audience, string oldId)
    {
        var anchor = Render(audience).Find($"#{oldId}");
        Assert.Equal("", anchor.TextContent);
        Assert.NotNull(anchor.Closest("section.pp-sec"));
    }

    [Theory]
    [InlineData(TermsAudience.Employer)]
    [InlineData(TermsAudience.Candidate)]
    public void Every_section_has_an_in_short_block_and_the_toc_matches(TermsAudience audience)
    {
        var cut = Render(audience);

        var sectionIds = cut.FindAll("section.pp-sec[id]")
            .Select(s => s.GetAttribute("id"))
            .Where(id => id != "wat-is-veranderd")
            .ToList();
        var tocTargets = cut.FindAll("nav.pp-toc ol.pp-toc__list a")
            .Select(a => a.GetAttribute("href")?.TrimStart('#'))
            .ToList();

        Assert.Equal(13, sectionIds.Count);
        Assert.Equal(sectionIds, tocTargets);
        Assert.Equal(13, cut.FindAll(".pp-short__text").Count);
    }

    [Theory]
    [InlineData(TermsAudience.Employer)]
    [InlineData(TermsAudience.Candidate)]
    public void The_version_line_comes_from_the_terms_constant(TermsAudience audience)
    {
        var version = Render(audience).Find(".pp-doc__version").TextContent;

        Assert.Contains("oktober 2026", version, StringComparison.Ordinal);
        Assert.Equal("2026-10", LegalDocumentVersions.Terms.Version);
    }

    [Theory]
    [InlineData(TermsAudience.Employer, "nl")]
    [InlineData(TermsAudience.Employer, "en")]
    [InlineData(TermsAudience.Employer, "pl")]
    [InlineData(TermsAudience.Employer, "ro")]
    [InlineData(TermsAudience.Employer, "ar")]
    [InlineData(TermsAudience.Candidate, "nl")]
    [InlineData(TermsAudience.Candidate, "en")]
    [InlineData(TermsAudience.Candidate, "pl")]
    [InlineData(TermsAudience.Candidate, "ro")]
    [InlineData(TermsAudience.Candidate, "ar")]
    public void No_raw_key_no_inline_style_and_no_placeholder(TermsAudience audience, string language)
    {
        var placeholder = new Regex(@"\[[A-Z][A-Z_ ]{1,}\]");

        UseLanguage(language);
        var markup = Render(audience).Markup;

        Assert.DoesNotContain("style=", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Terms.Sec.", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Terms.Switch.", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Terms.Waiver.", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Legal.IdentityCard.", markup, StringComparison.Ordinal);
        Assert.False(placeholder.IsMatch(markup), $"Placeholder visible in {language}.");
    }

    [Theory]
    [InlineData(TermsAudience.Employer, "Terms.Sec.tokens.Summary")]
    [InlineData(TermsAudience.Candidate, "Terms.Sec.bedenktijd.Summary")]
    public void An_arabic_reader_gets_the_summary_in_arabic_and_the_dutch_body_ltr(
        TermsAudience audience, string summaryKey)
    {
        UseLanguage("ar");
        var cut = Render(audience);

        var summaries = cut.FindAll(".pp-short__text").Select(s => s.TextContent.Trim()).ToList();
        Assert.Contains(UiStrings.Get(summaryKey, "ar"), summaries);
        Assert.DoesNotContain(UiStrings.Get(summaryKey, "nl"), summaries);

        var body = cut.FindAll(".pp-sec__body").ToList()[0];
        Assert.Equal("nl", body.GetAttribute("lang"));
        Assert.Equal("ltr", body.GetAttribute("dir"));
    }
}

/// <summary>Copy guards: the bugs 04.1 found are gone and the decided wording is on the page.</summary>
public class TermsCopyTests : TermsRenderTestBase
{
    [Theory]
    [InlineData("bulkapakket")]
    [InlineData("early-adapter")]
    [InlineData("exclusief of inclusief btw")]
    public void The_typos_and_the_vague_vat_sentence_are_gone(string wrong)
    {
        Assert.DoesNotContain(wrong, Employer().Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(wrong, Candidate().Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_corrected_words_are_used()
    {
        var text = Employer().Markup;
        Assert.Contains("bulkpakket", text, StringComparison.Ordinal);
        Assert.Contains("early-adopterkorting", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Employer_prices_are_excl_btw_with_the_checkout_sentence()
    {
        var tokens = Employer().Find("#tokens").TextContent;

        Assert.Contains("exclusief btw", tokens, StringComparison.Ordinal);
        Assert.Contains("inclusief btw", tokens, StringComparison.Ordinal);
    }

    [Fact]
    public void Employer_liability_has_a_minimum_cap_and_never_a_lower_one()
    {
        var text = Employer().Find("#aansprakelijkheid").TextContent;

        Assert.Contains("minimum van € 250", text, StringComparison.Ordinal);
        Assert.Contains("12 maanden", text, StringComparison.Ordinal);
        Assert.DoesNotContain("als dat lager is", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Candidate_liability_keeps_statutory_consumer_rights_and_caps_nothing()
    {
        var text = Candidate().Find("#aansprakelijkheid").TextContent;

        Assert.Contains("wettelijke rechten als consument", text, StringComparison.Ordinal);
        Assert.DoesNotContain("€ 250", text, StringComparison.Ordinal);
        Assert.DoesNotContain("12 maanden", text, StringComparison.Ordinal);
    }

    [Fact]
    public void The_bedenktijd_section_quotes_the_shared_checkbox_sentence_word_for_word()
    {
        var text = Candidate().Find("#bedenktijd").TextContent;
        var sentence = UiStrings.Get("Terms.Waiver.Checkbox", "nl");

        Assert.Contains(sentence, text, StringComparison.Ordinal);
        Assert.Contains("14 dagen bedenktijd", sentence, StringComparison.Ordinal);
        Assert.Contains("inclusief btw", text, StringComparison.Ordinal);
        Assert.Contains("bevestiging per e-mail", text, StringComparison.Ordinal);
    }

    [Fact]
    public void The_checkbox_sentence_is_one_key_and_never_typed_in_the_markup()
    {
        var sentence = UiStrings.Get("Terms.Waiver.Checkbox", "nl");

        Assert.DoesNotContain(sentence, CandidateMarkupSource, StringComparison.Ordinal);
        foreach (var language in Languages)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(UiStrings.Get("Terms.Waiver.Checkbox", language)),
                $"Terms.Waiver.Checkbox missing for {language}.");
        }
    }

    [Fact]
    public void The_parental_consent_age_in_the_bedenktijd_section_comes_from_the_constant()
    {
        var text = Candidate().Find("#bedenktijd").TextContent;

        Assert.Contains(
            CandidateConsentRules.ParentalConsentAge.ToString(System.Globalization.CultureInfo.InvariantCulture),
            text,
            StringComparison.Ordinal);
        Assert.DoesNotContain("jonger dan 16", CandidateMarkupSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Reporting_says_what_happens_and_names_the_contact_point()
    {
        foreach (var audience in new[] { TermsAudience.Employer, TermsAudience.Candidate })
        {
            var text = Render(audience).Find("#melden").TextContent;

            Assert.Contains("Meld deze vacature", text, StringComparison.Ordinal);
            Assert.Contains("Meld dit bedrijf", text, StringComparison.Ordinal);
            Assert.Contains("5 werkdagen", text, StringComparison.Ordinal);
            Assert.Contains("6 maanden", text, StringComparison.Ordinal);
            Assert.Contains("Contactpunt voor autoriteiten", text, StringComparison.Ordinal);
            Assert.Contains("support@lobsy.nl", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Reporting_links_the_melden_form_and_the_support_mail()
    {
        var section = Candidate().Find("#melden");
        var links = section.QuerySelectorAll("a")
            .Select(a => a.GetAttribute("href"))
            .ToList();

        Assert.Contains("/melden", links);
        Assert.Contains(links, href => href?.StartsWith("mailto:", StringComparison.Ordinal) == true);
        // Public-pages 06 requires the text to name the button of the form.
        Assert.Contains("Melding versturen", section.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Reporting_without_a_configured_support_mail_shows_no_empty_mailto()
    {
        LegalJson = """{"name":"Lobsy B.V."}""";
        var section = Candidate().Find("#melden");

        Assert.Empty(section.QuerySelectorAll("a[href^='mailto:']"));
        Assert.DoesNotContain("mailto:", section.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void The_kvk_rule_matches_the_company_page_rule_of_file_01()
        => Assert.Contains(
            "is je bedrijfspagina niet openbaar",
            Employer().Find("#account").TextContent,
            StringComparison.Ordinal);

    [Fact]
    public void No_price_amount_is_typed_in_either_terms_document()
    {
        foreach (var source in new[] { EmployerMarkupSource, CandidateMarkupSource })
        {
            Assert.DoesNotContain("2,99", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_acceptance_links_keep_pointing_at_the_two_routes()
    {
        Assert.Equal("/algemene-voorwaarden", PublicRoutes.Terms);
        Assert.Equal("/gebruiksvoorwaarden", PublicRoutes.UsageTerms);

        Assert.Contains(
            Candidate().FindAll("#voor-wie a").Select(a => a.GetAttribute("href")),
            href => href == PublicRoutes.Terms);
        Assert.Contains(
            Employer().FindAll("#toepassing a").Select(a => a.GetAttribute("href")),
            href => href == PublicRoutes.UsageTerms);
    }
}

/// <summary>The identity card on both terms pages follows <c>LegalIdentityProvider</c> (D1).</summary>
public class TermsIdentityTests : TermsRenderTestBase
{
    [Theory]
    [InlineData(TermsAudience.Employer)]
    [InlineData(TermsAudience.Candidate)]
    public void Section_wie_shows_the_identity_card_with_the_btw_number_and_support_mail(TermsAudience audience)
    {
        var section = Render(audience).Find("#wie");
        var labels = section.QuerySelectorAll(".pp-identity__label")
            .Select(l => l.TextContent.Trim())
            .ToList();

        Assert.Contains(UiStrings.Get("Legal.IdentityCard.Name"), labels);
        Assert.Contains(UiStrings.Get("Legal.IdentityCard.Address"), labels);
        Assert.Contains(UiStrings.Get("Legal.IdentityCard.Kvk"), labels);
        Assert.Contains(UiStrings.Get("Legal.IdentityCard.Vat"), labels);
        Assert.Contains(UiStrings.Get("Legal.IdentityCard.Contact"), labels);
        Assert.DoesNotContain(UiStrings.Get("Legal.IdentityCard.PrivacyQuestions"), labels);
    }

    [Theory]
    [InlineData(TermsAudience.Employer)]
    [InlineData(TermsAudience.Candidate)]
    public void An_empty_legal_value_hides_its_row_and_never_shows_a_placeholder(TermsAudience audience)
    {
        LegalJson = """{"name":"Lobsy B.V.","supportEmail":"support@lobsy.nl"}""";
        var section = Render(audience).Find("#wie");
        var labels = section.QuerySelectorAll(".pp-identity__label")
            .Select(l => l.TextContent.Trim())
            .ToList();

        Assert.Contains(UiStrings.Get("Legal.IdentityCard.Name"), labels);
        Assert.DoesNotContain(UiStrings.Get("Legal.IdentityCard.Address"), labels);
        Assert.DoesNotContain(UiStrings.Get("Legal.IdentityCard.Kvk"), labels);
        Assert.DoesNotContain(UiStrings.Get("Legal.IdentityCard.Vat"), labels);
    }

    [Fact]
    public void No_company_name_address_or_kvk_is_typed_in_the_markup()
    {
        foreach (var source in new[] { EmployerMarkupSource, CandidateMarkupSource })
        {
            Assert.DoesNotContain("KvK 1", source, StringComparison.Ordinal);
            Assert.DoesNotContain("B.V.", source, StringComparison.Ordinal);
            Assert.DoesNotContain("@lobsy.nl", source, StringComparison.Ordinal);
            Assert.DoesNotContain("NL00", source, StringComparison.Ordinal);
        }
    }
}

/// <summary>
/// The one age sentence of the platform is rendered by the same component in privacy and in the
/// gebruiksvoorwaarden (D11 / 04.9).
/// </summary>
public class TermsAgeRulesSharingTests : TermsRenderTestBase
{
    [Fact]
    public void Privacy_and_the_terms_render_the_same_sentence()
    {
        var shared = Normalize(Render<AgeRulesText>().Markup);
        var inTerms = Normalize(Candidate().Find("#voor-wie").TextContent);
        var inPrivacy = Normalize(Render<PrivacyNl>().Find("#jonger").TextContent);

        Assert.Contains(shared, inTerms, StringComparison.Ordinal);
        Assert.Contains(shared, inPrivacy, StringComparison.Ordinal);
    }

    [Fact]
    public void Neither_document_types_an_age_by_hand()
    {
        foreach (var source in new[] { EmployerMarkupSource, CandidateMarkupSource })
        {
            Assert.DoesNotContain("vanaf 13 jaar", source, StringComparison.Ordinal);
            Assert.DoesNotContain("vanaf 18 jaar", source, StringComparison.Ordinal);
            Assert.DoesNotContain("16 jaar", source, StringComparison.Ordinal);
        }
    }

    private static string Normalize(string value)
    {
        var text = Regex.Replace(value, "<[^>]+>", " ");
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
}

/// <summary>
/// Werkgevers actief OFF (Dependency F present): the audience switch keeps both pills, while the
/// footer drops the employer terms link (04.2).
/// </summary>
public class TermsEmployersSwitchTests : TermsRenderTestBase
{
    [Fact]
    public void The_audience_switch_shows_both_pills_whatever_the_variant()
        => Assert.Equal(2, Candidate().FindAll("nav.pp-switch a").Count);

    [Fact]
    public void Off_hides_the_employer_terms_from_the_footer_but_keeps_the_candidate_terms()
    {
        var off = PublicNavCatalog.Footer(LandingVariant.Zw)
            .SelectMany(c => c.Items)
            .Select(i => i.Href)
            .ToList();

        Assert.DoesNotContain(PublicRoutes.Terms, off);
        Assert.Contains(PublicRoutes.UsageTerms, off);
        Assert.Contains(PublicRoutes.Privacy, off);
    }

    [Fact]
    public void On_keeps_both_terms_links_in_the_footer()
    {
        var on = PublicNavCatalog.Footer(LandingVariant.On)
            .SelectMany(c => c.Items)
            .Select(i => i.Href)
            .ToList();

        Assert.Contains(PublicRoutes.Terms, on);
        Assert.Contains(PublicRoutes.UsageTerms, on);
    }
}
