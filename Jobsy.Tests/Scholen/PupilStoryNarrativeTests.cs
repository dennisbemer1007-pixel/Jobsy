using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilStoryNarrativeTests
{
    private readonly PupilStoryRenderer _renderer = new();

    [Fact]
    public void G78_story_uses_paragraphs_and_merges_autonomy_value_culture()
    {
        var result = new PupilResult
        {
            CompetenceScoresJson = """{"Samenwerken":72,"Resultaatgerichtheid":40,"Stressbestendigheid":50,"Innovatie":30,"Extraversie":45}""",
            RiasecScoresJson = """{"Realistic":30,"Investigative":35,"Artistic":40,"Social":50,"Enterprising":70,"Conventional":68}""",
            HollandCode = "CE",
            TopValue = "Autonomy",
            TopCulture = "Autonomy",
            ValuesScoresJson = """{"Autonomy":80,"Connection":50,"Achievement":40,"Stability":45,"Impact":35}""",
            CultureScoresJson = """{"Autonomy":75,"Informal":40,"Collaboration":50,"Flexibility":45,"Innovation":35,"PeopleFirst":50}"""
        };
        var progress = new PupilProgress
        {
            LikesJson = """["theater","fietsen-repareren"]"""
        };

        var view = _renderer.Render(result, progress, "nl");
        var paragraphs = PupilStoryNarrative.SplitParagraphs(view.Body);

        Assert.InRange(paragraphs.Count, 2, 3);
        Assert.DoesNotContain("zelf mag kiezen", view.Body, StringComparison.Ordinal);
        Assert.Contains("theater", view.Body, StringComparison.Ordinal);
        Assert.Contains("fietsen repareren", view.Body, StringComparison.Ordinal);
        Assert.Contains("In je vrije tijd", view.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void Story_body_keeps_only_template_facts_for_sparse_profile()
    {
        var result = new PupilResult
        {
            CompetenceScoresJson = """{"Samenwerken":50,"Resultaatgerichtheid":50,"Stressbestendigheid":50,"Innovatie":50,"Extraversie":50}""",
            RiasecScoresJson = """{"Realistic":50,"Investigative":50,"Artistic":50,"Social":50,"Enterprising":50,"Conventional":50}""",
            HollandCode = "IS",
            TopValue = "Stability",
            TopCulture = "Flexibility",
            ValuesScoresJson = """{"Autonomy":50,"Connection":50,"Achievement":50,"Stability":70,"Impact":50}""",
            CultureScoresJson = """{"Autonomy":50,"Informal":50,"Collaboration":50,"Flexibility":68,"Innovation":50,"PeopleFirst":50}"""
        };

        var view = _renderer.Render(result, progress: null, "nl");
        Assert.False(string.IsNullOrWhiteSpace(view.Body));
        Assert.DoesNotContain("LeerlingStory.", view.Body, StringComparison.Ordinal);
        Assert.InRange(PupilStoryNarrative.SplitParagraphs(view.Body).Count, 1, 3);
    }
}
