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
/// Quick-Scan v3 onboarding mini blocks (steps 7–10) must each expose five catalog items and open at vraag 1/5.
/// </summary>
public sealed class OnboardingV3MiniBlockTests : BunitContext
{
    private const int Version = AssessmentTestVersions.V3;

    public OnboardingV3MiniBlockTests()
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

    [Theory]
    [InlineData(AssessmentKind.Competence, 7)]
    [InlineData(AssessmentKind.Career, 8)]
    [InlineData(AssessmentKind.Culture, 9)]
    [InlineData(AssessmentKind.Values, 10)]
    public void V3_mini_catalog_has_five_items_per_block(AssessmentKind kind, int _)
    {
        var miniIds = TestDepthRules.MiniIds(kind, Version);
        Assert.Equal(5, miniIds.Length);
        Assert.Equal(5, miniIds.Distinct().Count());

        var catalogIds = kind switch
        {
            AssessmentKind.Competence => AssessmentQuickScanCatalog.CompetenceQuestions(Version).Select(q => q.Id),
            AssessmentKind.Career => AssessmentQuickScanCatalog.CareerQuestions(Version).Select(q => q.Id),
            AssessmentKind.Culture => AssessmentQuickScanCatalog.CultureQuestions(Version).Select(q => q.Id),
            AssessmentKind.Values => AssessmentQuickScanCatalog.ValuesQuestions(Version).Select(q => q.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        var catalogSet = catalogIds.ToHashSet();
        Assert.All(miniIds, id => Assert.Contains(id, catalogSet));
    }

    [Theory]
    [InlineData(AssessmentKind.Competence, 7)]
    [InlineData(AssessmentKind.Career, 8)]
    [InlineData(AssessmentKind.Culture, 9)]
    [InlineData(AssessmentKind.Values, 10)]
    public void OnboardingMiniTest_renders_vraag_1_for_fresh_v3_block(AssessmentKind kind, int step)
    {
        var miniIds = TestDepthRules.MiniIds(kind, Version);
        var questions = miniIds
            .Select(id => new OnboardingMiniTest.MiniQuestion(id, $"{kind}.{id}"))
            .ToList();

        var cut = Render<OnboardingMiniTest>(parameters => parameters
            .Add(p => p.Step, step)
            .Add(p => p.FlowEpoch, step)
            .Add(p => p.CurrentQuestion, 0)
            .Add(p => p.TestTabs, [])
            .Add(p => p.Questions, questions)
            .Add(p => p.Answers, new Dictionary<int, int>())
            .Add(p => p.Depth, TestDepthLevel.First)
            .Add(p => p.ResolveQuestionText, (Func<OnboardingMiniTest.MiniQuestion, string>)(q => q.TextKey))
            .Add(p => p.OnFinished, EventCallback.Factory.Create(this, () => { })));

        Assert.Equal(5, questions.Count);
        Assert.Contains("Vraag 1 van 5", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Onboarding.MiniAllDone", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Culture_block_still_shows_five_questions_when_all_mini_ids_answered_on_server()
    {
        var miniIds = TestDepthRules.MiniIds(AssessmentKind.Culture, Version);
        var questions = miniIds
            .Select(id => new OnboardingMiniTest.MiniQuestion(id, $"Culture.{id}"))
            .ToList();
        var serverAnswers = miniIds.ToDictionary(id => id, _ => 3);

        var cut = Render<OnboardingMiniTest>(parameters => parameters
            .Add(p => p.Step, 9)
            .Add(p => p.FlowEpoch, 9)
            .Add(p => p.CurrentQuestion, 0)
            .Add(p => p.TestTabs, [])
            .Add(p => p.Questions, questions)
            .Add(p => p.Answers, serverAnswers)
            .Add(p => p.Depth, TestDepthLevel.First)
            .Add(p => p.ResolveQuestionText, (Func<OnboardingMiniTest.MiniQuestion, string>)(q => q.TextKey))
            .Add(p => p.OnFinished, EventCallback.Factory.Create(this, () => { })));

        Assert.Equal(5, questions.Count);
        Assert.Contains("test-flow", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Onboarding.MiniAllDone", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Wizard_lists_all_mini_items_not_only_unlocked()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var wizard = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/OnboardingWizard.razor"));
        Assert.Contains("_miniBlockQuestionIds = questionIds", wizard, StringComparison.Ordinal);
        Assert.DoesNotContain("!_lockedIds.Contains(item.Id)", wizard, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
