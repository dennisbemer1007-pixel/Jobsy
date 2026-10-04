using System.Text.RegularExpressions;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public class CandidateQuestionLocalizationTests
{
    private static readonly string[] Languages = ["pl", "ro", "ar"];

    private static readonly string[] QuestionKeys =
    [
        ..Enumerable.Range(1, 25).Select(i => $"Competency.Q{i:00}"),
        ..Enumerable.Range(1, 25).Select(i => $"Career.Q{i:00}"),
        ..Enumerable.Range(1, 18).Select(i => $"CultureScan.Q{i:00}"),
        ..Enumerable.Range(1, 25).Select(i => $"ValuesScan.Q{i:00}"),
    ];

    private static readonly Regex EnglishLeak = new(
        @"\b(working|thinking|making|helping|listening|taking|investigating|diving|inventing|instructions|schedule|colleagues|customers|someone|exactly|without|tasks|with)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [Fact]
    public void Values_q14_uses_overtref()
    {
        var nl = UiStrings.Get("ValuesScan.Q14", "nl");
        Assert.Contains("overtref", nl, StringComparison.Ordinal);
        Assert.DoesNotContain("overtreft", nl, StringComparison.Ordinal);
    }

    [Fact]
    public void Test_flow_continue_lines_are_not_half_english()
    {
        foreach (var language in new[] { "pl", "ro" })
        {
            var cont = UiStrings.Get("TestFlow.ContinueAt", language);
            var bubble = UiStrings.Get("TestFlow.Bubble.IntroContinue", language);
            Assert.DoesNotContain(" at ", cont, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("question", cont, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("You've", bubble, StringComparison.Ordinal);
            Assert.DoesNotContain("already", bubble, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("five", bubble, StringComparison.OrdinalIgnoreCase);
        }

        var ar = UiStrings.Get("TestFlow.ContinueAt", "ar");
        Assert.DoesNotContain("Continue", ar, StringComparison.Ordinal);
        Assert.Contains("{0}", ar, StringComparison.Ordinal);
    }

    [Fact]
    public void Deep_competence_duration_matches_bottom_minutes()
    {
        var minutes = TestDepthRules.MinutesFor(AssessmentKind.Competence, TestDepthLevel.Bottom);
        Assert.Equal(30, minutes);
        foreach (var language in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            var line = UiStrings.Get("Deep.CompetenceTeaser.SecureLine", language);
            Assert.Contains("30", line, StringComparison.Ordinal);
            Assert.DoesNotContain("20", line, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Deep_offer_lead_names_the_test_without_over()
    {
        var nl = UiStrings.Get("Deep.Offer.Lead", "nl");
        Assert.Contains("vragen in de {1}", nl, StringComparison.Ordinal);
        Assert.DoesNotContain("vragen over", nl, StringComparison.Ordinal);
        Assert.DoesNotContain("about", UiStrings.Get("Deep.Offer.Lead", "pl"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("about", UiStrings.Get("Deep.Offer.Lead", "ro"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Translated_test_questions_are_not_half_english()
    {
        foreach (var language in Languages)
        {
            foreach (var key in QuestionKeys)
            {
                var text = UiStrings.Get(key, language);
                Assert.False(
                    EnglishLeak.IsMatch(text),
                    $"{language} {key} still mixes English: {text}");
                Assert.False(text.StartsWith("PL:", StringComparison.Ordinal) || text.StartsWith("RO:", StringComparison.Ordinal));
            }
        }

        Assert.Equal(
            "Lubię pracować z innymi, żeby dokończyć zadanie.",
            UiStrings.Get("Competency.Q01", "pl"));
    }
}
