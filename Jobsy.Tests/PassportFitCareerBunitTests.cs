using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Bunit;
using Jobsy.Core.Features;
using Jobsy.Web.Components.Candidate.Passport;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class PassportFitCareerBunitTests : BunitContext
{
    public PassportFitCareerBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<CareerPathService>();
    }

    [Fact]
    public void Fit_tab_hides_culture_without_label()
    {
        Services.AddSingleton<IFeatureFlags>(new FixedFlags(employers: true, passport: true));
        RegisterApi(UnlockedJson(cultureLabel: null));

        var cut = Render<PassportFitTab>(p => p
            .Add(x => x.Active, true)
            .Add(x => x.Snapshot, new CandidateKompasState()));

        Assert.DoesNotContain("passport-fit__culture\"", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("passport-fit__culture ", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Doe de cultuurscan", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Fit_tab_hides_vacancies_when_employers_off()
    {
        Services.AddSingleton<IFeatureFlags>(new FixedFlags(employers: false, passport: true));
        RegisterApi(UnlockedJson(cultureLabel: null));

        var cut = Render<PassportFitTab>(p => p
            .Add(x => x.Active, true)
            .Add(x => x.Snapshot, new CandidateKompasState
            {
                TopMatches =
                [
                    new CandidateMatchedVacancy
                    {
                        Id = Guid.NewGuid(),
                        Title = "Zorghulp",
                        CompanyName = "Acme",
                        MatchPercent = 80
                    }
                ]
            }));

        Assert.DoesNotContain("passport-fit__vacancies", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Vacatures die bij jou passen", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Fit_tab_four_results_use_band_not_hero_percent()
    {
        Services.AddSingleton<IFeatureFlags>(new FixedFlags(employers: true, passport: true));
        RegisterApi(UnlockedJson(cultureLabel: null));

        var cut = Render<PassportFitTab>(p => p
            .Add(x => x.Active, true)
            .Add(x => x.Snapshot, new CandidateKompasState()));

        Assert.Contains("Past goed", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Zorgzaam", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Diploma niveau 2", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Leren en werken (BBL)", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("role-fit-result__pct", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(">82%<", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Fit_tab_shows_culture_only_with_label()
    {
        Services.AddSingleton<IFeatureFlags>(new FixedFlags(employers: true, passport: true));
        RegisterApi(UnlockedJson(cultureLabel: "klein, warm team", culturePercent: 78));

        var cut = Render<PassportFitTab>(p => p
            .Add(x => x.Active, true)
            .Add(x => x.Snapshot, new CandidateKompasState()));

        Assert.Contains("passport-fit__culture", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("klein, warm team", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Doe de cultuurscan", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Career_tab_empty_state()
    {
        Services.AddSingleton<IFeatureFlags>(new FixedFlags(employers: false, passport: true));
        RegisterApi(roleFitJson: UnlockedJson(null), careerJson: "null");

        var cut = Render<PassportCareerTab>(p => p.Add(x => x.Active, true));

        Assert.Contains("Kies je droombaan", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("/carriere", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Vacatures voor deze stap", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Career_tab_plan_hides_vacancy_link_when_employers_off()
    {
        Services.AddSingleton<IFeatureFlags>(new FixedFlags(employers: false, passport: true));
        RegisterApi(roleFitJson: UnlockedJson(null), careerJson: SampleCareerJson());

        var cut = Render<PassportCareerTab>(p => p.Add(x => x.Active, true));

        Assert.Contains("MBO-verpleegkundige", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("passport-career__shell", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("is-current", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Diploma", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Vacatures voor deze stap", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Career_tab_shows_vacancy_link_when_employers_on()
    {
        Services.AddSingleton<IFeatureFlags>(new FixedFlags(employers: true, passport: true));
        RegisterApi(roleFitJson: UnlockedJson(null), careerJson: SampleCareerJson());

        var cut = Render<PassportCareerTab>(p => p.Add(x => x.Active, true));

        Assert.Contains("Vacatures voor deze stap", cut.Markup, StringComparison.Ordinal);
    }

    private void RegisterApi(string roleFitJson, string? careerJson = null)
    {
        var handler = new FakeHandler(roleFitJson, careerJson ?? "null");
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(new JobsyApiClient(http));
    }

    private static string UnlockedJson(string? cultureLabel, int? culturePercent = null)
    {
        var culture = cultureLabel is null
            ? "null"
            : JsonSerializer.Serialize(cultureLabel);
        var pct = culturePercent is int p ? p.ToString() : "null";
        return $$"""
            {
              "isUnlocked": true,
              "lockMessage": "",
              "lastResult": {
                "jobTitle": "Helpende zorg & welzijn",
                "matchPercent": 82,
                "strengths": ["Zorgzaam", "Geduldig"],
                "gaps": ["Diploma niveau 2"],
                "actionSteps": ["Leren en werken (BBL)"],
                "searchKeys": ["helpende", "zorg"],
                "similarRoles": [],
                "directVacancies": [],
                "formalItems": [],
                "trainingOffers": [],
                "cultureFitLabel": {{culture}},
                "cultureFitPercent": {{pct}}
              }
            }
            """;
    }

    private static string SampleCareerJson()
        => """
            {
              "dreamTitle": "MBO-verpleegkundige",
              "matchPercent": 40,
              "matchSummary": "test",
              "goalReached": false,
              "steps": [
                {
                  "id": "a",
                  "order": 1,
                  "title": "Zorghulp",
                  "status": "Completed",
                  "skillsGap": [],
                  "courses": [],
                  "courseStatuses": [],
                  "minRequirements": [],
                  "stepMatchPercent": 70
                },
                {
                  "id": "b",
                  "order": 2,
                  "title": "Helpende",
                  "status": "Active",
                  "skillsGap": ["Diploma"],
                  "courses": ["Cursus X"],
                  "courseStatuses": [{ "name": "Cursus X", "onProfile": false }],
                  "minRequirements": [],
                  "actionHref": "/?q=helpende",
                  "actionLabel": "Zoek",
                  "stepMatchPercent": 70
                }
              ]
            }
            """;

    private sealed class FakeHandler(string roleFitJson, string careerJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            string body = "[]";
            if (path.Contains("role-fit", StringComparison.OrdinalIgnoreCase))
            {
                body = roleFitJson;
            }
            else if (path.Contains("career-path", StringComparison.OrdinalIgnoreCase)
                     || path.Contains("career", StringComparison.OrdinalIgnoreCase))
            {
                body = careerJson;
            }
            else if (path.Contains("talent-contacts", StringComparison.OrdinalIgnoreCase))
            {
                body = "[]";
            }
            else if (path.Contains("training", StringComparison.OrdinalIgnoreCase)
                     || path.Contains("passport", StringComparison.OrdinalIgnoreCase))
            {
                body = "[]";
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, "t"), new Claim(ClaimTypes.Role, "Candidate")], "t"))));
    }

    private sealed class FixedFlags(bool employers, bool passport) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(employers, passport));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature switch
            {
                PlatformFeature.Employers => employers,
                PlatformFeature.CandidatePassport => passport,
                _ => false
            });

        public void Invalidate()
        {
        }
    }
}
