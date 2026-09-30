using Bunit;
using Jobsy.Core.Rules;
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
            builder.AddAttribute(6, "ShowSticky", true);
            builder.CloseComponent();
        });

        var markup = cut.Markup;
        Assert.Contains("Eerste indruk · op basis van 20 vragen", markup, StringComparison.Ordinal);
        Assert.Contains("Zo werk jij", markup, StringComparison.Ordinal);
        Assert.Contains("Dit vind je leuk", markup, StringComparison.Ordinal);
        Assert.Contains("Hier voel je je thuis", markup, StringComparison.Ordinal);
        Assert.Contains("Dit vind je belangrijk", markup, StringComparison.Ordinal);
        Assert.Contains("Meld je aan om te zien welke banen bij je passen", markup, StringComparison.Ordinal);
        Assert.Contains("WhatsApp", markup, StringComparison.Ordinal);
        Assert.Contains("Instagram", markup, StringComparison.Ordinal);
        Assert.Contains("Wis mijn antwoorden van dit apparaat", markup, StringComparison.Ordinal);
        Assert.Contains("Bewaar je DNA – maak gratis account", markup, StringComparison.Ordinal);
        Assert.Contains("/account-maken?van=ontdek", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("/register", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("%", markup, StringComparison.Ordinal);
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
}
