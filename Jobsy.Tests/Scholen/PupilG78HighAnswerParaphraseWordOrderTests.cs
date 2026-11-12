using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public class PupilG78HighAnswerParaphraseWordOrderTests
{
    [Theory]
    [InlineData(9016, "je graag iets maakt of repareert", "je maakt graag iets of repareert dingen")]
    [InlineData(9017, "je graag wilt weten hoe dingen werken", "je wilt graag weten hoe dingen werken")]
    [InlineData(9018, "je graag iets moois maakt, zoals een tekening of muziek", "je maakt graag iets moois, zoals een tekening of muziek")]
    [InlineData(9019, "je graag iemand helpt die iets niet snapt", "je helpt graag iemand die iets niet snapt")]
    [InlineData(9020, "je graag de leiding neemt bij een groepsopdracht", "je neemt graag de leiding bij een groepsopdracht")]
    [InlineData(9021, "je graag je spullen en planning netjes houdt", "je houdt graag je spullen en planning netjes")]
    [InlineData(9022, "je graag met je handen aan iets tastbaars werkt", "je werkt graag met je handen aan iets tastbaars")]
    [InlineData(9023, "je graag een eigen verhaal, dans of outfit bedenkt", "je bedenkt graag een eigen verhaal, dans of outfit")]
    [InlineData(9024, "je het fijn vindt om anderen te steunen", "je vindt het fijn om anderen te steunen")]
    [InlineData(9025, "je graag uitzoekt hoe een proefje of puzzel werkt", "je zoekt graag uit hoe een proefje of puzzel werkt")]
    [InlineData(9026, "je graag anderen overtuigt van je idee", "je overtuigt graag anderen van je idee")]
    [InlineData(9027, "je graag vaste stappen en duidelijke lijstjes gebruikt", "je gebruikt graag vaste stappen en duidelijke lijstjes")]
    [InlineData(9028, "je graag buiten iets bouwt of fixt", "je bouwt graag buiten iets of fixt het")]
    [InlineData(9029, "je graag speelt of tekent om iets te laten zien", "je speelt of tekent graag om iets te laten zien")]
    [InlineData(9030, "je graag met mensen oefent of speelt", "je oefent of speelt graag met mensen")]
    public void Each_paraphrase_has_subordinate_and_main_want_forms(int itemId, string subordinate, string mainWant)
    {
        Assert.True(PupilG78HighAnswerParaphrases.TryGetSubordinateClause(itemId, out var sub));
        Assert.True(PupilG78HighAnswerParaphrases.TryGetMainClauseAfterWant(itemId, out var main));
        Assert.Equal(subordinate, sub);
        Assert.Equal(mainWant, main);
        if (itemId != 9024)
        {
            Assert.Contains("graag", sub);
        }

        Assert.DoesNotMatch(@"\bwant je graag\b", $"main clause should not keep subordinate order: {main}");
    }

    [Fact]
    public void Built_text_uses_main_order_after_want()
    {
        var result = new Jobsy.Core.Entities.Scholen.PupilResult
        {
            HollandCode = "RI",
            RiasecScoresJson =
                """{"realistic":88,"investigative":70,"artistic":30,"social":45,"enterprising":40,"conventional":35}""",
        };
        var progress = new Jobsy.Core.Entities.Scholen.PupilProgress
        {
            LikesJson = """["techniek"]""",
            AnswersJson = """{"9016":5,"9028":5}""",
        };
        var text = PupilG78JobMatchExplanation.Build("hovenier", result, progress, "nl");
        Assert.Contains("want je maakt graag iets", text, StringComparison.Ordinal);
        Assert.DoesNotContain("want je graag iets maakt", text, StringComparison.Ordinal);
        Assert.Contains("Je gaf ook aan dat je graag buiten", text, StringComparison.Ordinal);
        Assert.DoesNotContain("In de vragen koos je", text, StringComparison.Ordinal);
        Assert.Contains("Een hovenier ontwerpt", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Als hovenier", text, StringComparison.Ordinal);
    }
}
