using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class RoleFitFunnelTests
{
    [Fact]
    public void Overall_percent_blends_travel_culture_and_formal_gaps()
    {
        var req = VacancyBarrierCatalog.Normalize(VacancyBarrierKind.High, null, ["BIG"], null, null);
        var formal = VacancyBarrierCatalog.Evaluate(req, new CandidatePreferencesDto([], null, null));
        var percent = RoleFitFunnel.CombineOverall(80, availabilityOk: true, culturePercent: 70, formal);
        Assert.InRange(percent, 40, 70);
        Assert.True(percent < 80);
    }

    [Fact]
    public void Juf_suggests_classroom_stepping_stones()
    {
        var similar = RoleFitFunnel.SuggestSimilar("juf", new RiasecScores(20, 20, 40, 90, 30, 25));
        Assert.Contains(similar, r => r.Title.Contains("Onderwijsassistent", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(similar, r => r.Title.Contains("Pedagogisch", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Nurse_title_suggests_helpende_as_stepping_stone()
    {
        var similar = RoleFitFunnel.SuggestSimilar(
            "Verpleegkundige",
            new RiasecScores(20, 30, 25, 95, 40, 35));
        Assert.Contains(similar, r => r.Title.Contains("Helpende", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(similar, r => r.Title.Contains("Verpleegkundige", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Direct_start_requires_met_papers_and_availability()
    {
        var req = VacancyBarrierCatalog.Normalize(VacancyBarrierKind.High, null, ["BIG"], null, null);
        var missing = VacancyBarrierCatalog.Evaluate(req, new CandidatePreferencesDto([], null, null));
        Assert.False(RoleFitFunnel.CanStartImmediately(true, 80, true, missing, true));

        var ready = VacancyBarrierCatalog.Evaluate(
            req,
            new CandidatePreferencesDto([], null, null, Certificates: [new CandidateCertificateDto("BIG")]));
        Assert.True(RoleFitFunnel.CanStartImmediately(true, 80, true, ready, true));
        Assert.False(RoleFitFunnel.CanStartImmediately(true, 80, true, ready, availabilityOk: false));
    }

    [Fact]
    public void Similar_roles_roundtrip_in_role_fit_json_without_pii()
    {
        var snapshot = RoleFitCheckBuilder.Sanitize(new RoleFitCheckSnapshot(
            "Verpleegkundige",
            70,
            ["Je werkt graag met mensen."],
            ["BIG ontbreekt nog."],
            ["Volg een korte cursus of omscholing om dit gat te dichten."],
            ["zorg"],
            false,
            SimilarRoles: [new RoleFitSimilarRole("test@example.com", "why", 90), new RoleFitSimilarRole("Helpende zorg", "Opstap zonder BIG.", 88)]));
        var json = RoleFitCheckJson.Serialize(snapshot);
        Assert.DoesNotContain("@", json, StringComparison.Ordinal);
        var again = RoleFitCheckJson.TryDeserialize(json, "Verpleegkundige", false);
        Assert.Contains(again!.SimilarRoles ?? [], r => r.Title.Contains("Helpende", StringComparison.OrdinalIgnoreCase));
    }
}
