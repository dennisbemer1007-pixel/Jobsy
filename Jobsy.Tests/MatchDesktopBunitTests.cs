using System.Net;
using System.Security.Claims;
using System.Text;
using Bunit;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Web.Components;
using Jobsy.Web.Components.Match;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class MatchDesktopBunitTests : BunitContext
{
    private readonly CountingLikeHandler _likeHandler = new();

    public MatchDesktopBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth("Candidate"));
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        var http = new HttpClient(_likeHandler) { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(new JobsyApiClient(http));
        JSInterop.SetupVoid("jobsyDialog.trap", _ => true);
        JSInterop.SetupVoid("jobsyDialog.release", _ => true);
    }

    [Theory]
    [InlineData(true, true, true, 1, true)]
    [InlineData(false, true, true, 1, false)]
    [InlineData(true, false, true, 1, false)]
    [InlineData(true, true, false, 1, false)]
    [InlineData(true, true, true, 0, false)]
    public void Visibility_rule(bool wide, bool candidate, bool gate, int count, bool expected)
        => Assert.Equal(expected, TopMatchTileVisibility.ShouldShow(wide, candidate, gate, count));

    [Fact]
    public void TopMatchTile_renders_eyebrow_title_badge_and_aria()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<TopMatchTile>(0);
            builder.AddAttribute(1, "Model", new SwipeViewModel
            {
                VacancyId = Guid.NewGuid(),
                JobTitle = "Barista Voorhof",
                CompanyName = "Zorg Delft Oost",
                MatchPercentage = 83,
                ShowMatchPercentage = true
            });
            builder.CloseComponent();
        });

        Assert.Contains("Jouw top-match", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Barista Voorhof", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Zorg Delft Oost", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("83%", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("aria-haspopup=\"dialog\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("highlight-carousel__match-badge", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void HighlightCarousel_renders_LeadingItem_first_and_empty_without_both()
    {
        var withLead = Render(builder =>
        {
            builder.OpenComponent<HighlightVacancyCarousel>(0);
            builder.AddAttribute(1, "Vacancies", Array.Empty<VacancyListItem>());
            builder.AddAttribute(2, "LeadingItem", (RenderFragment)(b =>
            {
                b.OpenElement(0, "button");
                b.AddAttribute(1, "role", "listitem");
                b.AddAttribute(2, "class", "lead-probe");
                b.AddContent(3, "LEAD");
                b.CloseElement();
            }));
            builder.CloseComponent();
        });
        Assert.Contains("lead-probe", withLead.Markup, StringComparison.Ordinal);
        Assert.Contains("LEAD", withLead.Markup, StringComparison.Ordinal);

        var empty = Render(builder =>
        {
            builder.OpenComponent<HighlightVacancyCarousel>(0);
            builder.AddAttribute(1, "Vacancies", Array.Empty<VacancyListItem>());
            builder.CloseComponent();
        });
        Assert.True(string.IsNullOrWhiteSpace(empty.Markup) || !empty.Markup.Contains("highlight-carousel", StringComparison.Ordinal));
    }

    [Fact]
    public void SwipeCard_mobile_keeps_dutch_labels()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<SwipeCard>(0);
            builder.AddAttribute(1, "Model", FullCard());
            builder.AddAttribute(2, "Variant", SwipeCardVariant.Mobile);
            builder.CloseComponent();
        });

        Assert.Contains("Laten schieten", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Snel kennismaken", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Waarom jij past", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Meer info", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationStatus", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void SwipeCard_dialog_shows_four_dna_rows_and_not_done_for_missing()
    {
        var model = FullCard();
        model.FitDimensions = new CandidateFitDimensionsModel
        {
            Culture = 80,
            Values = 70,
            Competencies = null,
            Interests = null
        };

        var cut = Render(builder =>
        {
            builder.OpenComponent<SwipeCard>(0);
            builder.AddAttribute(1, "Model", model);
            builder.AddAttribute(2, "Variant", SwipeCardVariant.Dialog);
            builder.CloseComponent();
        });

        Assert.Equal(4, System.Text.RegularExpressions.Regex.Matches(cut.Markup, "data-dna=").Count);
        Assert.Contains("Nog niet gedaan", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void SwipeCard_mobile_shows_at_most_three_dna_rows_without_not_done_links()
    {
        var model = FullCard();
        var cut = Render(builder =>
        {
            builder.OpenComponent<SwipeCard>(0);
            builder.AddAttribute(1, "Model", model);
            builder.AddAttribute(2, "Variant", SwipeCardVariant.Mobile);
            builder.CloseComponent();
        });

        Assert.Equal(3, System.Text.RegularExpressions.Regex.Matches(cut.Markup, "data-dna=").Count);
        Assert.DoesNotContain("Nog niet gedaan", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("kb-dna__todo", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void SwipeCard_without_dna_or_why_omits_block()
    {
        var model = FullCard();
        model.FitDimensions = null;
        model.FitWhyKinds = [];
        model.WhyYouFit = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<SwipeCard>(0);
            builder.AddAttribute(1, "Model", model);
            builder.CloseComponent();
        });

        Assert.DoesNotContain("data-dna=", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("swipe-why", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void SwipeCard_hidden_mode_shows_bureau_and_rank_lower_chip()
    {
        var model = FullCard();
        model.IsHiddenMode = true;
        model.CompanyName = "FlexBureau Westland";
        model.RankLowerReason = "Kb.Dislike.night-shifts";

        var cut = Render(builder =>
        {
            builder.OpenComponent<SwipeCard>(0);
            builder.AddAttribute(1, "Model", model);
            builder.CloseComponent();
        });

        Assert.Contains("via uitzendbureau FlexBureau Westland", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Staat lager", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("nachtdienst", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void MatchUnlockPanel_fit_gate_shows_no_percent()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<MatchUnlockPanel>(0);
            builder.AddAttribute(1, "Gate", new MatchProfileGateViewModel
            {
                IsAuthenticated = true,
                IsProfileComplete = true,
                CultureCompleted = false,
                ValuesCompleted = false
            });
            builder.AddAttribute(2, "FitGateOnly", true);
            builder.CloseComponent();
        });

        Assert.Contains("Maak je paspoort af", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("%", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void MatchDeckDialog_progress_upnext_escape_and_arrows()
    {
        var deck = new MatchDeck();
        deck.ReplaceItems(Enumerable.Range(1, 12).Select(i => new SwipeViewModel
        {
            VacancyId = Guid.NewGuid(),
            JobTitle = $"Job {i}",
            CompanyName = $"Co {i}",
            MatchPercentage = 90 - i,
            ShowMatchPercentage = true,
            TravelTimeMinutes = 8 + i
        }));

        var closed = false;
        var cut = Render(builder =>
        {
            builder.OpenComponent<MatchDeckDialog>(0);
            builder.AddAttribute(1, "Deck", deck);
            builder.AddAttribute(2, "IsOpen", true);
            builder.AddAttribute(3, "OnClose", EventCallback.Factory.Create(
                Receiver.Instance,
                () => closed = true));
            builder.CloseComponent();
        });

        Assert.Contains("1 van 12", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("je kunt niets fout doen", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Hierna", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Toetsen:", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(3, System.Text.RegularExpressions.Regex.Matches(cut.Markup, "lobsy-dialog--match__upnext-row").Count);

        cut.Find("button.share-modal__close").Click();
        Assert.True(closed);

        closed = false;
        _likeHandler.LikePosts = 0;
        deck.ReplaceItems(deck.Items.ToList(), index: 0);
        cut = Render(builder =>
        {
            builder.OpenComponent<MatchDeckDialog>(0);
            builder.AddAttribute(1, "Deck", deck);
            builder.AddAttribute(2, "IsOpen", true);
            builder.AddAttribute(3, "OnClose", EventCallback.Factory.Create(
                Receiver.Instance,
                () => closed = true));
            builder.CloseComponent();
        });

        cut.Find("[role=dialog]").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        cut.WaitForAssertion(() => Assert.True(closed), TimeSpan.FromSeconds(1));

        closed = false;
        _likeHandler.LikePosts = 0;
        deck.ReplaceItems(deck.Items.ToList(), index: 0);
        cut = Render(builder =>
        {
            builder.OpenComponent<MatchDeckDialog>(0);
            builder.AddAttribute(1, "Deck", deck);
            builder.AddAttribute(2, "IsOpen", true);
            builder.CloseComponent();
        });

        cut.Find("button.swipe-actions__btn--interest").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, deck.Position), TimeSpan.FromSeconds(2));
        cut.WaitForAssertion(() => Assert.Equal(1, _likeHandler.LikePosts), TimeSpan.FromSeconds(2));
        Assert.Contains("2 van 12", cut.Markup, StringComparison.Ordinal);

        var skippedId = deck.Current!.VacancyId;
        cut.Find("button.swipe-actions__btn--reject").Click();
        cut.WaitForAssertion(() => Assert.Equal(skippedId, deck.Items[^1].VacancyId), TimeSpan.FromSeconds(2));
        Assert.Equal(1, _likeHandler.LikePosts);
        Assert.Contains("We laten hem later nog eens zien", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(2, deck.Position);

        cut.Find("[role=dialog]").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        cut.WaitForAssertion(() => Assert.Equal(3, deck.Position), TimeSpan.FromSeconds(2));
        cut.WaitForAssertion(() => Assert.Equal(2, _likeHandler.LikePosts), TimeSpan.FromSeconds(2));

        for (var i = deck.Index; i < deck.Count; i++)
        {
            deck.Advance();
        }

        var done = Render(builder =>
        {
            builder.OpenComponent<MatchDeckDialog>(0);
            builder.AddAttribute(1, "Deck", deck);
            builder.AddAttribute(2, "IsOpen", true);
            builder.CloseComponent();
        });
        Assert.Contains("Je hebt ze allemaal gezien", done.Markup, StringComparison.Ordinal);
        Assert.Contains("Bekijk bewaarde banen", done.Markup, StringComparison.Ordinal);
        Assert.Contains("Terug naar de kaart", done.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void MatchDeckDialog_opens_traps_focus_and_close_releases()
    {
        var deck = new MatchDeck();
        deck.ReplaceItems([FullCard()]);
        var closed = false;
        var cut = Render(builder =>
        {
            builder.OpenComponent<MatchDeckDialog>(0);
            builder.AddAttribute(1, "Deck", deck);
            builder.AddAttribute(2, "IsOpen", true);
            builder.AddAttribute(3, "OnClose", EventCallback.Factory.Create(Receiver.Instance, () => closed = true));
            builder.CloseComponent();
        });

        cut.WaitForAssertion(
            () => Assert.Contains(JSInterop.Invocations, i => i.Identifier == "jobsyDialog.trap"),
            TimeSpan.FromSeconds(1));

        cut.Find("button.share-modal__close").Click();
        Assert.True(closed);
        cut.WaitForAssertion(
            () => Assert.Contains(JSInterop.Invocations, i => i.Identifier == "jobsyDialog.release"),
            TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void TopMatchLeadingFragment_attributes_before_reference_capture_renders()
    {
        TopMatchTile? captured = null;
        var model = new SwipeViewModel
        {
            VacancyId = Guid.Parse("a1000000-0000-4000-8000-000000000036"),
            JobTitle = "Helpende Zorg & Welzijn",
            CompanyName = "Groenhof Wateringen",
            MatchPercentage = 92,
            ShowMatchPercentage = true,
            TravelTimeMinutes = 14,
            Tags = ["informeel & handen uit de mouwen", "Zorg"]
        };

        RenderFragment leading = builder =>
        {
            builder.OpenComponent<TopMatchTile>(0);
            builder.AddAttribute(1, "Model", model);
            builder.AddAttribute(2, "TravelText", "14 min fietsen");
            builder.AddAttribute(3, "IsOpen", false);
            builder.AddAttribute(4, "OnOpen", EventCallback.Factory.Create(Receiver.Instance, () => Task.CompletedTask));
            builder.AddComponentReferenceCapture(5, inst => captured = (TopMatchTile)inst);
            builder.CloseComponent();
        };

        var cut = Render(builder =>
        {
            builder.OpenComponent<HighlightVacancyCarousel>(0);
            builder.AddAttribute(1, "Vacancies", Array.Empty<VacancyListItem>());
            builder.AddAttribute(2, "LeadingItem", leading);
            builder.CloseComponent();
        });

        Assert.Contains("Jouw top-match", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Helpende Zorg", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("92%", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("14 min fietsen", cut.Markup, StringComparison.Ordinal);
        Assert.NotNull(captured);
    }

    [Fact]
    public void TopMatchLeadingFragment_reference_capture_before_attributes_throws()
    {
        RenderFragment broken = builder =>
        {
            builder.OpenComponent<TopMatchTile>(0);
            builder.AddComponentReferenceCapture(1, _ => { });
            builder.AddAttribute(2, "Model", new SwipeViewModel
            {
                VacancyId = Guid.NewGuid(),
                JobTitle = "X",
                CompanyName = "Y",
                MatchPercentage = 80,
                ShowMatchPercentage = true
            });
            builder.CloseComponent();
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            Render(builder =>
            {
                builder.OpenComponent<HighlightVacancyCarousel>(0);
                builder.AddAttribute(1, "Vacancies", Array.Empty<VacancyListItem>());
                builder.AddAttribute(2, "LeadingItem", broken);
                builder.CloseComponent();
            });
        });
        Assert.Contains("Attributes may only be added", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Match_format_keys_accept_argument_counts_in_five_languages()
    {
        var cases = new (string Key, object[] Args)[]
        {
            ("Match.TopMatchAria", [83, "Barista", "Café Delft"]),
            ("Match.DialogProgress", [1, 12])
        };

        foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            foreach (var (key, args) in cases)
            {
                var template = UiStrings.Get(key, lang);
                Assert.False(string.IsNullOrWhiteSpace(template), $"{lang}:{key}");
                var formatted = string.Format(System.Globalization.CultureInfo.InvariantCulture, template, args);
                Assert.False(string.IsNullOrWhiteSpace(formatted), $"{lang}:{key} format");
                Assert.DoesNotContain("{0}", formatted, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Match_keys_exist_in_five_languages()
    {
        string[] keys =
        [
            "Match.Reject", "Match.Interest", "Match.WhyYouFit", "Match.MoreInfo",
            "Match.KeyFacts", "Match.Actions", "Match.Percentage", "Match.CompanyFallback",
            "Match.TopMatch", "Match.TopMatchAria", "Match.DialogTitle", "Match.DialogProgress",
            "Match.UpNext", "Match.DialogFootnote", "Match.DeckDone", "Match.ToastLiked",
            "Match.ToastSkipped", "Kb.Match.Skipped", "Kb.Match.NoWrongChoice",
            "Kb.Match.ViewSaved", "Kb.Match.BackToMap", "Kb.Match.SwipeHint"
        ];

        foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            foreach (var key in keys)
            {
                var text = UiStrings.Get(key, lang);
                Assert.False(string.IsNullOrWhiteSpace(text), $"{lang}:{key}");
                Assert.DoesNotContain("Match.", text, StringComparison.Ordinal);
                Assert.DoesNotContain("Kb.Match.", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void MatchDnaRows_helper_counts()
    {
        var dims = new CandidateFitDimensionsModel
        {
            Culture = 80,
            Values = 70,
            Competencies = null,
            Interests = 60
        };
        Assert.Equal(4, MatchDnaRows.Build(dims, ["culture"], includeMissing: true).Count);
        Assert.Equal(3, MatchDnaRows.Build(dims, ["culture"], includeMissing: false, maxWithData: 3).Count);
        Assert.Empty(MatchDnaRows.Build(null, null, includeMissing: false));
    }

    private static SwipeViewModel FullCard() => new()
    {
        VacancyId = Guid.NewGuid(),
        JobTitle = "Zorgmedewerker thuiszorg",
        CompanyName = "Groenhof Zorg",
        MatchPercentage = 82,
        ShowMatchPercentage = true,
        FitBand = "Strong",
        TravelTimeMinutes = 8,
        TransportMode = "Fiets",
        HoursMin = 24,
        HoursMax = 32,
        HourlyWage = 15.20m,
        WageVisible = true,
        Location = "Wateringen",
        FitWhyKinds = ["culture", "values", "competency", "interest"],
        FitDimensions = new CandidateFitDimensionsModel
        {
            Culture = 82,
            Values = 78,
            Competencies = 74,
            Interests = 80
        },
        Tags = ["Leerwerkplek"]
    };

    private sealed class CountingLikeHandler : HttpMessageHandler
    {
        public int LikePosts { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post
                && request.RequestUri is not null
                && request.RequestUri.AbsolutePath.Contains("/like", StringComparison.Ordinal))
            {
                LikePosts++;
            }

            var json = """{"liked":true}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class Receiver : IHandleEvent
    {
        public static readonly Receiver Instance = new();
        public Task HandleEventAsync(EventCallbackWorkItem item, object? arg) => item.InvokeAsync(arg);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _user;
        public FakeAuth(string role)
        {
            var id = new ClaimsIdentity("test");
            id.AddClaim(new Claim(ClaimTypes.Role, role));
            _user = new ClaimsPrincipal(id);
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(_user));
    }
}
