using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Xunit;

namespace Jobsy.Tests;

public class EmployerFitQuestionnaireTests
{
    [Fact]
    public void ToCultureSliders_maps_core_questions()
    {
        var sliders = EmployerFitQuestionnaireCatalog.ToCultureSliders(new Dictionary<string, int>
        {
            ["Autonomy"] = 4,
            ["Collaboration"] = 2
        });
        Assert.Equal(4, sliders["Autonomy"]);
        Assert.Equal(2, sliders["Collaboration"]);
    }

    [Fact]
    public void VacancyMatchProfileResolver_prefers_explicit_competencies()
    {
        var inferred = VacancyCompetencyProfile.Infer(["Logistiek"], "Magazijnmedewerker", "team");
        var extras = new VacancyMatchExtrasSnapshot(
            CompetencyTargets: new VacancyCompetencyTargetsSnapshot(Samenwerken: 90),
            UseInferredCompetencies: false);
        var resolved = VacancyMatchProfileResolver.Resolve(
            ["Logistiek"],
            "Magazijnmedewerker",
            "team",
            extras,
            null,
            null);
        Assert.True(resolved.VacancyCompetenciesExplicit);
        Assert.Equal(90, resolved.VacancyCompetencies.Samenwerken);
        Assert.True(resolved.VacancyCompetencies.Samenwerken > (inferred.Samenwerken ?? 0));
    }

    [Fact]
    public void VacancyMatchProfileResolver_falls_back_to_infer()
    {
        var resolved = VacancyMatchProfileResolver.Resolve(
            ["Horeca"],
            "Bediening",
            "gastvrij",
            VacancyMatchExtrasSnapshot.Empty,
            null,
            null);
        Assert.False(resolved.VacancyCompetenciesExplicit);
        Assert.NotNull(resolved.VacancyCompetencies.Samenwerken);
    }

    [Fact]
    public void WorkContext_hard_mismatch_outdoor()
    {
        var candidate = new CandidateWorkContextSnapshot(Outdoor: "rather-not");
        var vacancy = new VacancyWorkContextSnapshot(IndoorOutdoor: "prefer-outdoor");
        var result = WorkContextMatchRules.Evaluate(candidate, vacancy);
        Assert.True(result.HardMismatch);
    }

    [Fact]
    public void WorkContext_no_mismatch_when_vacancy_unknown()
    {
        var candidate = new CandidateWorkContextSnapshot(Outdoor: "rather-not");
        var result = WorkContextMatchRules.Evaluate(candidate, null);
        Assert.False(result.HardMismatch);
    }

    [Fact]
    public void Regional_manager_cannot_mutate_employer_fit_save()
    {
        Assert.False(JobsyRoles.CanMutateEmployerData(UserRole.RegionalManager));
        Assert.True(JobsyRoles.CanMutateEmployerData(UserRole.BranchManager));
    }

    [Fact]
    public void MatchCalibration_raises_confidence_when_both_sides_answered()
    {
        var input = new ProfileVacancyMatchInput
        {
            Core = new MatchScoreInput(),
            VacancyTitle = "Test",
            EmployerFitQuestionnairesEnabled = true,
            VacancyCompetenciesFromEmployer = true,
            VacancyWorkContextFromEmployer = true,
            CandidateCompetencies = new CompetencyScores(70, 70, 70, 70),
            CandidateWorkContext = new CandidateWorkContextSnapshot(Pace: "norm-ok"),
            VacancyWorkContext = new VacancyWorkContextSnapshot(Pace: "norm-ok")
        };
        var legacy = ProfileVacancyMatchCalculator.CalculateLegacy(input);
        var without = MatchCalibrationV1Calculator.Compute(input, legacy);
        var baselineInput = new ProfileVacancyMatchInput
        {
            Core = input.Core,
            VacancyTitle = input.VacancyTitle,
            EmployerFitQuestionnairesEnabled = true,
            CandidateCompetencies = input.CandidateCompetencies,
            CandidateWorkContext = input.CandidateWorkContext,
            VacancyWorkContext = input.VacancyWorkContext
        };
        var baseline = MatchCalibrationV1Calculator.Compute(baselineInput, legacy);
        Assert.True(without.ConfidencePercent > baseline.ConfidencePercent);
    }
}
