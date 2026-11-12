using Jobsy.Core.Entities.Scholen;

namespace Jobsy.Tests.Scholen;

internal static class PupilG78RealisticFixtures
{
    internal static (PupilResult Result, PupilProgress Progress) HelpsOthers()
        => Build("SC", "Connection", "Collaboration",
            """["sport","dieren"]""",
            """{"tekenen":{"leuk":5,"goed":4},"muziek":{"leuk":5,"goed":3}}""",
            BuildHelpsOthersAnswers());

    internal static (PupilResult Result, PupilProgress Progress) LeadsGroup()
        => Build("ES", "Autonomy", "Informal",
            """["theater","lezen"]""",
            """{"geschiedenis":{"leuk":5,"goed":4},"engels":{"leuk":4,"goed":5}}""",
            BuildLeadsGroupAnswers());

    internal static (PupilResult Result, PupilProgress Progress) Maker()
        => Build("IR", "Achievement", "Flexibility",
            """["techniek","natuur"]""",
            """{"natuur-techniek":{"leuk":5,"goed":5},"rekenen":{"leuk":4,"goed":5}}""",
            BuildMakerAnswers());

  /** SC: growth communiceren + creativiteit; ≥22 answers at 4–5. */
    private static Dictionary<string, int> BuildHelpsOthersAnswers()
    {
        var d = BaselineThrees();
        for (var id = 9001; id <= 9060; id++)
        {
            if (id is 9005 or 9010 or 9015 or 9004 or 9009 or 9014 or 9018)
            {
                continue;
            }

            d[id.ToString()] = id % 7 == 0 ? 4 : 5;
        }

        Set(d, 2, 9005, 9010, 9004, 9009);
        Set(d, 5, 9001, 9006, 9007, 9008, 9021, 9024, 9032, 9048);
        return d;
    }

  /** ES: growth doorzetten + plannen; ≥22 highs. */
    private static Dictionary<string, int> BuildLeadsGroupAnswers()
    {
        var d = BaselineThrees();
        for (var id = 9001; id <= 9060; id++)
        {
            if (id is 9002 or 9007 or 9012 or 9021 or 9027 or 9034 or 9003 or 9008)
            {
                continue;
            }

            d[id.ToString()] = id % 5 == 0 ? 4 : 5;
        }

        Set(d, 2, 9002, 9007, 9012, 9021, 9027, 9034, 9003, 9008);
        Set(d, 5, 9005, 9010, 9020, 9026, 9049, 9058);
        return d;
    }

  /** IR: growth communiceren + samenwerken; ≥22 highs. */
    private static Dictionary<string, int> BuildMakerAnswers()
    {
        var d = BaselineThrees();
        for (var id = 9001; id <= 9060; id++)
        {
            if (id is 9005 or 9010 or 9015 or 9001 or 9006 or 9019 or 9024 or 9030 or 9049)
            {
                continue;
            }

            d[id.ToString()] = id % 6 == 0 ? 4 : 5;
        }

        Set(d, 2, 9005, 9001, 9006, 9019);
        Set(d, 5, 9002, 9007, 9024, 9052, 9056, 9057, 9046, 9010);
        return d;
    }

    private static Dictionary<string, int> BaselineThrees()
    {
        var d = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var id = 9001; id <= 9060; id++)
        {
            d[id.ToString()] = 3;
        }

        return d;
    }

    private static void Set(Dictionary<string, int> d, int score, params int[] ids)
    {
        foreach (var id in ids)
        {
            d[id.ToString()] = score;
        }
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
