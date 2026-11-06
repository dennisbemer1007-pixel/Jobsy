namespace Jobsy.Core.Careers;

/// <summary>Resolves pasted ESCO ids or Dutch occupation names for a small admin pilot.</summary>
public static class OccupationDaySelection
{
    public const int MaxIds = 40;

    public static OccupationDayPick Resolve(string? raw)
    {
        var jobs = new List<Occupation>();
        var unknown = new List<string>();
        foreach (var part in Split(raw))
        {
            if (jobs.Count >= MaxIds)
            {
                break;
            }

            var job = OccupationCatalog.Shared.Get(part) ?? OccupationCatalog.Shared.Resolve(part);
            if (job is null)
            {
                var hits = OccupationCatalog.Shared.Search(part, 1);
                if (hits.Count == 1)
                {
                    job = OccupationCatalog.Shared.Get(hits[0].EscoId);
                }
            }

            if (job is null)
            {
                unknown.Add(part);
                continue;
            }

            if (jobs.All(item => !string.Equals(item.Id, job.Id, StringComparison.OrdinalIgnoreCase)))
            {
                jobs.Add(job);
            }
        }

        return new OccupationDayPick(jobs, unknown);
    }

    private static IEnumerable<string> Split(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            yield break;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var piece in raw.Split([',', ';', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (piece.Length == 0 || piece.Length > 160 || !seen.Add(piece))
            {
                continue;
            }

            yield return piece;
        }
    }
}

public sealed record OccupationDayPick(IReadOnlyList<Occupation> Jobs, IReadOnlyList<string> Unknown);
