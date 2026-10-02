using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Bunit;
using Bunit.TestDoubles;
using Jobsy.Core.Features;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Web.Components.Pages.Candidate;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

/// <summary>
/// Kandidaat polish 05: Bewaard compact list markup (rows + desktop cards).
/// </summary>
public class KbSavedListBunitTests : BunitContext
{
    private readonly StubLikesHandler _handler = new();

    public KbSavedListBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<AuthenticationStateProvider>(new FakeCandidateAuth());
        Services.AddAuthorizationCore();
        this.AddAuthorization().SetAuthorized("kandidaat").SetRoles("Candidate");
        Services.AddCascadingAuthenticationState();
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IFeatureFlags>(new FixedFlags());
        Services.AddSingleton(new JobsyApiClient(new HttpClient(_handler)
        {
            BaseAddress = new Uri("http://localhost/")
        }));
        Services.AddLogging();
        this.AddBunitPersistentComponentState();
    }

    [Fact]
    public void Saved_row_renders_thumb_title_company_fit_status_and_heart()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        _handler.Items =
        [
            Item(id, "Schoonmaker kantoren", "CleanWest", "Rijswijk",
                fit: 61, band: "Good", state: nameof(KbSavedJobStateKind.Open))
        ];

        var cut = Render<Liked>();
        cut.WaitForAssertion(() => Assert.Contains("kb-saved-row", cut.Markup, StringComparison.Ordinal));

        var row = cut.Find("article.kb-saved-row");
        Assert.Contains("kb-saved-row__photo", row.InnerHtml, StringComparison.Ordinal);
        Assert.Contains("Schoonmaker kantoren", row.TextContent, StringComparison.Ordinal);
        Assert.Contains("CleanWest · Rijswijk", row.TextContent, StringComparison.Ordinal);
        Assert.Contains("61%", row.TextContent, StringComparison.Ordinal);
        Assert.Contains("Open", row.TextContent, StringComparison.Ordinal);

        var heart = row.QuerySelector("button.kb-saved-row__heart");
        Assert.NotNull(heart);
        Assert.Equal("Uit Bewaard halen", heart!.GetAttribute("aria-label"));
        Assert.Null(heart.Closest("a"));
    }

    [Fact]
    public void Title_link_targets_vacancy_and_closed_targets_similar()
    {
        var openId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var closedId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var category = Guid.Parse("33333333-3333-3333-3333-333333333333");
        _handler.Items =
        [
            Item(openId, "Open baan", "Acme", "Delft",
                fit: 80, band: "Strong", state: nameof(KbSavedJobStateKind.Open),
                createdAt: DateTime.UtcNow.AddMinutes(-1)),
            Item(closedId, "Gesloten baan", "Beta", "Den Haag",
                fit: 50, band: "Some", state: nameof(KbSavedJobStateKind.Closed), categoryId: category,
                createdAt: DateTime.UtcNow.AddMinutes(-10))
        ];

        var cut = Render<Liked>();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("article.kb-saved-row").Count));

        var openRow = cut.FindAll("article.kb-saved-row")
            .First(r => r.TextContent.Contains("Open baan", StringComparison.Ordinal));
        var closedRow = cut.FindAll("article.kb-saved-row")
            .First(r => r.TextContent.Contains("Gesloten baan", StringComparison.Ordinal));
        Assert.Equal($"/vacancies/{openId}", openRow.QuerySelector("a.kb-saved-row__link")!.GetAttribute("href"));
        Assert.Equal(
            $"{KbRoutes.Map}?categorie={category:D}",
            closedRow.QuerySelector("a.kb-saved-row__link")!.GetAttribute("href"));
        Assert.Contains("kb-saved-row--closed", closedRow.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Gate_closed_shows_paspoort_link_instead_of_percent()
    {
        _handler.Items =
        [
            Item(Guid.NewGuid(), "Zonder fit", "Co", "Plaats",
                fit: null, band: null, state: nameof(KbSavedJobStateKind.Open), gateClosed: true)
        ];

        var cut = Render<Liked>();
        cut.WaitForAssertion(() => Assert.Contains("kb-saved-row", cut.Markup, StringComparison.Ordinal));
        var row = cut.Find("article.kb-saved-row");
        Assert.Contains("Maak je paspoort af", row.TextContent, StringComparison.Ordinal);
        Assert.Contains(KbRoutes.PaspoortTests, row.InnerHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("%", row.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Sort_select_has_three_options_and_defaults_to_newest()
    {
        _handler.Items =
        [
            Item(Guid.NewGuid(), "A", "B", "C",
                fit: 70, band: "Good", state: nameof(KbSavedJobStateKind.Open))
        ];

        var cut = Render<Liked>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("select")));

        var select = (AngleSharp.Html.Dom.IHtmlSelectElement)cut.Find(".kb-saved__sort select");
        var options = select.Options.ToList();
        Assert.Equal(3, options.Count);
        Assert.Equal("newest", options[0].Value);
        Assert.Equal("fit", options[1].Value);
        Assert.Equal("closing", options[2].Value);
        Assert.Equal("newest", select.Value);
    }

    [Fact]
    public void Count_label_and_desktop_card_media_link_are_present()
    {
        _handler.Items =
        [
            Item(Guid.NewGuid(), "Titel", "Firma", "Stad",
                fit: 74, band: "Good", state: nameof(KbSavedJobStateKind.Open))
        ];

        var cut = Render<Liked>();
        cut.WaitForAssertion(() => Assert.Contains("1 banen", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("kb-saved-card__media-link", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("kb-saved-card", cut.Markup, StringComparison.Ordinal);
    }

    private static CandidateEngagementItem Item(
        Guid vacancyId,
        string title,
        string company,
        string place,
        int? fit,
        string? band,
        string state,
        bool gateClosed = false,
        Guid? categoryId = null,
        bool applied = false,
        DateTime? createdAt = null)
        => new()
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            VacancyTitle = title,
            CompanyName = company,
            LocationLabel = place,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            FitPercent = fit,
            FitBand = band,
            FitGateClosed = gateClosed,
            SavedStateKind = state,
            DaysUntilEnd = state == nameof(KbSavedJobStateKind.ClosingSoon) ? 5 : 20,
            CategoryId = categoryId,
            HasApplied = applied,
            ImageUrl = "/img/demo.webp"
        };

    private sealed class StubLikesHandler : HttpMessageHandler
    {
        public List<CandidateEngagementItem> Items { get; set; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("api/me/likes", StringComparison.OrdinalIgnoreCase))
            {
                var json = JsonSerializer.Serialize(Items);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }

            if (path.Contains("/like", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"liked":false}""", Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class FakeCandidateAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, "kandidaat"),
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")),
                    new Claim(ClaimTypes.Role, "Candidate")
                ],
                "test");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
    }

    private sealed class FixedFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(EmployersEnabled: true, CandidatePassportEnabled: true));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature is PlatformFeature.Employers or PlatformFeature.CandidatePassport);

        public void Invalidate()
        {
        }
    }
}
