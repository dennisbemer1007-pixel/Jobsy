using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Bunit;
using Jobsy.Core.Features;
using Jobsy.Core.Privacy;
using Jobsy.Web.Components.Pages.Candidate;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

/// <summary>
/// After a free unlock the player must leave <c>test-skeleton</c> even when a
/// legacy API still returns the 150–200 question bank. A failed load shows a
/// B1 error and retry instead of staying <c>aria-busy</c>.
/// </summary>
public sealed class DeepAnalysisSkeletonBunitTests : BunitContext
{
    private static readonly JsonSerializerOptions WireJson = new(JsonSerializerDefaults.Web);

    public DeepAnalysisSkeletonBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IFeatureFlags>(new OffFlags());
    }

    [Fact]
    public void Unlocked_bank_does_not_keep_the_skeleton()
    {
        var handler = new DeepHandler(failFirst: false);
        Services.AddSingleton(new JobsyApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));

        var cut = Render<DeepAnalysis>(p => p.Add(x => x.Kind, "competence"));

        cut.WaitForElement("[role=radiogroup]", TimeSpan.FromSeconds(15));
        Assert.Empty(cut.FindAll("[aria-busy=true]"));
        Assert.Single(cut.FindAll("[role=radiogroup]"));
        Assert.DoesNotContain("prompt-from-wire-", cut.Markup, StringComparison.Ordinal);
        Assert.True(handler.SawQuestions);
    }

    [Fact]
    public void Failed_load_shows_b1_retry_instead_of_skeleton()
    {
        var handler = new DeepHandler(failFirst: true);
        Services.AddSingleton(new JobsyApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));

        var cut = Render<DeepAnalysis>(p => p.Add(x => x.Kind, "career"));

        cut.WaitForElement("[data-testid=test-load-error]", TimeSpan.FromSeconds(15));
        Assert.Empty(cut.FindAll("[aria-busy=true]"));
        Assert.Contains("De vragen zijn niet geladen", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Opnieuw proberen", cut.Markup, StringComparison.Ordinal);

        cut.Find("[data-testid=test-load-error] button").Click();
        cut.WaitForElement("[role=radiogroup]", TimeSpan.FromSeconds(15));
        Assert.Empty(cut.FindAll("[aria-busy=true]"));
        Assert.Empty(cut.FindAll("[data-testid=test-load-error]"));
    }

    [Fact]
    public async Task Wire_reader_drops_the_question_bank()
    {
        var payload = new
        {
            kind = "Career",
            status = "Draft",
            isUnlocked = true,
            isCompleted = false,
            answeredCount = 0,
            questionCount = 200,
            priceEuro = 0m,
            questions = Enumerable.Range(1, 200).Select(i => new
            {
                id = i,
                family = "RIASEC",
                domain = "Realistic",
                promptNl = "prompt-from-wire-" + i,
                exampleNl = "voorbeeld " + i,
                domainLabel = "Doen"
            })
        };
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, WireJson)));
        var state = await DeepAnalysisWireReader.ReadAsync(stream, CancellationToken.None);

        Assert.NotNull(state);
        Assert.True(state!.IsUnlocked);
        Assert.Equal(200, state.QuestionCount);
        Assert.Empty(state.Questions);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.Role, "Candidate")], "test"))));
    }

    private sealed class OffFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(FeatureFlagSnapshot.Defaults);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(false);

        public void Invalidate()
        {
        }
    }

    private sealed class DeepHandler(bool failFirst) : HttpMessageHandler
    {
        private int _deepReads;
        public bool SawQuestions { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("/api/me/profile", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Json(new MeProfile
                {
                    FullName = "Test K",
                    DateOfBirth = new DateOnly(1990, 1, 1),
                    TestAiConsentAt = DateTime.UtcNow,
                    TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
                }));
            }

            if (path.Contains("/payment-mode", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Json(new { mode = "gratis" }));
            }

            if (path.Contains("/api/me/deep-analysis", StringComparison.OrdinalIgnoreCase)
                && request.Method == HttpMethod.Get)
            {
                var n = Interlocked.Increment(ref _deepReads);
                if (failFirst && n == 1)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
                }

                SawQuestions = true;
                var questions = Enumerable.Range(1, 200).Select(i => new
                {
                    id = i,
                    family = "RIASEC",
                    domain = "Realistic",
                    promptNl = "prompt-from-wire-" + i,
                    exampleNl = "voorbeeld " + i,
                    domainLabel = "Doen"
                });
                return Task.FromResult(Json(new
                {
                    kind = "Competence",
                    status = "Draft",
                    isUnlocked = true,
                    isCompleted = false,
                    answeredCount = 0,
                    questionCount = 150,
                    priceEuro = 0m,
                    answers = new Dictionary<string, int>(),
                    questions
                }));
            }

            return Task.FromResult(Json(new { }));
        }

        private static HttpResponseMessage Json(object payload)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, WireJson), Encoding.UTF8, "application/json")
            };
    }
}
