using System.Text.Json;

namespace Jobsy.Core.Rules;

/// <summary>
/// Employer-selected culture pillars (3–5) stored on the vacancy.
/// Weights map onto workplace competency scores from the competence Quick-Scan.
/// Candidate-facing copy stays in everyday Dutch.
/// </summary>
public static class CulturePillarCatalog
{
    public const int MinSelected = 3;
    public const int MaxSelected = 5;

    public static readonly CulturePillarDefinition[] All =
    [
        new("informeel", "Employer.Culture.Informeel", "informeel & handen uit de mouwen",
            Samenwerken: 70, Resultaat: 55, Stress: 65, Innovatie: 60, Extraversie: 80),
        new("groei", "Employer.Culture.Groei", "groei & innovatie",
            Samenwerken: 60, Resultaat: 70, Stress: 60, Innovatie: 88, Extraversie: 65),
        new("stabiel", "Employer.Culture.Stabiel", "stabiel & gestructureerd",
            Samenwerken: 55, Resultaat: 85, Stress: 80, Innovatie: 40, Extraversie: 45),
        new("zelfstandig", "Employer.Culture.Zelfstandig", "zelfstandig & resultaatgericht",
            Samenwerken: 45, Resultaat: 90, Stress: 70, Innovatie: 55, Extraversie: 40),
        new("samen", "Employer.Culture.Samen", "samen & klantgericht",
            Samenwerken: 90, Resultaat: 60, Stress: 65, Innovatie: 50, Extraversie: 75),
        new("kalm", "Employer.Culture.Kalm", "kalm onder druk",
            Samenwerken: 60, Resultaat: 65, Stress: 90, Innovatie: 45, Extraversie: 50),
        new("creatief", "Employer.Culture.Creatief", "creatief & nieuwsgierig",
            Samenwerken: 55, Resultaat: 50, Stress: 55, Innovatie: 92, Extraversie: 70),
        new("zorgvuldig", "Employer.Culture.Zorgvuldig", "zorgvuldig & betrouwbaar",
            Samenwerken: 65, Resultaat: 88, Stress: 70, Innovatie: 40, Extraversie: 45)
    ];

    private static readonly Dictionary<string, CulturePillarDefinition> ById =
        All.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

    public static bool TryGet(string? id, out CulturePillarDefinition pillar)
    {
        pillar = null!;
        return !string.IsNullOrWhiteSpace(id) && ById.TryGetValue(id.Trim(), out pillar!);
    }

    public static IReadOnlyList<string> Normalize(IEnumerable<string>? raw)
    {
        if (raw is null)
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<string>();
        foreach (var item in raw)
        {
            if (!TryGet(item, out var pillar) || !seen.Add(pillar.Id))
            {
                continue;
            }

            list.Add(pillar.Id);
            if (list.Count == MaxSelected)
            {
                break;
            }
        }

        return list;
    }

    public static bool HasProfile(IEnumerable<string>? raw)
    {
        var n = Normalize(raw).Count;
        return n is >= MinSelected and <= MaxSelected;
    }

    public static string? Serialize(IEnumerable<string>? raw)
    {
        var ids = Normalize(raw);
        return ids.Count == 0 ? null : JsonSerializer.Serialize(ids);
    }

    public static IReadOnlyList<string> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<string[]>(json);
            return Normalize(parsed);
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static IReadOnlyList<string> Labels(IEnumerable<string>? ids)
        => Normalize(ids).Select(id => ById[id].Label).ToList();
}

public sealed record CulturePillarDefinition(
    string Id,
    string LabelKey,
    string Label,
    int Samenwerken,
    int Resultaat,
    int Stress,
    int Innovatie,
    int Extraversie);
