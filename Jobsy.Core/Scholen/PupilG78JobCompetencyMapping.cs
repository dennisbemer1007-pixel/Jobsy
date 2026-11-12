using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>
/// Job competencies from <see cref="PupilDreamJobRoutes"/> only (one row per route need with a
/// <see cref="PupilNeedSignal"/> competence signal). Labels derive from the route <c>NeedKey</c>, not a global default list.
/// ESCO skills are not duplicated here; when a job has fewer than two competence needs in the route, only those are shown.
/// </summary>
internal static class PupilG78JobCompetencyMapping
{
    internal sealed record JobCompetencyRequirement(
        string JobKey,
        string NeedKey,
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
            if (!IsCompetenceSignal(need.Signal))
            {
                continue;
            }

            var label = Capitalize(LabelForNeedKey(need.NeedKey, need.Signal));
            list.Add(new JobCompetencyRequirement(
                jobKey,
                need.NeedKey,
                CategoryForSignal(need.Signal),
                label,
                need.Signal));
        }

        return list
            .DistinctBy(x => x.NeedKey)
            .Take(4)
            .ToList();
    }

    private static bool IsCompetenceSignal(PupilNeedSignal signal)
        => signal is PupilNeedSignal.CompetenceSamenwerken
            or PupilNeedSignal.CompetenceResultaat
            or PupilNeedSignal.CompetenceStress
            or PupilNeedSignal.CompetenceInnovatie
            or PupilNeedSignal.CompetenceExtraversie;

    private static string CategoryForSignal(PupilNeedSignal signal) => signal switch
    {
        PupilNeedSignal.CompetenceSamenwerken => CompetencyTestCatalog.Samenwerken,
        PupilNeedSignal.CompetenceResultaat => CompetencyTestCatalog.Resultaatgerichtheid,
        PupilNeedSignal.CompetenceStress => CompetencyTestCatalog.Stressbestendigheid,
        PupilNeedSignal.CompetenceInnovatie => CompetencyTestCatalog.Innovatie,
        PupilNeedSignal.CompetenceExtraversie => CompetencyTestCatalog.Extraversie,
        _ => ""
    };

    private static string LabelForNeedKey(string needKey, PupilNeedSignal signal) => needKey switch
    {
        "Precies" or "Nauwkeurig" or "Netjes" => "netjes en precies werken",
        "Maken" or "Verantwoordelijk" or "Concentratie" => "netjes maken en afmaken",
        "Geduld" or "Kalm" or "RustigBlijven" or "Stress" => "kalm blijven als het druk is",
        "Nieuwsgierig" or "Ideeën" or "Nieuw" or "Probleem" => "nieuwe ideeën bedenken",
        "Mensen" or "Overtuigen" => "makkelijk met mensen praten",
        "Samen" => "goed kunnen samenwerken",
        _ => signal switch
        {
            PupilNeedSignal.CompetenceSamenwerken => "goed kunnen samenwerken",
            PupilNeedSignal.CompetenceResultaat => "netjes en precies werken",
            PupilNeedSignal.CompetenceStress => "kalm blijven als het druk is",
            PupilNeedSignal.CompetenceInnovatie => "nieuwe ideeën bedenken",
            PupilNeedSignal.CompetenceExtraversie => "makkelijk met mensen praten",
            _ => needKey.ToLowerInvariant()
        }
    };

    private static string Capitalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return char.ToUpper(text[0]) + text[1..];
    }
}
