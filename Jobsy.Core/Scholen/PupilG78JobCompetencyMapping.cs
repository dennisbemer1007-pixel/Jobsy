using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen;

/// <summary>Job-specific competency labels and explicit evidence item ids (competence 9001–9015 or career 9016–9030).</summary>
internal static class PupilG78JobCompetencyMapping
{
    internal sealed record JobCompetencyRequirement(
        string JobKey,
        string SlotKey,
        string JobAsksLabel,
        IReadOnlyList<int> EvidenceItemIds);

    private static readonly Dictionary<string, IReadOnlyList<JobCompetencyRequirement>> Explicit =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["fotograaf"] =
            [
                new("fotograaf", "detail", "Oog voor detail", [9021, 9027, 9012, 9008]),
                new("fotograaf", "creatief", "Creativiteit", [9004, 9009, 9018, 9023]),
                new("fotograaf", "mensen", "Omgaan met mensen", [9006, 9005, 9030, 9010]),
            ],
            ["hovenier"] =
            [
                new("hovenier", "buiten", "Fysiek buiten werken", [9016, 9028, 9022]),
                new("hovenier", "doorzet", "Doorzetten", [9002, 9012]),
                new("hovenier", "samen", "Samenwerken", [9006, 9001, 9011]),
            ],
            ["bioloog"] =
            [
                new("bioloog", "onderzoek", "Onderzoeken", [9017, 9025]),
                new("bioloog", "nauw", "Nauwkeurig werken", [9021, 9027, 9012]),
                new("bioloog", "doorzet", "Doorzetten", [9012, 9002]),
            ],
        };

    private static readonly Dictionary<string, IReadOnlyList<JobCompetencyRequirement>> FallbackByNeed =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Precies"] = [new("", "precies", "Nauwkeurig werken", [9021, 9027, 9012])],
            ["Nauwkeurig"] = [new("", "nauw", "Nauwkeurig werken", [9021, 9027, 9012])],
            ["Netjes"] = [new("", "netjes", "Nauwkeurig werken", [9021, 9027, 9012])],
            ["Maken"] = [new("", "doorzet", "Doorzetten", [9002, 9012])],
            ["Geduld"] = [new("", "kalm", "Kalm blijven onder druk", [9003, 9008, 9013])],
            ["Kalm"] = [new("", "kalm", "Kalm blijven onder druk", [9003, 9008, 9013])],
            ["Stress"] = [new("", "kalm", "Kalm blijven onder druk", [9003, 9008, 9013])],
            ["Nieuwsgierig"] = [new("", "idee", "Nieuwe ideeën bedenken", [9004, 9009, 9014])],
            ["Ideeën"] = [new("", "idee", "Nieuwe ideeën bedenken", [9004, 9009, 9014])],
            ["Nieuw"] = [new("", "idee", "Nieuwe ideeën bedenken", [9004, 9009, 9014])],
            ["Mensen"] = [new("", "mensen", "Omgaan met mensen", [9005, 9010, 9006])],
            ["Samen"] = [new("", "samen", "Samenwerken", [9006, 9001, 9011])],
        };

    public static IReadOnlyList<JobCompetencyRequirement> ForJob(string jobKey)
    {
        if (Explicit.TryGetValue(jobKey, out var list))
        {
            return list;
        }

        var route = PupilDreamJobRoutes.TryGet(jobKey);
        if (route is null)
        {
            return [];
        }

        var built = new List<JobCompetencyRequirement>();
        foreach (var need in route.Needs)
        {
            if (need.Signal is not (PupilNeedSignal.CompetenceSamenwerken
                or PupilNeedSignal.CompetenceResultaat
                or PupilNeedSignal.CompetenceStress
                or PupilNeedSignal.CompetenceInnovatie
                or PupilNeedSignal.CompetenceExtraversie))
            {
                continue;
            }

            if (!FallbackByNeed.TryGetValue(need.NeedKey, out var templates) || templates.Count == 0)
            {
                continue;
            }

            var t = templates[0];
            built.Add(new JobCompetencyRequirement(jobKey, $"{jobKey}-{t.SlotKey}", t.JobAsksLabel, t.EvidenceItemIds));
        }

        return built
            .DistinctBy(x => x.SlotKey)
            .Take(4)
            .ToList();
    }
}
