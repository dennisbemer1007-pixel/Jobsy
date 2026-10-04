using System.Net;
using System.Security.Claims;
using System.Text;
using Bunit;
using Jobsy.Core.Features;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Components.Candidate.Passport;
using Jobsy.Web.Components.Candidate.ProfileSections;
using Jobsy.Web.Components.Werkgever.Sections;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class Wave1HonestClearBunitTests : BunitContext
{
    private readonly MutableFlags _flags = new();

    public Wave1HonestClearBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IGeocodingClient>(new NullGeo());
        Services.AddSingleton(new HttpClient(new ApiStub()) { BaseAddress = new Uri("http://localhost/") });
        Services.AddSingleton(sp => new JobsyApiClient(sp.GetRequiredService<HttpClient>()));
        Services.AddSingleton<CandidateProfileEditor>();
        Services.AddSingleton<IFeatureFlags>(_flags);
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    }

    [Theory]
    [InlineData("nl")]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("ro")]
    [InlineData("ar")]
    public void Privacy_promise_shows_on_overview_consent_and_data_tab(string lang)
    {
        var culture = Services.GetRequiredService<CultureState>();
        culture.InitializeFromLanguage(lang);
        var promise = UiStrings.Get("Passport.PrivacyPromise", lang);

        var overview = Render<PassportOverview>(p => p.Add(x => x.CompletedCount, 0));
        Assert.Contains("data-testid=\"passport-privacy-promise\"", overview.Markup, StringComparison.Ordinal);
        Assert.Contains(promise, overview.Markup, StringComparison.Ordinal);

        var consent = Render<TestConsentStep>(p => p.Add(c => c.Draft, new OnboardingProfileDraft()));
        Assert.Contains("data-testid=\"passport-privacy-promise\"", consent.Markup, StringComparison.Ordinal);
        Assert.Contains(promise, consent.Markup, StringComparison.Ordinal);

        var data = Render<PassportDataTab>(p => p.Add(x => x.Active, true));
        data.WaitForAssertion(() =>
            Assert.Contains(UiStrings.Get("Profile.Consent.Title", lang), data.Markup, StringComparison.Ordinal));
        data.FindAll("button").First(b => b.TextContent.Contains(UiStrings.Get("Profile.Consent.Title", lang), StringComparison.Ordinal)).Click();
        data.WaitForAssertion(() => Assert.Contains(promise, data.Markup, StringComparison.Ordinal));
        Assert.Contains("data-testid=\"passport-privacy-promise\"", data.Markup, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("nl")]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("ro")]
    [InlineData("ar")]
    public void Download_row_sits_between_privacy_and_delete(string lang)
    {
        var culture = Services.GetRequiredService<CultureState>();
        culture.InitializeFromLanguage(lang);

        var data = Render<PassportDataTab>(p => p.Add(x => x.Active, true));
        data.WaitForAssertion(() =>
            Assert.Contains("data-testid=\"passport-download-data\"", data.Markup, StringComparison.Ordinal));

        var link = data.Find("[data-testid=passport-download-data]");
        Assert.Equal("/privacy/data", link.GetAttribute("href"));
        Assert.Contains(UiStrings.Get("Passport.Data.DownloadMine", lang), link.TextContent, StringComparison.Ordinal);
        Assert.Contains(UiStrings.Get("Passport.Data.DownloadMineSub", lang), link.TextContent, StringComparison.Ordinal);

        var markup = data.Markup;
        var privacy = markup.IndexOf(UiStrings.Get("Profile.Consent.Title", lang), StringComparison.Ordinal);
        var download = markup.IndexOf("passport-download-data", StringComparison.Ordinal);
        var delete = markup.IndexOf(UiStrings.Get("Profile.UnsubscribeTitle", lang), StringComparison.Ordinal);
        Assert.True(privacy >= 0 && privacy < download && download < delete);
    }

    [Fact]
    public void Talent_card_hides_riasec_labels_when_the_switch_is_off()
    {
        var card = SampleCard();
        var hidden = Render<TalentPoolResultCard>(p => p
            .Add(x => x.Card, card)
            .Add(x => x.ShowRiasec, false));
        Assert.Contains("Samenwerken", hidden.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Social", hidden.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("data-testid=\"talent-holland\"", hidden.Markup, StringComparison.Ordinal);

        var shown = Render<TalentPoolResultCard>(p => p
            .Add(x => x.Card, card)
            .Add(x => x.ShowRiasec, true));
        Assert.Contains("Social", shown.Markup, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"talent-holland\"", shown.Markup, StringComparison.Ordinal);
        Assert.Contains("Holland-code: S", shown.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Scores:", shown.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("%", shown.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("diepte-analyse", shown.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Talent_pool_tag_input_follows_the_switch(bool showRiasec)
    {
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [Jobsy.Core.Rules.TalentPoolRiasecVisibility.ConfigKey] = showRiasec ? "true" : "false"
            })
            .Build());

        var cut = Render<TalentPoolSection>();
        cut.WaitForAssertion(() => Assert.Contains("Zoeken", cut.Markup, StringComparison.Ordinal));
        if (showRiasec)
        {
            Assert.Contains("RIASEC", cut.Markup, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("RIASEC", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Samenwerken, Social", cut.Markup, StringComparison.Ordinal);
        }
    }

    private static AnonymousTalentCard SampleCard() => new()
    {
        CandidateUserId = Guid.Parse("c3000000-0000-0000-0000-000000000001"),
        RegionLabel = "Westland",
        MatchTags = ["Samenwerken"],
        RiasecTags = ["Social"],
        HollandCode = "S"
    };

    private sealed class MutableFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(true, true));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature is PlatformFeature.Employers or PlatformFeature.CandidatePassport);

        public void Invalidate()
        {
        }
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.Name, "bm"),
                        new Claim(ClaimTypes.Role, "BranchManager")
                    ],
                    "t"))));
    }

    private sealed class NullGeo : IGeocodingClient
    {
        public Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AddressSuggestion>>([]);

        public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    private sealed class ApiStub : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("private-preferences", StringComparison.Ordinal))
            {
                return Task.FromResult(Json("""{"dislikes":[],"customDislikes":[]}"""));
            }

            if (path.Contains("masterdata", StringComparison.Ordinal))
            {
                return Task.FromResult(Json("[]"));
            }

            if (path.Contains("profile", StringComparison.Ordinal))
            {
                return Task.FromResult(Json(
                    """
                    {"id":"11111111-1111-1111-1111-111111111111","email":"sam@example.com","fullName":"Sam Tester","firstName":"Sam","lastName":"Tester","role":"Candidate","preferences":{"roles":[]},"emailVerified":true,"phoneVerified":false}
                    """));
            }

            if (path.Contains("talentpool", StringComparison.Ordinal))
            {
                return Task.FromResult(Json("[]"));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage Json(string body)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
    }
}
