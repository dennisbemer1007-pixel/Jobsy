using Jobsy.Core.Entities.Scholen;

namespace Jobsy.Tests.Scholen;

internal static class PupilG78RealisticFixtures
{
    internal static (PupilResult Result, PupilProgress Progress) HelpsOthers()
        => Build("SC", "Connection", "Collaboration",
            """["sport","dieren"]""",
            """{"tekenen":{"leuk":5,"goed":4},"muziek":{"leuk":5,"goed":3}}""",
            RealisticAnswers("SC"));

    internal static (PupilResult Result, PupilProgress Progress) LeadsGroup()
        => Build("ES", "Autonomy", "Informal",
            """["theater","lezen"]""",
            """{"geschiedenis":{"leuk":5,"goed":4},"engels":{"leuk":4,"goed":5}}""",
            RealisticAnswers("ES"));

    internal static (PupilResult Result, PupilProgress Progress) Maker()
        => Build("IR", "Achievement", "Flexibility",
            """["techniek","natuur"]""",
            """{"natuur-techniek":{"leuk":5,"goed":5},"rekenen":{"leuk":4,"goed":5}}""",
            RealisticAnswers("IR"));

    private static Dictionary<string, int> RealisticAnswers(string profile)
    {
        var d = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var id = 9001; id <= 9060; id++)
        {
            d[id.ToString()] = ScoreFor(id, profile);
        }

        return d;
    }

    private static int ScoreFor(int itemId, string profile)
    {
        var strong = profile switch
        {
            "SC" => itemId is 9003 or 9006 or 9007 or 9019 or 9021 or 9024 or 9030
                or 9032 or 9037 or 9042 or 9048 or 9053 or 9054 or 9059 or 9060,
            "ES" => itemId is 9005 or 9009 or 9010 or 9020 or 9026 or 9029 or 9031 or 9036
                or 9041 or 9043 or 9049 or 9050 or 9052 or 9058,
            "IR" => itemId is 9002 or 9008 or 9016 or 9017 or 9018 or 9022 or 9025 or 9028
                or 9033 or 9038 or 9043 or 9046 or 9051 or 9056,
            _ => false
        };
        if (strong)
        {
            return 5;
        }

        var low = profile switch
        {
            "SC" => itemId is 9014 or 9015 or 9020,
            "ES" => itemId is 9019 or 9024 or 9012,
            "IR" => itemId is 9020 or 9010 or 9026,
            _ => false
        };
        if (low)
        {
            return itemId switch
            {
                9019 when profile == "ES" => 1,
                _ => 2
            };
        }

        var mid = profile switch
        {
            "SC" => itemId % 4 == 0,
            "ES" => itemId % 5 == 1,
            "IR" => itemId % 6 == 2,
            _ => itemId % 3 == 0
        };
        if (mid)
        {
            return 4;
        }

        return 2 + (itemId * 7 + StringComparer.Ordinal.GetHashCode(profile)) % 3;
    }

    private static (PupilResult, PupilProgress) Build(
        string holland,
        string topValue,
        string topCulture,
        string likes,
        string vakJson,
        Dictionary<string, int> answers)
    {
        var id = Guid.NewGuid();
        var result = new PupilResult
        {
            PupilCodeId = id,
            ScoringVersion = "g78-1",
            HollandCode = holland,
            TopValue = topValue,
            TopCulture = topCulture,
            CompetenceScoresJson = """{"Samenwerken":80,"Resultaatgerichtheid":50,"Stressbestendigheid":50,"Innovatie":50,"Extraversie":70}""",
            RiasecScoresJson = """{"Realistic":40,"Investigative":50,"Artistic":45,"Social":80,"Enterprising":75,"Conventional":40}""",
            ValuesScoresJson = """{"Autonomy":50,"Connection":85,"Achievement":55,"Stability":45,"Impact":50}""",
            CultureScoresJson = """{"Autonomy":55,"Informal":45,"Collaboration":70,"Flexibility":50,"Innovation":45,"PeopleFirst":60}"""
        };
        var progress = new PupilProgress
        {
            PupilCodeId = id,
            AnswersJson = System.Text.Json.JsonSerializer.Serialize(answers),
            LikesJson = likes,
            VakScoresJson = vakJson
        };
        return (result, progress);
    }
}
