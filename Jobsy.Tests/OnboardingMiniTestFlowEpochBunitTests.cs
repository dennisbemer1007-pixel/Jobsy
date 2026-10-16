using Bunit;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Components.Shared.Questionnaire;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

/// <summary>
/// Regression: onboarding mini steps 7–10 must remount <see cref="TestQuestionFlow"/>
/// (via <see cref="OnboardingMiniTest.FlowEpoch"/>) so the counter resets to Vraag 1.
/// </summary>
public sealed class OnboardingMiniTestFlowEpochBunitTests : BunitContext
{
    public OnboardingMiniTestFlowEpochBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp =>
        {
            var culture = new CultureState(
                sp.GetRequiredService<IJSRuntime>(),
                sp,
                sp.GetRequiredService<AuthenticationStateProvider>());
            culture.InitializeFromLanguage("nl");
            return culture;
        });
    }

    [Fact]
    public void TestQuestionFlow_keeps_last_index_when_only_parameters_change()
    {
        var comp = Enumerable.Range(1, 5)
            .Select(i => new TestQuestionFlow.FlowQuestion(i, $"Comp {i}"))
            .ToList();
        var career = OnboardingWizardCatalog.CareerQuestionIds
            .Select(id => new TestQuestionFlow.FlowQuestion(id, $"Career {id}"))
            .ToList();
        var answers = new Dictionary<int, int>();

        var cut = Render<TestQuestionFlow>(parameters => parameters
            .Add(p => p.Kind, AssessmentKind.Competence)
            .Add(p => p.Questions, comp)
            .Add(p => p.Answers, answers)
            .Add(p => p.Target, TestDepthLevel.First)
            .Add(p => p.OnAnswer, (Action<(int Id, int Value)>)(pair => answers[pair.Id] = pair.Value)));

        for (var i = 0; i < 4; i++)
        {
            cut.FindAll(".test-likert__opt")[3].Click();
            cut.Find("button.test-flow__next").Click();
        }

        cut.WaitForAssertion(() => Assert.Contains("Vraag 5 van 5", cut.Markup, StringComparison.Ordinal));

        cut.Render(parameters => parameters
            .Add(p => p.Kind, AssessmentKind.Career)
            .Add(p => p.Questions, career)
            .Add(p => p.Answers, new Dictionary<int, int>())
            .Add(p => p.Target, TestDepthLevel.First));

        Assert.Contains("Vraag 5 van 5", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Career", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void OnboardingMiniTest_new_FlowEpoch_starts_career_at_vraag_1()
    {
        var careerQuestions = OnboardingWizardCatalog.CareerQuestionIds
            .Select(id => new OnboardingMiniTest.MiniQuestion(id, $"Career.{id}"))
            .ToList();
        var tabs = new List<OnboardingMiniTest.TestTab>
        {
            new(8, "Onboarding.Step8.Tab", "Onboarding.Step8.Lead"),
        };

        var cut = Render<OnboardingMiniTest>(parameters => parameters
            .Add(p => p.Step, 8)
            .Add(p => p.FlowEpoch, 8)
            .Add(p => p.CurrentQuestion, 0)
            .Add(p => p.TestTabs, tabs)
            .Add(p => p.Questions, careerQuestions)
            .Add(p => p.Answers, new Dictionary<int, int>())
            .Add(p => p.Depth, TestDepthLevel.First)
            .Add(p => p.ResolveQuestionText, (Func<OnboardingMiniTest.MiniQuestion, string>)(q => q.TextKey))
            .Add(p => p.OnFinished, EventCallback.Factory.Create(this, () => { })));

        Assert.Contains("Vraag 1 van 5", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Career.", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Wizard_wires_FlowEpoch_to_counted_step()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var wizard = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/OnboardingWizard.razor"));
        Assert.Contains("FlowEpoch=\"_step\"", wizard, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
