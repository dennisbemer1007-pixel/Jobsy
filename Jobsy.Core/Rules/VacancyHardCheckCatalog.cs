namespace Jobsy.Core.Rules;

/// <summary>
/// Non-negotiable selection gates on a vacancy: medical, vision, fitness.
/// Missing proof on the candidate profile is a dealbreaker.
/// </summary>
public static class VacancyHardCheckCatalog
{
    public const string Medical = "medical";
    public const string Eye = "eye";
    public const string Fitness = "fitness";

    public static readonly (string Kind, string Label)[] Suggested =
    [
        (Medical, "Medische keuring"),
        (Eye, "Ogentest"),
        (Fitness, "Fysieke fitheidstest")
    ];

    private static readonly Dictionary<string, string[]> Evidence = new(StringComparer.Ordinal)
    {
        [Medical] = ["medische keuring", "klasse 1", "medical class"],
        [Eye] = ["ogentest", "visus"],
        [Fitness] = ["fitheidstest", "fysieke keuring", "conditietest"]
    };

    public static string? NormalizeKind(string? raw)
    {
        var folded = CareerOccupationKeys.Fold(raw ?? "");
        return folded switch
        {
            "medical" or "medisch" or "medische keuring" => Medical,
            "eye" or "ogen" or "ogentest" => Eye,
            "fitness" or "fitheid" or "fysieke fitheidstest" => Fitness,
            _ => null
        };
    }

    public static string Label(string kind) => kind switch
    {
        Medical => "Medische keuring",
        Eye => "Ogentest",
        Fitness => "Fysieke fitheidstest",
        _ => kind
    };

    public static bool CandidateMeets(string kind, CandidateEvidence evidence)
    {
        if (!Evidence.TryGetValue(kind, out var keys))
        {
            return false;
        }

        foreach (var key in keys)
        {
            var folded = CareerOccupationKeys.Fold(key);
            if (folded.Length >= 3
                && (CareerOccupationKeys.Hits(evidence.Blob, folded)
                    || evidence.Blob.Contains(folded, StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    public readonly record struct CandidateEvidence(string Blob)
    {
        public static CandidateEvidence From(Contracts.CandidatePreferencesDto? prefs)
        {
            var parts = new List<string>();
            if (prefs?.Educations is not null)
            {
                parts.AddRange(prefs.Educations);
            }

            if (prefs?.Certificates is not null)
            {
                parts.AddRange(prefs.Certificates.Select(c => c.Name));
            }

            return new CandidateEvidence(CareerOccupationKeys.Fold(string.Join(' ', parts)));
        }
    }
}
