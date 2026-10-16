using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen.PupilPassport;

/// <summary>Fixed side-job / snuffelstage types for VO passports (D9: never real vacancies).</summary>
public static class PupilVoSideJobCatalog
{
    public sealed record SideJobDef(string Key, char RiasecLetter, string[] LikeKeys, int MinAge);

    private static readonly SideJobDef[] All =
    [
        new("kinderboerderij", 'S', ["dieren", "kleine-kinderen"], 15),
        new("dierenkliniek-snuffel", 'S', ["dieren"], 15),
        new("drogisterij-vakken", 'C', ["koken", "puzzels"], 15),
        new("bibliotheek", 'C', ["lezen", "puzzels"], 15),
        new("sportclub", 'S', ["sport"], 15),
        new("techniek-werkplaats", 'R', ["techniek", "bouwen"], 15),
        new("horeca-help", 'E', ["koken"], 16),
        new("zorg-thuis", 'S', ["kleine-kinderen"], 16),
    ];

    public static IReadOnlyList<PupilPassportSideJobType> Pick(
        string? hollandCode,
        IReadOnlyList<string> likeKeys,
        int pupilAgeYears)
    {
        var letters = hollandCode?.Select(char.ToUpperInvariant).ToList() ?? ['S', 'I'];
        var scored = All
            .Where(j => pupilAgeYears >= j.MinAge && pupilAgeYears >= YouthLaborRules.MinWorkingAgeYears)
            .Select(j =>
            {
                var score = 0;
                if (letters.Contains(j.RiasecLetter))
                {
                    score += 2;
                }

                score += likeKeys.Count(l => j.LikeKeys.Contains(l, StringComparer.OrdinalIgnoreCase));
                return (j, score);
            })
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.j.Key, StringComparer.Ordinal)
            .Take(3)
            .Select(x => ToType(x.j))
            .ToList();

        return scored;
    }

    private static PupilPassportSideJobType ToType(SideJobDef def)
        => new(
            PupilPassportCopy.Get($"LeerlingPassport.Vo.SideJob.{def.Key}.Title"),
            PupilPassportCopy.Get($"LeerlingPassport.Vo.SideJob.{def.Key}.Sub"),
            [
                PupilPassportCopy.Get("LeerlingPassport.Vo.SideJob.Meta.Weekend"),
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    PupilPassportCopy.Get("LeerlingPassport.Vo.SideJob.Meta.FromAge"),
                    def.MinAge)
            ],
            PupilPassportCopy.Get($"LeerlingPassport.Vo.SideJob.{def.Key}.Why"));
}
