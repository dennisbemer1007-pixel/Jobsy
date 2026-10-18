using System.Text.RegularExpressions;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public sealed class AfterMatchSensitiveContentRulesTests
{
    private static (Dictionary<string, string> Nl, Dictionary<string, string> En) LoadAfterMatchStrings()
    {
        var nl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var en = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ro = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ar = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        UiStringsAfterMatch.MergeAll(nl, en, pl, ro, ar);
        return (nl, en);
    }

    private static readonly Regex EmDash = new(@"\u2014|—", RegexOptions.CultureInvariant);
    private static readonly Regex ScorePattern = new(@"\b\d+\s*%|\b\d+/\d+\b|sterren|stars?\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [Fact]
    public void Detect_phone_and_block_rules_for_after_match_messaging()
    {
        Assert.Equal(SensitiveMessageContentRules.SensitiveKind.Phone,
            SensitiveMessageContentRules.Detect("Bel me op 06 12 34 56 78"));
        Assert.Equal(SensitiveMessageContentRules.SensitiveKind.None,
            SensitiveMessageContentRules.Detect("Tot morgen op de afspraak"));

        var share = AfterMatchContactShareState.From(InterviewStatus.Proposing, null);
        Assert.Equal(
            ApplicationMessageBlockReason.IbanBeforeOffer,
            SensitiveMessageContentRules.BlockReasonForEmployer(
                SensitiveMessageContentRules.SensitiveKind.Iban,
                share));
    }

    [Fact]
    public void Candidate_nl_strings_have_no_em_dash_or_scores()
    {
        var (nl, _) = LoadAfterMatchStrings();
        foreach (var key in nl.Keys.Where(k => k.StartsWith("AfterMatch.", StringComparison.Ordinal)))
        {
            var text = nl[key];
            Assert.False(EmDash.IsMatch(text), $"Key {key} contains an em dash: {text}");
            if (key.Contains("Screen", StringComparison.Ordinal) && key.Contains("Kandidaat", StringComparison.Ordinal) == false)
            {
                // All candidate-facing screen copy (odd screens + shared timeline/messages)
                if (IsCandidateFacingKey(key))
                {
                    Assert.False(ScorePattern.IsMatch(text), $"Key {key} looks like a score: {text}");
                }
            }
        }
    }

    [Fact]
    public void AfterMatch_english_table_has_matching_keys()
    {
        var (nl, en) = LoadAfterMatchStrings();
        var nlKeys = nl.Keys.ToHashSet(StringComparer.Ordinal);
        var enKeys = en.Keys.ToHashSet(StringComparer.Ordinal);
        Assert.Equal(nlKeys, enKeys);
    }

    private static bool IsCandidateFacingKey(string key)
    {
        if (key.Contains("Screen02", StringComparison.Ordinal)
            || key.Contains("Screen03", StringComparison.Ordinal)
            || key.Contains("Screen09", StringComparison.Ordinal)
            || key.Contains("Screen11", StringComparison.Ordinal)
            || key.Contains("Screen12", StringComparison.Ordinal))
        {
            return false;
        }

        return key.Contains("Screen", StringComparison.Ordinal)
               || key.Contains("Timeline", StringComparison.Ordinal)
               || key.Contains("What.", StringComparison.Ordinal)
               || key.Contains("Messages.", StringComparison.Ordinal);
    }
}
