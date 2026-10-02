using System.Net;
using System.Security.Claims;
using System.Text;
using Bunit;
using Jobsy.Web.Components.Legal;
using Jobsy.Web.Components.Pages.Legal;
using Jobsy.Web.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
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
/// <c>/wie-zijn-wij</c> (public-pages 08): static SSR in five languages, contact via
/// <c>Legal:SupportEmail</c>, and no HTML from the database anymore.
/// </summary>
public class AboutPageTests : TestContext
{
    private readonly DefaultHttpContext _http = new();
    private readonly AmbientCultureScope _culture = new();

    private string _legalJson =
        """
        {"name":"Lobsy B.V.","street":"Teststraat 1","postalCode":"1234 AB","city":"Testdorp",
         "kvkNumber":"12345678","vatNumber":"NL001234567B01","supportEmail":"support@lobsy.nl",
         "privacyContact":"privacy@lobsy.nl"}
        """;

    public AboutPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddMemoryCache();
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<ICookieConsentTokenService, CookieConsentTokenService>();
        Services.AddSingleton<IEmployersSwitch, AlwaysOnEmployersSwitch>();
        Services.AddSingleton<IHostEnvironment>(new AboutHostEnvironment());
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = _http });
        Services.AddSingleton<IHttpClientFactory>(new AboutHttpClientFactory(() => _legalJson));
        Services.AddSingleton<LegalIdentityProvider>();
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddScoped<LandingVariantResolver>();
        Services.AddScoped(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddScoped<PageSeoContext>();
        Services.AddSingleton<NavigationManager>(new AboutStaticNavigation(PublicRoutes.About));
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

    private IRenderedComponent<WieZijnWij> Render() => RenderComponent<WieZijnWij>();

    private static string PageSource => File.ReadAllText(Path.Combine(
        HowLobsyRenderTestBase.RepoRoot(), "Jobsy.Web", "Components", "Pages", "Legal", "WieZijnWij.razor"));

    [Fact]
    public void The_page_is_static_ssr_without_a_loading_state()
    {
        var source = PageSource;

        Assert.Contains("[ExcludeFromInteractiveRouting]", source, StringComparison.Ordinal);
        Assert.Contains("[NoBlazorRuntime]", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveServer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Laden", Render().Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_hero_and_the_three_story_cards_are_rendered()
    {
        var cut = Render();

        Assert.Equal(UiStrings.Get("About.Title"), cut.Find("h1.pp-doc__title").TextContent.Trim());

        var stories = cut.FindAll("article.pp-about__story").ToList();
        Assert.Equal(3, stories.Count);
        Assert.Equal(UiStrings.Get("About.Story.Lobster.Title"), stories[0].QuerySelector("h2")!.TextContent.Trim());
        Assert.Equal(UiStrings.Get("About.Story.Westland.Title"), stories[1].QuerySelector("h2")!.TextContent.Trim());
        Assert.Equal(UiStrings.Get("About.Story.BothSides.Title"), stories[2].QuerySelector("h2")!.TextContent.Trim());
    }

    [Fact]
    public void The_founder_card_names_the_founder_and_falls_back_to_the_emoji_avatar()
    {
        var card = Render().Find("article.pp-about__founder");

        Assert.Equal(UiStrings.Get("About.Founder.Name"), card.QuerySelector("h2")!.TextContent.Trim());
        Assert.Contains(UiStrings.Get("About.Founder.Text"), card.TextContent, StringComparison.Ordinal);

        if (AboutAssets.HasFounderPhoto)
        {
            Assert.NotNull(card.QuerySelector("img.pp-about__photo"));
        }
        else
        {
            Assert.NotNull(card.QuerySelector("span.pp-about__avatar"));
        }
    }

    [Fact]
    public void The_photo_flag_matches_the_file_on_disk()
    {
        var path = Path.Combine(
            HowLobsyRenderTestBase.RepoRoot(), "Jobsy.Web", "wwwroot", AboutAssets.FounderPhotoPath);

        Assert.Equal(AboutAssets.HasFounderPhoto, File.Exists(path));
    }

    [Fact]
    public void Contact_is_the_configured_support_mail_and_never_the_privacy_mail()
    {
        var card = Render().Find("article.pp-about__contact");
        var mailto = card.QuerySelectorAll("a[href^='mailto:']")
            .Select(a => a.GetAttribute("href"))
            .ToList();

        Assert.Contains("mailto:support@lobsy.nl", mailto);
        Assert.DoesNotContain(mailto, href => href?.Contains("privacy@", StringComparison.Ordinal) == true);
        Assert.DoesNotContain("privacy@", card.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("@lobsy.nl", PageSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_configured_support_mail_there_is_no_empty_mailto()
    {
        _legalJson = """{"name":"Lobsy B.V."}""";
        var card = Render().Find("article.pp-about__contact");

        Assert.Empty(card.QuerySelectorAll("a[href^='mailto:']"));
        Assert.DoesNotContain("mailto:", card.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void The_identity_card_shows_no_privacy_row_but_the_privacy_statement_is_linked()
    {
        var card = Render().Find("article.pp-about__contact");
        var labels = card.QuerySelectorAll(".pp-identity__label").Select(l => l.TextContent.Trim()).ToList();

        Assert.Contains(UiStrings.Get("Legal.IdentityCard.Name"), labels);
        Assert.DoesNotContain(UiStrings.Get("Legal.IdentityCard.PrivacyQuestions"), labels);
        Assert.Contains(
            card.QuerySelectorAll("a").Select(a => a.GetAttribute("href")),
            href => href == PublicRoutes.Privacy);
    }

    [Theory]
    [InlineData("nl")]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("ro")]
    [InlineData("ar")]
    public void Every_language_renders_its_own_copy_without_raw_keys_or_inline_styles(string language)
    {
        UseLanguage(language);
        var cut = Render();

        Assert.Equal(UiStrings.Get("About.Title", language), cut.Find("h1.pp-doc__title").TextContent.Trim());
        Assert.DoesNotContain("About.", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", cut.Markup, StringComparison.Ordinal);
        if (language != "nl")
        {
            Assert.NotEqual(UiStrings.Get("About.Title", "nl"), UiStrings.Get("About.Title", language));
        }
    }

    [Fact]
    public void An_arabic_reader_gets_arabic_copy_and_the_layout_flips_to_rtl()
    {
        UseLanguage("ar");

        Assert.True(Services.GetRequiredService<CultureState>().IsRightToLeft);
        Assert.Contains(UiStrings.Get("About.Lead", "ar"), Render().Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void No_html_comes_from_the_database_anymore()
    {
        var source = PageSource;

        Assert.DoesNotContain("AboutPage", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MarkupString", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HtmlSanitize", source, StringComparison.Ordinal);
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class AboutHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = Environments.Production;
    }

    private sealed class AboutHttpClientFactory(Func<string> json) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new AboutJsonHandler(json)) { BaseAddress = new Uri("http://localhost/") };
    }

    private sealed class AboutJsonHandler(Func<string> json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json(), Encoding.UTF8, "application/json")
            });
    }

    private sealed class AboutStaticNavigation : NavigationManager
    {
        public AboutStaticNavigation(string relativePath)
            => Initialize("http://localhost/", "http://localhost" + relativePath);

        protected override void NavigateToCore(string uri, bool forceLoad)
            => throw new InvalidOperationException($"The page must not redirect, but navigated to {uri}.");
    }
}

/// <summary>The admin text editor for the about page is gone for good (08.3).</summary>
public class AboutAdminRemovedTests
{
    [Fact]
    public void The_legacy_admin_route_answers_a_permanent_redirect_to_the_admin_home()
    {
        var map = AdminLegacyRoutes.All.ToDictionary(r => r.OldPath, r => r.NewPath, StringComparer.OrdinalIgnoreCase);

        Assert.True(map.TryGetValue("/admin/about", out var target));
        Assert.Equal("/admin", target);
    }

    [Fact]
    public void The_about_settings_service_is_no_longer_referenced_in_the_code()
    {
        var root = HowLobsyRenderTestBase.RepoRoot();
        foreach (var project in new[] { "Jobsy.Api", "Jobsy.Core", "Jobsy.Infrastructure", "Jobsy.Web" })
        {
            foreach (var file in Directory.EnumerateFiles(
                Path.Combine(root, project), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.DoesNotContain("IAboutPageSettingsService", File.ReadAllText(file), StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void The_api_no_longer_exposes_an_about_endpoint()
    {
        var root = HowLobsyRenderTestBase.RepoRoot();
        foreach (var controller in new[] { "SettingsController.cs", "SiteController.cs" })
        {
            var source = File.ReadAllText(Path.Combine(root, "Jobsy.Api", "Controllers", controller));
            Assert.DoesNotContain("\"about\"", source, StringComparison.Ordinal);
            Assert.DoesNotContain("AboutPage", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_admin_section_component_and_the_web_client_methods_are_deleted()
    {
        var root = HowLobsyRenderTestBase.RepoRoot();

        Assert.False(File.Exists(Path.Combine(
            root, "Jobsy.Web", "Components", "Admin", "Sections", "AboutPageSection.razor")));
        Assert.False(File.Exists(Path.Combine(
            root, "Jobsy.Core", "Interfaces", "IAboutPageSettingsService.cs")));
        Assert.False(File.Exists(Path.Combine(
            root, "Jobsy.Infrastructure", "Services", "AboutPageSettingsService.cs")));

        var client = File.ReadAllText(Path.Combine(
            root, "Jobsy.Web", "Services", "ApiClient", "JobsyApiClient.Admin.cs"));
        Assert.DoesNotContain("AboutPage", client, StringComparison.Ordinal);
    }

    [Fact]
    public void The_follow_up_note_records_the_table_drop()
    {
        var followups = File.ReadAllText(Path.Combine(
            HowLobsyRenderTestBase.RepoRoot(), "docs", "public-pages-followups.md"));

        Assert.Contains("AboutPageSettings", followups, StringComparison.Ordinal);
    }
}
