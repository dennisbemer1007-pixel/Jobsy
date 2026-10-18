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
/// Four onboarding mini blocks (V3) must each collect five answers; overlap ids must not complete a later block.
/// </summary>
public sealed class OnboardingFourBlockMiniCompletionTests : BunitContext
{
    private const int Version = AssessmentTestVersions.V3;

    public OnboardingFourBlockMiniCompletionTests()
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
    public void Values_block_stays_incomplete_until_values_store_has_five_answers()
    {
        var empty = new Dictionary<int, int>();
        var filled = TestDepthRules.MiniIds(AssessmentKind.Values, Version)
            .ToDictionary(id => id, _ => 3);

        Assert.False(OnboardingMiniBlockCompletion.IsMiniBlockComplete(10, Version, empty));
        Assert.True(OnboardingMiniBlockCompletion.IsMiniBlockComplete(10, Version, filled));
        Assert.Equal(
            TestDepthRules.MiniIds(AssessmentKind.Competence, Version),
            TestDepthRules.MiniIds(AssessmentKind.Values, Version));
    }

    [Fact]
    public void Completing_all_four_blocks_from_scratch_records_twenty_answers()
    {
        var total = 0;
        foreach (var (step, kind) in new (int Step, AssessmentKind Kind)[]
                 {
                     (7, AssessmentKind.Competence),
                     (8, AssessmentKind.Career),
                     (9, AssessmentKind.Culture),
                     (10, AssessmentKind.Values),
                 })
        {
            var miniIds = TestDepthRules.MiniIds(kind, Version);
            var questions = miniIds
                .Select(id => new TestQuestionFlow.FlowQuestion(id, $"{kind}.{id}"))
                .ToList();

            var cut = Render<TestQuestionFlow>(parameters => parameters
                .Add(p => p.Kind, kind)
                .Add(p => p.Questions, questions)
                .Add(p => p.Answers, new Dictionary<int, int>())
                .Add(p => p.Target, TestDepthLevel.First)
                .Add(p => p.Compact, true));

            Assert.Contains("Vraag 1 van 5", cut.Markup, StringComparison.Ordinal);

            var answers = miniIds.ToDictionary(id => id, _ => 3);
            total += answers.Count;
            Assert.True(OnboardingMiniBlockCompletion.IsMiniBlockComplete(step, Version, answers));

            for (var q = 1; q <= 5; q++)
            {
                var atIndex = Render<TestQuestionFlow>(parameters => parameters
                    .Add(p => p.Kind, kind)
                    .Add(p => p.Questions, questions)
                    .Add(p => p.Answers, answers)
                    .Add(p => p.StartAt, q - 1)
                    .Add(p => p.Target, TestDepthLevel.First)
                    .Add(p => p.Compact, true));
                Assert.Contains($"Vraag {q} van 5", atIndex.Markup, StringComparison.Ordinal);
            }
        }

        Assert.Equal(20, total);
    }

    [Fact]
    public void Journey_try_auto_advance_requires_persisted_mini_complete()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var journey = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/DiscoveryJourney.razor"));
        Assert.Contains("IsPersistedMiniBlockCompleteAsync", journey, StringComparison.Ordinal);
        Assert.Contains("TryRouteToFirstIncompleteMiniBlockAsync", journey, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
