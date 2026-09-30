using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>
/// Fixed kid-friendly dream-job ideas per RIASEC letter (6 each). Chosen by top-2 letters, deterministic.
/// Keys match <see cref="DreamJobCatalog"/> where possible.
/// </summary>
public static class PupilRiasecJobIdeas
{
    public static readonly IReadOnlyDictionary<char, IReadOnlyList<string>> ByLetter =
        new Dictionary<char, IReadOnlyList<string>>
        {
            ['R'] = ["timmerman", "automonteur", "elektricien", "hovenier", "bouwkundige", "bakker"],
            ['I'] = ["wetenschapper", "bioloog", "data-scientist", "ingenieur", "dierenarts", "programmeur"],
            ['A'] = ["grafisch-ontwerper", "fotograaf", "muzikant", "acteur", "contentmaker", "kok"],
            ['S'] = ["leraar", "verpleegkundige", "pedagogisch-medewerker", "dierenverzorger", "fysiotherapeut", "personal-trainer"],
            ['E'] = ["ondernemer", "advocaat", "marketeer", "journalist", "evenementenorganisator", "politie"],
            ['C'] = ["accountant", "apotheker", "notaris", "makelaar", "kapper", "programmeur"]
        };

    /// <summary>Returns exactly 4 catalog keys from the top-1 then top-2 letter pools (deduped, stable order).</summary>
    public static IReadOnlyList<string> ForLetters(IReadOnlyList<char> letters)
    {
        var result = new List<string>(4);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var letter in letters.Concat(['S', 'R', 'I', 'A', 'E', 'C']))
        {
            var upper = char.ToUpperInvariant(letter);
            if (!ByLetter.TryGetValue(upper, out var pool))
            {
                continue;
            }

            foreach (var key in pool)
            {
                if (seen.Add(key))
                {
                    result.Add(key);
                    if (result.Count == 4)
                    {
                        return result;
                    }
                }
            }
        }

        return result;
    }

    public static string TitleFor(string key)
        => DreamJobCatalog.All.FirstOrDefault(j =>
               string.Equals(j.Key, key, StringComparison.OrdinalIgnoreCase))?.TitleNl
           ?? key;
}
