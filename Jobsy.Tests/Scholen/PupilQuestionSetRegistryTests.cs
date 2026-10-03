using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using AssessmentKind = Jobsy.Core.Enums.AssessmentKind;

namespace Jobsy.Tests.Scholen;

public class PupilQuestionSetRegistryTests
{
    private readonly IPupilQuestionSetRegistry _registry = new PupilQuestionSetRegistry();

    [Fact]
    public void ForClass_returns_def_of_class_own_question_set_no_fallback()
    {
        var g78Class = new SchoolClass { QuestionSet = PupilQuestionSet.Groep78 };
        var voClass = new SchoolClass { QuestionSet = PupilQuestionSet.Vo };

        var g78 = _registry.ForClass(g78Class);
        var vo = _registry.ForClass(voClass);

        Assert.Equal(PupilQuestionSet.Groep78, g78.Set);
        Assert.Equal("g78", g78.Key);
        Assert.Equal("g78-1", g78.ScoringVersion);
        Assert.Equal(60, g78.Bank.AllItems.Count);
        Assert.Equal(30, g78.IslandAfter);
        Assert.False(g78.PartBreakAfterIsland);

        Assert.Equal(PupilQuestionSet.Vo, vo.Set);
        Assert.Equal("vo", vo.Key);
        Assert.Equal("vo-1", vo.ScoringVersion);
        Assert.Equal(100, vo.Bank.AllItems.Count);
        Assert.Equal(50, vo.IslandAfter);
        Assert.True(vo.PartBreakAfterIsland);
        Assert.Equal(10, vo.ItemsPerPlate);
        Assert.Equal("LeerlingQ.Vo.Cheer.", vo.CheerKeyPrefix);
        Assert.Equal("Leerling.Vo.Start.NoteTime", vo.StartTimeKey);

        Assert.NotEqual(
            g78.Bank.AllItems.Select(i => i.Id),
            vo.Bank.AllItems.Select(i => i.Id));
        Assert.DoesNotContain(_registry.All, d => d.ScoringVersion == "1");
        Assert.DoesNotContain(_registry.All, d => d.Key == "legacy-vo");
    }

    [Fact]
    public void Get_throws_for_unknown_set_without_fallback()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => _registry.Get((PupilQuestionSet)999));
        Assert.Contains("no fallback", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IsLegacy_false_for_every_registered_set()
    {
        Assert.False(_registry.IsLegacy(PupilQuestionSet.Vo));
        Assert.False(_registry.IsLegacy(PupilQuestionSet.Groep78));
    }

    [Fact]
    public void FindByItemId_only_within_that_test()
    {
        Assert.NotNull(_registry.FindByItemId(PupilQuestionSet.Groep78, "9001"));
        Assert.Null(_registry.FindByItemId(PupilQuestionSet.Groep78, "9101"));
        Assert.NotNull(_registry.FindByItemId(PupilQuestionSet.Vo, "9101"));
        Assert.Null(_registry.FindByItemId(PupilQuestionSet.Vo, "9001"));
        Assert.Null(_registry.FindByItemId(PupilQuestionSet.Groep78, "nope"));
    }

    [Fact]
    public void Def_helpers_match_today_shell_math()
    {
        var def = _registry.Get(PupilQuestionSet.Groep78);
        Assert.Equal(0, def.PlatesShed(0));
        Assert.Equal(5, def.PlatesShed(30));
        Assert.Equal(10, def.PlatesShed(60));
        Assert.Equal(0, def.SceneDepth(0));
        Assert.Equal(6, def.SceneDepth(30));
        Assert.Equal(11, def.SceneDepth(60));
        Assert.Equal("LeerlingQ.Cheer.1", def.CheerKey(0));
        Assert.Equal("LeerlingQ.Cheer.6", def.CheerKey(30));
        Assert.Equal("koraalrif", def.WorldOf(0));
        Assert.Equal(0, def.IndexInWorld(0));
        Assert.Equal(0, def.IndexInWorld(15));
        Assert.Equal(
            new[] { "koraalrif", "schatgrot", "pauze-eiland", "vuurtoren", "lagune" },
            def.RailWorldKeys());
    }

    [Fact]
    public void Vo_def_helpers_use_ten_item_plates_and_island_after_schatgrot()
    {
        var def = _registry.Get(PupilQuestionSet.Vo);
        Assert.Equal(0, def.PlatesShed(0));
        Assert.Equal(5, def.PlatesShed(50));
        Assert.Equal(10, def.PlatesShed(100));
        Assert.Equal("LeerlingQ.Vo.Cheer.1", def.CheerKey(0));
        Assert.Equal("LeerlingQ.Vo.Cheer.6", def.CheerKey(50));
        Assert.Equal("koraalrif", def.WorldOf(0));
        Assert.Equal("vuurtoren", def.WorldOf(50));
        Assert.Equal(
            new[] { "koraalrif", "schatgrot", "pauze-eiland", "vuurtoren", "lagune" },
            def.RailWorldKeys());
        Assert.Equal("Leerling.Vo.Answer.1", def.AnswerLabels[0].Key);
    }

    [Fact]
    public void Wrong_set_ids_are_known_but_not_in_the_other_bank()
    {
        Assert.True(PupilFlow.IsKnownPupilItemId("9001"));
        Assert.True(PupilFlow.IsKnownPupilItemId("9101"));
        Assert.False(PupilFlow.IsKnownPupilItemId("42"));
        Assert.NotNull(_registry.Get(PupilQuestionSet.Groep78).Bank.GetById("9001"));
        Assert.Null(_registry.Get(PupilQuestionSet.Vo).Bank.GetById("9001"));
        _ = new EmptyBank();
    }

    private sealed class EmptyBank : IPupilQuestionBank
    {
        public int Version => 0;
        public IReadOnlyList<PupilQuestion> Questions => [];
        public IReadOnlyList<PupilQuestionItem> AllItems => [];
        public PupilQuestionItem? GetById(string itemId) => null;
        public PupilQuestionItem? GetByGlobalIndex(int globalIndex) => null;
        public IReadOnlyList<PupilQuestion> ForModel(AssessmentKind model) => [];
    }
}
