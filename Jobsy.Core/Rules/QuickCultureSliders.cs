namespace Jobsy.Core.Rules;

/// <summary>
/// Maps the six "Zo werken wij" registration sliders (1–5) onto Cultuurscan answers.
/// For dimension k (1–6) with value v: item 2k−1 = v, item 2k (reversed) = 6 − v.
/// </summary>
public static class QuickCultureSliders
{
    public const int Min = 1;
    public const int Max = 5;
    public const int DimensionCount = 6;

    public static bool IsValidValue(int value) => value is >= Min and <= Max;

    /// <summary>
    /// Build culture answers 1–12 from slider values keyed by <see cref="CulturePersonalityCatalog.CultureDimensionCodes"/>.
    /// Missing dimensions default to 3 (neutral).
    /// </summary>
    public static IReadOnlyDictionary<int, int> ToAnswers(IReadOnlyDictionary<string, int>? sliders)
    {
        var answers = new Dictionary<int, int>(12);
        for (var k = 1; k <= DimensionCount; k++)
        {
            var code = CulturePersonalityCatalog.CultureDimensionCodes[k - 1];
            var v = 3;
            if (sliders is not null
                && sliders.TryGetValue(code, out var raw)
                && IsValidValue(raw))
            {
                v = raw;
            }
            else if (sliders is not null)
            {
                // Accept case-insensitive key lookup.
                foreach (var (key, value) in sliders)
                {
                    if (string.Equals(key, code, StringComparison.OrdinalIgnoreCase) && IsValidValue(value))
                    {
                        v = value;
                        break;
                    }
                }
            }

            answers[2 * k - 1] = v;
            answers[2 * k] = 6 - v;
        }

        return answers;
    }

    /// <summary>Read slider value for dimension k from stored answers (v = item 2k−1).</summary>
    public static IReadOnlyDictionary<string, int> FromAnswers(IReadOnlyDictionary<int, int>? answers)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var k = 1; k <= DimensionCount; k++)
        {
            var code = CulturePersonalityCatalog.CultureDimensionCodes[k - 1];
            var itemId = 2 * k - 1;
            var v = 3;
            if (answers is not null && answers.TryGetValue(itemId, out var raw) && IsValidValue(raw))
            {
                v = raw;
            }

            result[code] = v;
        }

        return result;
    }

    public static string? Validate(IReadOnlyDictionary<string, int>? sliders)
    {
        if (sliders is null || sliders.Count == 0)
        {
            return null;
        }

        foreach (var (key, value) in sliders)
        {
            if (!CulturePersonalityCatalog.CultureDimensionCodes.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                return $"Onbekende cultuurdimensie: {key}.";
            }

            if (!IsValidValue(value))
            {
                return "Elke schuif moet tussen 1 en 5 liggen.";
            }
        }

        return null;
    }
}
