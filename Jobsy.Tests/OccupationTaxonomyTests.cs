using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;
using Jobsy.Tests.Uat;

namespace Jobsy.Tests;

public class OccupationTaxonomyTests
{
    [Fact]
    public void Pilot_similar_roles_stay_in_aviation_even_when_soft_skills_point_elsewhere()
    {
        var similar = RoleFitFunnel.SuggestSimilar(
            "piloot",
            new RiasecScores(40, 90, 20, 95, 80, 30));

        Assert.NotEmpty(similar);
        Assert.All(similar, role => Assert.Contains("luchtvaart", role.Why, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(similar, role => role.Title.Contains("Cabine", StringComparison.OrdinalIgnoreCase)
            || role.Title.Contains("Vliegtuig", StringComparison.OrdinalIgnoreCase)
            || role.Title.Contains("Grond", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(similar, role => role.Title.Contains("lab", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(similar, role => role.Title.Contains("café", StringComparison.OrdinalIgnoreCase)
            || role.Title.Contains("cafe", StringComparison.OrdinalIgnoreCase)
            || role.Title.Contains("horeca", StringComparison.OrdinalIgnoreCase));
        Assert.False(OccupationTaxonomy.VacancySharesDomain("piloot", "Café-hulp"));
        Assert.False(OccupationTaxonomy.VacancySharesDomain("piloot", "Laboratoriumassistent"));
        Assert.True(OccupationTaxonomy.VacancySharesDomain("piloot", "Cabinemedewerker Schiphol"));
    }

    [Fact]
    public void Specialist_path_states_six_years_and_drops_steps_the_candidate_already_has()
    {
        var full = CareerPathPlanner.ForTitle("Verpleegkundig specialist");
        Assert.NotNull(full);
        Assert.Equal(72, full!.TotalMonths);
        Assert.Equal("6 jaar", full.DurationLabel);
        Assert.Contains("binnen 6 jaar", full.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.True(full.Steps.Count >= 2);

        var done = CareerPathPlanner.Personalize(
            "Verpleegkundige",
            new CandidatePreferencesDto(
                [],
                null,
                null,
                Educations: ["HBO Verpleegkunde"],
                Certificates: [new CandidateCertificateDto("BIG-registratie")]),
            formal: null);
        Assert.NotNull(done);
        Assert.Equal(0, done!.TotalMonths);
        Assert.Contains("nu bereiken", done.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Missing_medical_and_licence_are_dealbreakers()
    {
        var req = VacancyBarrierCatalog.Normalize(
            VacancyBarrierKind.High,
            ["ATPL"],
            ["Vliegbrevet"],
            null,
            null,
            ["medical", "eye", "fitness"]);
        var missing = VacancyBarrierCatalog.Evaluate(req, new CandidatePreferencesDto([], null, null));
        Assert.Contains(missing.Items, item => item.Key == "hard:medical" && !item.Met && item.IsDealbreaker);
        Assert.Contains(missing.Items, item => item.Key == "hard:eye" && item.IsDealbreaker);
        Assert.Contains(missing.Items, item => item.Label.Contains("Vliegbrevet", StringComparison.Ordinal) && item.IsDealbreaker);

        var ready = VacancyBarrierCatalog.Evaluate(
            req,
            new CandidatePreferencesDto(
                [],
                null,
                null,
                Educations: ["ATPL"],
                Certificates:
                [
                    new CandidateCertificateDto("Medische keuring klasse 1"),
                    new CandidateCertificateDto("Ogentest"),
                    new CandidateCertificateDto("Fitheidstest"),
                    new CandidateCertificateDto("Vliegbrevet")
                ]));
        Assert.DoesNotContain(ready.Items, item => item.Key.StartsWith("hard:", StringComparison.Ordinal) && !item.Met);
        Assert.Contains(ready.Items, item => item.Key == "hard:medical" && item.Met);

        var json = VacancyBarrierCatalog.Serialize(req);
        Assert.Contains("medical", json, StringComparison.Ordinal);
        var again = VacancyBarrierCatalog.Deserialize(json);
        Assert.Equal(["eye", "fitness", "medical"], again.HardChecks.OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Fit_checker_shows_path_duration_and_dealbreaker_badge()
    {
        var snapshot = RoleFitCheckBuilder.Build(
            "piloot",
            new CompetencyScores(70, 80, 75, 60),
            new RiasecScores(80, 70, 30, 40, 55, 45),
            fromDeepAnalysis: false);
        Assert.NotNull(snapshot.CareerPath);
        Assert.Contains(snapshot.ActionSteps, step => step.Contains("binnen", StringComparison.OrdinalIgnoreCase)
            && step.Contains("deze vacature bereiken", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(snapshot.SimilarRoles ?? [], role => role.Title.Contains("horeca", StringComparison.OrdinalIgnoreCase));

        var root = RepoRoot.Find();
        var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/RoleFitCheckPanel.razor"));
        Assert.Contains("Fit.PathTitle", panel, StringComparison.Ordinal);
        Assert.Contains("Fit.Dealbreaker", panel, StringComparison.Ordinal);
        var create = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Branch/CreateVacancy.razor"));
        Assert.Contains("VacancyHardCheckCatalog", create, StringComparison.Ordinal);
        var detail = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/VacancyDetail.razor"));
        Assert.Contains("Vacancy.PathTitle", detail, StringComparison.Ordinal);
        Assert.Contains("BarrierHardChecks", detail, StringComparison.Ordinal);
    }
}
