using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Dna;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public class PassportTabsTests
{
    [Theory]
    [InlineData(null, "dna")]
    [InlineData("dna", "dna")]
    [InlineData("wie-ben-ik", "dna")]
    [InlineData("tests", "tests")]
    [InlineData("functiefit", "fit")]
    [InlineData("carriere", "career")]
    [InlineData("bewijzen", "proof")]
    [InlineData("gegevens", "data")]
    [InlineData("profile", "data")]
    [InlineData("profiel", "data")]
    public void Normalize_maps_legacy_and_canonical(string? raw, string expected)
        => Assert.Equal(expected, PassportTabs.Normalize(raw));

    [Fact]
    public void Neighbor_wraps()
    {
        Assert.Equal(PassportTabs.Tests, PassportTabs.Neighbor(PassportTabs.Dna, 1));
        Assert.Equal(PassportTabs.Data, PassportTabs.Neighbor(PassportTabs.Dna, -1));
    }

    [Fact]
    public void Kompas_roundtrip_mappings()
    {
        Assert.Equal(CandidateKompasTabs.Profile, PassportTabs.ToKompasTab(PassportTabs.Data));
        Assert.Equal(PassportTabs.Data, PassportTabs.FromKompasTab(CandidateKompasTabs.Profile));
        Assert.Equal(PassportTabs.Tests, PassportTabs.FromKompasTab(CandidateKompasTabs.Tests));
    }
}

public class PassportRedirectsTests
{
    [Fact]
    public void ToClassicProfileUrl_maps_data_to_profile()
    {
        var url = PassportRedirects.ToClassicProfileUrl("https://acc.example/candidate/paspoort?tab=data");
        Assert.Contains("/candidate/profile", url, StringComparison.Ordinal);
        Assert.Contains("tab=profile", url, StringComparison.Ordinal);
    }

    [Fact]
    public void TryToPassportUrl_maps_tests_and_keeps_returnUrl()
    {
        var url = PassportRedirects.TryToPassportUrl(
            "https://acc.example/candidate/profile?tab=tests&returnUrl=%2Fvacancies%2F1");
        Assert.NotNull(url);
        Assert.Contains("/candidate/paspoort", url, StringComparison.Ordinal);
        Assert.Contains("tab=tests", url, StringComparison.Ordinal);
        Assert.Contains("returnUrl=", url, StringComparison.Ordinal);
    }

    [Fact]
    public void TryToPassportUrl_skips_classic_profile_tab_until_phase4()
    {
        Assert.Null(PassportRedirects.TryToPassportUrl("/candidate/profile?tab=profile"));
        Assert.True(PassportRedirects.IsClassicTabUntilPhase4("profile"));
    }
}

public class PassportShellRulesTests
{
    [Fact]
    public void Evaluate_earns_shells_from_counts()
    {
        var progress = PassportShellRules.Evaluate(
            completedTests: 3,
            extendedReportsCompleted: 1,
            hasOwnCvOrTrackedLobsyCv: true,
            applicationCount: 0,
            employersEnabled: true,
            remainingQuestionsNearestUnfinished: 8);

        Assert.Equal(6, progress.TotalVisible);
        Assert.Equal(4, progress.EarnedCount);
        Assert.Equal(8, progress.QuestionsUntilNextShell);
        Assert.Contains(progress.Shells, s => s.Code == PassportShellRules.ThreeTests && s.Earned);
        Assert.Contains(progress.Shells, s => s.Code == PassportShellRules.FirstApplication && !s.Earned);
    }

    [Fact]
    public void Evaluate_hides_first_application_when_employers_off()
    {
        var progress = PassportShellRules.Evaluate(4, 1, true, 2, employersEnabled: false, 0);
        Assert.Equal(5, progress.TotalVisible);
        Assert.DoesNotContain(progress.Shells, s => s.Code == PassportShellRules.FirstApplication);
        Assert.Equal(5, progress.EarnedCount);
    }
}

public class PassportMemberNumberTests
{
    [Fact]
    public void Format_is_stable_non_guid()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var a = PassportMemberNumber.Format(id);
        var b = PassportMemberNumber.Format(id);
        Assert.Equal(a, b);
        Assert.StartsWith("LB-", a, StringComparison.Ordinal);
        Assert.Equal(8, a.Length);
        Assert.DoesNotContain(id.ToString("N"), a, StringComparison.OrdinalIgnoreCase);
    }
}

public class CandidateDnaViewBuilderParityTests
{
    [Fact]
    public void Empty_state_has_four_ghost_cards_and_no_highlights()
    {
        var dna = new CandidateDnaSummary
        {
            Competencies = new CandidateDnaCompetencySummary { Status = "Draft", QuestionCount = 25 },
            CareerInterests = new CandidateDnaCareerSummary { Status = "Draft", QuestionCount = 25 },
            Culture = new CandidateDnaCultureSummary { Status = "Draft", QuestionCount = 18 },
            Values = new CandidateDnaValuesSummary { Status = "Draft", QuestionCount = 25 }
        };

        string T(string key) => key;
        string F(string key, params object[] args) => key + ":" + string.Join(",", args);

        var view = CandidateDnaViewBuilder.FromDnaSummary(dna, T, F);
        Assert.Equal(4, view.Cards.Count);
        Assert.Equal(0, view.CompletedCount);
        Assert.Empty(view.Highlights);
        Assert.All(view.Slides, s => Assert.True(s.IsGhost));
        Assert.Equal(0, CandidateDnaViewBuilder.ArcProgress(view.Cards[0]));
    }

    [Fact]
    public void Completed_competence_produces_highlight_and_full_arc()
    {
        var dna = new CandidateDnaSummary
        {
            Competencies = new CandidateDnaCompetencySummary
            {
                Status = "Completed",
                AnsweredCount = 25,
                QuestionCount = 25,
                Scores = new CompetencyScoreSet
                {
                    Samenwerken = 90,
                    Resultaatgerichtheid = 70,
                    Stressbestendigheid = 60,
                    Innovatie = 50,
                    Extraversie = 40
                },
                DeepCompleted = true
            },
            CareerInterests = new CandidateDnaCareerSummary { Status = "Draft", QuestionCount = 25 },
            Culture = new CandidateDnaCultureSummary { Status = "Draft", QuestionCount = 18 },
            Values = new CandidateDnaValuesSummary { Status = "Draft", QuestionCount = 25 }
        };

        var view = CandidateDnaViewBuilder.FromDnaSummary(dna, k => k, (k, a) => k);
        Assert.Equal(1, view.CompletedCount);
        Assert.Contains(view.Highlights, h => h.LabelKey == "Dna.HighlightStrongest");
        Assert.Equal(1d, CandidateDnaViewBuilder.ArcProgress(view.Cards[0]));
        Assert.True(view.Cards[0].Extended);
    }

    [Fact]
    public void EffectiveCompletenessPercent_prefers_kompas_then_dna()
    {
        Assert.Equal(40, CandidateDnaViewBuilder.EffectiveCompletenessPercent(
            new CandidateKompasState { ProfileCompletenessPercent = 40 },
            new CandidateDnaSummary { ProfileCompletenessPercent = 80 }));
        Assert.Equal(80, CandidateDnaViewBuilder.EffectiveCompletenessPercent(
            null,
            new CandidateDnaSummary { ProfileCompletenessPercent = 80 }));
        Assert.Equal(12, CandidateDnaViewBuilder.EffectiveCompletenessPercent(null, null, 12));
    }
}

public class PassportFeatureReflectionTests
{
    [Fact]
    public void Passport_page_requires_CandidatePassport_feature()
    {
        var page = typeof(Jobsy.Web.Components.Pages.Candidate.Passport);
        var attr = page.GetCustomAttributes(typeof(RequiresFeatureAttribute), inherit: true)
            .OfType<RequiresFeatureAttribute>()
            .Single(a => a.Feature == PlatformFeature.CandidatePassport);
        Assert.Equal("/candidate/profile", attr.FallbackPath);
    }
}
