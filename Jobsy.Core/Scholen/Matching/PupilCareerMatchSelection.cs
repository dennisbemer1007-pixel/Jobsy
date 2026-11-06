namespace Jobsy.Core.Scholen.Matching;

public static class PupilCareerMatchSelection
{
    public static IReadOnlyList<T> SelectTop<T>(
        IEnumerable<T> rankedDescending,
        Func<T, double> score,
        Func<T, string?> groupKey,
        double minMatch = PupilCareerMatchWeights.DefaultMinMatch,
        int maxCount = PupilCareerMatchWeights.MaxSuggestions,
        int maxPerGroup = PupilCareerMatchWeights.MaxPerGroup)
    {
        var threshold = Math.Clamp(minMatch, 0, 1);
        var groupCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var result = new List<T>();
        foreach (var item in rankedDescending)
        {
            if (score(item) < threshold)
            {
                break;
            }

            var group = groupKey(item);
            if (!string.IsNullOrWhiteSpace(group))
            {
                var count = groupCounts.GetValueOrDefault(group);
                if (count >= maxPerGroup)
                {
                    continue;
                }

                groupCounts[group] = count + 1;
            }

            result.Add(item);
            if (result.Count >= maxCount)
            {
                break;
            }
        }

        return result;
    }
}
