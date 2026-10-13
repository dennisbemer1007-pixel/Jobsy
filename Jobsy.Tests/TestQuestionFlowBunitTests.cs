using Bunit;
using Microsoft.AspNetCore.Components;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Tests;
using Jobsy.Web.Components.Shared.Questionnaire;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public sealed class TestQuestionFlowBunitTests : BunitContext
{
    public TestQuestionFlowBunitTests()
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
    public void Renders_One_Radiogroup_And_Counter()
    {
        var questions = Enumerable.Range(1, 5)
            .Select(i => new TestQuestionFlow.FlowQuestion(i, $"{i}. Statement {i}"))
            .ToList();
        var answers = new Dictionary<int, int>();
        var cut = Render<TestQuestionFlow>(parameters => parameters
            .Add(p => p.Kind, AssessmentKind.Competence)
            .Add(p => p.Questions, questions)
            .Add(p => p.Answers, answers)
            .Add(p => p.Target, TestDepthLevel.First));

        Assert.Contains("Vraag 1 van 5", cut.Markup);
        Assert.Single(cut.FindAll("[role=radiogroup]"));
        Assert.DoesNotContain("1. Statement", cut.Markup);
        Assert.Contains("Statement 1", cut.Markup);
    }

    [Fact]
    public void Compact_Hides_Later_And_Recent()
    {
        var questions = Enumerable.Range(1, 5)
            .Select(i => new TestQuestionFlow.FlowQuestion(i, $"Q{i}"))
            .ToList();
        var cut = Render<TestQuestionFlow>(parameters => parameters
            .Add(p => p.Kind, AssessmentKind.Competence)
            .Add(p => p.Questions, questions)
            .Add(p => p.Answers, new Dictionary<int, int>())
            .Add(p => p.Target, TestDepthLevel.First)
            .Add(p => p.Compact, true));

        Assert.DoesNotContain("Later verder", cut.Markup);
        Assert.DoesNotContain("Beantwoord", cut.Markup);
    }

    [Fact]
    public void LikertRadioGroup_Has_Accessible_Names()
    {
        var cut = Render<LikertRadioGroup>(parameters => parameters
            .Add(p => p.QuestionId, 1)
            .Add(p => p.StatementId, "stmt-1")
            .Add(p => p.Value, (int?)null));

        var group = cut.Find("[role=radiogroup]");
        Assert.Equal("stmt-1", group.GetAttribute("aria-labelledby"));
        var radios = cut.FindAll("input[type=radio]").ToList();
        Assert.Equal(5, radios.Count);
        Assert.Contains("past niet", radios[0].GetAttribute("aria-label")!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("past heel goed", radios[4].GetAttribute("aria-label")!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Finish_with_a_gap_jumps_to_the_first_unanswered_question()
    {
        var questions = Enumerable.Range(1, 5)
            .Select(i => new TestQuestionFlow.FlowQuestion(i, $"{i}. Statement {i}"))
            .ToList();
        var answers = new Dictionary<int, int>
        {
            [1] = 4,
            [3] = 4,
            [4] = 4,
            [5] = 4
        };
        var finished = false;
        var cut = Render<TestQuestionFlow>(parameters => parameters
            .Add(p => p.Kind, AssessmentKind.Competence)
            .Add(p => p.Questions, questions)
            .Add(p => p.Answers, answers)
            .Add(p => p.Target, TestDepthLevel.First)
            .Add(p => p.ExternalIndex, 4)
            .Add(p => p.OnFinished, () => finished = true));

        cut.Find("button.test-flow__next").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.False(finished);
            Assert.Contains("Vraag 2 van 5", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Statement 2", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Next_cancels_pending_auto_advance_so_a_question_is_not_skipped()
    {
        var questions = Enumerable.Range(1, 5)
            .Select(i => new TestQuestionFlow.FlowQuestion(i, $"{i}. Statement {i}"))
            .ToList();
        var answers = new Dictionary<int, int>();
        IRenderedComponent<TestQuestionFlow>? rendered = null;
        rendered = Render<TestQuestionFlow>(parameters => parameters
            .Add(p => p.Kind, AssessmentKind.Competence)
            .Add(p => p.Questions, questions)
            .Add(p => p.Answers, answers)
            .Add(p => p.Target, TestDepthLevel.First)
            .Add(p => p.OnAnswer, (Action<(int Id, int Value)>)(pair => answers[pair.Id] = pair.Value))
            .Add(p => p.OnChanged, () => rendered!.Find("button.test-flow__next").Click()));

        rendered.Find("input[type=radio][value='4']").Change(new ChangeEventArgs { Value = "4" });
        await Task.Delay(400);

        Assert.Contains("Vraag 2 van 5", rendered.Markup, StringComparison.Ordinal);
        Assert.Contains("Statement 2", rendered.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Vraag 3 van 5", rendered.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TestDiveScene_Is_Aria_Hidden()
    {
        var cut = Render<TestDiveScene>(parameters => parameters
            .Add(p => p.Kind, AssessmentKind.Competence)
            .Add(p => p.Answered, 5)
            .Add(p => p.Target, 25)
            .Add(p => p.BubbleText, "Hallo"));

        var scene = cut.Find(".test-dive-scene");
        Assert.Equal("true", scene.GetAttribute("aria-hidden"));
        Assert.Contains("Hallo", scene.TextContent);
        Assert.DoesNotContain("test-dive-scene__lobster", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}

public sealed class TestsStackGuardTests
{
    private static string RepoRoot => TestRepo.FindRoot();

    [Fact]
    public void No_Hex_Colors_In_Tests_Css_And_Components()
    {
        var files = new[]
        {
            "Jobsy.Web/wwwroot/css/features/tests.css",
            "Jobsy.Web/Components/Shared/Questionnaire/TestQuestionFlow.razor",
            "Jobsy.Web/Components/Shared/Questionnaire/LikertRadioGroup.razor",
            "Jobsy.Web/Components/Candidate/Tests/TestDiveScene.razor",
            "Jobsy.Web/Components/Candidate/Tests/TestConsentGate.razor",
            "Jobsy.Web/Components/Pages/Candidate/CompetencyTest.razor",
            "Jobsy.Web/Components/Pages/Candidate/CareerTest.razor",
            "Jobsy.Web/Components/Pages/Candidate/CultureScan.razor",
            "Jobsy.Web/Components/Pages/Candidate/ValuesScan.razor",
        };
        var hex = new System.Text.RegularExpressions.Regex(@"#[0-9a-fA-F]{3,8}\b");
        foreach (var rel in files)
        {
            var text = File.ReadAllText(Path.Combine(RepoRoot, rel));
            Assert.False(hex.IsMatch(text), $"{rel} contains hex colour");
        }

        var testsCss = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/wwwroot/css/features/tests.css"));
        Assert.Contains("background-color: var(--surface);", testsCss, StringComparison.Ordinal);
        Assert.Contains("border-top: 1px solid var(--border);", testsCss, StringComparison.Ordinal);
        Assert.Contains("position: fixed;", testsCss, StringComparison.Ordinal);
        Assert.Contains("--sticky-footer-h", testsCss, StringComparison.Ordinal);

        var flow = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Shared/Questionnaire/TestQuestionFlow.razor"));
        Assert.Contains("jobsyCoachDock.bind", flow, StringComparison.Ordinal);
        Assert.Contains("(max-width: 639px)", flow, StringComparison.Ordinal);
        Assert.Contains("IAsyncDisposable", flow, StringComparison.Ordinal);
    }

    [Fact]
    public void No_ExMessage_On_Free_Test_Pages()
    {
        foreach (var rel in new[]
                 {
                     "Jobsy.Web/Components/Pages/Candidate/CompetencyTest.razor",
                     "Jobsy.Web/Components/Pages/Candidate/CareerTest.razor",
                     "Jobsy.Web/Components/Pages/Candidate/CultureScan.razor",
                     "Jobsy.Web/Components/Pages/Candidate/ValuesScan.razor",
                 })
        {
            var text = File.ReadAllText(Path.Combine(RepoRoot, rel));
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void No_Hardcoded_Depth_Counts_In_Free_Test_Pages()
    {
        foreach (var rel in new[]
                 {
                     "Jobsy.Web/Components/Pages/Candidate/CompetencyTest.razor",
                     "Jobsy.Web/Components/Pages/Candidate/CareerTest.razor",
                     "Jobsy.Web/Components/Pages/Candidate/CultureScan.razor",
                     "Jobsy.Web/Components/Pages/Candidate/ValuesScan.razor",
                 })
        {
            var text = File.ReadAllText(Path.Combine(RepoRoot, rel));
            Assert.DoesNotContain("QuestionCount", text);
            Assert.Contains("TestDepthLevel", text);
        }
    }
}
