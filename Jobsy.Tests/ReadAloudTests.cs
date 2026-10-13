using Bunit;
using Jobsy.Web.Components;
using Jobsy.Web.Components.Shared;
using Jobsy.Web.Components.Shared.Questionnaire;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public sealed class ReadAloudCoordinatorTests
{
    [Fact]
    public void SetActive_notifies_once_and_ignores_the_same_key()
    {
        var coordinator = new ReadAloudCoordinator();
        var calls = 0;
        coordinator.ActiveChanged += () => calls++;

        coordinator.SetActive("q-1");
        coordinator.SetActive("q-1");
        coordinator.SetActive(null);

        Assert.Equal(2, calls);
        Assert.Null(coordinator.ActiveKey);
    }

    [Fact]
    public void Preference_change_is_a_separate_signal()
    {
        var coordinator = new ReadAloudCoordinator();
        var active = 0;
        var prefs = 0;
        coordinator.ActiveChanged += () => active++;
        coordinator.PreferenceChanged += () => prefs++;

        coordinator.NotifyPreferenceChanged();

        Assert.Equal(0, active);
        Assert.Equal(1, prefs);
    }
}

public sealed class ReadAloudScriptTests
{
    [Fact]
    public void Script_uses_browser_speech_only_and_a_calm_rate()
    {
        var js = File.ReadAllText(Path.Combine(TestRepo.FindRoot(), "Jobsy.Web/wwwroot/js/read-aloud.js"));
        Assert.Contains("speechSynthesis", js, StringComparison.Ordinal);
        Assert.Contains("RATE = 0.9", js, StringComparison.Ordinal);
        Assert.Contains("utter.rate = RATE", js, StringComparison.Ordinal);
        Assert.Contains("localStorage.getItem(STORAGE_KEY) !== \"off\"", js, StringComparison.Ordinal);
        Assert.DoesNotContain("fetch(", js, StringComparison.Ordinal);
        Assert.DoesNotContain("XMLHttpRequest", js, StringComparison.Ordinal);
        Assert.DoesNotContain("sendBeacon", js, StringComparison.Ordinal);
        Assert.DoesNotContain("WebSocket", js, StringComparison.Ordinal);
        Assert.DoesNotContain("http://", js, StringComparison.Ordinal);
        Assert.DoesNotContain("https://", js, StringComparison.Ordinal);
        Assert.Contains("localService === true", js, StringComparison.Ordinal);
        Assert.Contains("selectLocalVoice", js, StringComparison.Ordinal);
    }

    [Fact]
    public void Button_is_not_fixed_over_the_page()
    {
        var css = File.ReadAllText(Path.Combine(TestRepo.FindRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        var start = css.IndexOf(".read-aloud {", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = css.IndexOf(".read-aloud:focus-visible", start, StringComparison.Ordinal);
        var block = css[start..end];
        Assert.DoesNotContain("position: fixed", block, StringComparison.Ordinal);
        Assert.DoesNotContain("position:fixed", block, StringComparison.Ordinal);
        Assert.Contains("min-height: 44px", block, StringComparison.Ordinal);
    }
}

public sealed class ReadAloudBunitTests : BunitContext
{
    private readonly ReadAloudCoordinator _coordinator = new();

    public ReadAloudBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new ReadAloudFakeAuth());
        Services.AddSingleton<CoachTipBus>();
        Services.AddSingleton<AssistantChatHost>();
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton(_coordinator);
    }

    [Fact]
    public void Coach_tip_has_a_speaker_beside_the_sentence()
    {
        JSInterop.Setup<ReadAloudProbe>("lobsyReadAloud.whenReady", _ => true)
            .SetResult(new ReadAloudProbe { Supported = true, Enabled = true });

        var cut = Render<LobsyCoach>();
        var nav = Services.GetRequiredService<NavigationManager>();
        Services.GetRequiredService<CoachTipBus>().Publish(nav.Uri, "Kijk nog eens naar je test.");

        cut.WaitForAssertion(() =>
        {
            var tip = cut.Find(".lobsy-coach-dock__tip");
            Assert.Contains("Kijk nog eens naar je test.", tip.TextContent, StringComparison.Ordinal);
            var speaker = tip.QuerySelector("[data-read-aloud]");
            Assert.NotNull(speaker);
            Assert.Equal("play", speaker!.GetAttribute("data-read-aloud-state"));
            Assert.Empty(cut.Find("#lobsy-coach-btn").QuerySelectorAll("[data-read-aloud]"));
        });
    }

    [Fact]
    public void Button_stays_hidden_when_the_language_has_no_voice()
    {
        JSInterop.Setup<ReadAloudProbe>("lobsyReadAloud.whenReady", _ => true)
            .SetResult(new ReadAloudProbe { Supported = false, Enabled = true });

        var cut = Render<ReadAloudButton>(p => p.Add(c => c.Text, "Hoe werk jij het liefst?"));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-read-aloud]")));
    }

    [Fact]
    public void Button_stays_hidden_when_voorlezen_is_off()
    {
        JSInterop.Setup<ReadAloudProbe>("lobsyReadAloud.whenReady", _ => true)
            .SetResult(new ReadAloudProbe { Supported = true, Enabled = false });

        var cut = Render<ReadAloudButton>(p => p.Add(c => c.Text, "Hoe werk jij het liefst?"));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-read-aloud]")));
    }

    [Fact]
    public void Button_shows_play_and_stop_with_an_accessible_name()
    {
        JSInterop.Setup<ReadAloudProbe>("lobsyReadAloud.whenReady", _ => true)
            .SetResult(new ReadAloudProbe { Supported = true, Enabled = true });
        JSInterop.Setup<bool>("lobsyReadAloud.speak", _ => true).SetResult(true);
        JSInterop.SetupVoid("lobsyReadAloud.stop", _ => true);

        var cut = Render<ReadAloudButton>(p => p
            .Add(c => c.Text, "Hoe werk jij het liefst?")
            .Add(c => c.SpeechKey, "q-1"));

        cut.WaitForAssertion(() =>
        {
            var button = cut.Find("[data-read-aloud]");
            Assert.Equal("BUTTON", button.TagName);
            Assert.Equal("Lees deze tekst voor", button.GetAttribute("aria-label"));
            Assert.Equal("false", button.GetAttribute("aria-pressed"));
            Assert.Equal("play", button.GetAttribute("data-read-aloud-state"));
            Assert.Contains("Lees voor", button.TextContent, StringComparison.Ordinal);
        });

        cut.Find("[data-read-aloud]").Click();

        cut.WaitForAssertion(() =>
        {
            var button = cut.Find("[data-read-aloud]");
            Assert.Equal("Stop met voorlezen", button.GetAttribute("aria-label"));
            Assert.Equal("true", button.GetAttribute("aria-pressed"));
            Assert.Equal("stop", button.GetAttribute("data-read-aloud-state"));
            Assert.Contains("Stoppen", button.TextContent, StringComparison.Ordinal);
            Assert.Equal("q-1", _coordinator.ActiveKey);
        });

        cut.Find("[data-read-aloud]").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("play", cut.Find("[data-read-aloud]").GetAttribute("data-read-aloud-state"));
            Assert.Null(_coordinator.ActiveKey);
        });
    }

    [Fact]
    public void Question_and_example_can_host_a_speaker()
    {
        JSInterop.Setup<ReadAloudProbe>("lobsyReadAloud.whenReady", _ => true)
            .SetResult(new ReadAloudProbe { Supported = true, Enabled = true });

        var questions = new List<TestQuestionFlow.FlowQuestion>
        {
            new(1, "1. Ik help graag mensen.", "Bijvoorbeeld in een winkel.")
        };
        var cut = Render<TestQuestionFlow>(p => p
            .Add(c => c.Kind, Jobsy.Core.Enums.AssessmentKind.Competence)
            .Add(c => c.Questions, questions)
            .Add(c => c.Answers, new Dictionary<int, int>())
            .Add(c => c.Target, Jobsy.Core.Rules.TestDepthLevel.First));

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[data-read-aloud]").Count));
        Assert.Contains("Ik help graag mensen.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Bijvoorbeeld in een winkel.", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Setting_defaults_on_and_can_be_turned_off()
    {
        JSInterop.Setup<ReadAloudProbe>("lobsyReadAloud.whenReady", _ => true)
            .SetResult(new ReadAloudProbe { Supported = true, Enabled = true });
        JSInterop.SetupVoid("lobsyReadAloud.setEnabled");

        var cut = Render<ReadAloudSetting>();

        cut.WaitForAssertion(() =>
        {
            var box = cut.Find("[data-read-aloud-setting] input");
            Assert.NotNull(box.GetAttribute("checked"));
            Assert.Contains("Voorlezen aan", cut.Markup, StringComparison.Ordinal);
        });

        var prefs = 0;
        _coordinator.PreferenceChanged += () => prefs++;
        cut.Find("[data-read-aloud-setting] input").Change(false);

        cut.WaitForAssertion(() => Assert.Contains("Voorlezen uit", cut.Markup, StringComparison.Ordinal));
        Assert.Equal(1, prefs);
        JSInterop.VerifyInvoke("lobsyReadAloud.setEnabled");
    }

    [Fact]
    public void Setting_explains_when_the_device_cannot_read_aloud()
    {
        JSInterop.Setup<ReadAloudProbe>("lobsyReadAloud.whenReady", _ => true)
            .SetResult(new ReadAloudProbe { Supported = false, Enabled = true });

        var cut = Render<ReadAloudSetting>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Er is geen stem op dit apparaat voor deze taal.", cut.Markup, StringComparison.Ordinal);
            Assert.Empty(cut.FindAll("[data-read-aloud-setting] input"));
        });
    }

    private sealed class ReadAloudFakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
