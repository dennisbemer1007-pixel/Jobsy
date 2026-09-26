using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CompetenceDeepReportTests
{
    [Fact]
    public void Facet_map_covers_every_item_exactly_once_with_five_per_facet()
    {
        Assert.Equal(150, DeepAnalysisCompetenceFacets.ByIndex.Length);
        Assert.All(DeepAnalysisCompetenceFacets.ByIndex, code => Assert.False(string.IsNullOrWhiteSpace(code)));

        var groups = DeepAnalysisCompetenceFacets.ByIndex
            .GroupBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // 5 traits × 6 facets, and every one of the 150 items is assigned to exactly one facet.
        Assert.Equal(30, groups.Count);
        Assert.Equal(150, groups.Sum(g => g.Count()));
        Assert.All(groups, g => Assert.Equal(5, g.Count()));
    }

    [Fact]
    public void Facet_codes_align_with_their_trait_item_block()
    {
        for (var i = 0; i < 30; i++)
        {
            Assert.StartsWith("O", DeepAnalysisCompetenceFacets.CodeForIndex(i), StringComparison.Ordinal);
        }

        for (var i = 30; i < 60; i++)
        {
            Assert.StartsWith("C", DeepAnalysisCompetenceFacets.CodeForIndex(i), StringComparison.Ordinal);
        }

        for (var i = 60; i < 90; i++)
        {
            Assert.StartsWith("E", DeepAnalysisCompetenceFacets.CodeForIndex(i), StringComparison.Ordinal);
        }

        for (var i = 90; i < 120; i++)
        {
            Assert.StartsWith("A", DeepAnalysisCompetenceFacets.CodeForIndex(i), StringComparison.Ordinal);
        }

        for (var i = 120; i < 150; i++)
        {
            Assert.StartsWith("N", DeepAnalysisCompetenceFacets.CodeForIndex(i), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Reverse_keyed_item_flips_its_facet_score()
    {
        var reverseQuestion = DeepAnalysisCatalog.Questions.First(
            q => q.Reverse && !string.IsNullOrEmpty(q.Facet));
        var facetCode = reverseQuestion.Facet;
        var facetQuestionIds = DeepAnalysisCatalog.Questions
            .Where(q => string.Equals(q.Facet, facetCode, StringComparison.OrdinalIgnoreCase))
            .Select(q => q.Id)
            .ToList();
        Assert.Equal(5, facetQuestionIds.Count);

        var lowRawAnswers = facetQuestionIds.ToDictionary(id => id, _ => 3);
        lowRawAnswers[reverseQuestion.Id] = 1;
        var scoreWithLowRaw = DeepAnalysisCatalog.ScoreFacets(lowRawAnswers)
            .First(f => string.Equals(f.Facet, facetCode, StringComparison.OrdinalIgnoreCase)).Percent;

        var highRawAnswers = new Dictionary<int, int>(lowRawAnswers) { [reverseQuestion.Id] = 5 };
        var scoreWithHighRaw = DeepAnalysisCatalog.ScoreFacets(highRawAnswers)
            .First(f => string.Equals(f.Facet, facetCode, StringComparison.OrdinalIgnoreCase)).Percent;

        // Raising the raw Likert answer on a reverse-keyed item must lower the corrected facet
        // score (its contribution is inverted), proving reverse-keying is actually applied.
        Assert.True(
            scoreWithHighRaw < scoreWithLowRaw,
            $"Expected reverse item {reverseQuestion.Id} ({facetCode}) to lower the facet score " +
            $"when its raw answer increases, but got {scoreWithLowRaw} -> {scoreWithHighRaw}.");
    }

    [Fact]
    public void Level_thresholds_are_Laag_below_40_Gemiddeld_below_70_else_Hoog()
    {
        Assert.Equal(CompetenceDeepReportLevels.Laag, CompetenceDeepReportLevels.LevelFor(0));
        Assert.Equal(CompetenceDeepReportLevels.Laag, CompetenceDeepReportLevels.LevelFor(39));
        Assert.Equal(CompetenceDeepReportLevels.Gemiddeld, CompetenceDeepReportLevels.LevelFor(40));
        Assert.Equal(CompetenceDeepReportLevels.Gemiddeld, CompetenceDeepReportLevels.LevelFor(69));
        Assert.Equal(CompetenceDeepReportLevels.Hoog, CompetenceDeepReportLevels.LevelFor(70));
        Assert.Equal(CompetenceDeepReportLevels.Hoog, CompetenceDeepReportLevels.LevelFor(100));
    }

    [Fact]
    public void Norm_file_loads_and_reports_available()
    {
        var provider = new Johnson2014NormProvider();
        Assert.True(provider.IsAvailable);
        Assert.NotNull(provider.Norms);
        Assert.Equal(2707, provider.Norms!.N);
        Assert.True(provider.Norms.Traits.ContainsKey("Conscientiousness"));
        Assert.False(string.IsNullOrWhiteSpace(provider.SourceDisclaimer));
    }

    [Fact]
    public void Trait_key_mapping_matches_norm_dataset_trait_names()
    {
        Assert.Equal("Conscientiousness", DeepAnalysisCompetenceFacets.TraitNormKey("Consciëntieusheid"));
        Assert.Equal("Agreeableness", DeepAnalysisCompetenceFacets.TraitNormKey("Vriendelijkheid"));
        Assert.Equal("EmotionalStability", DeepAnalysisCompetenceFacets.TraitNormKey("EmotioneleStabiliteit"));
        Assert.Equal("Openness", DeepAnalysisCompetenceFacets.TraitNormKey("Openheid"));
        Assert.Equal("Extraversion", DeepAnalysisCompetenceFacets.TraitNormKey("Extraversie"));

        var provider = new Johnson2014NormProvider();
        foreach (var domain in DeepAnalysisCatalog.BigFiveDomains)
        {
            var key = DeepAnalysisCompetenceFacets.TraitNormKey(domain);
            Assert.True(
                provider.Norms!.Traits.ContainsKey(key),
                $"Norm dataset is missing trait key '{key}' mapped from domain '{domain}'.");
        }
    }

    [Fact]
    public void Band_boundaries_are_exact_at_p25_and_p75()
    {
        var provider = new Johnson2014NormProvider();
        const string key = "Conscientiousness";
        var trait = provider.Norms!.Traits[key];

        // p25 itself is "similar" (not yet "lower"); just below p25 is "lower".
        Assert.Equal("Vergelijkbaar met de meeste mensen", provider.BandLabel(trait.P25, key));
        Assert.Equal("Lager dan de meeste mensen", provider.BandLabel(trait.P25 - 0.1, key));

        // p75 itself is "similar" (not yet "higher"); just above p75 is "higher".
        Assert.Equal("Vergelijkbaar met de meeste mensen", provider.BandLabel(trait.P75, key));
        Assert.Equal("Hoger dan de meeste mensen", provider.BandLabel(trait.P75 + 0.1, key));
    }

    [Fact]
    public void Report_builder_with_norms_fills_mean_and_band_for_traits_and_facets()
    {
        var report = CompetenceDeepReportBuilder.Build(
            FullCompetenceAnswers(),
            new Johnson2014NormProvider(),
            jobTitle: null,
            occupations: null,
            aiSummary: null,
            aiPlan: null,
            fromOpenAi: false,
            generatedAtUtc: DateTime.UtcNow);

        Assert.Equal(5, report.Traits.Count);
        Assert.False(string.IsNullOrWhiteSpace(report.NormSourceLine));
        Assert.All(report.Traits, trait =>
        {
            Assert.NotNull(trait.NormMean);
            Assert.False(string.IsNullOrWhiteSpace(trait.NormBand));
            Assert.Equal(6, trait.Facets.Count);
            Assert.All(trait.Facets, facet =>
            {
                Assert.NotNull(facet.NormMean);
                Assert.False(string.IsNullOrWhiteSpace(facet.NormBand));
            });
        });
    }

    [Fact]
    public void Report_builder_without_norms_hides_the_comparison()
    {
        var report = CompetenceDeepReportBuilder.Build(
            FullCompetenceAnswers(),
            new FakeNormProvider(available: false),
            jobTitle: null,
            occupations: null,
            aiSummary: null,
            aiPlan: null,
            fromOpenAi: false,
            generatedAtUtc: DateTime.UtcNow);

        Assert.Null(report.NormSourceLine);
        Assert.All(report.Traits, trait =>
        {
            Assert.Null(trait.NormMean);
            Assert.Null(trait.NormBand);
            Assert.All(trait.Facets, facet =>
            {
                Assert.Null(facet.NormMean);
                Assert.Null(facet.NormBand);
            });
        });
    }

    [Fact]
    public void Builder_falls_back_to_template_copy_when_no_ai_summary_is_supplied()
    {
        var report = CompetenceDeepReportBuilder.Build(
            FullCompetenceAnswers(),
            new FakeNormProvider(available: true),
            jobTitle: null,
            occupations: null,
            aiSummary: null,
            aiPlan: null,
            fromOpenAi: true,
            generatedAtUtc: DateTime.UtcNow);

        // fromOpenAi is only true when an actual AI summary was supplied.
        Assert.False(report.FromOpenAi);
        Assert.False(string.IsNullOrWhiteSpace(report.Summary));
        Assert.NotEmpty(report.ActionPlan);

        Assert.False(string.IsNullOrWhiteSpace(CompetenceDeepReportTexts.TemplateSummary(report.Traits)));
        Assert.NotEmpty(CompetenceDeepReportTexts.TemplateActionPlan(report.Traits));
    }

    [Fact]
    public void Builder_marks_from_open_ai_true_only_when_summary_is_present()
    {
        var report = CompetenceDeepReportBuilder.Build(
            FullCompetenceAnswers(),
            new FakeNormProvider(available: true),
            jobTitle: null,
            occupations: null,
            aiSummary: "Een persoonlijke samenvatting van OpenAI.",
            aiPlan: [("Stap 1", "Doe dit eerst.")],
            fromOpenAi: true,
            generatedAtUtc: DateTime.UtcNow);

        Assert.True(report.FromOpenAi);
        Assert.Equal("Een persoonlijke samenvatting van OpenAI.", report.Summary);
        Assert.Single(report.ActionPlan);
    }

    [Fact]
    public void LabelNl_uses_the_plain_Dutch_DomainLabel_wording()
    {
        Assert.Equal("Afmaken & netjes werken", CompetenceDeepReportTexts.LabelNl("Consciëntieusheid"));
        Assert.Equal("Nieuwe dingen proberen", CompetenceDeepReportTexts.LabelNl("Openheid"));
        Assert.Equal("Energie van mensen", CompetenceDeepReportTexts.LabelNl("Extraversie"));
        Assert.Equal("Samen & aardig", CompetenceDeepReportTexts.LabelNl("Vriendelijkheid"));
        Assert.Equal("Kalm blijven", CompetenceDeepReportTexts.LabelNl("EmotioneleStabiliteit"));

        foreach (var domain in DeepAnalysisCatalog.BigFiveDomains)
        {
            Assert.Equal(DeepAnalysisQuestionHelp.DomainLabel(domain), CompetenceDeepReportTexts.LabelNl(domain));
        }
    }

    private static Dictionary<int, int> FullCompetenceAnswers(int value = 4)
        => DeepAnalysisCatalog.Questions.ToDictionary(q => q.Id, _ => value);

    private sealed class FakeNormProvider : INormProvider
    {
        private readonly bool _available;

        public FakeNormProvider(bool available) => _available = available;

        public bool IsAvailable => _available;

        public BigFiveNormSet? Norms => null;

        public string? BandLabel(double score, string traitOrFacetKey)
            => _available ? "Vergelijkbaar met de meeste mensen" : null;

        public double? Mean(string traitOrFacetKey) => _available ? 50.0 : null;

        public string SourceDisclaimer => "Fake normgroep-disclaimer voor tests.";
    }
}
