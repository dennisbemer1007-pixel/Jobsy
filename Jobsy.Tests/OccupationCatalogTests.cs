using System.Text.Json.Nodes;
using Jobsy.Core.Careers;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class OccupationCatalogTests
{
    private static readonly RiasecScores Profile = new(66, 38, 37, 64, 43, 62);

    [Fact]
    public void Catalogue_matches_the_pinned_counts_and_keeps_oi_inside_the_scale()
    {
        var all = OccupationCatalog.Shared.All;
        Assert.Equal(3039, all.Count);
        Assert.Equal(2471, all.Count(item => item.Oi is not null));
        Assert.Equal(498, all.Count(item => item.Tier == "exact"));
        Assert.Equal(89, all.Count(item => item.Tier == "narrow"));
        Assert.Equal(745, all.Count(item => item.Tier == "close"));
        Assert.Equal(1139, all.Count(item => item.Tier == "broad"));
        Assert.Equal(568, all.Count(item => item.Tier == "none"));
        Assert.Equal(498, all.Count(item => item.Confidence == "high"));
        // 1,713 / 260, not the research note 1,712 / 261: ouderenwerker's spread is
        // exactly 1.50 on the 2-decimal OI scale (4.57 − 3.07). Binary float made that 1.5000000000000004.
        Assert.Equal(1713, all.Count(item => item.Confidence == "medium"));
        Assert.Equal(260, all.Count(item => item.Confidence == "low"));
        Assert.All(all.Where(item => item.Oi is not null), item =>
        {
            Assert.Equal(6, item.Oi!.Count);
            Assert.All(item.Oi, value => Assert.InRange(value, 1d, 7d));
        });
        Assert.All(OccupationCatalog.Shared.Listable, item =>
        {
            Assert.NotNull(item.Oi);
            Assert.Contains(item.Confidence, new[] { "high", "medium" });
        });
        Assert.All(CareerCompassBuilder.Listed(Profile), job =>
        {
            Assert.False(job.NoScore);
            Assert.Contains(job.Confidence, new[] { "high", "medium" });
        });
    }

    [Fact]
    public void Profile_match_stays_inside_the_candidate_scores()
    {
        var letters = CareerTestCatalog.RiasecCodes.Select(Profile.Get).ToList();
        var match = CareerCompassBuilder.ProfileMatch([7, 1.97, 1.59, 1.66, 2.09, 3.35], Profile);
        Assert.Equal(59, match);
        Assert.InRange(match, letters.Min(), letters.Max());
    }

    [Fact]
    public void Unknown_titles_and_occupations_without_a_profile_have_no_percent()
    {
        Assert.Null(CareerCompassBuilder.CatalogueFit("dit beroep bestaat niet xyz", Profile));
        var orderpicker = OccupationCatalog.Shared.Get("cd94def5-3442-4c2e-ae3d-0761a3008bcb");
        Assert.NotNull(orderpicker);
        Assert.NotNull(orderpicker!.Oi);
        Assert.Equal("low", orderpicker.Confidence);
        Assert.Null(CareerCompassBuilder.CatalogueFit(orderpicker.Nl, Profile));
        var hits = OccupationCatalog.Shared.Search("orderpicker", 5);
        Assert.Contains(hits, hit => hit.EscoId == orderpicker.Id && hit.NoScore);
    }

    [Fact]
    public void Committed_corrections_do_not_change_the_official_profiles()
    {
        foreach (var job in OccupationCatalog.Shared.All)
        {
            Assert.Equal(job.OfficialOi, job.Oi);
            Assert.Equal(job.OfficialTier, job.Tier);
            Assert.Equal(job.OfficialConfidence, job.Confidence);
            Assert.Equal(job.OfficialOnet, job.Onet);
        }
    }

    [Fact]
    public void Approved_orderpicker_correction_uses_the_onet_profile()
    {
        var catalog = LoadWithStatus("cd94def5-3442-4c2e-ae3d-0761a3008bcb", OccupationCatalog.ApprovedStatus, applyConcept: false);
        var job = catalog.Get("cd94def5-3442-4c2e-ae3d-0761a3008bcb");
        Assert.NotNull(job);
        Assert.Equal([4.79, 1.0, 1.0, 1.88, 3.29, 6.25], job!.Oi);
        Assert.Equal("high", job.Confidence);
        Assert.Equal("lobsy", job.Tier);
    }

    [Fact]
    public void Unknown_onet_code_fails_the_loader()
    {
        var error = Assert.Throws<InvalidOperationException>(() => LoadWithOnet("cd94def5-3442-4c2e-ae3d-0761a3008bcb", "99-9999.00"));
        Assert.Contains("99-9999.00", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_ignores_the_preview_flag()
    {
        Assert.False(OccupationCatalog.ApplyConcept(previewRequested: true, isProduction: true));
        Assert.True(OccupationCatalog.ApplyConcept(previewRequested: true, isProduction: false));
        var official = OccupationCatalog.LoadEmbedded(applyConcept: false).Get("cd94def5-3442-4c2e-ae3d-0761a3008bcb");
        var preview = OccupationCatalog.LoadEmbedded(applyConcept: true).Get("cd94def5-3442-4c2e-ae3d-0761a3008bcb");
        Assert.Equal("low", official!.Confidence);
        Assert.Equal("high", preview!.Confidence);
        Assert.Equal([4.79, 1.0, 1.0, 1.88, 3.29, 6.25], preview.Oi);
    }

    [Fact]
    public void Golden_westland_profiles_come_from_the_official_join()
    {
        // C is 3.36: (3.35 + 3.36) / 2 = 3.355, rounded half up. The research note wrote 3.35.
        Expect("1a9d99ba-4c08-4864-8f6a-1b0f2b4cf883", [7.0, 1.97, 1.59, 1.66, 2.09, 3.36], "medium", null);
        Expect("cd94def5-3442-4c2e-ae3d-0761a3008bcb", [3.68, 1.0, 1.0, 2.2, 3.92, 6.61], "low", null);
        Expect("eeac6251-1974-42f5-91ed-513c6c883755", [7.0, 1.78, 1.0, 1.11, 1.23, 3.71], "medium", null);
        Expect("245be6d1-fe9a-4ac8-9f81-122a687e4724", null, "high", "51-9198.00");
        Expect("303a1e34-cb16-4054-b323-81e5eec17397", null, "high", "37-2011.00");
        var office = Expect("6c999fc7-c6b7-4ef3-a4b9-af124a1783a2", null, "high", "43-9061.00");
        Assert.Equal("4110", office.Isco);
        var admin = Expect("044d78cc-f62f-4532-83a5-8e04f2889652", null, "low", null);
        Assert.Equal("3343", admin.Isco);
    }

    [Theory]
    [InlineData("mbo 2", 2, true)]
    [InlineData("mbo 3", 2, true)]
    [InlineData("mbo 4", 3, false)]
    [InlineData("hbo", 4, false)]
    [InlineData("wo", 4, false)]
    [InlineData("vmbo", 2, false)]
    [InlineData("havo", 3, false)]
    [InlineData("vwo", 3, false)]
    public void Education_gate_follows_the_approved_map(string education, int max, bool flagLevel3)
    {
        var gate = CareerEducationGate.MaxIscoLevel(education);
        Assert.Equal(max, gate.MaxLevel);
        Assert.Equal(flagLevel3, gate.FlagLevel3);
    }

    [Theory]
    [InlineData("havo")]
    [InlineData("vwo")]
    [InlineData("klas 3 havo")]
    public void Havo_and_vwo_without_further_education_use_the_mbo_4_gate(string education)
    {
        Assert.Equal(CareerEducationGate.MaxIscoLevel("mbo 4"), CareerEducationGate.MaxIscoLevel(education));
    }

    [Theory]
    [InlineData("havo mbo 2", "mbo 2")]
    [InlineData("vwo mbo 4", "mbo 4")]
    [InlineData("havo hbo", "hbo")]
    [InlineData("vwo wo", "wo")]
    public void Further_diploma_takes_precedence_over_havo_and_vwo(string education, string further)
    {
        Assert.Equal(CareerEducationGate.MaxIscoLevel(further), CareerEducationGate.MaxIscoLevel(education));
    }

    [Fact]
    public void Unknown_education_has_no_gate_and_mbo_blocks_level_4()
    {
        Assert.False(CareerEducationGate.MaxIscoLevel(null).HasGate);
        Assert.False(CareerEducationGate.MaxIscoLevel("").HasGate);
        Assert.False(CareerEducationGate.MaxIscoLevel("iets anders").HasGate);
        Assert.False(CareerEducationGate.Passes(4, CareerEducationGate.MaxIscoLevel("mbo 2")));
        Assert.True(CareerEducationGate.Passes(3, CareerEducationGate.MaxIscoLevel("mbo 2")));
        Assert.True(CareerEducationGate.NeedsExtraTraining(3, CareerEducationGate.MaxIscoLevel("mbo 2")));
        Assert.False(CareerEducationGate.Passes(4, CareerEducationGate.MaxIscoLevel("mbo 4")));
        Assert.True(CareerEducationGate.Passes(4, CareerEducationGate.MaxIscoLevel("hbo")));
        Assert.True(CareerEducationGate.Passes(4, CareerEducationGate.MaxIscoLevel(null)));

        var listed = CareerCompassBuilder.Listed(new RiasecScores(20, 15, 10, 30, 25, 95), "mbo 2");
        Assert.DoesNotContain(listed, job => OccupationCatalog.Shared.Get(job.EscoId)?.IscoLevel == 4);
        Assert.All(
            listed.Where(job => OccupationCatalog.Shared.Get(job.EscoId)?.IscoLevel == 3),
            job => Assert.True(CareerEducationGate.NeedsExtraTraining(3, CareerEducationGate.MaxIscoLevel("mbo 2"))));
    }

    [Fact]
    public void Mbo_candidate_sees_kantoorbediende_instead_of_administratief_medewerker()
    {
        var listed = CareerCompassBuilder.Listed(new RiasecScores(10, 10, 10, 20, 15, 95), "mbo 4");
        Assert.DoesNotContain(listed, job => job.EscoId == "044d78cc-f62f-4532-83a5-8e04f2889652");
        var hits = OccupationCatalog.Shared.Search("administratief", 8, "mbo 4");
        Assert.Contains(hits, hit => hit.EscoId == "044d78cc-f62f-4532-83a5-8e04f2889652");
        var office = hits.First(hit => hit.EscoId == "6c999fc7-c6b7-4ef3-a4b9-af124a1783a2");
        var admin = hits.First(hit => hit.EscoId == "044d78cc-f62f-4532-83a5-8e04f2889652");
        Assert.True(hits.ToList().IndexOf(office) < hits.ToList().IndexOf(admin));
        Assert.True(admin.LevelNote);
    }

    [Fact]
    public void Leadership_jobs_stay_out_when_enterprising_is_not_in_the_top_3()
    {
        var scores = new RiasecScores(90, 80, 70, 40, 10, 30);
        Assert.False(CareerCompassBuilder.EnterprisingInTop3(scores));
        var listed = CareerCompassBuilder.Listed(scores, "hbo");
        Assert.All(listed, job =>
        {
            var occupation = OccupationCatalog.Shared.Get(job.EscoId);
            Assert.NotNull(occupation);
            Assert.False(OccupationCatalog.IsLeadership(occupation!));
        });
    }

    [Fact]
    public void Bronnen_page_keeps_the_required_attribution()
    {
        var root = TestRepo.FindRoot();
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Legal/Bronnen.razor"));
        Assert.Contains("This service uses the ESCO classification of the European Commission.", page, StringComparison.Ordinal);
        Assert.Contains("O*NET 31.0 Database", page, StringComparison.Ordinal);
        Assert.Contains("https://creativecommons.org/licenses/by/4.0/", page, StringComparison.Ordinal);
        Assert.Contains("European Commission &amp; U.S. Department of Labor (2022).", page, StringComparison.Ordinal);
        Assert.Contains("CBS, Beroepenclassificatie BRC 2014 editie 2025 (CC BY 4.0).", page, StringComparison.Ordinal);
        Assert.Equal(OccupationCopy.NoScoreSentence, UiStrings("Occ.NoScoreWhy"));
    }

    private static string UiStrings(string key)
    {
        var root = TestRepo.FindRoot();
        var text = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Localization/UiStringsLegal.cs"));
        Assert.Contains(key, text, StringComparison.Ordinal);
        Assert.Contains(OccupationCopy.NoScoreSentence, text, StringComparison.Ordinal);
        return OccupationCopy.NoScoreSentence;
    }

    private static Occupation Expect(string id, double[]? oi, string confidence, string? onet)
    {
        var job = OccupationCatalog.Shared.Get(id);
        Assert.NotNull(job);
        Assert.Equal(confidence, job!.Confidence);
        if (oi is not null)
        {
            Assert.Equal(oi, job.Oi);
        }

        if (onet is not null)
        {
            Assert.Equal([onet], job.Onet);
        }

        return job;
    }

    private static OccupationCatalog LoadWithStatus(string escoId, string status, bool applyConcept)
    {
        var json = File.ReadAllText(Path.Combine(TestRepo.FindRoot(), "Jobsy.Core/Data/Occupations/corrections.json"));
        var node = JsonNode.Parse(json) ?? throw new InvalidOperationException("corrections");
        foreach (var item in node["items"]!.AsArray())
        {
            if (item?["escoId"]?.GetValue<string>() == escoId)
            {
                item["status"] = status;
            }
        }

        return LoadCorrections(node.ToJsonString(), applyConcept);
    }

    private static OccupationCatalog LoadWithOnet(string escoId, string onet)
    {
        var json = File.ReadAllText(Path.Combine(TestRepo.FindRoot(), "Jobsy.Core/Data/Occupations/corrections.json"));
        var node = JsonNode.Parse(json) ?? throw new InvalidOperationException("corrections");
        foreach (var item in node["items"]!.AsArray())
        {
            if (item?["escoId"]?.GetValue<string>() == escoId)
            {
                item["kind"] = "change";
                item["status"] = OccupationCatalog.ApprovedStatus;
                item["onet"] = new JsonArray(onet);
                item["confidence"] = "high";
            }
        }

        return LoadCorrections(node.ToJsonString(), applyConcept: false);
    }

    private static OccupationCatalog LoadCorrections(string correctionsJson, bool applyConcept)
    {
        var assembly = typeof(OccupationCatalog).Assembly;
        using var occupations = Open(assembly, "occupations.nl.json");
        using var onet = Open(assembly, "onet-oi.json");
        using var cbs = Open(assembly, "cbs-title-index.json");
        using var corrections = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(correctionsJson));
        return OccupationCatalog.Load(occupations, onet, corrections, cbs, applyConcept);
    }

    private static Stream Open(System.Reflection.Assembly assembly, string fileName)
    {
        var name = assembly.GetManifestResourceNames().First(resource => resource.EndsWith(fileName, StringComparison.Ordinal));
        return assembly.GetManifestResourceStream(name)!;
    }
}
