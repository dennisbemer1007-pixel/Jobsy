namespace Jobsy.Core.Rules;

/// <summary>
/// Maps general career-compass occupations onto a live vacancy (title, text, branche).
/// Independent of whether that occupation is currently advertised on Lobsy.
/// </summary>
public static class VacancyOccupationMatch
{
    public readonly record struct Fit(double Score01, string Title);

    public static Fit? TryFit(
        IReadOnlyList<CareerOccupationMatch>? occupations,
        IReadOnlyList<string>? workTypes,
        string? title,
        string? description)
    {
        if (occupations is not { Count: > 0 })
        {
            return null;
        }

        var blob = CareerOccupationKeys.Fold($"{title} {description} {string.Join(' ', workTypes ?? [])}");
        if (blob.Length == 0)
        {
            return null;
        }

        Fit? best = null;
        foreach (var occupation in occupations)
        {
            if (string.IsNullOrWhiteSpace(occupation.Title))
            {
                continue;
            }

            var keys = occupation.SearchKeys;
            var phrase = CareerOccupationKeys.Fold(occupation.Title);
            var phraseHit = phrase.Length >= 3 && CareerOccupationKeys.Hits(blob, phrase);
            var hits = keys.Count(key => CareerOccupationKeys.Hits(blob, key));
            var ratio = phraseHit
                ? 1.0
                : keys.Count == 0
                    ? 0
                    : hits / (double)keys.Count;
            if (ratio <= 0 || (!phraseHit && hits < 1))
            {
                continue;
            }

            var score = Math.Clamp(ratio * (occupation.Percent / 100.0), 0, 1);
            if (best is null || score > best.Value.Score01)
            {
                best = new Fit(score, occupation.Title);
            }
        }

        return best;
    }
}
