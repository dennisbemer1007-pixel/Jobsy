using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Jobsy.Web.Capture;

/// <summary>Development-only harness for Playwright screenshots (JOBSY_CAPTURE_MODE=1).</summary>
public static class CaptureModeExtensions
{
    public const string EnabledConfigKey = "Jobsy:CaptureMode";
    public const string TargetConfigKey = "Jobsy:CaptureTarget";

    public static bool IsEnabled(IConfiguration configuration)
        => configuration.GetValue(EnabledConfigKey, false);

    internal static CaptureTarget ActiveTarget { get; private set; } = CaptureTarget.Default;

    public static WebApplication UseCaptureMode(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment() || !IsEnabled(app.Configuration))
        {
            return app;
        }

        ActiveTarget = ParseTarget(app.Configuration[TargetConfigKey]);

        app.MapGet("/__test/capture-target", (string? kind, int? answered, bool deep = false) =>
        {
            ActiveTarget = kind?.ToLowerInvariant() switch
            {
                "career" => new CaptureTarget(AssessmentKind.Career, answered ?? 6, deep),
                "culture" => new CaptureTarget(AssessmentKind.Culture, answered ?? 6, deep),
                "values" => new CaptureTarget(AssessmentKind.Values, answered ?? 6, deep),
                _ => new CaptureTarget(AssessmentKind.Competence, answered ?? 6, deep)
            };
            return Results.Json(new { ok = true, target = ActiveTarget });
        });

        app.Use(async (ctx, next) =>
        {
            if (!ctx.Request.Path.Equals("/__test/sign-in", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "Candidate"),
                new Claim(ClaimTypes.Name, "Capture Mode")
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);
            await ctx.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
            var ret = ctx.Request.Query["return"].ToString();
            if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
            {
                ret = "/candidate/competencies?stap=vraag";
            }

            ctx.Response.Redirect(ret);
        });

        return app;
    }

    public static IServiceCollection AddCaptureModeStubs(this IServiceCollection services, IConfiguration configuration)
    {
        if (!IsEnabled(configuration))
        {
            return services;
        }

        services.RemoveAll<IFeatureFlags>();
        services.AddSingleton<IFeatureFlags>(new CaptureFeatureFlags());

        services.RemoveAll<IHttpClientFactory>();
        services.AddSingleton<IHttpClientFactory, CaptureHttpClientFactory>();

        return services;
    }

    private static CaptureTarget ParseTarget(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return CaptureTarget.Default;
        }

        var parts = raw.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return CaptureTarget.Default;
        }

        var kind = parts[0].ToLowerInvariant() switch
        {
            "career" => AssessmentKind.Career,
            "culture" => AssessmentKind.Culture,
            "values" => AssessmentKind.Values,
            _ => AssessmentKind.Competence
        };
        _ = int.TryParse(parts[1], out var answered);
        var paid = parts.Length > 2 && parts[2].Equals("deep", StringComparison.OrdinalIgnoreCase);
        return new CaptureTarget(kind, Math.Max(answered, 1), paid);
    }

    internal sealed record CaptureTarget(AssessmentKind Kind, int Answered, bool PaidDeep)
    {
        public static CaptureTarget Default { get; } = new(AssessmentKind.Competence, 6, false);
    }

    private sealed class CaptureFeatureFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: false,
                CandidatePassportEnabled: true,
                TestsV2Enabled: true,
                TestItemExamplesEnabled: true,
                TestProgressInsightEnabled: true));

        public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }

    private sealed class CaptureHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new CaptureStubHandler(), disposeHandler: true) { BaseAddress = new Uri("http://capture.stub/") };
    }

    private sealed class CaptureStubHandler : HttpMessageHandler
    {
        private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            object payload = path switch
            {
                var p when p.Contains("/profile", StringComparison.OrdinalIgnoreCase) => BuildProfile(),
                var p when p.Contains("/test-ai-consent", StringComparison.OrdinalIgnoreCase) => BuildProfile(),
                var p when p.Contains("/competencies", StringComparison.OrdinalIgnoreCase) => BuildQuickScan(AssessmentKind.Competence),
                var p when p.Contains("/career-interests", StringComparison.OrdinalIgnoreCase) => BuildQuickScan(AssessmentKind.Career),
                var p when p.Contains("/culture", StringComparison.OrdinalIgnoreCase) => BuildQuickScan(AssessmentKind.Culture),
                var p when p.Contains("/values", StringComparison.OrdinalIgnoreCase) => BuildQuickScan(AssessmentKind.Values),
                var p when p.Contains("/deep-analysis/payment-mode", StringComparison.OrdinalIgnoreCase) => new { mode = "stub" },
                var p when p.Contains("/deep-analysis", StringComparison.OrdinalIgnoreCase) => BuildDeep(),
                var p when p.Contains("/site/status", StringComparison.OrdinalIgnoreCase) => new { maintenance = false },
                _ => new { }
            };

            var json = JsonSerializer.Serialize(payload, Json);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        private static MeProfile BuildProfile()
            => new()
            {
                FullName = "Capture Mode",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                TestAiConsentAt = DateTime.UtcNow.AddDays(-1),
                TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
            };

        private static object BuildQuickScan(AssessmentKind kind)
        {
            var target = ActiveTarget;
            if (target.PaidDeep || target.Kind != kind)
            {
                return EmptyQuickScan(kind);
            }

            return kind switch
            {
                AssessmentKind.Career => new CandidateCareerInterestState
                {
                    Status = "Draft",
                    QuickScanVersion = AssessmentTestVersions.V3,
                    QuickScanFullCount = 25,
                    QuestionCount = 25,
                    Answers = BuildAnswers(kind, target.Answered)
                },
                AssessmentKind.Culture => new CandidateCultureState
                {
                    Status = "Draft",
                    QuickScanVersion = AssessmentTestVersions.V3,
                    QuickScanFullCount = 25,
                    Answers = BuildAnswers(kind, target.Answered)
                },
                AssessmentKind.Values => new CandidateValuesState
                {
                    Status = "Draft",
                    QuickScanVersion = AssessmentTestVersions.V3,
                    QuickScanFullCount = 25,
                    Answers = BuildAnswers(kind, target.Answered)
                },
                _ => new CandidateCompetencyState
                {
                    Status = "Draft",
                    QuickScanVersion = AssessmentTestVersions.V3,
                    QuickScanFullCount = 25,
                    QuestionCount = 25,
                    IsIndicativeDepth = true,
                    Answers = BuildAnswers(kind, target.Answered)
                }
            };
        }

        private static object EmptyQuickScan(AssessmentKind kind)
            => kind switch
            {
                AssessmentKind.Career => new CandidateCareerInterestState { Status = "Draft", QuickScanVersion = AssessmentTestVersions.V3, Answers = [] },
                AssessmentKind.Culture => new CandidateCultureState { Status = "Draft", QuickScanVersion = AssessmentTestVersions.V3, Answers = [] },
                AssessmentKind.Values => new CandidateValuesState { Status = "Draft", QuickScanVersion = AssessmentTestVersions.V3, Answers = [] },
                _ => new CandidateCompetencyState { Status = "Draft", QuickScanVersion = AssessmentTestVersions.V3, Answers = [] }
            };

        private static Dictionary<string, int> BuildAnswers(AssessmentKind kind, int answeredCount)
        {
            var ids = TestDepthRules.QuestionIdsFor(kind, TestDepthLevel.Full, AssessmentTestVersions.V3)
                .Take(Math.Clamp(answeredCount, 0, 25));
            return ids.ToDictionary(
                id => id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                id => 3 + (id % 2));
        }

        private static DeepAnalysisState BuildDeep()
        {
            var target = ActiveTarget;
            var answers = target.PaidDeep
                ? BuildDeepAnswers(target.Kind, target.Answered)
                : [];
            return new()
            {
                Kind = AssessmentKindLabels.ToSlug(target.Kind),
                IsUnlocked = target.PaidDeep,
                IsCompleted = false,
                QuestionCount = TestDepthRules.Bottom(target.Kind),
                PriceEuro = 2.99m,
                Answers = answers
            };
        }

        private static Dictionary<string, int> BuildDeepAnswers(AssessmentKind kind, int answeredCount)
        {
            var ids = TestDepthRules.Parts(kind)
                .SelectMany(p => p.QuestionIds)
                .Take(Math.Clamp(answeredCount, 0, TestDepthRules.Bottom(kind)));
            return ids.ToDictionary(
                id => id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                id => 3 + (id % 2));
        }
    }
}
