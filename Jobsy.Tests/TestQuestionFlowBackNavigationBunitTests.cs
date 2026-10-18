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

public sealed class TestQuestionFlowBackNavigationBunitTests : BunitContext
{
    public TestQuestionFlowBackNavigationBunitTests()
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
    public void Terug_shows_saved_likert_selection_and_enables_Volgende()
    {
        var questions = Enumerable.Range(1, 5)
            .Select(i => new TestQuestionFlow.FlowQuestion(i, $"{i}. Statement {i}"))
            .ToList();
        var answers = new Dictionary<int, int>();
        var cut = Render<TestQuestionFlow>(parameters => parameters
            .Add(p => p.Kind, AssessmentKind.Competence)
            .Add(p => p.Questions, questions)
            .Add(p => p.Answers, answers)
            .Add(p => p.Target, TestDepthLevel.First)
            .Add(p => p.Compact, true)
            .Add(p => p.OnAnswer, (Action<(int Id, int Value)>)(pair => answers[pair.Id] = pair.Value)));

        cut.Find("input[type=radio][value='4']").Change(new ChangeEventArgs { Value = "4" });
        cut.WaitForAssertion(() => Assert.Contains("Vraag 2 van 5", cut.Markup, StringComparison.Ordinal));

        cut.Find("button.test-flow__back").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Vraag 1 van 5", cut.Markup, StringComparison.Ordinal);
            var selected = cut.Find(".test-likert__opt.is-on input[type=radio]");
            Assert.Equal("4", selected.GetAttribute("value"));
            Assert.False(cut.Find("button.test-flow__next").HasAttribute("disabled"));
        });
    }

    [Fact]
    public void Resume_from_saved_answers_shows_selection_after_Terug()
    {
        var questions = OnboardingWizardCatalogV2.CompetencyQuestionIds
            .Select(id => new TestQuestionFlow.FlowQuestion(id, $"Q{id}"))
            .ToList();
        var saved = new Dictionary<int, int>
        {
            [questions[0].Id] = 2,
            [questions[1].Id] = 5,
        };

        var cut = Render<TestQuestionFlow>(parameters => parameters
            .Add(p => p.Kind, AssessmentKind.Competence)
            .Add(p => p.Questions, questions)
            .Add(p => p.Answers, saved)
            .Add(p => p.StartAt, 2)
            .Add(p => p.Target, TestDepthLevel.First)
            .Add(p => p.Compact, true));

        cut.Find("button.test-flow__back").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Vraag 2 van 5", cut.Markup, StringComparison.Ordinal);
            var selected = cut.Find(".test-likert__opt.is-on input[type=radio]");
            Assert.Equal("5", selected.GetAttribute("value"));
        });
    }

    [Fact]
    public void OnboardingMiniTest_forwards_index_changes_from_flow()
    {
        var questions = TestDepthRules.MiniIds(AssessmentKind.Career, AssessmentTestVersions.V3)
            .Select(id => new OnboardingMiniTest.MiniQuestion(id, $"C.{id}"))
            .ToList();
        var answers = new Dictionary<int, int> { [questions[0].Id] = 3 };
        var currentIndex = 1;

        var cut = Render<OnboardingMiniTest>(parameters => parameters
            .Add(p => p.Step, 8)
            .Add(p => p.FlowEpoch, 8)
            .Add(p => p.CurrentQuestion, currentIndex)
            .Add(p => p.TestTabs, [])
            .Add(p => p.Questions, questions)
            .Add(p => p.Answers, answers)
            .Add(p => p.Depth, TestDepthLevel.First)
            .Add(p => p.ExpandQuestion, (Action<int>)(i => currentIndex = i))
            .Add(p => p.ResolveQuestionText, (Func<OnboardingMiniTest.MiniQuestion, string>)(q => q.TextKey))
            .Add(p => p.OnFinished, EventCallback.Factory.Create(this, () => { })));

        cut.Find("button.test-flow__back").Click();
        Assert.Equal(0, currentIndex);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
