using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using Jobsy.Core.Features;
using Jobsy.Web.Components.Candidate.Journey;
using Jobsy.Web.Components.Pages.Candidate;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

/// <summary>
/// Carrière 05: <c>/candidate/hoe-werkt-lobsy</c> as five stones in the journey style, plus the
/// H1–H3 fixes (no "_message" literal, stay on error, no inline style on the shared panel).
/// </summary>
public class CandidateHowLobsy05BunitTests : BunitContext
{
    private readonly StubHandler _handler = new();
    private bool _employers = true;
    private bool _passport = true;

    public CandidateHowLobsy05BunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeCandidateAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IFeatureFlags>(new LazyFlags(() => _employers, () => _passport));
        Services.AddSingleton(new JobsyApiClient(new HttpClient(_handler)
        {
            BaseAddress = new Uri("http://localhost/")
        }));
        Services.AddLogging();
    }

    // ---------- shell + order (D15) ----------

    [Fact]
    public void Page_renders_five_stones_in_Dennis_order()
    {
        var cut = Render<HowLobsyWorks>();

        Assert.Contains("journey-page career-page", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(cut.Markup, "<h1"));
        Assert.Contains("Vijf stenen, in je eigen tempo", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Je hoeft niet alles tegelijk.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Zo werkt Lobsy\"", cut.Markup, StringComparison.Ordinal);

        Assert.Equal(
            [
                "/candidate/ontdekkingsreis",
                "/candidate/paspoort",
                "/carriere",
                "/banenkaart",
                "/candidate/applications"
            ],
            StoneHrefs(cut));
    }

    [Fact]
    public void Page_never_renders_the_message_literal_or_the_old_guide_panel()
    {
        var cut = Render<HowLobsyWorks>();

        Assert.DoesNotContain("_message", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("how-lobsy-steps", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("panel-page", cut.Markup, StringComparison.Ordinal);

        var page = File.ReadAllText(Path.Combine(
            RepoRoot(), "Jobsy.Web", "Components", "Pages", "Candidate", "HowLobsyWorks.razor"));
        Assert.DoesNotContain("Message=\"_message\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_guide_panel_has_no_inline_style()
    {
        var panel = File.ReadAllText(Path.Combine(
            RepoRoot(), "Jobsy.Web", "Components", "Shared", "HowLobsyGuidePanel.razor"));

        Assert.DoesNotContain("style=", panel, StringComparison.Ordinal);
        Assert.Contains("how-lobsy-cta", panel, StringComparison.Ordinal);
        Assert.Contains(
            ".how-lobsy-cta",
            File.ReadAllText(Path.Combine(
                RepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "carriere.css")),
            StringComparison.Ordinal);
    }

    // ---------- flag combinations ----------

    [Fact]
    public void Werkgevers_off_hides_the_job_map_and_applications_stones()
    {
        _employers = false;
        var cut = Render<HowLobsyWorks>();

        Assert.Contains("Drie stenen, in je eigen tempo", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(
            ["/candidate/ontdekkingsreis", "/candidate/paspoort", "/carriere"],
            StoneHrefs(cut));
        Assert.DoesNotContain("Werkgevers zien je naam", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Passport_off_swaps_stone_two_for_my_profile()
    {
        _passport = false;
        var cut = Render<HowLobsyWorks>();

        Assert.Equal(
            [
                "/candidate/start",
                "/candidate/profile",
                "/carriere",
                "/banenkaart",
                "/candidate/applications"
            ],
            StoneHrefs(cut));
        Assert.Contains("Mijn profiel", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Mijn Paspoort", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Je profiel invullen", cut.Markup, StringComparison.Ordinal);
    }

    // ---------- done / now ----------

    [Fact]
    public void Done_and_now_come_from_the_summary()
    {
        _handler.Summary = new
        {
            discoveryDone = true,
            passportDone = true,
            careerDone = false,
            jobMapDone = false,
            applicationsDone = false
        };

        var cut = Render<HowLobsyWorks>();

        var rows = cut.FindAll("li.how-stone").ToList();
        Assert.Equal(5, rows.Count);
        Assert.Contains("how-stone--done", rows[0].ClassName, StringComparison.Ordinal);
        Assert.Contains("how-stone--done", rows[1].ClassName, StringComparison.Ordinal);
        Assert.Contains("how-stone--now", rows[2].ClassName, StringComparison.Ordinal);
        Assert.Equal("step", rows[2].GetAttribute("aria-current"));
        Assert.Contains("how-stone--todo", rows[3].ClassName, StringComparison.Ordinal);

        Assert.Contains("Kijk terug", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Ga verder", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Verder met Carrière", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Nothing_done_puts_now_on_the_first_stone_and_uses_the_starting_bubble()
    {
        var cut = Render<HowLobsyWorks>();

        var rows = cut.FindAll("li.how-stone").ToList();
        Assert.Contains("how-stone--now", rows[0].ClassName, StringComparison.Ordinal);
        Assert.Contains("Begin bij de eerste steen.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Verder met De ontdekkingsreis", cut.Markup, StringComparison.Ordinal);
    }

    // ---------- footer actions ----------

    [Fact]
    public void Ik_snap_het_calls_the_api_once_and_stays_on_the_page()
    {
        var cut = Render<HowLobsyWorks>();
        var navigation = Services.GetRequiredService<NavigationManager>();
        var before = navigation.Uri;

        cut.Find(".career-btn--text").Click();

        Assert.Equal(1, _handler.CompleteCalls);
        Assert.Equal(before, navigation.Uri);
        Assert.Contains("Top. Je vindt deze uitleg altijd terug in het menu.", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[role=\"alert\"]"));
    }

    [Fact]
    public void A_failing_save_keeps_the_candidate_on_the_page_with_an_alert()
    {
        _handler.CompleteStatus = HttpStatusCode.InternalServerError;
        var cut = Render<HowLobsyWorks>();
        var navigation = Services.GetRequiredService<NavigationManager>();
        var before = navigation.Uri;

        cut.Find(".career-btn--text").Click();

        Assert.Equal(before, navigation.Uri);
        var alert = cut.Find("[role=\"alert\"]");
        Assert.Equal("Dat lukte niet. Probeer het straks opnieuw.", alert.TextContent.Trim());
        Assert.DoesNotContain("Top. Je vindt deze uitleg", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void The_primary_marks_the_guide_as_seen_and_walks_to_the_now_stone()
    {
        _handler.Summary = new
        {
            discoveryDone = true,
            passportDone = false,
            careerDone = false,
            jobMapDone = false,
            applicationsDone = false
        };

        var cut = Render<HowLobsyWorks>();
        cut.Find(".career-btn--primary").Click();

        Assert.Equal(1, _handler.CompleteCalls);
        Assert.Equal(
            "http://localhost/candidate/paspoort",
            Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void The_primary_still_navigates_when_the_save_fails()
    {
        _handler.CompleteStatus = HttpStatusCode.InternalServerError;
        var cut = Render<HowLobsyWorks>();

        cut.Find(".career-btn--primary").Click();

        Assert.Equal(
            "http://localhost/candidate/ontdekkingsreis",
            Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Empty(cut.FindAll("[role=\"alert\"]"));
    }

    // ---------- languages / rtl ----------

    [Fact]
    public void The_language_line_tags_every_name_with_its_own_lang()
    {
        var cut = Render<HowLobsyWorks>();
        var langs = cut.Find(".how-safe__langs");

        Assert.Contains("Lobsy in jouw taal:", langs.TextContent, StringComparison.Ordinal);
        foreach (var (code, name) in new[]
                 {
                     ("nl", "Nederlands"),
                     ("en", "English"),
                     ("pl", "Polski"),
                     ("ro", "Română"),
                     ("ar", "العربية")
                 })
        {
            var span = langs.QuerySelector($"span[lang=\"{code}\"]");
            Assert.NotNull(span);
            Assert.Equal(name, span!.TextContent.Trim());
        }

        Assert.Equal("rtl", langs.QuerySelector("span[lang=\"ar\"]")!.GetAttribute("dir"));
    }

    [Fact]
    public async Task Arabic_renders_the_page_right_to_left_with_the_mirrored_assistant_hint()
    {
        var previous = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            var culture = Services.GetRequiredService<CultureState>();
            await culture.SetLanguageAsync("ar");
            Assert.True(culture.IsRightToLeft);

            var cut = Render<HowLobsyWorks>();

            Assert.Contains("كيف يعمل لوبسي؟", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("على يسار الشاشة", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("على يمين الشاشة", cut.Markup, StringComparison.Ordinal);

            var langs = cut.Find(".how-safe__langs");
            Assert.Equal("rtl", langs.QuerySelector("span[lang=\"ar\"]")!.GetAttribute("dir"));
            Assert.Null(langs.QuerySelector("span[lang=\"nl\"]")!.GetAttribute("dir"));

            var layout = await File.ReadAllTextAsync(Path.Combine(
                RepoRoot(), "Jobsy.Web", "Components", "Layout", "MainLayout.razor"));
            Assert.Contains("\"rtl\"", layout, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentUICulture = previous;
            CultureInfo.DefaultThreadCurrentCulture = previous;
            CultureInfo.CurrentUICulture = previous ?? CultureInfo.GetCultureInfo("nl-NL");
            CultureInfo.CurrentCulture = previous ?? CultureInfo.GetCultureInfo("nl-NL");
        }
    }

    // ---------- helpers ----------

    private static IReadOnlyList<string> StoneHrefs(IRenderedComponent<HowLobsyWorks> cut)
        => cut.FindAll("a.how-stone__row").Select(a => a.GetAttribute("href") ?? "").ToList();

    private static int Occurrences(string haystack, string needle)
    {
        var count = 0;
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found.");
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public object? Summary { get; set; }

        public HttpStatusCode CompleteStatus { get; set; } = HttpStatusCode.NoContent;

        public int CompleteCalls { get; private set; }

        public int SummaryCalls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;

            if (path.EndsWith("/candidate-how-to-completed", StringComparison.Ordinal))
            {
                CompleteCalls++;
                return Task.FromResult(new HttpResponseMessage(CompleteStatus)
                {
                    Content = new StringContent("", Encoding.UTF8, "application/json")
                });
            }

            if (path.EndsWith("/journey-summary", StringComparison.Ordinal))
            {
                SummaryCalls++;
                return Task.FromResult(Ok(Summary ?? new
                {
                    discoveryDone = false,
                    passportDone = false,
                    careerDone = false,
                    jobMapDone = false,
                    applicationsDone = false
                }));
            }

            return Task.FromResult(Ok(new { }));
        }

        private static HttpResponseMessage Ok(object payload)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload, Json),
                    Encoding.UTF8,
                    "application/json")
            };
    }

    private sealed class LazyFlags(Func<bool> employers, Func<bool> passport) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(employers(), passport()));

        public ValueTask<bool> IsEnabledAsync(
            PlatformFeature feature,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(
                feature == PlatformFeature.Employers ? employers() : passport());

        public void Invalidate()
        {
        }
    }

    private sealed class FakeCandidateAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.Name, "kandidaat@lobsy.local"),
                        new Claim(ClaimTypes.Role, "Candidate")
                    ],
                    "test"))));
    }
}
