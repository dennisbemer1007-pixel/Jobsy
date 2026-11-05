using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bunit;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Components.Leerling;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;
using Jobsy.Tests.TestSupport;

namespace Jobsy.Tests.Scholen;

public class PupilPassportPolishBunitTests : JobsyBunitContext

{
    private readonly DelayedResultHandler _handler = new();

    public PupilPassportPolishBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IFeatureFlags>(new PassportOn());
        Services.AddSingleton(new JobsyApiClient(new HttpClient(_handler)
        {
            BaseAddress = new Uri("http://api.test/")
        }));
        Services.AddHttpContextAccessor();
        Services.AddAntiforgery();
    }

    [Fact]
    public async Task Paspoort_page_binds_display_name_not_literal_parameter_name()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        _handler.PassportJson = Json(MinimalPassport("Noor"));

        var cut = Render<LeerlingPaspoortG78View>(ps => ps
            .Add(p => p.Model, MinimalPassport("Noor"))
            .Add(p => p.AllowNameEdit, true)
            .Add(p => p.DisplayName, "Noor"));

        Assert.Contains("data-testid=\"leerling-paspoort-g78\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"leerling-paspoort-dislikes\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Liever niet", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("lp-g78__strip", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("value=\"Noor\"", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Paspoort_can_be_opened_twice_without_session_lost_markup()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        _handler.PassportJson = Json(MinimalPassport(null));

        var first = Render<LeerlingPaspoortG78View>(ps => ps.Add(p => p.Model, MinimalPassport(null)));
        Assert.Contains("data-testid=\"leerling-paspoort-g78\"", first.Markup, StringComparison.Ordinal);

        var second = Render<LeerlingPaspoortG78View>(ps => ps.Add(p => p.Model, MinimalPassport(null)));
        Assert.Contains("data-testid=\"leerling-paspoort-g78\"", second.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Je sessie is verlopen", second.Markup, StringComparison.Ordinal);
    }

    private static PupilG78PassportWebDto MinimalPassport(string? name)
        => new(
            Enabled: true,
            DisplayName: name,
            DisplayCode: "K7Q-M2P",
            ClassLabel: "8A",
            SchoolName: "School",
            DateLabel: "14 okt 2026",
            StoryBody: "Jij helpt graag.",
            Worlds: [],
            Likes: [],
            Dislikes: [],
            Strengths: [],
            OccupationCards: [],
            DiscoveryQuestions: [],
            HomeClassTips:
            [
                PupilPassportCopy.Get("LeerlingPassport.Tip.Letter.I.1"),
                PupilPassportCopy.Get("LeerlingPassport.Tip.Generic.2")
            ],
            DreamJobTitle: "Dierenarts",
            RouteSteps: [],
            DreamHaveItems: [],
            DreamLearnItems: []);

    private static string Json<T>(T value)
        => JsonSerializer.Serialize(value, ApiJson);

    private static readonly JsonSerializerOptions ApiJson = CreateApiJson();

    private static JsonSerializerOptions CreateApiJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed class DelayedResultHandler : HttpMessageHandler
    {
        public string? ResultJson { get; set; }
        public string? PassportJson { get; set; }
        public int ResultDelayMs { get; set; }
        public int PassportCalls { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Equals("/api/pupil/result", StringComparison.OrdinalIgnoreCase))
            {
                if (ResultDelayMs > 0)
                {
                    await Task.Delay(ResultDelayMs, cancellationToken);
                }

                return JsonResponse(ResultJson ?? "{}");
            }

            if (path.StartsWith("/api/pupil/passport/g78", StringComparison.OrdinalIgnoreCase))
            {
                PassportCalls++;
                return JsonResponse(PassportJson ?? """{"enabled":false}""");
            }

            return JsonResponse("{}");
        }

        private static HttpResponseMessage JsonResponse(string json)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
    }

    private sealed class PassportOn : IFeatureFlags
    {
        private static readonly FeatureFlagSnapshot Snap = new(
            false,
            false,
            SchoolsEnabled: true,
            PupilPassportV2Enabled: true);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
