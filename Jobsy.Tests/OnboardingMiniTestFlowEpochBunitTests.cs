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
    public void TestQuestionFlow_resets_to_vraag_1_when_block_questions_change()
    {
        var comp = Enumerable.Range(1, 5)
            .Select(i => new TestQuestionFlow.FlowQuestion(i, $"Comp {i}"))
            .ToList();
        var career = OnboardingWizardCatalogV2.CareerQuestionIds
            .Select(id => new TestQuestionFlow.FlowQuestion(id, $"Career {id}"))
            .ToList();
        var answers = new Dictionary<int, int>();

        var cut = Render<TestQuestionFlow>(parameters => parameters
            .Add(p => p.Kind, AssessmentKind.Competence)
            .Add(p => p.Questions, comp)
            .Add(p => p.Answers, answers)
            .Add(p => p.Target, TestDepthLevel.First)
            .Add(p => p.OnAnswer, (Action<(int Id, int Value)>)(pair => answers[pair.Id] = pair.Value)));

        cut.WaitForElement(".test-likert__opt", TimeSpan.FromSeconds(15));
        for (var i = 0; i < 4; i++)
        {
            cut.FindAll(".test-likert__opt")[3].Click();
            cut.Find("button.test-flow__next").Click();
            cut.WaitForAssertion(
                () => Assert.Contains($"Vraag {i + 2} van 5", cut.Markup, StringComparison.Ordinal),
                TimeSpan.FromSeconds(15));
        }

        cut.WaitForAssertion(
            () => Assert.Contains("Vraag 5 van 5", cut.Markup, StringComparison.Ordinal),
            timeout: TimeSpan.FromSeconds(30));

        cut.Render(parameters => parameters
            .Add(p => p.Kind, AssessmentKind.Career)
            .Add(p => p.Questions, career)
            .Add(p => p.Answers, new Dictionary<int, int>())
            .Add(p => p.Target, TestDepthLevel.First));

        Assert.Contains("Vraag 1 van 5", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Career", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Compact_mini_hides_depth_ticks_on_progress_bar()
    {
        var questions = Enumerable.Range(1, 5)
            .Select(i => new TestQuestionFlow.FlowQuestion(i, $"Q{i}"))
            .ToList();
        var cut = Render<TestQuestionFlow>(parameters => parameters
            .Add(p => p.Kind, AssessmentKind.Career)
            .Add(p => p.Questions, questions)
            .Add(p => p.Answers, new Dictionary<int, int>())
            .Add(p => p.Target, TestDepthLevel.First)
            .Add(p => p.Compact, true));

        Assert.DoesNotContain("test-flow__tick", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Wizard_scopes_server_answers_to_current_mini_question_ids()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var wizard = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/OnboardingWizard.razor"));
        Assert.Contains("questionIds.Contains(id)", wizard, StringComparison.Ordinal);
        Assert.Contains("FilterMiniAnswers", wizard, StringComparison.Ordinal);
    }

    [Fact]
    public void TestQuestionFlow_resumes_mid_block_at_first_unanswered()
    {
        var career = OnboardingWizardCatalogV2.CareerQuestionIds
            .Select(id => new TestQuestionFlow.FlowQuestion(id, $"Career {id}"))
            .ToList();
        var partial = new Dictionary<int, int>
        {
            [career[0].Id] = 4,
            [career[1].Id] = 3,
        };

        var cut = Render<TestQuestionFlow>(parameters => parameters
            .Add(p => p.Kind, AssessmentKind.Career)
            .Add(p => p.Questions, career)
            .Add(p => p.Answers, partial)
            .Add(p => p.Target, TestDepthLevel.First)
            .Add(p => p.Compact, true));

        Assert.Contains("Vraag 3 van 5", cut.Markup, StringComparison.Ordinal);
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
        Assert.Contains("FlowEpoch=\"_miniFlowEpoch\"", wizard, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
