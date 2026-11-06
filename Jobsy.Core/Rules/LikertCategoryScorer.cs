namespace Jobsy.Core.Rules;

/// <summary>
/// Shared Likert category averaging used by adult catalogs and the pupil question bank.
/// Reverse items: <c>LikertMax + LikertMin − raw</c>, then <see cref="LikertAnswerJson.ToPercent"/>.
/// </summary>
public static class LikertCategoryScorer
{
    public static int? ScoreCategory(
        IEnumerable<(int Id, string Category, bool Reverse)> items,
        IReadOnlyDictionary<int, int> answers,
        string category)
    {
        var scored = new List<int>();
        foreach (var item in items)
        {
            if (!string.Equals(item.Category, category, StringComparison.Ordinal))
            {
                continue;
            }

            if (!answers.TryGetValue(item.Id, out var raw) || !LikertAnswerJson.IsValidAnswer(raw))
            {
                continue;
            }

            scored.Add(item.Reverse
                ? LikertAnswerJson.LikertMax + LikertAnswerJson.LikertMin - raw
                : raw);
        }

        return scored.Count == 0 ? null : LikertAnswerJson.ToPercent(scored);
    }
}
