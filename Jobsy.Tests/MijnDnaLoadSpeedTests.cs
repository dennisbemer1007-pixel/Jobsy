using System.Text.Json;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Tests.Uat;

namespace Jobsy.Tests;

public class MijnDnaLoadSpeedTests
{
    [Fact]
    public void Dna_summary_dto_has_no_questions_or_answers_payload()
    {
        var dto = new CandidateDnaSummaryDto(
            new WhoAmIStorySummaryDto("Story", ["kw"], DateTime.UtcNow, "Ready"),
            50,
            new CandidateDnaCompetencySummaryDto("Completed", 25, 25, new CompetencyScores(1, 2, 3, 4, 5), null, false),
            new CandidateDnaCareerSummaryDto("Draft", 3, 25, null, new RiasecScores(1, 2, 3, 4, 5, 6), false),
            new CandidateDnaCultureSummaryDto("Draft", 0, 18, null, null, false),
            new CandidateDnaValuesSummaryDto("Draft", 0, 25, null, null, false));

        var json = JsonSerializer.Serialize(dto);
        Assert.DoesNotContain("\"questions\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"answers\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DeepAnalysisQuestionDto", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Profile_persists_slim_dna_not_full_kompas()
    {
        var root = RepoRoot.Find();
        var profile = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/Profile.razor"));
        Assert.Contains("candidateProfileDna", profile, StringComparison.Ordinal);
        Assert.Contains("PersistAsJson(DnaPersistKey, _dnaState)", profile, StringComparison.Ordinal);
        Assert.DoesNotContain("candidateProfileKompas", profile, StringComparison.Ordinal);
        Assert.DoesNotContain("PersistAsJson(PersistKey, _kompasState)", profile, StringComparison.Ordinal);
        Assert.Contains("GetMyKompasDnaResultAsync", profile, StringComparison.Ordinal);
        Assert.Contains("EnsureFullKompasLoadedAsync", profile, StringComparison.Ordinal);
    }

    [Fact]
    public void Dna_endpoint_and_client_are_wired()
    {
        var root = RepoRoot.Find();
        var controller = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/CandidateKompasController.cs"));
        Assert.Contains("HttpGet(\"dna\")", controller, StringComparison.Ordinal);
        Assert.Contains("GetDnaAsync", controller, StringComparison.Ordinal);

        var client = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Services/ApiClient/JobsyApiClient.cs"));
        Assert.Contains("KompasDnaCacheKey", client, StringComparison.Ordinal);
        Assert.Contains("api/me/kompas/dna", client, StringComparison.Ordinal);

        var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/DnaPanel.razor"));
        Assert.Contains("GetMyKompasDnaResultAsync", panel, StringComparison.Ordinal);
    }
}
