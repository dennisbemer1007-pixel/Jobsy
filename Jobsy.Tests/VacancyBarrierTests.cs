using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;
using Jobsy.Tests.Uat;

namespace Jobsy.Tests;

public class VacancyBarrierTests
{
    [Fact]
    public void Low_barrier_without_papers_hides_the_formal_block()
    {
        var req = VacancyBarrierCatalog.Normalize(VacancyBarrierKind.Low, null, null, null, null);
        Assert.False(VacancyBarrierCatalog.HasFormalRequirements(req));
        Assert.False(VacancyBarrierCatalog.ShowFormalBlock(req));
        Assert.Null(VacancyBarrierCatalog.Serialize(req));
    }

    [Fact]
    public void High_barrier_nursing_flags_missing_big_and_keeps_met_diploma()
    {
        var req = VacancyBarrierCatalog.Normalize(
            VacancyBarrierKind.High,
            ["MBO Verpleegkunde"],
            ["BIG", "VCA"],
            2,
            2000);
        var prefs = new CandidatePreferencesDto(
            [],
            30,
            "Fiets",
            Educations: ["MBO Verpleegkunde"],
            Certificates: [new CandidateCertificateDto("VCA vol", 2022)],
            Employers: [new CandidateEmployerHistoryDto("Zorggroep", Years: 3)]);
        var check = VacancyBarrierCatalog.Evaluate(req, prefs);
        Assert.True(check.HasFormalRequirements);
        Assert.Contains(check.Items, i => i.Key.StartsWith("diploma", StringComparison.Ordinal) && i.Met);
        Assert.Contains(check.Items, i => i.Label.Contains("BIG", StringComparison.Ordinal) && !i.Met);
        Assert.Contains(check.Items, i => i.Key == "years" && i.Met);
        Assert.True(VacancyBarrierCatalog.ShowFormalBlock(req));

        var fit = RoleFitCheckBuilder.BuildVacancyFit(
            Guid.NewGuid(),
            req,
            check,
            new CultureFitResult(80, "high", "Cultuur Fit: Hoog", "Past bij een zorgzaam team.", false),
            availabilityOk: true);
        Assert.True(fit.ShowUpskill);
        Assert.True(fit.ShowFormalBlock);
    }

    [Fact]
    public void Upskill_stays_off_when_culture_is_too_low()
    {
        var req = VacancyBarrierCatalog.Normalize(VacancyBarrierKind.High, null, ["BIG"], null, null);
        var check = VacancyBarrierCatalog.Evaluate(req, new CandidatePreferencesDto([], null, null));
        var fit = RoleFitCheckBuilder.BuildVacancyFit(
            Guid.NewGuid(),
            req,
            check,
            new CultureFitResult(40, "low", "Cultuur Fit: Laag", "De sfeer botst nog.", false),
            availabilityOk: true);
        Assert.False(fit.ShowUpskill);
    }

    [Fact]
    public void Roundtrip_json_keeps_high_barrier_fields()
    {
        var req = VacancyBarrierCatalog.Normalize(VacancyBarrierKind.High, ["WO"], ["Vliegbrevet"], 5, 4000);
        var json = VacancyBarrierCatalog.Serialize(req);
        Assert.Contains("vliegbrevet", json, StringComparison.OrdinalIgnoreCase);
        var again = VacancyBarrierCatalog.Deserialize(json);
        Assert.Equal(VacancyBarrierKind.High, again.Barrier);
        Assert.Equal(["WO"], again.Diplomas);
        Assert.Equal(5, again.MinExperienceYears);
    }

    [Fact]
    public void Pages_wire_vacancy_fit_checklist_and_employer_barrier()
    {
        var root = RepoRoot.Find();
        var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/RoleFitCheckPanel.razor"));
        Assert.Contains("Fit.CultureBlock", panel, StringComparison.Ordinal);
        Assert.Contains("Fit.FormalBlock", panel, StringComparison.Ordinal);
        Assert.Contains("ShowUpskill", panel, StringComparison.Ordinal);
        Assert.Contains("Fit.Step1", panel, StringComparison.Ordinal);
        Assert.Contains("Fit.DirectTitle", panel, StringComparison.Ordinal);

        var create = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Branch/CreateVacancy.razor"));
        Assert.Contains("Employer.Barrier.High", create, StringComparison.Ordinal);
        Assert.Contains("BarrierRequirements", File.ReadAllText(Path.Combine(root, "Jobsy.Core/Entities/Vacancy.cs")), StringComparison.Ordinal);

        var controller = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/RoleFitCheckController.cs"));
        Assert.Contains("VacancyId", controller, StringComparison.Ordinal);

        var service = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/RoleFitCheckService.cs"));
        Assert.Contains("NormalizeTitle(vacancy.Title)", service, StringComparison.Ordinal);
        Assert.Contains("ICultureFitAiService", service, StringComparison.Ordinal);
    }
}
