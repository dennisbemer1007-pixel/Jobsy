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

    private static Dictionary<string, int> BuildHelpsOthersAnswers()
    {
        var d = BaselineThrees();
        Set(d, 5, 9001, 9003, 9006, 9007, 9008, 9019, 9024, 9030, 9032, 9037, 9042, 9048, 9053, 9054, 9059, 9060, 9021, 9022, 9038, 9017);
        Set(d, 3, 9002, 9016, 9034, 9039, 9040, 9050, 9056);
        Set(d, 2, 9004, 9009, 9014, 9018, 9023, 9029, 9051, 9057);
        Set(d, 1, 9005, 9010, 9015, 9020, 9026, 9049);
        return d;
    }

    private static Dictionary<string, int> BuildLeadsGroupAnswers()
    {
        var d = BaselineThrees();
        Set(d, 5, 9001, 9005, 9009, 9010, 9020, 9026, 9029, 9031, 9036, 9041, 9043, 9049, 9050, 9052, 9056, 9058, 9018, 9023);
        Set(d, 4, 9047);
        Set(d, 2, 9002, 9007, 9012, 9021, 9027, 9034, 9044, 9055);
        Set(d, 1, 9003, 9008, 9013);
        return d;
    }

    private static Dictionary<string, int> BuildMakerAnswers()
    {
        var d = BaselineThrees();
        Set(d, 5, 9002, 9004, 9007, 9008, 9016, 9017, 9018, 9022, 9025, 9027, 9028, 9033, 9038, 9043, 9052, 9055, 9057, 9029);
        Set(d, 4, 9056);
        Set(d, 3, 9009, 9036, 9050, 9041);
        Set(d, 2, 9001, 9006, 9019, 9024, 9046, 9047, 9053, 9059);
        Set(d, 1, 9005, 9010, 9015, 9030, 9049);
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
