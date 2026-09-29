using System.Text;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;

namespace Jobsy.Tests.Scholen;

public class PupilQuestionBankTests
{
    private static readonly string[] Forbidden =
    [
        "collega", "klant", "baas", "leidinggevende", "sollicit", "salaris", "vacature",
        "carrière", "carriere", "werkgever", "kantoor", "instagram", "tiktok", "snapchat",
        "youtube", "vader", "moeder"
    ];

    private readonly PupilQuestionBank _bank = new();
    private readonly Dictionary<string, string> _strings;

    public PupilQuestionBankTests()
    {
        _strings = new Dictionary<string, string>(StringComparer.Ordinal);
        UiStringsLeerlingVragen.MergeNl(_strings);
    }

    [Fact]
    public void Bank_has_exactly_60_items_ids_9001_9060_fifteen_per_model()
    {
        Assert.Equal(60, _bank.Questions.Count);
        Assert.Equal(60, _bank.AllItems.Count);
        Assert.Equal(Enumerable.Range(9001, 60), _bank.Questions.Select(q => q.Id));
        Assert.Equal(15, _bank.ForModel(AssessmentKind.Competence).Count);
        Assert.Equal(15, _bank.ForModel(AssessmentKind.Career).Count);
        Assert.Equal(15, _bank.ForModel(AssessmentKind.Values).Count);
        Assert.Equal(15, _bank.ForModel(AssessmentKind.Culture).Count);
    }

    [Fact]
    public void Distribution_and_reverse_flags_match_spec_table()
    {
        void Count(AssessmentKind model, string cat, int expected, int reverseExpected)
        {
            var items = _bank.ForModel(model).Where(q => q.Category == cat).ToList();
            Assert.Equal(expected, items.Count);
            Assert.Equal(reverseExpected, items.Count(q => q.Reverse));
        }

        Count(AssessmentKind.Competence, CompetencyTestCatalog.Samenwerken, 3, 1);
        Count(AssessmentKind.Competence, CompetencyTestCatalog.Resultaatgerichtheid, 3, 1);
        Count(AssessmentKind.Competence, CompetencyTestCatalog.Stressbestendigheid, 3, 1);
        Count(AssessmentKind.Competence, CompetencyTestCatalog.Innovatie, 3, 1);
        Count(AssessmentKind.Competence, CompetencyTestCatalog.Extraversie, 3, 1);

        Count(AssessmentKind.Career, CareerTestCatalog.Realistic, 3, 0);
        Count(AssessmentKind.Career, CareerTestCatalog.Investigative, 2, 0);
        Count(AssessmentKind.Career, CareerTestCatalog.Artistic, 3, 0);
        Count(AssessmentKind.Career, CareerTestCatalog.Social, 3, 0);
        Count(AssessmentKind.Career, CareerTestCatalog.Enterprising, 2, 0);
        Count(AssessmentKind.Career, CareerTestCatalog.Conventional, 2, 0);

        foreach (var cat in SchwartzValuesCatalog.CategoryCodes)
        {
            Count(AssessmentKind.Values, cat, 3, 0);
        }

        Count(AssessmentKind.Culture, CulturePersonalityCatalog.Autonomy, 3, 0);
        Count(AssessmentKind.Culture, CulturePersonalityCatalog.Collaboration, 3, 0);
        Count(AssessmentKind.Culture, CulturePersonalityCatalog.PeopleFirst, 3, 0);
        Count(AssessmentKind.Culture, CulturePersonalityCatalog.Informal, 2, 1);
        Count(AssessmentKind.Culture, CulturePersonalityCatalog.Flexibility, 2, 0);
        Count(AssessmentKind.Culture, CulturePersonalityCatalog.Innovation, 2, 0);
    }

    [Fact]
    public void Adjacent_items_never_share_category_within_a_world()
    {
        foreach (var world in Enum.GetValues<PupilWorld>())
        {
            var items = _bank.Questions.Where(q => q.World == world).ToList();
            for (var i = 1; i < items.Count; i++)
            {
                Assert.NotEqual(items[i - 1].Category, items[i].Category);
            }
        }
    }

    [Fact]
    public void Texts_meet_word_limits_example_prefix_no_digits_no_forbidden()
    {
        foreach (var q in _bank.Questions)
        {
            Assert.True(_strings.TryGetValue(q.TextKey, out var text) && !string.IsNullOrWhiteSpace(text), q.TextKey);
            Assert.True(_strings.TryGetValue(q.ExampleKey, out var example) && !string.IsNullOrWhiteSpace(example), q.ExampleKey);
            Assert.True(DutchReadability.CountWords(text) <= 14, $"{q.Id} words={DutchReadability.CountWords(text)}: {text}");
            Assert.True(DutchReadability.CountWords(example) <= 30, $"{q.Id} example words={DutchReadability.CountWords(example)}");
            Assert.StartsWith("Stel je voor:", example, StringComparison.Ordinal);
            Assert.True(text.All(c => !char.IsDigit(c)), $"{q.Id} has digits: {text}");
            var hay = (text + " " + example).ToLowerInvariant();
            foreach (var bad in Forbidden)
            {
                Assert.DoesNotContain(bad, hay, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Readability_average_sentence_and_long_word_share_within_limits()
    {
        var texts = _bank.Questions.SelectMany(q => new[] { _strings[q.TextKey], _strings[q.ExampleKey] });
        var report = DutchReadability.Analyze(texts);
        Assert.True(report.AverageWordsPerSentence <= 12,
            $"avg words/sentence={report.AverageWordsPerSentence:0.00}");
        Assert.True(report.LongWordShare <= 0.10,
            $"long-word share={DutchReadability.FormatPercent(report.LongWordShare)}%");
    }

    [Fact]
    public void Cheer_and_world_intro_keys_exist()
    {
        for (var i = 1; i <= 4; i++)
        {
            Assert.True(_strings.ContainsKey($"LeerlingQ.World.{i}.Intro"));
            Assert.True(_strings.ContainsKey($"LeerlingQ.World.{i}.Done"));
        }

        for (var i = 1; i <= 10; i++)
        {
            Assert.True(_strings.ContainsKey($"LeerlingQ.Cheer.{i}"));
        }
    }

    [Fact]
    public void All_3_answers_yield_50_percent_per_category()
    {
        var answers = _bank.Questions.ToDictionary(q => q.Id, _ => 3);
        var c = CompetencyTestCatalog.Score(answers, _bank.AsCompetencyItems())!;
        Assert.Equal(50, c.Samenwerken);
        Assert.Equal(50, c.Resultaatgerichtheid);
        Assert.Equal(50, c.Stressbestendigheid);
        Assert.Equal(50, c.Innovatie);
        Assert.Equal(50, c.Extraversie);

        var r = CareerTestCatalog.Score(answers, _bank.AsCareerItems())!;
        Assert.Equal(50, r.Realistic);
        Assert.Equal(50, r.Social);

        var v = SchwartzValuesCatalog.Score(answers, _bank.AsValuesItems())!;
        Assert.Equal(50, v.Autonomy);
        Assert.Equal(50, v.Impact);

        var cult = CulturePersonalityCatalog.Score(answers, _bank.AsCultureItems())!;
        Assert.Equal(50, cult.Autonomy);
        Assert.Equal(50, cult.Collaboration);
        Assert.Null(cult.Openness); // Big Five facets not in pupil culture items
    }

    [Fact]
    public void Hand_computed_fixtures_incl_reverse_items()
    {
        // Samenwerken: 9001=5, 9006=5, 9011 reverse raw=1 → scored 5 → 100%
        var answers = _bank.Questions.ToDictionary(q => q.Id, _ => 3);
        answers[9001] = 5;
        answers[9006] = 5;
        answers[9011] = 1;
        var c = CompetencyTestCatalog.Score(answers, _bank.AsCompetencyItems())!;
        Assert.Equal(100, c.Samenwerken);

        // Informal reverse 9055: raw 5 → scored 1 → with 9049=1 → avg 1 → 0%
        answers[9049] = 1;
        answers[9055] = 5;
        var cult = CulturePersonalityCatalog.Score(answers, _bank.AsCultureItems())!;
        Assert.Equal(0, cult.Informal);
    }
}
