using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>
/// Fixed dream-job ideas per RIASEC letter (6 each). Chosen by top-2 letters, deterministic.
/// Groep 7/8 uses a younger pool and prefers jobs that match the pupil's likes.
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

    private static readonly IReadOnlyDictionary<char, IReadOnlyList<string>> Groep78ByLetter =
        new Dictionary<char, IReadOnlyList<string>>
        {
            ['R'] = ["bakker", "hovenier", "timmerman", "elektricien", "automonteur", "schilder"],
            ['I'] = ["dierenarts", "bioloog", "programmeur", "wetenschapper", "ingenieur", "doktersassistent"],
            ['A'] = ["kok", "fotograaf", "muzikant", "grafisch-ontwerper", "acteur", "contentmaker"],
            ['S'] = ["dierenverzorger", "leraar", "verpleegkundige", "pedagogisch-medewerker", "fysiotherapeut", "personal-trainer"],
            ['E'] = ["ondernemer", "politie", "evenementenorganisator", "verkoper", "journalist", "brandweer"],
            ['C'] = ["kapper", "programmeur", "doktersassistent", "logistiek-medewerker", "ict-medewerker", "verkoper"]
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> LikeJobs =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["dieren"] = ["dierenverzorger", "dierenarts", "bioloog"],
            ["sport"] = ["personal-trainer", "profsporter", "fysiotherapeut"],
            ["techniek"] = ["elektricien", "programmeur", "ingenieur", "automonteur"],
            ["buiten"] = ["hovenier", "brandweer"],
            ["natuur"] = ["bioloog", "hovenier"],
            ["gamen"] = ["game-developer", "programmeur"],
            ["computers"] = ["programmeur", "ict-medewerker"],
            ["programmeren"] = ["programmeur", "ict-medewerker"],
            ["muziek"] = ["muzikant"],
            ["koken"] = ["kok", "bakker"],
            ["tekenen"] = ["grafisch-ontwerper", "fotograaf"],
            ["autos"] = ["automonteur"],
            ["bouwen"] = ["timmerman", "bouwkundige"],
            ["kleine-kinderen"] = ["pedagogisch-medewerker", "leraar"],
            ["dansen"] = ["acteur", "personal-trainer"],
            ["filmpjes"] = ["contentmaker", "fotograaf"]
        };

    /// <summary>Returns exactly 4 catalog keys from the top-1 then top-2 letter pools (deduped, stable order).</summary>
    public static IReadOnlyList<string> ForLetters(IReadOnlyList<char> letters)
        => ForLetters(letters, PupilQuestionSet.Vo, null);

    public static IReadOnlyList<string> ForLetters(
        IReadOnlyList<char> letters,
        PupilQuestionSet set,
        IReadOnlyList<string>? likeKeys)
    {
        var pool = set == PupilQuestionSet.Groep78 ? Groep78ByLetter : ByLetter;
        var result = new List<string>(4);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (set == PupilQuestionSet.Groep78 && likeKeys is not null)
        {
            foreach (var like in likeKeys)
            {
                if (!LikeJobs.TryGetValue(like, out var jobs))
                {
                    continue;
                }

                foreach (var key in jobs)
                {
                    Add(result, seen, key);
                    if (result.Count == 4)
                    {
                        return result;
                    }
                }
            }
        }

        foreach (var letter in letters.Concat(['S', 'R', 'I', 'A', 'E', 'C']))
        {
            var upper = char.ToUpperInvariant(letter);
            if (!pool.TryGetValue(upper, out var letterPool))
            {
                continue;
            }

            foreach (var key in letterPool)
            {
                Add(result, seen, key);
                if (result.Count == 4)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private static void Add(List<string> result, HashSet<string> seen, string key)
    {
        if (DreamJobCatalog.All.All(j => !string.Equals(j.Key, key, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        if (seen.Add(key))
        {
            result.Add(key);
        }
    }

    public static string TitleFor(string key)
        => DreamJobCatalog.All.FirstOrDefault(j =>
               string.Equals(j.Key, key, StringComparison.OrdinalIgnoreCase))?.TitleNl
           ?? key;
}
