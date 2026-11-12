namespace Jobsy.Tests.Scholen;

internal static class PupilG78JobMatchExplanationTestsFixtures
{
    public static Jobsy.Core.Entities.Scholen.PupilResult ArtisticResult() => new()
    {
        HollandCode = "AI",
        RiasecScoresJson =
            """{"realistic":40,"investigative":55,"artistic":90,"social":50,"enterprising":35,"conventional":30}""",
    };

    public static Jobsy.Core.Entities.Scholen.PupilResult TechResult() => new()
    {
        HollandCode = "RI",
        RiasecScoresJson =
            """{"realistic":88,"investigative":70,"artistic":30,"social":45,"enterprising":40,"conventional":35}""",
    };

    public static Jobsy.Core.Entities.Scholen.PupilProgress ProgressWithLikes(IReadOnlyList<string> likes, string answersJson)
        => new()
        {
            LikesJson = System.Text.Json.JsonSerializer.Serialize(likes),
            VakScoresJson = """{"tekenen":{"leuk":3,"goed":3},"natuur-techniek":{"leuk":5,"goed":5}}""",
            AnswersJson = answersJson
        };
}
