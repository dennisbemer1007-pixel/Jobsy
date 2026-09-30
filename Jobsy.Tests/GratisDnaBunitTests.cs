using Bunit;
using Jobsy.Core.Rules;
using Jobsy.Web.Auth;
using Jobsy.Web.Components.Public;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public class GratisDnaBunitTests : TestContext
{
    public GratisDnaBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IExternalAuthCredentialSource, FakeExternalAuth>();
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Result_view_renders_four_tiles_teaser_share_cta_wipe_without_percent()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<GratisDnaResultView>(0);
            builder.AddAttribute(1, "RegisterHref", "/account-maken?van=ontdek");
            builder.AddAttribute(2, "StrengthSentence", OnboardingImpressionLibrary.StrengthSentence(CompetencyTestCatalog.Samenwerken));
            builder.AddAttribute(3, "RiasecSentence", OnboardingImpressionLibrary.RiasecSentence(CareerTestCatalog.Social));
            builder.AddAttribute(4, "CultureSentence", OnboardingImpressionLibrary.CultureSentence(CulturePersonalityCatalog.Collaboration));
            builder.AddAttribute(5, "ValueSentence", OnboardingImpressionLibrary.ValueSentence(SchwartzValuesCatalog.Achievement));
            builder.AddAttribute(6, "StrengthLabel", "Samenwerker");
            builder.AddAttribute(7, "ShowSticky", true);
            builder.CloseComponent();
        });

        var markup = cut.Markup;
        Assert.Contains("Eerste indruk · op basis van 20 vragen", markup, StringComparison.Ordinal);
        Assert.Contains("Zo werk jij", markup, StringComparison.Ordinal);
        Assert.Contains("Dit vind je leuk", markup, StringComparison.Ordinal);
        Assert.Contains("Hier voel je je thuis", markup, StringComparison.Ordinal);
        Assert.Contains("Dit vind je belangrijk", markup, StringComparison.Ordinal);
        Assert.Contains("Dit ben jij", markup, StringComparison.Ordinal);
        Assert.Contains("Meld je aan om te zien welke banen bij je passen", markup, StringComparison.Ordinal);
        Assert.Contains("WhatsApp", markup, StringComparison.Ordinal);
        Assert.Contains("Instagram", markup, StringComparison.Ordinal);
        Assert.Contains("Wis mijn antwoorden", markup, StringComparison.Ordinal);
        Assert.Contains("Bewaar je DNA · Maak gratis account", markup, StringComparison.Ordinal);
        Assert.Contains("/account-maken?van=ontdek", markup, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"gd-passport-teaser\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-hidden=\"true\"", markup, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"gd-signup\"", markup, StringComparison.Ordinal);
        Assert.Contains("data-kpi=\"ResultCtaSignup\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("/register", markup, StringComparison.Ordinal);
        var tilesText = cut.Find("[data-testid=gd-tiles]").TextContent;
        Assert.DoesNotContain("%", tilesText, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".cookie-consent"));
    }

    [Fact]
    public void Signup_card_links_to_account_maken_and_external_providers_not_register()
    {
        var cut = RenderComponent<GratisDnaSignupCard>();
        var markup = cut.Markup;
        Assert.Contains("/account-maken?van=ontdek#email", markup, StringComparison.Ordinal);
        Assert.Contains("/account/external/google?", markup, StringComparison.Ordinal);
        Assert.Contains("/account/external/entra?", markup, StringComparison.Ordinal);
        Assert.Contains("van=ontdek", markup, StringComparison.Ordinal);
        Assert.Contains("data-kpi=\"ResultCtaSignup\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/register", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Under16_view_renders_register_cta_and_page_never_saves_on_under16_choice()
    {
        var cut = RenderComponent<GratisDnaUnder16View>();

        Assert.Contains("Leuk dat je mee wilt doen!", cut.Markup, StringComparison.Ordinal);
        var cta = cut.Find("[data-testid=gd-under16-cta]");
        Assert.Equal("/account-maken?van=onder16", cta.GetAttribute("href"));

        // ChooseAge(false) on the page only flips FlowState — no Storage.Create/Save/Clear.
        var page = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Public", "GratisDna.razor"));
        var chooseAgeIdx = page.IndexOf("private void ChooseAge(bool is16Plus)", StringComparison.Ordinal);
        Assert.True(chooseAgeIdx > 0);
        var chooseAgeEnd = page.IndexOf("private async Task StartAsync()", chooseAgeIdx, StringComparison.Ordinal);
        Assert.True(chooseAgeEnd > chooseAgeIdx);
        var chooseAgeBody = page[chooseAgeIdx..chooseAgeEnd];
        Assert.Contains("_state = FlowState.Under16", chooseAgeBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Storage.", chooseAgeBody, StringComparison.Ordinal);
        Assert.DoesNotContain("PersistAsync", chooseAgeBody, StringComparison.Ordinal);
        Assert.Contains("@layout PublicLayout", page, StringComparison.Ordinal);
        Assert.DoesNotContain("cookie-consent", page, StringComparison.Ordinal);
        Assert.Contains("FeedbackWidget", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Page_source_has_warm_likert_accessible_names_and_faces_aria_hidden()
    {
        var page = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Public", "GratisDna.razor"));
        Assert.Contains("GratisDna.Likert.AriaLow", page, StringComparison.Ordinal);
        Assert.Contains("GratisDna.Likert.AriaHigh", page, StringComparison.Ordinal);
        Assert.Contains("gd-lik__face", page, StringComparison.Ordinal);
        Assert.Contains("aria-hidden=\"true\"", page, StringComparison.Ordinal);
        Assert.Contains("voor", page, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GratisDna.Landing.OwnLanguage", page, StringComparison.Ordinal);
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

        return Directory.GetCurrentDirectory();
    }

    private sealed class FakeAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class FakeExternalAuth : IExternalAuthCredentialSource
    {
        public Task<bool> IsEntraConfiguredAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<bool> IsGoogleConfiguredAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<ExternalOAuthCredentials?> GetEntraAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<ExternalOAuthCredentials?>(new ExternalOAuthCredentials("id", "secret", null));

        public Task<ExternalOAuthCredentials?> GetGoogleAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<ExternalOAuthCredentials?>(new ExternalOAuthCredentials("id", "secret", null));
    }
}
