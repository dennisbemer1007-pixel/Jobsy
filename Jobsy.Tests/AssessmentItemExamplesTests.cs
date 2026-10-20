using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class AssessmentItemExamplesTests
{
    [Fact]
    public void Every_catalog_id_has_nl_and_en_examples_within_max_length()
    {
        foreach (var (kind, id) in AssessmentItemExamples.AllCatalogIds())
        {
            var nl = AssessmentItemExamples.ExampleNl(kind, id, domain: "", reverse: false);
            var en = AssessmentItemExamples.ExampleEn(kind, id, domain: "", reverse: false);

            Assert.False(string.IsNullOrWhiteSpace(nl));
            Assert.False(string.IsNullOrWhiteSpace(en));
            Assert.DoesNotContain('\u2014', nl);
            Assert.DoesNotContain('\u2014', en);
            Assert.True(nl.Length <= AssessmentItemExamples.MaxLength, $"nl {kind}#{id} length {nl.Length}");
            Assert.True(en.Length <= AssessmentItemExamples.MaxLength, $"en {kind}#{id} length {en.Length}");
            Assert.Contains("Voorbeeld", nl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Example", en, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Zo reageer jij meestal", nl, StringComparison.Ordinal);
            Assert.DoesNotContain("how you usually react when it counts", en, StringComparison.OrdinalIgnoreCase);
            Assert.True(nl.TrimEnd().EndsWith('.') || nl.TrimEnd().EndsWith('?'), $"nl {kind}#{id}: {nl}");
            Assert.True(en.TrimEnd().EndsWith('.') || en.TrimEnd().EndsWith('?'), $"en {kind}#{id}: {en}");
        }
    }

    [Theory]
    [InlineData(AssessmentKind.Competence)]
    [InlineData(AssessmentKind.Career)]
    [InlineData(AssessmentKind.Culture)]
    [InlineData(AssessmentKind.Values)]
    public void Quick_scan_v3_examples_are_unique_within_kind(AssessmentKind kind)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in QuickScanFreeTierV3.QuestionIds(kind))
        {
            var nl = AssessmentItemExamples.ExampleNl(kind, id, domain: "", reverse: false);
            Assert.True(seen.Add(nl), $"Duplicate NL example in {kind} for id {id}: {nl}");
        }
    }

    [Theory]
    [InlineData(AssessmentKind.Competence)]
    [InlineData(AssessmentKind.Career)]
    [InlineData(AssessmentKind.Culture)]
    [InlineData(AssessmentKind.Values)]
    public void Quick_scan_v3_free_tier_ids_are_in_catalog(AssessmentKind kind)
    {
        foreach (var id in QuickScanFreeTierV3.QuestionIds(kind))
        {
            var nl = AssessmentItemExamples.ExampleNl(kind, id, "", false);
            var en = AssessmentItemExamples.ExampleEn(kind, id, "", false);
            Assert.False(string.IsNullOrWhiteSpace(nl));
            Assert.False(string.IsNullOrWhiteSpace(en));
        }
    }

    [Fact]
    public void Deep_analysis_questions_use_catalog_via_help()
    {
        foreach (var kind in new[] { AssessmentKind.Competence, AssessmentKind.Career, AssessmentKind.Culture, AssessmentKind.Values })
        {
            foreach (var q in DeepAnalysisCatalog.QuestionsFor(kind))
            {
                var example = DeepAnalysisQuestionHelp.ExampleFor(q);
                Assert.False(string.IsNullOrWhiteSpace(example));
                Assert.True(example.Length <= AssessmentItemExamples.MaxLength);
            }
        }
    }
}
