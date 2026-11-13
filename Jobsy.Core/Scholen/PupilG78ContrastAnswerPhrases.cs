using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Phrases from g78-2 chosen-pole answers (never the unchosen pole).</summary>
internal static class PupilG78ContrastAnswerPhrases
{
    public static bool IsContrastSession(PupilProgress? progress, PupilResult? result = null)
        => string.Equals(
               progress?.QuestionScoringVersion ?? result?.ScoringVersion,
               PupilG78ScoringVersions.V2,
               StringComparison.Ordinal)
           || string.Equals(result?.ScoringVersion, PupilG78ScoringVersions.V2, StringComparison.Ordinal);

    public static bool TryGetSubordinate(PupilQuestion q, int raw, out string subordinate)
    {
        subordinate = "";
        var fact = PupilQuestionStoryParaphrase.ContrastChosenFact(q, raw);
        if (fact is null || fact.Value.EffectiveScore < 4)
        {
            return false;
        }

        var sentence = PupilG78JeNarrative.AsSentence(fact.Value.Text).TrimEnd('.');
        if (PupilG78StoryFlow.ContrastLooksNegative(sentence))
        {
            return false;
        }

        if (sentence.StartsWith("Je ", StringComparison.Ordinal))
        {
            subordinate = "je " + sentence[3..];
        }
        else
        {
            subordinate = sentence;
        }

        return subordinate.Length > 0;
    }

    public static bool TryGetCareerLetter(PupilQuestion q, int raw, out char letter)
    {
        letter = default;
        var fact = PupilQuestionStoryParaphrase.ContrastChosenFact(q, raw);
        if (fact is null || fact.Value.EffectiveScore < 4)
        {
            return false;
        }

        return CategoryToLetter.TryGetValue(fact.Value.Category, out letter);
    }

    private static readonly Dictionary<string, char> CategoryToLetter = new(StringComparer.OrdinalIgnoreCase)
    {
        [CareerTestCatalog.Realistic] = 'R',
        [CareerTestCatalog.Investigative] = 'I',
        [CareerTestCatalog.Artistic] = 'A',
        [CareerTestCatalog.Social] = 'S',
        [CareerTestCatalog.Enterprising] = 'E',
        [CareerTestCatalog.Conventional] = 'C',
    };

    public static int ContrastEffective(PupilQuestion q, int raw)
    {
        if (raw is < 1 or > 5 || raw == 3)
        {
            return 0;
        }

        if (string.IsNullOrEmpty(q.CategoryB))
        {
            return raw is 1 or 2
                ? PupilG78ContrastLikertScorer.TowardA(raw)
                : PupilG78ContrastLikertScorer.TowardB(raw);
        }

        if (raw is 1 or 2)
        {
            return PupilG78ContrastLikertScorer.TowardA(raw);
        }

        return PupilG78ContrastLikertScorer.TowardB(raw);
    }
}
