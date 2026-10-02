using System.Security.Claims;
using Bunit;
using Jobsy.Core.Legal;
using Jobsy.Web.Components;
using Jobsy.Web.Components.Layout;
using Jobsy.Web.Components.Legal;
using Jobsy.Web.Components.Legal.Docs;
using Jobsy.Web.Features;
using Jobsy.Web.Localization;
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
/// LegalDocument shell: table of contents matches the section ids, "In het kort" follows the reader's
/// language, the Dutch body stays <c>lang="nl"</c> and the ar page is right-to-left (02.8).
/// </summary>
public class LegalDocumentRenderTests : TestContext
{
    private readonly DefaultHttpContext _http = new();
    private readonly AmbientCultureScope _culture = new();

    public LegalDocumentRenderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddMemoryCache();
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<ICookieConsentTokenService, CookieConsentTokenService>();
        Services.AddSingleton<IEmployersSwitch, AlwaysOnEmployersSwitch>();
        Services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = _http });
        Services.AddSingleton<IHttpClientFactory>(new OfflineHttpClientFactory());
        Services.AddSingleton<LegalIdentityProvider>();
        // PublicLayout shows the maintenance admin banner (errors 05).
        Services.AddSingleton(new Jobsy.Web.Hosting.MaintenanceState());
        Services.AddSingleton<LandingStatsClient>();
        Services.AddSingleton<LandingPriceClient>();
        Services.AddScoped<LandingVariantResolver>();
        Services.AddScoped(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddScoped<PageSeoContext>();
        Services.AddSingleton<NavigationManager>(new StaticNavigation("/privacy"));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _culture.Dispose();
        }
    }

    private void UseLanguage(string language)
    {
        _http.Request.QueryString = new QueryString($"?lang={language}");
        Services.GetRequiredService<CultureState>().InitializeFromRequest(_http);
    }

    private IRenderedComponent<PrivacyNl> RenderPrivacy()
        => RenderComponent<PrivacyNl>();

    [Fact]
    public void Toc_links_match_the_section_ids()
    {
        var cut = RenderPrivacy();

        var sectionIds = cut.FindAll("section.pp-sec[id]")
            .Select(s => s.GetAttribute("id"))
            .Where(id => id != "wat-is-veranderd")
            .ToList();

        var tocTargets = cut.FindAll("nav.pp-toc ol.pp-toc__list a")
            .Select(a => a.GetAttribute("href")?.TrimStart('#'))
            .ToList();

        Assert.NotEmpty(sectionIds);
        Assert.Equal(sectionIds, tocTargets);
        Assert.Equal(sectionIds.Count, sectionIds.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Mobile_toc_counts_the_sections_and_links_to_each_one()
    {
        var cut = RenderPrivacy();
        var sections = cut.FindAll("section.pp-sec[id]").Count - 1;

        var summary = cut.Find("details.pp-toc-mobile summary").TextContent;
        Assert.Contains(sections.ToString(System.Globalization.CultureInfo.InvariantCulture), summary, StringComparison.Ordinal);
        Assert.Equal(sections, cut.FindAll("details.pp-toc-mobile a").Count);
    }

    [Fact]
    public void Privacy_toc_links_to_my_data_and_offers_print()
    {
        var cut = RenderPrivacy();
        Assert.Contains(cut.FindAll("nav.pp-toc a"), a => a.GetAttribute("href") == "/privacy/data");
        Assert.Single(cut.FindAll("nav.pp-toc button[data-pp-print]"));
    }

    [Fact]
    public void Terms_toc_has_no_my_data_link()
    {
        var cut = RenderComponent<AlgemeneVoorwaardenNl>();
        Assert.DoesNotContain(cut.FindAll("nav.pp-toc a"), a => a.GetAttribute("href") == "/privacy/data");
    }

    [Fact]
    public void Dutch_reader_sees_no_language_note_and_no_dutch_label()
    {
        var cut = RenderPrivacy();
        Assert.Empty(cut.FindAll(".pp-doc__note"));
        Assert.Empty(cut.FindAll(".pp-sec__dutch-label"));
        Assert.Empty(cut.FindAll(".pp-sec__body[lang]"));
    }

    [Fact]
    public void In_short_block_uses_the_reader_language()
    {
        UseLanguage("ar");
        var cut = RenderPrivacy();

#pragma warning disable CA1826 // bunit FindAll indexer hits AngleSharp MissingMethodException
        var label = cut.FindAll(".pp-short__label").First().TextContent.Trim();
        Assert.Equal(UiStrings.Get("Legal.InShort", "ar"), label);

        var summary = cut.FindAll(".pp-short__text").First().TextContent.Trim();
        Assert.Equal(UiStrings.Get("Privacy.Sec.wie.Summary", "ar"), summary);
        Assert.NotEqual(UiStrings.Get("Privacy.Sec.wie.Summary", "nl"), summary);
    }

    [Fact]
    public void Arabic_reader_gets_an_ltr_dutch_body_with_lang_nl()
    {
        UseLanguage("ar");
        var cut = RenderPrivacy();

        Assert.Single(cut.FindAll(".pp-doc__note"));
        var body = cut.FindAll(".pp-sec__body").First();
#pragma warning restore CA1826
        Assert.Equal("nl", body.GetAttribute("lang"));
        Assert.Equal("ltr", body.GetAttribute("dir"));
        Assert.NotEmpty(cut.FindAll(".pp-sec__dutch-label"));
    }

    [Fact]
    public void Arabic_page_root_is_right_to_left()
    {
        UseLanguage("ar");
        var cut = RenderComponent<PublicLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(b =>
            {
                b.OpenComponent<PrivacyNl>(0);
                b.CloseComponent();
            })));

        var root = cut.Find("div.pub-theme");
        Assert.Equal("rtl", root.GetAttribute("dir"));
        Assert.Equal("ar", root.GetAttribute("lang"));
    }

    [Fact]
    public void Version_line_comes_from_the_constant_and_no_hand_typed_date_remains()
    {
        var cut = RenderPrivacy();
        var version = cut.Find(".pp-doc__version").TextContent;

        Assert.Contains("oktober 2026", version, StringComparison.Ordinal);
        Assert.Contains("1 oktober 2026", version, StringComparison.Ordinal);

        var markup = cut.Markup;
        Assert.DoesNotContain("26 september 2026", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Laatst bijgewerkt", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Change_log_shows_at_most_three_versions_from_the_history()
    {
        var cut = RenderPrivacy();
        var items = cut.FindAll(".pp-changes__item");
        Assert.InRange(items.Count, 1, 3);
        Assert.Equal(
            Math.Min(3, LegalDocumentVersions.PrivacyHistory.Count),
            items.Count);
    }

    [Fact]
    public void Section_headings_are_h2_under_one_h1_and_are_labelled()
    {
        var cut = RenderPrivacy();
        Assert.Single(cut.FindAll("h1"));

        foreach (var section in cut.FindAll("section.pp-sec"))
        {
            var labelledBy = section.GetAttribute("aria-labelledby");
            Assert.False(string.IsNullOrWhiteSpace(labelledBy));
            Assert.NotNull(cut.Find($"h2#{labelledBy}"));
        }
    }

    [Fact]
    public void No_inline_styles_and_no_raw_keys_in_the_markup()
    {
        foreach (var language in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            UseLanguage(language);
            var markup = RenderPrivacy().Markup;
            Assert.DoesNotContain("style=", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Privacy.Sec.", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Legal.", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("[ADRES]", markup, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void All_three_documents_render_their_own_sections()
    {
        Assert.NotEmpty(RenderComponent<PrivacyNl>().FindAll("section.pp-sec"));
        Assert.NotEmpty(RenderComponent<AlgemeneVoorwaardenNl>().FindAll("section.pp-sec"));
        Assert.NotEmpty(RenderComponent<GebruiksvoorwaardenNl>().FindAll("section.pp-sec"));
    }

    [Fact]
    public void Retention_table_shows_durations_from_PrivacyConstants()
    {
        var cut = RenderPrivacy();
        var table = cut.Find("#bewaren .pp-table__grid").TextContent;
        Assert.Contains("48 uur", table, StringComparison.Ordinal);
        Assert.Contains("10 minuten", table, StringComparison.Ordinal);
        Assert.Contains("2 jaar", table, StringComparison.Ordinal);
        Assert.Contains(LegalRetention.UntilAccountDeleted, table, StringComparison.Ordinal);
    }

    [Fact]
    public void Processor_table_renders_inside_the_sharing_section()
        => Assert.Single(RenderPrivacy().FindAll("#delen .pp-table__grid"));

    [Theory]
    [InlineData(typeof(Jobsy.Web.Components.Pages.Legal.Privacy))]
    [InlineData(typeof(Jobsy.Web.Components.Pages.Legal.AlgemeneVoorwaarden))]
    [InlineData(typeof(Jobsy.Web.Components.Pages.Legal.Gebruiksvoorwaarden))]
    public void Legal_routes_are_anonymous_static_ssr_on_the_public_layout(Type page)
    {
        Assert.NotNull(page.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), false).SingleOrDefault());
        Assert.NotNull(page.GetCustomAttributes(typeof(ExcludeFromInteractiveRoutingAttribute), false).SingleOrDefault());
        Assert.NotNull(page.GetCustomAttributes(typeof(NoBlazorRuntimeAttribute), false).SingleOrDefault());

        var layout = (LayoutAttribute?)page
            .GetCustomAttributes(typeof(LayoutAttribute), false)
            .SingleOrDefault();
        Assert.Equal(typeof(PublicLayout), layout?.LayoutType);
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = Environments.Development;
    }

    private sealed class OfflineHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new OfflineHandler()) { BaseAddress = new Uri("http://localhost/") };
    }

    private sealed class OfflineHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.RequestTimeout));
    }

    private sealed class StaticNavigation : NavigationManager
    {
        public StaticNavigation(string relativePath)
            => Initialize("http://localhost/", "http://localhost" + relativePath);

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }
}
