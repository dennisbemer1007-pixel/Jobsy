using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Tests.Scholen;

public sealed class PupilMboHboActiveMinutesTests
{
    [Fact]
    public void ActiveMinutes_sums_capped_block_gaps_not_calendar_span()
    {
        var state = new PupilMboHboProgressState();
        var t0 = DateTime.UtcNow;
        state.BlocksCompleted["vakken"] = t0.ToString("O");
        state.BlocksCompleted["interesse"] = t0.AddMinutes(3).ToString("O");
        state.BlocksCompleted["competentie"] = t0.AddMinutes(6).ToString("O");
        state.BlocksCompleted["beroepen"] = t0.AddMinutes(8).ToString("O");
        state.BlocksCompleted["werkwaarden"] = t0.AddMinutes(8).ToString("O");
        var progress = new PupilProgress { MboHboProgressJson = state.ToJson() };

        var minutes = PupilMboHboProgressReset.ActiveMinutes(progress);
        Assert.InRange(minutes!.Value, 6, 12);
    }

    [Fact]
    public void ActiveMinutes_ignores_multi_day_idle_between_blocks()
    {
        var state = new PupilMboHboProgressState();
        var t0 = DateTime.UtcNow;
        state.BlocksCompleted["vakken"] = t0.ToString("O");
        state.BlocksCompleted["interesse"] = t0.AddDays(2).ToString("O");
        state.BlocksCompleted["competentie"] = t0.AddDays(2).AddMinutes(5).ToString("O");
        state.BlocksCompleted["beroepen"] = t0.AddDays(2).AddMinutes(7).ToString("O");
        state.BlocksCompleted["werkwaarden"] = t0.AddDays(2).AddMinutes(8).ToString("O");
        var progress = new PupilProgress { MboHboProgressJson = state.ToJson() };

        var minutes = PupilMboHboProgressReset.ActiveMinutes(progress);
        Assert.InRange(minutes!.Value, 8, 15);
    }

    [Fact]
    public void MedianMinutes_excludes_outlier_skew()
    {
        var median = PupilMboHboProgressReset.MedianMinutes([8, 9, 10, 180]);
        Assert.Equal(10, median);
    }
}
