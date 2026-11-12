using Jobsy.Core.Entities.Scholen;

namespace Jobsy.Tests.Scholen;

internal static class PupilG78RandomFixtures
{
    internal static (PupilResult Result, PupilProgress Progress) ForSeed(int seed)
    {
        var rng = new Random(seed);
        var answers = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var id = 9001; id <= 9060; id++)
        {
            answers[id.ToString()] = 1 + rng.Next(5);
        }

        var pupilId = Guid.NewGuid();
        var result = new PupilResult
        {
            PupilCodeId = pupilId,
            ScoringVersion = "g78-1",
            HollandCode = "SC",
            TopValue = "Connection",
            TopCulture = "Collaboration",
            CompetenceScoresJson = """{"Samenwerken":70,"Resultaatgerichtheid":60,"Stressbestendigheid":55,"Innovatie":50,"Extraversie":65}""",
            RiasecScoresJson = """{"Realistic":50,"Investigative":50,"Artistic":50,"Social":70,"Enterprising":50,"Conventional":50}""",
            ValuesScoresJson = """{"Autonomy":50,"Connection":70,"Achievement":50,"Stability":50,"Impact":50}""",
            CultureScoresJson = """{"Autonomy":50,"Informal":50,"Collaboration":60,"Flexibility":50,"Innovation":50,"PeopleFirst":55}"""
        };
        var progress = new PupilProgress
        {
            PupilCodeId = pupilId,
            AnswersJson = System.Text.Json.JsonSerializer.Serialize(answers),
            LikesJson = """["sport","lezen"]""",
            VakScoresJson = """{"rekenen":{"leuk":4,"goed":4},"engels":{"leuk":3,"goed":4}}"""
        };
        return (result, progress);
    }
}
