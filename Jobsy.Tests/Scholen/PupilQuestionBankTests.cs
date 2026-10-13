using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;

namespace Jobsy.Tests.Scholen;

public class PupilQuestionBankTests
{
    // D2: G78 keeps the original list; VO drops "collega" and "klant" (those words appear in VO items).
    private static readonly string[] ForbiddenG78 =
    [
        "collega", "klant", "baas", "leidinggevende", "sollicit", "salaris", "vacature",
        "carrière", "carriere", "werkgever", "kantoor", "instagram", "tiktok", "snapchat",
        "youtube", "vader", "moeder"
    ];

    private static readonly string[] ForbiddenVo =
    [
        "baas", "leidinggevende", "sollicit", "salaris", "vacature",
        "carrière", "carriere", "werkgever", "kantoor", "instagram", "tiktok", "snapchat",
        "youtube", "vader", "moeder"
    ];

    private static readonly string[] Negation =
        ["niet", "geen", "nooit", "niemand", "niets"];

    private readonly IPupilQuestionSetRegistry _registry = new PupilQuestionSetRegistry();
    private readonly Dictionary<string, string> _strings;

    public PupilQuestionBankTests()
    {
        _strings = new Dictionary<string, string>(StringComparer.Ordinal);
        UiStringsLeerlingVragen.MergeNl(_strings);
        UiStringsLeerlingVragenVo.MergeNl(_strings);
        UiStringsScholen.MergeNl(_strings);
    }

    public static TheoryData<PupilQuestionSet> AllSets()
        => [PupilQuestionSet.Groep78, PupilQuestionSet.Vo];

    [Fact]
    public void Bank_has_exactly_60_items_ids_9001_9060_fifteen_per_model()
    {
        var bank = new PupilQuestionBank();
        Assert.Equal(60, bank.Questions.Count);
        Assert.Equal(60, bank.AllItems.Count);
        Assert.Equal(Enumerable.Range(9001, 60), bank.Questions.Select(q => q.Id));
        Assert.Equal(15, bank.ForModel(AssessmentKind.Competence).Count);
        Assert.Equal(15, bank.ForModel(AssessmentKind.Career).Count);
        Assert.Equal(15, bank.ForModel(AssessmentKind.Values).Count);
        Assert.Equal(15, bank.ForModel(AssessmentKind.Culture).Count);
    }

    [Fact]
    public void Vo_bank_has_exactly_100_items_ids_9101_9200_twenty_five_per_model()
    {
        var bank = new PupilQuestionBankVo();
        Assert.Equal(100, bank.Questions.Count);
        Assert.Equal(Enumerable.Range(9101, 100), bank.Questions.Select(q => q.Id));
        Assert.Equal(25, bank.ForModel(AssessmentKind.Competence).Count);
        Assert.Equal(25, bank.ForModel(AssessmentKind.Career).Count);
        Assert.Equal(25, bank.ForModel(AssessmentKind.Values).Count);
        Assert.Equal(25, bank.ForModel(AssessmentKind.Culture).Count);
    }

    [Fact]
    public void G78_distribution_and_reverse_flags_match_spec_table()
    {
        var bank = new PupilQuestionBank();
        void Count(AssessmentKind model, string cat, int expected, int reverseExpected)
        {
            var items = bank.ForModel(model).Where(q => q.Category == cat).ToList();
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
    public void Vo_distribution_and_reverse_flags_match_spec_table()
    {
        var bank = new PupilQuestionBankVo();
        void Count(AssessmentKind model, string cat, int expected, int reverseExpected)
        {
            var items = bank.ForModel(model).Where(q => q.Category == cat).ToList();
            Assert.Equal(expected, items.Count);
            Assert.Equal(reverseExpected, items.Count(q => q.Reverse));
        }

        Count(AssessmentKind.Competence, CompetencyTestCatalog.Samenwerken, 5, 1);
        Count(AssessmentKind.Competence, CompetencyTestCatalog.Resultaatgerichtheid, 5, 1);
        Count(AssessmentKind.Competence, CompetencyTestCatalog.Stressbestendigheid, 5, 1);
        Count(AssessmentKind.Competence, CompetencyTestCatalog.Innovatie, 5, 1);
        Count(AssessmentKind.Competence, CompetencyTestCatalog.Extraversie, 5, 1);

        Count(AssessmentKind.Career, CareerTestCatalog.Realistic, 5, 0);
        Count(AssessmentKind.Career, CareerTestCatalog.Investigative, 4, 0);
        Count(AssessmentKind.Career, CareerTestCatalog.Artistic, 4, 0);
        Count(AssessmentKind.Career, CareerTestCatalog.Social, 4, 0);
        Count(AssessmentKind.Career, CareerTestCatalog.Enterprising, 4, 0);
        Count(AssessmentKind.Career, CareerTestCatalog.Conventional, 4, 0);

        foreach (var cat in SchwartzValuesCatalog.CategoryCodes)
        {
            Count(AssessmentKind.Values, cat, 5, 0);
        }

        Count(AssessmentKind.Culture, CulturePersonalityCatalog.Collaboration, 5, 0);
        Count(AssessmentKind.Culture, CulturePersonalityCatalog.Autonomy, 4, 0);
        Count(AssessmentKind.Culture, CulturePersonalityCatalog.PeopleFirst, 4, 0);
        Count(AssessmentKind.Culture, CulturePersonalityCatalog.Informal, 4, 1);
        Count(AssessmentKind.Culture, CulturePersonalityCatalog.Flexibility, 4, 0);
        Count(AssessmentKind.Culture, CulturePersonalityCatalog.Innovation, 4, 0);
    }

    [Theory]
    [MemberData(nameof(AllSets))]
    public void Adjacent_items_never_share_category_within_a_world(PupilQuestionSet set)
    {
        var bank = Bank(set);
        foreach (var world in Enum.GetValues<PupilWorld>())
        {
            var items = bank.Questions.Where(q => q.World == world).ToList();
            for (var i = 1; i < items.Count; i++)
            {
                Assert.NotEqual(items[i - 1].Category, items[i].Category);
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllSets))]
    public void Texts_meet_word_limits_example_prefix_no_digits_no_forbidden(PupilQuestionSet set)
    {
        var bank = Bank(set);
        var forbidden = set == PupilQuestionSet.Vo ? ForbiddenVo : ForbiddenG78;
        foreach (var q in bank.Questions)
        {
            Assert.True(_strings.TryGetValue(q.TextKey, out var text) && !string.IsNullOrWhiteSpace(text), q.TextKey);
            Assert.True(_strings.TryGetValue(q.ExampleKey, out var example) && !string.IsNullOrWhiteSpace(example), q.ExampleKey);
            Assert.True(DutchReadability.CountWords(text) <= 14, $"{set} {q.Id} words={DutchReadability.CountWords(text)}: {text}");
            Assert.True(DutchReadability.CountWords(example) <= 30, $"{set} {q.Id} example words={DutchReadability.CountWords(example)}");
            Assert.StartsWith("Stel je voor:", example, StringComparison.Ordinal);
            Assert.True(text.All(c => !char.IsDigit(c)), $"{q.Id} has digits: {text}");
            var hay = (text + " " + example).ToLowerInvariant();
            foreach (var bad in forbidden)
            {
                Assert.DoesNotContain(bad, hay, StringComparison.Ordinal);
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllSets))]
    public void Readability_average_sentence_and_long_word_share_within_limits(PupilQuestionSet set)
    {
        var bank = Bank(set);
        var texts = bank.Questions.SelectMany(q => new[] { _strings[q.TextKey], _strings[q.ExampleKey] });
        var report = DutchReadability.Analyze(texts);
        Assert.True(report.AverageWordsPerSentence <= 12,
            $"{set} avg words/sentence={report.AverageWordsPerSentence:0.00}");
        Assert.True(report.LongWordShare <= 0.10,
            $"{set} long-word share={DutchReadability.FormatPercent(report.LongWordShare)}%");
    }

    [Fact]
    public void Cheer_and_world_intro_keys_exist_for_both_sets()
    {
        for (var i = 1; i <= 4; i++)
        {
            Assert.True(_strings.ContainsKey($"LeerlingQ.World.{i}.Intro"));
            Assert.True(_strings.ContainsKey($"LeerlingQ.World.{i}.Done"));
        }

        for (var i = 1; i <= 12; i++)
        {
            Assert.True(_strings.ContainsKey($"LeerlingQ.Cheer.{i}"));
            Assert.True(_strings.ContainsKey($"LeerlingQ.Vo.Cheer.{i}"));
            Assert.True(DutchReadability.CountWords(_strings[$"LeerlingQ.Vo.Cheer.{i}"]) <= 14);
            var hay = _strings[$"LeerlingQ.Vo.Cheer.{i}"].ToLowerInvariant();
            foreach (var bad in ForbiddenVo)
            {
                Assert.DoesNotContain(bad, hay, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Vo_reversed_item_texts_contain_no_negation_word()
    {
        var bank = new PupilQuestionBankVo();
        foreach (var q in bank.Questions.Where(q => q.Reverse))
        {
            var text = _strings[q.TextKey];
            var folded = text.ToLowerInvariant();
            foreach (var word in Negation)
            {
                Assert.DoesNotContain(word, folded, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void All_3_answers_yield_50_percent_per_category()
    {
        var bank = new PupilQuestionBank();
        var answers = bank.Questions.ToDictionary(q => q.Id, _ => 3);
        var c = CompetencyTestCatalog.Score(answers, bank.AsCompetencyItems())!;
        Assert.Equal(50, c.Samenwerken);
        Assert.Equal(50, c.Resultaatgerichtheid);
        Assert.Equal(50, c.Stressbestendigheid);
        Assert.Equal(50, c.Innovatie);
        Assert.Equal(50, c.Extraversie);

        var r = CareerTestCatalog.Score(answers, bank.AsCareerItems())!;
        Assert.Equal(50, r.Realistic);
        Assert.Equal(50, r.Social);

        var v = SchwartzValuesCatalog.Score(answers, bank.AsValuesItems())!;
        Assert.Equal(50, v.Autonomy);
        Assert.Equal(50, v.Impact);

        var cult = CulturePersonalityCatalog.Score(answers, bank.AsCultureItems())!;
        Assert.Equal(50, cult.Autonomy);
        Assert.Equal(50, cult.Collaboration);
        Assert.Null(cult.Openness);
    }

    [Fact]
    public void Vo_all_3_answers_yield_50_percent_and_scoring_version_is_vo_1()
    {
        var def = _registry.Get(PupilQuestionSet.Vo);
        Assert.Equal("vo-1", def.ScoringVersion);
        var bank = (PupilQuestionBankVo)def.Bank;
        var answers = bank.Questions.ToDictionary(q => q.Id, _ => 3);
        var c = CompetencyTestCatalog.Score(answers, bank.AsCompetencyItems())!;
        Assert.Equal(50, c.Samenwerken);
        Assert.Equal(50, c.Resultaatgerichtheid);
        Assert.Equal(50, c.Stressbestendigheid);
        Assert.Equal(50, c.Innovatie);
        Assert.Equal(50, c.Extraversie);

        var r = CareerTestCatalog.Score(answers, bank.AsCareerItems())!;
        Assert.Equal(50, r.Realistic);
        Assert.Equal(50, r.Investigative);
        Assert.Equal(50, r.Artistic);
        Assert.Equal(50, r.Social);
        Assert.Equal(50, r.Enterprising);
        Assert.Equal(50, r.Conventional);

        var v = SchwartzValuesCatalog.Score(answers, bank.AsValuesItems())!;
        Assert.Equal(50, v.Autonomy);
        Assert.Equal(50, v.Connection);
        Assert.Equal(50, v.Achievement);
        Assert.Equal(50, v.Stability);
        Assert.Equal(50, v.Impact);

        var cult = CulturePersonalityCatalog.Score(answers, bank.AsCultureItems())!;
        Assert.Equal(50, cult.Autonomy);
        Assert.Equal(50, cult.Informal);
        Assert.Equal(50, cult.Collaboration);
        Assert.Equal(50, cult.Flexibility);
        Assert.Equal(50, cult.Innovation);
        Assert.Equal(50, cult.PeopleFirst);
    }

    [Fact]
    public void Hand_computed_fixtures_incl_reverse_items()
    {
        var bank = new PupilQuestionBank();
        var answers = bank.Questions.ToDictionary(q => q.Id, _ => 3);
        answers[9001] = 5;
        answers[9006] = 5;
        answers[9011] = 1;
        var c = CompetencyTestCatalog.Score(answers, bank.AsCompetencyItems())!;
        Assert.Equal(100, c.Samenwerken);

        answers[9049] = 1;
        answers[9055] = 5;
        var cult = CulturePersonalityCatalog.Score(answers, bank.AsCultureItems())!;
        Assert.Equal(0, cult.Informal);
    }

    [Fact]
    public void Vo_all_5_yields_100_except_reversed_dimensions()
    {
        var bank = new PupilQuestionBankVo();
        var answers = bank.Questions.ToDictionary(q => q.Id, _ => 5);

        var c = CompetencyTestCatalog.Score(answers, bank.AsCompetencyItems())!;
        // 4×5 + reversed 5→1 = 21 / 5 = 4.2 → 80 %
        Assert.Equal(80, c.Samenwerken);
        Assert.Equal(80, c.Resultaatgerichtheid);
        Assert.Equal(80, c.Stressbestendigheid);
        Assert.Equal(80, c.Innovatie);
        Assert.Equal(80, c.Extraversie);

        var r = CareerTestCatalog.Score(answers, bank.AsCareerItems())!;
        Assert.Equal(100, r.Realistic);
        Assert.Equal(100, r.Social);

        var v = SchwartzValuesCatalog.Score(answers, bank.AsValuesItems())!;
        Assert.Equal(100, v.Autonomy);
        Assert.Equal(100, v.Impact);

        var cult = CulturePersonalityCatalog.Score(answers, bank.AsCultureItems())!;
        Assert.Equal(100, cult.Collaboration);
        Assert.Equal(100, cult.Autonomy);
        Assert.Equal(100, cult.PeopleFirst);
        Assert.Equal(100, cult.Flexibility);
        Assert.Equal(100, cult.Innovation);
        // Informal: 3×5 + reversed 5→1 = 16 / 4 = 4.0 → 75 %
        Assert.Equal(75, cult.Informal);
    }

    [Fact]
    public void Pupil_facing_strings_do_not_hardcode_g78_counts_on_shared_or_vo_keys()
    {
        foreach (var (key, value) in _strings)
        {
            if (key.EndsWith(".Vo", StringComparison.Ordinal) || key.Contains(".Vo.", StringComparison.Ordinal))
            {
                Assert.DoesNotContain("60 van 60", value, StringComparison.Ordinal);
                Assert.DoesNotContain("Elke 6 vragen", value, StringComparison.Ordinal);
            }

            if (string.Equals(key, "LeerlingStory.DonePill", StringComparison.Ordinal))
            {
                Assert.DoesNotContain("60 van 60", value, StringComparison.Ordinal);
            }
        }
    }

    private PupilQuestionBankBase Bank(PupilQuestionSet set)
        => (PupilQuestionBankBase)_registry.Get(set).Bank;
}
