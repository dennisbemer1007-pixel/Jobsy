using Jobsy.Core.Entities.Scholen;

namespace Jobsy.Tests.Scholen;

internal static class PupilG78RealisticFixtures
{
    internal static (PupilResult Result, PupilProgress Progress) HelpsOthers()
        => Build("SC", "Connection", "Collaboration",
            """["sport","dieren"]""",
            """{"tekenen":{"leuk":5,"goed":4},"handvaardigheid":{"leuk":5,"goed":4},"muziek":{"leuk":5,"goed":3}}""",
            BuildHelpsOthersAnswers());

    internal static (PupilResult Result, PupilProgress Progress) LeadsGroup()
        => Build("ES", "Autonomy", "Informal",
            """["theater","lezen"]""",
            """{"geschiedenis":{"leuk":5,"goed":4},"engels":{"leuk":4,"goed":5},"nederlands":{"leuk":5,"goed":4}}""",
            BuildLeadsGroupAnswers());

    internal static (PupilResult Result, PupilProgress Progress) Maker()
        => Build("IR", "Achievement", "Flexibility",
            """["techniek","natuur"]""",
            """{"natuur-techniek":{"leuk":5,"goed":5},"rekenen":{"leuk":4,"goed":5},"techniek":{"leuk":5,"goed":4}}""",
            BuildMakerAnswers());

  /** SC: growth communiceren + creativiteit; story strengths samenwerken + doorzetten. */
    private static Dictionary<string, int> BuildHelpsOthersAnswers()
    {
        var d = BaselineThrees();
        for (var id = 9001; id <= 9060; id++)
        {
            d[id.ToString()] = 4;
        }

        Set(d, 2, 9005, 9010);
        Set(d, 4, 9004, 9009);
        Set(d, 2, 9012, 9007, 9021, 9002, 9014);
        Set(d, 5, 9001, 9003, 9006, 9008, 9013, 9054, 9019, 9030, 9024);
        Set(d, 2, 9017, 9025, 9018, 9028, 9016);
        Set(d, 5, 9017, 9025, 9028, 9032, 9040, 9045, 9048, 9053);
        Set(d, 2, 9031, 9032, 9036, 9041, 9042, 9046, 9047, 9020);
        Set(d, 5, 9037, 9047);
        return d;
    }

  /** ES: growth doorzetten + plannen; strengths communiceren/creativiteit. */
    private static Dictionary<string, int> BuildLeadsGroupAnswers()
    {
        var d = BaselineThrees();
        for (var id = 9001; id <= 9060; id++)
        {
            d[id.ToString()] = 4;
        }

        Set(d, 2, 9002, 9003, 9008, 9007, 9021, 9027, 9034);
        Set(d, 5, 9001, 9004, 9005, 9009, 9010, 9020, 9026, 9018, 9029, 9041, 9042, 9049, 9050, 9054, 9056);
        Set(d, 5, 9038, 9043);
        Set(d, 5, 9006, 9019);
        Set(d, 2, 9031, 9036, 9041, 9046, 9058, 9053, 9055);
        return d;
    }

  /** IR: growth samenwerken + communiceren; strengths plannen/creativiteit/doorzetten. */
    private static Dictionary<string, int> BuildMakerAnswers()
    {
        var d = BaselineThrees();
        for (var id = 9001; id <= 9060; id++)
        {
            d[id.ToString()] = 4;
        }

        Set(d, 2, 9001, 9006, 9011, 9005, 9010, 9015, 9024, 9030, 9034);
        Set(d, 2, 9008, 9049);
        Set(d, 5, 9002, 9004, 9007, 9021, 9010, 9017, 9019, 9026, 9025, 9033, 9037, 9038, 9043, 9054, 9057);
        Set(d, 2, 9031, 9036, 9052, 9056, 9050);
        Set(d, 3, 9027);
        Set(d, 4, 9016, 9022);
        Set(d, 2, 9020, 9032, 9042, 9051, 9058);
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
