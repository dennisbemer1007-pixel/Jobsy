namespace Jobsy.Core.Rules;

/// <summary>
/// Optional employer team culture-fit items (T1–T6 core, T7–T10 values extension) per psychometrie doc §3.2.
/// Likert 1–5 maps to the same dimensions as candidate modules C and D.
/// </summary>
public static class EmployerFitQuestionnaireCatalog
{
    public const int LikertMin = 1;
    public const int LikertMax = 5;

    public sealed record TeamQuestion(string Code, string TextKey, string DimensionKind);

    public static readonly TeamQuestion[] CoreCultureQuestions =
    [
        new("Autonomy", "EmployerFit.T1", "culture"),
        new("Informal", "EmployerFit.T2", "culture"),
        new("Collaboration", "EmployerFit.T3", "culture"),
        new("Flexibility", "EmployerFit.T4", "culture"),
        new("Innovation", "EmployerFit.T5", "culture"),
        new("PeopleFirst", "EmployerFit.T6", "culture"),
    ];

    public static readonly TeamQuestion[] ExtensionValuesQuestions =
    [
        new("Achievement", "EmployerFit.T7", "values"),
        new("Stability", "EmployerFit.T8", "values"),
        new("Connection", "EmployerFit.T9", "values"),
        new("Impact", "EmployerFit.T10", "values"),
    ];

    public static bool IsValidLikert(int value) => value is >= LikertMin and <= LikertMax;

    /// <summary>Culture sliders for <see cref="QuickCultureSliders"/> from T1–T6 answers.</summary>
    public static IReadOnlyDictionary<string, int> ToCultureSliders(IReadOnlyDictionary<string, int>? answers)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in CoreCultureQuestions)
        {
            if (answers is not null && TryGet(answers, q.Code, out var v) && IsValidLikert(v))
            {
                result[q.Code] = v;
            }
        }

        return result;
    }

    public static int LikertToPercent(int likert)
        => (int)Math.Clamp(Math.Round((likert - LikertMin) / (double)(LikertMax - LikertMin) * 100), 0, 100);

    public static SchwartzValuesScores? ToValuesScores(IReadOnlyDictionary<string, int>? extensionAnswers)
    {
        if (extensionAnswers is null || extensionAnswers.Count == 0)
        {
            return null;
        }

        int? achievement = null;
        int? stability = null;
        int? connection = null;
        int? impact = null;
        foreach (var q in ExtensionValuesQuestions)
        {
            if (!TryGet(extensionAnswers, q.Code, out var v) || !IsValidLikert(v))
            {
                continue;
            }

            var pct = LikertToPercent(v);
            switch (q.Code)
            {
                case SchwartzValuesCatalog.Achievement: achievement = pct; break;
                case SchwartzValuesCatalog.Stability: stability = pct; break;
                case SchwartzValuesCatalog.Connection: connection = pct; break;
                case SchwartzValuesCatalog.Impact: impact = pct; break;
            }
        }

        if (achievement is null && stability is null && connection is null && impact is null)
        {
            return null;
        }

        return new SchwartzValuesScores(
            Autonomy: 50,
            Connection: connection ?? 50,
            Achievement: achievement ?? 50,
            Stability: stability ?? 50,
            Impact: impact ?? 50);
    }

    public static string? ValidateCultureAnswers(IReadOnlyDictionary<string, int>? answers)
    {
        if (answers is null || answers.Count == 0)
        {
            return null;
        }

        foreach (var (key, value) in answers)
        {
            if (!CoreCultureQuestions.Any(q => string.Equals(q.Code, key, StringComparison.OrdinalIgnoreCase)))
            {
                return $"Onbekende teamcultuur-vraag: {key}.";
            }

            if (!IsValidLikert(value))
            {
                return "Elke stelling moet tussen 1 en 5 liggen.";
            }
        }

        return null;
    }

    public static string? ValidateValuesExtension(IReadOnlyDictionary<string, int>? answers)
        => ValidateExtensionAnswers(answers, ExtensionValuesQuestions);

    private static string? ValidateExtensionAnswers(
        IReadOnlyDictionary<string, int>? answers,
        IReadOnlyList<TeamQuestion> allowed)
    {
        if (answers is null || answers.Count == 0)
        {
            return null;
        }

        foreach (var (key, value) in answers)
        {
            if (!allowed.Any(q => string.Equals(q.Code, key, StringComparison.OrdinalIgnoreCase)))
            {
                return $"Onbekende waarde-vraag: {key}.";
            }

            if (!IsValidLikert(value))
            {
                return "Elke stelling moet tussen 1 en 5 liggen.";
            }
        }

        return null;
    }

    private static bool TryGet(IReadOnlyDictionary<string, int> answers, string code, out int value)
    {
        if (answers.TryGetValue(code, out value))
        {
            return true;
        }

        foreach (var (key, v) in answers)
        {
            if (string.Equals(key, code, StringComparison.OrdinalIgnoreCase))
            {
                value = v;
                return true;
            }
        }

        value = 0;
        return false;
    }
}
