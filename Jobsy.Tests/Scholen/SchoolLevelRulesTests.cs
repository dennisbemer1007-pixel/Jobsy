using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;

namespace Jobsy.Tests.Scholen;

public class SchoolLevelRulesTests
{
    [Theory]
    [InlineData(SchoolLevel.Groep78, PupilQuestionSet.Groep78)]
    [InlineData(SchoolLevel.VmboB, PupilQuestionSet.Vo)]
    [InlineData(SchoolLevel.VmboK, PupilQuestionSet.Vo)]
    [InlineData(SchoolLevel.VmboGt, PupilQuestionSet.Vo)]
    [InlineData(SchoolLevel.Mavo, PupilQuestionSet.Vo)]
    [InlineData(SchoolLevel.Havo, PupilQuestionSet.Vo)]
    [InlineData(SchoolLevel.Vwo, PupilQuestionSet.Vo)]
    [InlineData(SchoolLevel.Mix, PupilQuestionSet.Vo)]
    [InlineData(SchoolLevel.Anders, PupilQuestionSet.Vo)]
    public void QuestionSetFor_maps_every_level(SchoolLevel level, PupilQuestionSet expected)
        => Assert.Equal(expected, SchoolLevelRules.QuestionSetFor(level));

    [Fact]
    public void QuestionSetFor_covers_every_enum_value()
    {
        foreach (SchoolLevel level in Enum.GetValues<SchoolLevel>())
        {
            var set = SchoolLevelRules.QuestionSetFor(level);
            Assert.True(set is PupilQuestionSet.Groep78 or PupilQuestionSet.Vo, level.ToString());
        }
    }

    [Theory]
    [InlineData(SchoolLevel.Groep78, 7, true)]
    [InlineData(SchoolLevel.Groep78, 8, true)]
    [InlineData(SchoolLevel.Groep78, 6, false)]
    [InlineData(SchoolLevel.Groep78, 9, false)]
    [InlineData(SchoolLevel.Havo, 1, true)]
    [InlineData(SchoolLevel.Havo, 6, true)]
    [InlineData(SchoolLevel.Havo, 0, false)]
    [InlineData(SchoolLevel.Havo, 7, false)]
    [InlineData(SchoolLevel.VmboB, 3, true)]
    public void ValidateYear_accepts_and_rejects(SchoolLevel level, int year, bool ok)
    {
        var error = SchoolLevelRules.ValidateYear(level, year);
        if (ok)
        {
            Assert.Null(error);
        }
        else
        {
            Assert.NotNull(error);
            Assert.Equal(
                SchoolLevelRules.IsPrimary(level) ? "Kies groep 7 of groep 8." : "Leerjaar moet 1–6 zijn.",
                error);
        }
    }

    [Fact]
    public void LabelKey_exists_for_every_level_in_nl_strings()
    {
        var nl = new Dictionary<string, string>(StringComparer.Ordinal);
        UiStringsScholen.MergeNl(nl);
        foreach (SchoolLevel level in Enum.GetValues<SchoolLevel>())
        {
            var key = SchoolLevelRules.LabelKey(level);
            Assert.True(nl.ContainsKey(key), $"Missing string {key}");
            Assert.False(string.IsNullOrWhiteSpace(nl[key]));
        }
    }

    [Theory]
    [InlineData(SchoolLevel.Groep78, SchoolLevel.Havo, true)]
    [InlineData(SchoolLevel.Havo, SchoolLevel.Groep78, true)]
    [InlineData(SchoolLevel.Havo, SchoolLevel.Vwo, false)]
    [InlineData(SchoolLevel.VmboB, SchoolLevel.Anders, false)]
    [InlineData(SchoolLevel.Groep78, SchoolLevel.Groep78, false)]
    public void StartedCodesBlockChange_truth_table(SchoolLevel from, SchoolLevel to, bool blocked)
        => Assert.Equal(blocked, SchoolLevelRules.StartedCodesBlockChange(from, to));

    [Theory]
    [InlineData(SchoolLevel.Groep78, 7, "Groep 7")]
    [InlineData(SchoolLevel.Groep78, 8, "Groep 8")]
    [InlineData(SchoolLevel.Havo, 2, "Klas 2")]
    public void YearLabel_is_dutch(SchoolLevel level, int year, string expected)
        => Assert.Equal(expected, SchoolLevelRules.YearLabel(level, year));

    [Fact]
    public void VoLevels_display_order()
    {
        Assert.Equal(
            new[]
            {
                SchoolLevel.VmboB, SchoolLevel.VmboK, SchoolLevel.VmboGt, SchoolLevel.Mavo,
                SchoolLevel.Havo, SchoolLevel.Vwo, SchoolLevel.Mix, SchoolLevel.Anders
            },
            SchoolLevelRules.VoLevels);
    }

    [Fact]
    public void Pupil_entities_have_no_QuestionSet_property()
    {
        foreach (var type in new[]
                 {
                     typeof(Jobsy.Core.Entities.Scholen.PupilCode),
                     typeof(Jobsy.Core.Entities.Scholen.PupilProgress),
                     typeof(Jobsy.Core.Entities.Scholen.PupilResult)
                 })
        {
            var props = type.GetProperties()
                .Where(p => p.PropertyType == typeof(PupilQuestionSet)
                            || p.PropertyType == typeof(PupilQuestionSet?))
                .ToList();
            Assert.Empty(props);
        }
    }
}
