using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Tests.Scholen;

public class PupilG78PassportPdfDataTests
{
    [Fact]
    public void BuildRiasecBars_reads_camelCase_json_from_result_builder()
    {
        var result = new PupilResult
        {
            RiasecScoresJson =
                """{"realistic":72,"investigative":68,"artistic":55,"social":61,"enterprising":40,"conventional":38}"""
        };

        var (bars, pending) = PupilG78PassportPdfData.BuildRiasecBars(result);

        Assert.False(pending);
        Assert.Equal(6, bars.Count);
        Assert.True(bars.All(b => b.Score > 0));
        Assert.Equal(72, bars.First(b => b.Letter == "R").Score);
    }

    [Fact]
    public void BuildRiasecBars_pending_when_no_measurements_not_zero_bars()
    {
        var result = new PupilResult { RiasecScoresJson = "{}" };

        var (bars, pending) = PupilG78PassportPdfData.BuildRiasecBars(result);

        Assert.True(pending);
        Assert.Empty(bars);
    }

    [Fact]
    public void BuildValueBars_top_five_use_everyday_value_labels()
    {
        var result = new PupilResult
        {
            ValuesScoresJson =
                """{"autonomy":55,"connection":88,"achievement":72,"stability":40,"impact":65}"""
        };

        var bars = PupilG78PassportPdfData.BuildValueBars(result);

        Assert.Equal(5, bars.Count);
        Assert.Equal(SchwartzValuesCatalog.EverydayLabel(SchwartzValuesCatalog.Connection), bars[0].Label);
        Assert.DoesNotContain("Zo ben jij", bars[0].Label, StringComparison.Ordinal);
        Assert.True(bars[0].ScorePercent >= bars[^1].ScorePercent);
    }
}
