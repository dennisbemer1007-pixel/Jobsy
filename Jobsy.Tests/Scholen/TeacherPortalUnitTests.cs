using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests.Scholen;

public class TeacherPortalUnitTests
{
    [Fact]
    public void Teacher_group_aggregate_hides_below_k_and_shows_bars_at_five()
    {
        var four = Enumerable.Range(0, 4).Select(_ => MakeResult("SAE", "Helpen", "Klein", "arts")).ToList();
        var hidden = ClassResultsAggregator.AggregateTeacherGroup(four, PupilQuestionSet.Vo);
        Assert.False(hidden.Visible);
        Assert.Empty(hidden.RiasecBars);

        four.Add(MakeResult("RIC", "Vrijheid", "Druk", "kok"));
        var shown = ClassResultsAggregator.AggregateTeacherGroup(four, PupilQuestionSet.Vo);
        Assert.True(shown.Visible);
        Assert.Equal(6, shown.RiasecBars.Count);
        Assert.True(shown.TopValues.Count is >= 1 and <= 3);
        Assert.Contains(shown.DreamJobs, d => d.Key == "Overig" || d.Key == "arts");
    }

    [Fact]
    public void Story_renderer_has_four_tiles_and_dream_route()
    {
        var renderer = new PupilStoryRenderer();
        var result = MakeResult("SAE", SchwartzValuesCatalog.Connection, CulturePersonalityCatalog.PeopleFirst, "dierenarts");
        result.CompetenceScoresJson = """{"samenwerken":80,"resultaatgerichtheid":70,"stressbestendigheid":65,"innovatie":50,"extraversie":55}""";
        result.RiasecScoresJson = """{"realistic":70,"investigative":40,"artistic":30,"social":85,"enterprising":50,"conventional":40}""";
        result.ValuesScoresJson = """{"autonomy":40,"connection":80,"achievement":50,"stability":45,"impact":60}""";
        result.CultureScoresJson = """{"autonomy":40,"informal":40,"collaboration":50,"flexibility":40,"innovation":40,"peopleFirst":80}""";
        var keys = PupilStoryTemplates.SelectKeys(result, ["dieren"]);
        result.StoryKeysJson = PupilStoryTemplates.Serialize(keys);
        var story = renderer.Render(result, new PupilProgress { LikesJson = """["dieren"]""" });
        Assert.Equal(4, story.Tiles.Count);
        Assert.False(string.IsNullOrWhiteSpace(story.Body));
        Assert.DoesNotContain("Leraar.Detail.StoryPlaceholder", story.Body, StringComparison.Ordinal);
        var route = renderer.RenderDreamRoute(
            result,
            new PupilProgress { DreamJobKey = "dierenarts", LikesJson = """["dieren"]""" },
            new PupilClassContext(SchoolLevel.Groep78, 8, PupilQuestionSet.Groep78));
        Assert.Equal("dierenarts", route.JobKey);
        Assert.Equal(5, route.TotalCount);
        Assert.Contains("Nu: groep 8", route.RouteSteps[0], StringComparison.Ordinal);
        var voRoute = renderer.RenderDreamRoute(
            result,
            new PupilProgress { DreamJobKey = "dierenarts", LikesJson = """["dieren"]""" },
            new PupilClassContext(SchoolLevel.Havo, 3, PupilQuestionSet.Vo));
        Assert.Equal(route.HaveCount, voRoute.HaveCount);
        Assert.Contains("Nu: klas 3 havo", voRoute.RouteSteps[0], StringComparison.Ordinal);
        Assert.Equal(3, renderer.ConversationStarterKeys(result).Count);
        Assert.Equal(3, renderer.ClassDiscussionPromptKeys().Count);
    }

    [Fact]
    public void ScholenNav_teacher_groups_for_class_are_available_and_scoped()
    {
        var classId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var other = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var groups = ScholenNav.TeacherGroupsForClass(classId, [(classId, "2B"), (other, "3A")]);
        Assert.Equal(3, groups.Count);
        var myClass = groups[0].Items;
        Assert.All(myClass, i => Assert.True(i.IsAvailable));
        Assert.Contains(myClass, i => i.Href.Contains(classId.ToString("D"), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(myClass, i => i.Href.Contains("/leraar/codes", StringComparison.Ordinal)
                                            && !i.Href.Contains("/klas/", StringComparison.Ordinal));
        var myClasses = groups[2].Items;
        Assert.Equal(2, myClasses.Count);
        Assert.Contains(myClasses, i => i.FixedLabel == "Klas 2B");
        Assert.Contains(myClasses, i => i.FixedLabel == "Klas 3A");
    }

    [Fact]
    public void Leraar_ui_strings_cover_overview_and_detail_keys()
    {
        var nl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        UiStringsScholen.MergeNl(nl);
        Assert.Equal("Maken", nl["Leraar.Riasec.R"]);
        Assert.Equal("Zichtbaar vanaf 5 afgeronde tests. Nu: {0}.", nl["Leraar.Group.Hidden"]);
        Assert.Equal("PDF downloaden", nl["Leraar.Detail.Pdf"]);
        Assert.Equal("Bezig met afronden", nl["Leraar.Detail.ResultPending"]);
        Assert.Equal("Deel de kaartjes uit.", nl["Leraar.Material.Step1"]);
        Assert.Equal(
            "Je antwoorden zijn bewaard. We maken je verhaal klaar. Probeer het zo nog eens.",
            nl["Leerling.Reis.ResultPending"]);
        Assert.Equal("Probeer opnieuw", nl["Leerling.Reis.Retry"]);
    }

    [Fact]
    public void Teacher_detail_dto_property_names_exclude_answers()
    {
        var names = typeof(TeacherCodeDetailDto).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("AnswersJson", names);
        Assert.Contains("Story", names);
        Assert.Contains("Likes", names);
        Assert.Contains("DreamJob", names);
    }

    private static PupilResult MakeResult(string holland, string topValue, string culture, string dream) => new()
    {
        PupilCodeId = Guid.NewGuid(),
        SchoolClassId = Guid.NewGuid(),
        CompletedAtUtc = DateTime.UtcNow,
        HollandCode = holland,
        TopValue = topValue,
        TopCulture = culture,
        DreamJobKey = dream,
        CompetenceScoresJson = """{"samenwerken":70,"resultaatgerichtheid":70,"stressbestendigheid":70,"innovatie":50,"extraversie":50}""",
        RiasecScoresJson = """{"realistic":70,"investigative":40,"artistic":30,"social":80,"enterprising":40,"conventional":40}""",
        ValuesScoresJson = """{"autonomy":40,"connection":80,"achievement":50,"stability":45,"impact":60}""",
        CultureScoresJson = """{"autonomy":40,"informal":40,"collaboration":50,"flexibility":40,"innovation":40,"peopleFirst":80}""",
        ScoringVersion = "1",
        StoryTemplateVersion = "1",
        StoryKeysJson = "[]"
    };
}
