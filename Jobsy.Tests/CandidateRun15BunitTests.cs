using Bunit;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Pages.Candidate.DeepReport;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class CandidateRun15BunitTests : BunitContext
{
    public CandidateRun15BunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth("Candidate"));
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Career_report_shows_the_recomputed_percent_without_a_formula_dump()
    {
        var scores = new RiasecScores(66, 38, 37, 64, 43, 62);
        var report = new CareerDeepReport
        {
            HollandCode = "RSC",
            Domains =
            [
                new DeepDomainScore { Domain = CareerTestCatalog.Realistic, Score = 66 },
                new DeepDomainScore { Domain = CareerTestCatalog.Investigative, Score = 38 },
                new DeepDomainScore { Domain = CareerTestCatalog.Artistic, Score = 37 },
                new DeepDomainScore { Domain = CareerTestCatalog.Social, Score = 64 },
                new DeepDomainScore { Domain = CareerTestCatalog.Enterprising, Score = 43 },
                new DeepDomainScore { Domain = CareerTestCatalog.Conventional, Score = 62 }
            ],
            Occupations =
            [
                new DeepOccupationFit { TitleNl = "Dierenverzorger", MatchPercent = 65, ReasonNl = "Oud 65%." }
            ]
        };
        var shown = CareerDeepReportBuilder.ShownOccupations(report, "mbo");
        Assert.NotEmpty(shown);
        var cut = Render<CareerDeepReportView>(parameters => parameters
            .Add(view => view.Report, report)
            .Add(view => view.EducationLabel, "mbo"));
        var markup = cut.Markup;
        Assert.Contains(shown[0].TitleNl, markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(CareerCompassBuilder.FormatPercent(shown[0].MatchPercent, "nl"), markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Oud 65%", markup, StringComparison.Ordinal);
        Assert.DoesNotMatch(new System.Text.RegularExpressions.Regex(@"\d+[.,]\d+ / \d+"), markup);
        Assert.DoesNotContain("ESCO id", markup, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeAuth(string role) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role)],
                    "test"))));
    }
}
