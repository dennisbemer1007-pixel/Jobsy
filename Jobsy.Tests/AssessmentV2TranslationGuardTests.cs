using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

/// <summary>Parity and psychometric guard rails for Quick-Scan v2 item banks.</summary>
public sealed class AssessmentV2TranslationGuardTests
{
    private static readonly string[] Langs = ["nl", "en", "pl", "ro", "ar"];

    [Fact]
    public void Every_v2_item_key_exists_in_all_five_languages_and_is_not_literal()
    {
        foreach (var (key, _) in AllV2ItemKeys())
        {
            var nl = UiStrings.Get(key, "nl");
            Assert.False(string.IsNullOrWhiteSpace(nl), key);
            Assert.NotEqual(key, nl, StringComparer.OrdinalIgnoreCase);

            foreach (var lang in Langs)
            {
                var value = UiStrings.Get(key, lang);
                Assert.False(string.IsNullOrWhiteSpace(value), $"{key}/{lang}");
                Assert.NotEqual(key, value, StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Non_nl_translations_must_not_equal_nl_except_whitelisted_proper_nouns()
    {
        var allow = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "RIASEC", "OCEAN", "IPIP", "PDF", "Lobsy", "OK", "CV"
        };

        foreach (var (key, _) in AllV2ItemKeys())
        {
            var nl = UiStrings.Get(key, "nl");
            foreach (var lang in Langs.Where(l => l != "nl"))
            {
                var value = UiStrings.Get(key, lang);
                if (allow.Any(a => value.Contains(a, StringComparison.OrdinalIgnoreCase) && value.Length <= nl.Length + 4))
                {
                    continue;
                }

                Assert.False(
                    string.Equals(value, nl, StringComparison.Ordinal),
                    $"{key}/{lang} equals nl: {value}");
            }
        }
    }

    [Theory]
    [InlineData(AssessmentKind.Competence, 8)]
    [InlineData(AssessmentKind.Career, 8)]
    [InlineData(AssessmentKind.Culture, 4)]
    [InlineData(AssessmentKind.Values, 8)]
    public void V2_each_scale_has_minimum_item_count(AssessmentKind kind, int minimumPerScale)
    {
        var version = AssessmentTestVersions.V2;
        var counts = kind switch
        {
            AssessmentKind.Competence => CompetencyTestCatalogV2.Questions
                .GroupBy(q => q.Category, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count()),
            AssessmentKind.Career => CareerTestCatalogV2.Questions
                .GroupBy(q => q.Category, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count()),
            AssessmentKind.Culture => CulturePersonalityCatalogV2.Questions
                .GroupBy(q => q.Category, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count()),
            AssessmentKind.Values => SchwartzValuesCatalogV2.Questions
                .GroupBy(q => q.Category, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count()),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        Assert.All(counts.Values, c => Assert.True(c >= minimumPerScale, $"{kind} scale has {c} items"));
        _ = version;
    }

    [Theory]
    [InlineData(AssessmentKind.Competence)]
    [InlineData(AssessmentKind.Values)]
    public void V2_competence_and_values_scales_have_four_forward_and_four_reverse(AssessmentKind kind)
    {
        IEnumerable<(string Category, bool Reverse)> items = kind switch
        {
            AssessmentKind.Competence => CompetencyTestCatalogV2.Questions.Select(q => (q.Category, q.Reverse)),
            AssessmentKind.Values => SchwartzValuesCatalogV2.Questions.Select(q => (q.Category, q.Reverse)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        foreach (var group in items.GroupBy(x => x.Category, StringComparer.OrdinalIgnoreCase))
        {
            var forward = group.Count(x => !x.Reverse);
            var reverse = group.Count(x => x.Reverse);
            Assert.Equal(4, forward);
            Assert.Equal(4, reverse);
        }
    }

    [Fact]
    public void Career_v2_each_riasec_type_has_at_least_three_reverse_items()
    {
        foreach (var group in CareerTestCatalogV2.Questions.GroupBy(q => q.Category, StringComparer.OrdinalIgnoreCase))
        {
            Assert.Equal(8, group.Count());
            Assert.True(group.Count(q => q.Reverse) >= 3);
            Assert.True(group.Count(q => !q.Reverse) >= 3);
        }
    }

    [Fact]
    public void V2_all_instruments_reverse_items_use_contrary_nl_wording()
    {
        string[] contraryMarkers =
        [
            "liever", "weinig", " niet ", "niet belangrijk", "vermijd", "zonder", "geen ",
            "hoef niet", "interesseert me weinig", "boeit", "prima als", "afleiding",
            "precies zegt", "verdragen", "plaatsen", "zwaarder", "hoeft", "zoek geen",
            "maakt mij weinig", "maakt me weinig", "alleen als",
            "dan bij veel vrijheid", "vaste routines", "afzijdig", "lastig", "uitstellen",
            "tijdverspilling", "paniek", "gefrustreerd", "uit balans", "onzeker", "saai",
            "stel klusjes vaak uit", "uit tot het laatste", "makkelijk liggen", "laat rommel",
            "slordig", "corrigeren", "pieker", "van slag", "liefst vast", "vermoeiend", "uitgeput",
            "bewez", "voorspelbare", "formelere", "vaste kaders", "alleen kan afmaken",
            "bekende werkwijzen", "resultaat komt eerst", "raken me lang", "onrustig als",
            "minimaal blijven", "goed genoeg", "maken me leeg", "hard kiezen", "'s avonds",
            "hoef niet te weten", "eigen gemak", "zonder extra", "coachen", "pitch", "ordenen vind ik",
            "spreekt me weinig", "tend to believe", "prefer not", "prefer to avoid", "appeals little",
            "mostly feels boring", "ongeduldig", "onderbuikgevoel", "gut feeling", "saai lijken",
            "zonder duidelijke regels", "zonder veel contact", "laat liever anderen",
            "elke stap goedkeurt", "niemand zich", "targets halen is belangrijker",
            "verwacht een formele", "word onrustig", "verandering moet minimaal", "put me", "snel uit"
        ];

        var forbiddenProTraitByKey = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["AssessmentV2.Values.Q21"] = ["drijven mij aan", "motiveert me extra", "drijven"],
            ["AssessmentV2.Values.Q31"] = ["storen me", "storen mij"],
            ["AssessmentV2.Values.Q39"] = ["belangrijkst", "doe ik graag mee"],
            ["AssessmentV2.Culture.Q20"] = ["belangen van anderen", "others' interests", "interesele altora"],
            ["AssessmentV2.Career.Q07"] = ["schrik me niet", "does not put me off", "nie odstrasza", "nu mă sperie", "لا يخيفني"],
            ["AssessmentV2.Career.Q14"] = ["vergelijk graag", "comparing different sources", "Porównuję różne", "Compar surse"],
            ["AssessmentV2.Career.Q22"] = ["graag doe", "enjoy adjusting", "poprawiam muzykę", "adjustez muzică"],
            ["AssessmentV2.Career.Q30"] = ["geef graag praktische", "giving practical tips", "praktyczne wskazówki"],
            ["AssessmentV2.Career.Q38"] = ["pitch graag", "pitching an idea", "przedstawiam pomysł"],
            ["AssessmentV2.Career.Q46"] = ["sorteer en orden graag", "sorting and ordering", "sortować i porządkować"],
        };

        foreach (var item in AllV2ReverseItems())
        {
            var nl = UiStrings.Get(item.TextKey, "nl");
            if (forbiddenProTraitByKey.TryGetValue(item.TextKey, out var forbidden))
            {
                foreach (var phrase in forbidden)
                {
                    Assert.DoesNotContain(phrase, nl, StringComparison.OrdinalIgnoreCase);
                }
            }

            Assert.True(
                contraryMarkers.Any(m => nl.Contains(m, StringComparison.OrdinalIgnoreCase)),
                $"{item.TextKey} (reverse, {item.Kind}) NL stem lacks contrary marker: {nl}");
        }
    }

    private static IEnumerable<(string TextKey, AssessmentKind Kind)> AllV2ReverseItems()
    {
        foreach (var q in CompetencyTestCatalogV2.Questions.Where(x => x.Reverse))
        {
            yield return (q.TextKey, AssessmentKind.Competence);
        }

        foreach (var q in CareerTestCatalogV2.Questions.Where(x => x.Reverse))
        {
            yield return (q.TextKey, AssessmentKind.Career);
        }

        foreach (var q in CulturePersonalityCatalogV2.Questions.Where(x => x.Reverse))
        {
            yield return (q.TextKey, AssessmentKind.Culture);
        }

        foreach (var q in SchwartzValuesCatalogV2.Questions.Where(x => x.Reverse))
        {
            yield return (q.TextKey, AssessmentKind.Values);
        }
    }

    [Theory]
    [InlineData("nl")]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("ro")]
    [InlineData("ar")]
    public void V2_item_stems_are_unique_within_each_language(string lang)
    {
        var stems = AllV2ItemKeys()
            .Select(t => UiStrings.Get(t.Key, lang).Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var duplicates = stems
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => (Stem: g.Key, Count: g.Count()))
            .ToList();

        Assert.True(
            duplicates.Count == 0,
            $"{lang}: duplicate v2 stems: {string.Join("; ", duplicates.Select(d => $"{d.Stem} (×{d.Count})"))}");
    }

    [Fact]
    public void Culture_v2_dimensions_have_four_items_with_two_reverse_each()
    {
        foreach (var group in CulturePersonalityCatalogV2.Questions.GroupBy(q => q.Category, StringComparer.OrdinalIgnoreCase))
        {
            Assert.Equal(CulturePersonalityCatalogV2.ItemsPerDimension, group.Count());
            Assert.Equal(2, group.Count(q => q.Reverse));
            Assert.Equal(2, group.Count(q => !q.Reverse));
        }
    }

    private static IEnumerable<(string Key, AssessmentKind Kind)> AllV2ItemKeys()
    {
        foreach (var q in CompetencyTestCatalogV2.Questions)
        {
            yield return (q.TextKey, AssessmentKind.Competence);
        }

        foreach (var q in CareerTestCatalogV2.Questions)
        {
            yield return (q.TextKey, AssessmentKind.Career);
        }

        foreach (var q in CulturePersonalityCatalogV2.Questions)
        {
            yield return (q.TextKey, AssessmentKind.Culture);
        }

        foreach (var q in SchwartzValuesCatalogV2.Questions)
        {
            yield return (q.TextKey, AssessmentKind.Values);
        }
    }
}
