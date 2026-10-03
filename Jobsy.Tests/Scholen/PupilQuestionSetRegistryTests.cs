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

        Assert.Equal(PupilQuestionSet.Vo, vo.Set);
        Assert.Equal("legacy-vo", vo.Key);
        Assert.Equal("1", vo.ScoringVersion);
        Assert.Equal(60, vo.Bank.AllItems.Count);

        // Byte-identical bank contents in 03a.
        Assert.Equal(
            g78.Bank.AllItems.Select(i => i.Id),
            vo.Bank.AllItems.Select(i => i.Id));
    }

    [Fact]
    public void Get_throws_for_unknown_set_without_fallback()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => _registry.Get((PupilQuestionSet)999));
        Assert.Contains("no fallback", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IsLegacy_true_only_for_Vo_in_03a()
    {
        Assert.True(_registry.IsLegacy(PupilQuestionSet.Vo));
        Assert.False(_registry.IsLegacy(PupilQuestionSet.Groep78));
    }

    [Fact]
    public void FindByItemId_only_within_that_test()
    {
        Assert.NotNull(_registry.FindByItemId(PupilQuestionSet.Groep78, "9001"));
        Assert.Null(_registry.FindByItemId(PupilQuestionSet.Groep78, "9101"));
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
    public void Wrong_set_detection_with_fake_def_only()
    {
        // In 03a both real defs share 9001–9060; wrong_set needs a fake foreign bank.
        var foreign = new PupilQuestionBank();
        var fake = _registry.Get(PupilQuestionSet.Groep78) with
        {
            Set = PupilQuestionSet.Groep78,
            Key = "fake-empty",
            Bank = new EmptyBank()
        };

        Assert.Null(fake.Bank.GetById("9001"));
        Assert.NotNull(foreign.GetById("9001"));
        Assert.True(PupilFlow.IsKnownPupilItemId("9001"));
        Assert.True(PupilFlow.IsKnownPupilItemId("9101"));
        Assert.False(PupilFlow.IsKnownPupilItemId("42"));
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
