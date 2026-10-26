using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CandidateExternalVacancyRulesTests
{
    [Fact]
    public void Fact_guard_rejects_invented_history_in_match_sanitize()
    {
        var sheet = CandidateFactSheet.Personal([], [], [], checkJobTitles: false);
        var result = CandidateExternalVacancyRules.SanitizeMatchInsights(
            ["Je werkte jaren in de zorg."],
            ["Geen uitdagingen"],
            sheet);
        Assert.Contains("Dat weet ik niet", result.Strengths[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Filter_match_insights_omits_external_placeholders()
    {
        var filtered = CandidateExternalVacancyRules.FilterMatchInsightsForDisplay(
            new Jobsy.Core.Contracts.ExternalVacancyMatchInsightsDto(
                [
                    CandidateExternalVacancyRules.MatchInsightCompetenciesPlaceholder,
                    "Echte match op teamwork"
                ],
                [CandidateExternalVacancyRules.MatchInsightUnknown]));

        Assert.Single(filtered.Strengths);
        Assert.Equal("Echte match op teamwork", filtered.Strengths[0]);
        Assert.Empty(filtered.Challenges);
    }

    [Fact]
    public void Rate_limit_constant_is_twenty_per_day()
    {
        Assert.Equal(20, CandidateExternalVacancyRules.MaxImportsPerUserPerDay);
    }

    [Fact]
    public void Suppression_matches_exact_email_and_domain()
    {
        var email = CandidateExternalVacancyRules.NormalizeEmployerEmail("Hr@Example.com");
        var domain = CandidateExternalVacancyRules.NormalizeDomain(email);
        Assert.True(CandidateExternalVacancyRules.IsSuppressed(
            email,
            [email],
            []));
        Assert.True(CandidateExternalVacancyRules.IsSuppressed(
            "other@" + domain,
            [],
            [domain]));
        Assert.False(CandidateExternalVacancyRules.IsSuppressed(
            email,
            [],
            ["other.nl"]));
    }
}
