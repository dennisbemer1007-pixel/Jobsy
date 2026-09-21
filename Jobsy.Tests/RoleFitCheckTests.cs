using Jobsy.Core.Rules;
using Jobsy.Tests.Uat;

namespace Jobsy.Tests;

public class RoleFitCheckTests
{
    [Fact]
    public void Gate_copy_and_upsell_match_the_product_spec()
    {
        Assert.Equal(
            "Ontgrendel de Functie-Fit Checker door eerst je korte competentie- en beroepentest in te vullen (ca. 3 minuten).",
            RoleFitCheckCopy.Locked);
        Assert.Contains("€ 2,99", RoleFitCheckCopy.DeepUpsell, StringComparison.Ordinal);
        Assert.Contains("150-vragen", RoleFitCheckCopy.DeepUpsell, StringComparison.Ordinal);
        Assert.Equal(RoleFitCheckCopy.Locked, UiStringsNl("Fit.Locked"));
        Assert.Equal(RoleFitCheckCopy.DeepUpsell, UiStringsNl("Fit.DeepUpsell"));
        Assert.Equal("Mijn profiel", UiStringsNl("Kompas.TabProfile"));
        Assert.Equal("Mijn competenties", UiStringsNl("Kompas.TabCompetencies"));
        Assert.Equal("Mijn beroepen", UiStringsNl("Kompas.TabCareers"));
        Assert.Equal("Past dit bij mij?", UiStringsNl("Kompas.TabFit"));
    }

    [Fact]
    public void Normalize_title_rejects_pii_and_jargon()
    {
        Assert.Null(RoleFitCheckBuilder.NormalizeTitle(" "));
        Assert.Null(RoleFitCheckBuilder.NormalizeTitle("a"));
        Assert.Null(RoleFitCheckBuilder.NormalizeTitle("test@example.com"));
        Assert.Null(RoleFitCheckBuilder.NormalizeTitle("RIASEC coach"));
        Assert.Equal("Verpleegkundige", RoleFitCheckBuilder.NormalizeTitle("  Verpleegkundige  "));
    }

    [Fact]
    public void Local_fit_for_nurse_is_a_clear_indication_without_jargon()
    {
        var snapshot = RoleFitCheckBuilder.Build(
            "Verpleegkundige",
            new CompetencyScores(88, 70, 72, 40),
            new RiasecScores(20, 30, 25, 95, 40, 35),
            fromDeepAnalysis: false);

        Assert.InRange(snapshot.MatchPercent, 60, 100);
        Assert.NotEmpty(snapshot.Strengths);
        Assert.NotEmpty(snapshot.Gaps);
        Assert.NotEmpty(snapshot.ActionSteps);
        Assert.Contains(snapshot.SimilarRoles ?? [], r => r.Title.Contains("Helpende", StringComparison.OrdinalIgnoreCase));
        Assert.True(snapshot.ShowDeepUpsell);
        Assert.False(snapshot.FromOpenAi);
        Assert.Contains(snapshot.SearchKeys, k => k.Contains("zorg", StringComparison.OrdinalIgnoreCase)
            || k.Contains("verpleeg", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("q=", BuildMapHref(snapshot.MapQuery), StringComparison.Ordinal);
        AssertNoJargon(snapshot);
        Assert.Contains(snapshot.ActionSteps, s => s.Contains("banenkaart", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(snapshot.ActionSteps, s => s == TrainingCopy.GapAdvice);
        Assert.Contains(snapshot.ActionSteps, s => s.Contains("150-vragen", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Deep_analysis_hides_upsell_and_tightens_the_plan()
    {
        var snapshot = RoleFitCheckBuilder.Build(
            "Verpleegkundige",
            new CompetencyScores(88, 70, 72, 40),
            new RiasecScores(20, 30, 25, 95, 40, 35),
            fromDeepAnalysis: true);
        Assert.False(snapshot.ShowDeepUpsell);
        Assert.DoesNotContain(snapshot.ActionSteps, s => s.Contains("150-vragen", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(snapshot.ActionSteps, s => s.Contains("loopbaanrapport", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OpenAi_prompt_sends_job_title_and_scores_without_pii_or_jargon()
    {
        var user = RoleFitCheckPrompt.User(
            "Verpleegkundige",
            new CompetencyScores(80, 70, 75, 40),
            new RiasecScores(20, 25, 20, 90, 30, 25),
            fromDeepAnalysis: false,
            maxTravelMinutes: 30,
            transport: "E-bike",
            licenses: ["B"],
            roles: ["Zorg"]);
        Assert.Contains("Verpleegkundige", user, StringComparison.Ordinal);
        Assert.Contains("Samenwerken", user, StringComparison.Ordinal);
        Assert.Contains("Mensen helpen", user, StringComparison.Ordinal);
        Assert.DoesNotContain("@", user, StringComparison.Ordinal);
        Assert.False(CareerCompassBuilder.ContainsForbiddenJargon(user));
        Assert.Contains("similarRoles", RoleFitCheckPrompt.System, StringComparison.Ordinal);
        Assert.Contains("Jip-en-Janneke", RoleFitCheckPrompt.System, StringComparison.Ordinal);
        Assert.Contains("Volg een korte cursus", RoleFitCheckPrompt.System, StringComparison.Ordinal);
        Assert.Contains("extraversie", RoleFitCheckPrompt.System, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pages_wire_tabs_and_gated_fit_checker()
    {
        var root = RepoRoot.Find();
        var kompas = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateKompas.razor"));
        Assert.Contains("CandidateKompasTabs.Fit", kompas, StringComparison.Ordinal);
        Assert.Contains("RoleFitCheckPanel", kompas, StringComparison.Ordinal);
        Assert.Contains("kompas-tab-fit", kompas, StringComparison.Ordinal);
        Assert.Contains("CareerCompassPanel", kompas, StringComparison.Ordinal);

        var profile = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/Profile.razor"));
        Assert.Contains("<CandidateKompas", profile, StringComparison.Ordinal);

        var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/RoleFitCheckPanel.razor"));
        Assert.Contains("Fit.Locked", panel, StringComparison.Ordinal);
        Assert.Contains("Fit.DeepUpsell", panel, StringComparison.Ordinal);
        Assert.Contains("TrainingOffersBlock", panel, StringComparison.Ordinal);
        Assert.Contains("Fit.OpenMap", panel, StringComparison.Ordinal);
        Assert.Contains("Fit.Step1", panel, StringComparison.Ordinal);
        Assert.Contains("Fit.SimilarTitle", panel, StringComparison.Ordinal);
        Assert.Contains("Fit.DirectTitle", panel, StringComparison.Ordinal);
        Assert.DoesNotContain("RIASEC", panel, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OCEAN", panel, StringComparison.OrdinalIgnoreCase);

        var controller = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/RoleFitCheckController.cs"));
        Assert.Contains("RequireCandidate", controller, StringComparison.Ordinal);
        Assert.Contains("role-fit", controller, StringComparison.Ordinal);
        Assert.Contains("public-write", controller, StringComparison.Ordinal);

        var privacy = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/PrivacyDataService.cs"));
        Assert.Contains("RoleFitChecks", privacy, StringComparison.Ordinal);
        Assert.Contains("CandidateRoleFitChecks.RemoveRange", privacy, StringComparison.Ordinal);
        Assert.Contains("TrainingClicks", privacy, StringComparison.Ordinal);

        var di = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/DependencyInjection.cs"));
        Assert.Contains("ITrainingUpskillService", di, StringComparison.Ordinal);
        Assert.Contains("IRoleFitCheckService", di, StringComparison.Ordinal);
    }

    private static string UiStringsNl(string key)
        => Jobsy.Web.Localization.UiStrings.Get(key, "nl");

    private static string BuildMapHref(string query)
        => "/?q=" + Uri.EscapeDataString(query);

    private static void AssertNoJargon(RoleFitCheckSnapshot snapshot)
    {
        foreach (var text in snapshot.Strengths.Concat(snapshot.Gaps).Concat(snapshot.ActionSteps).Append(snapshot.JobTitle))
        {
            Assert.False(CareerCompassBuilder.ContainsForbiddenJargon(text), text);
        }
    }
}
