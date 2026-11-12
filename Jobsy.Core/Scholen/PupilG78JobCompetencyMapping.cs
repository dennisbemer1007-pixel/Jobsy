using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Job competencies from <see cref="PupilDreamJobRoutes"/> (no invented labels).</summary>
internal static class PupilG78JobCompetencyMapping
{
    internal sealed record JobCompetencyRequirement(
        string Category,
        string JobAsksLabel,
        PupilNeedSignal SourceSignal);

    public static IReadOnlyList<JobCompetencyRequirement> ForJob(string jobKey)
    {
        var route = PupilDreamJobRoutes.TryGet(jobKey);
        if (route is null)
        {
            return [];
        }

        var list = new List<JobCompetencyRequirement>();
        foreach (var need in route.Needs)
        {
            if (TryMapNeed(need, out var req))
            {
                list.Add(req);
            }
        }

        return list
            .DistinctBy(x => x.Category)
            .Take(4)
            .ToList();
    }

    private static bool TryMapNeed(PupilDreamJobNeed need, out JobCompetencyRequirement req)
    {
        req = default!;
        return need.Signal switch
        {
            PupilNeedSignal.CompetenceSamenwerken => Set(
                CompetencyTestCatalog.Samenwerken,
                "goed kunnen samenwerken",
                need.Signal,
                out req),
            PupilNeedSignal.CompetenceResultaat => Set(
                CompetencyTestCatalog.Resultaatgerichtheid,
                "netjes en precies werken",
                need.Signal,
                out req),
            PupilNeedSignal.CompetenceStress => Set(
                CompetencyTestCatalog.Stressbestendigheid,
                "kalm blijven als het druk is",
                need.Signal,
                out req),
            PupilNeedSignal.CompetenceInnovatie => Set(
                CompetencyTestCatalog.Innovatie,
                "nieuwe ideeën bedenken",
                need.Signal,
                out req),
            PupilNeedSignal.CompetenceExtraversie => Set(
                CompetencyTestCatalog.Extraversie,
                "makkelijk met mensen praten",
                need.Signal,
                out req),
            _ => false
        };
    }

    private static bool Set(
        string category,
        string label,
        PupilNeedSignal signal,
        out JobCompetencyRequirement req)
    {
        req = new JobCompetencyRequirement(category, label, signal);
        return true;
    }
}
