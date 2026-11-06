using Jobsy.Web.Models;

namespace Jobsy.Web.Features;

public enum OnboardingFinaleInsightKind
{
    Strength,
    Work,
    Value
}

public sealed record OnboardingFinaleInsight(
    OnboardingFinaleInsightKind Kind,
    string LabelKey,
    string Text);

public static class OnboardingFinaleInsights
{
    public static IReadOnlyList<OnboardingFinaleInsight> FromImpression(OnboardingImpression? impression, string andSeparator)
    {
        if (impression is null)
        {
            return [];
        }

        var items = new List<OnboardingFinaleInsight>();
        if (impression.Strengths.FirstOrDefault() is { Sentence: { Length: > 0 } } strength)
        {
            items.Add(new(
                OnboardingFinaleInsightKind.Strength,
                "Onboarding.Finish.Strength",
                strength.Sentence));
        }

        var riasec = impression.Riasec
            .Where(x => !string.IsNullOrWhiteSpace(x.Sentence))
            .Take(2)
            .Select(x => x.Sentence.Trim())
            .ToList();
        if (riasec.Count > 0)
        {
            var workText = riasec.Count == 1
                ? riasec[0]
                : $"{riasec[0]}{andSeparator}{riasec[1]}";
            items.Add(new(
                OnboardingFinaleInsightKind.Work,
                "Onboarding.Finish.Work",
                workText));
        }

        if (impression.TopValue is { } value)
        {
            var text = !string.IsNullOrWhiteSpace(value.Sentence)
                ? value.Sentence
                : value.Label;
            if (!string.IsNullOrWhiteSpace(text))
            {
                items.Add(new(
                    OnboardingFinaleInsightKind.Value,
                    "Onboarding.Finish.Value",
                    text));
            }
        }

        return items;
    }
}
