using System.Globalization;
using System.Text;

namespace Jobsy.Core.Rules;

/// <summary>Curated dream-job catalog for onboarding wizard v2 step 6.</summary>
public sealed record DreamJob(string Key, string TitleNl, string IconKey, string[] Synonyms);

public static class DreamJobCatalog
{
    public static readonly IReadOnlyList<DreamJob> All =
    [
        new("dierenarts", "Dierenarts", "paw-print", ["dierenartsassistent", "vet"]),
        new("piloot", "Piloot", "plane", ["vliegenier", "cockpit"]),
        new("advocaat", "Advocaat", "scale", ["jurist", "recht"]),
        new("leraar", "Leraar", "presentation", ["docent", "onderwijzer"]),
        new("arts", "Arts", "stethoscope", ["dokter", "huisarts", "chirurg"]),
        new("architect", "Architect", "drafting-compass", ["bouwkunst"]),
        new("kok", "Kok", "chef-hat", ["chef", "keuken"]),
        new("brandweer", "Brandweerman/-vrouw", "flame", ["brandweerman", "brandweervrouw"]),
        new("game-developer", "Game developer", "gamepad-2", ["gamedev", "spelontwikkelaar"]),
        new("astronaut", "Astronaut", "rocket", ["ruimtevaarder"]),
        new("politie", "Politieagent", "shield", ["agent", "politieagent"]),
        new("verpleegkundige", "Verpleegkundige", "heart-pulse", ["verpleger", "nurse"]),
        new("ondernemer", "Ondernemer", "briefcase", ["startup", "eigen baas"]),
        new("journalist", "Journalist", "mic", ["verslaggever", "redacteur"]),
        new("fotograaf", "Fotograaf", "camera", ["fotografie"]),
        new("kapper", "Kapper", "scissors", ["haarstylist", "barber"]),
        new("programmeur", "Programmeur", "code", ["developer", "software", "coder"]),
        new("bouwkundige", "Bouwkundige", "hard-hat", ["bouw"]),
        new("tandarts", "Tandarts", "smile", ["mondzorg"]),
        new("psycholoog", "Psycholoog", "brain", ["therapie"]),
        new("fysiotherapeut", "Fysiotherapeut", "activity", ["fysio"]),
        new("verloskundige", "Verloskundige", "baby", ["vroedvrouw"]),
        new("apotheker", "Apotheker", "pill", ["farmacie"]),
        new("rechter", "Rechter", "gavel", ["rechtspraak"]),
        new("notaris", "Notaris", "stamp", []),
        new("ingenieur", "Ingenieur", "cog", ["engineer", "techniek"]),
        new("elektricien", "Elektricien", "plug-zap", ["elektromonteur"]),
        new("automonteur", "Automonteur", "wrench", ["monteur auto", "APK"]),
        new("timmerman", "Timmerman", "hammer", ["timmervrouw", "hout"]),
        new("hovenier", "Hovenier", "trees", ["tuinman", "groenvoorziening"]),
        new("bioloog", "Bioloog", "leaf", ["biologie"]),
        new("wetenschapper", "Wetenschapper", "flask-conical", ["onderzoeker", "lab"]),
        new("grafisch-ontwerper", "Grafisch ontwerper", "pen-tool", ["designer", "vormgever"]),
        new("muzikant", "Muzikant", "music", ["muziek", "artiest"]),
        new("acteur", "Acteur", "drama", ["actrice", "toneel"]),
        new("profsporter", "Profsporter", "trophy", ["sporter", "atleet"]),
        new("personal-trainer", "Personal trainer", "dumbbell", ["fitnesscoach", "PT"]),
        new("contentmaker", "Contentmaker", "video", ["influencer", "creator"]),
        new("marketeer", "Marketeer", "megaphone", ["marketing"]),
        new("accountant", "Accountant", "calculator", ["boekhouder"]),
        new("makelaar", "Makelaar", "house", ["vastgoed"]),
        new("cabinepersoneel", "Cabinepersoneel", "luggage", ["stewardess", "purser"]),
        new("militair", "Militair", "medal", ["defensie", "soldaat"]),
        new("pedagogisch-medewerker", "Pedagogisch medewerker", "blocks", ["kinderopvang", "PM"]),
        new("dierenverzorger", "Dierenverzorger", "rabbit", ["dierenartsassistent", "asiel"]),
        new("evenementenorganisator", "Evenementenorganisator", "party-popper", ["event", "organisatie"]),
        new("data-scientist", "Data scientist", "chart-line", ["data", "analist"]),
        new("bakker", "Bakker", "croissant", ["banketbakker"])
    ];

    private static readonly Dictionary<string, DreamJob> ByKey =
        All.ToDictionary(j => j.Key, StringComparer.OrdinalIgnoreCase);

    public static DreamJob? FindByKey(string? key)
        => !string.IsNullOrWhiteSpace(key) && ByKey.TryGetValue(key.Trim(), out var job) ? job : null;

    public static DreamJob? FindByTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var needle = Normalize(title);
        return All.FirstOrDefault(j => Normalize(j.TitleNl) == needle);
    }

    /// <summary>
    /// Case- and diacritics-insensitive search: prefix on title first, then contains on title/synonyms.
    /// </summary>
    public static IReadOnlyList<DreamJob> Search(string? query, int take = 6)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var q = Normalize(query);
        if (q.Length == 0)
        {
            return [];
        }

        var prefix = new List<DreamJob>();
        var contains = new List<DreamJob>();
        foreach (var job in All)
        {
            var title = Normalize(job.TitleNl);
            if (title.StartsWith(q, StringComparison.Ordinal))
            {
                prefix.Add(job);
                continue;
            }

            if (title.Contains(q, StringComparison.Ordinal)
                || job.Synonyms.Any(s => Normalize(s).Contains(q, StringComparison.Ordinal)))
            {
                contains.Add(job);
            }
        }

        return prefix.Concat(contains).Take(Math.Clamp(take, 1, 24)).ToList();
    }

    public static string Normalize(string value)
    {
        var formD = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
