using Jobsy.Web.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilSchelpenrijPuzzleTests
{
    [Fact]
    public void Intro_and_play_use_five_shells_matching_mockup_row()
    {
        Assert.Equal(5, PupilSchelpenrijPuzzle.ShellCount);
        Assert.Equal(5, PupilSchelpenrijPuzzle.PreviewImageUrls.Count);
        Assert.Equal(PupilSchelpenrijPuzzle.TargetOrder.Length, PupilSchelpenrijPuzzle.InitialPlayOrder.Length);
    }

    [Fact]
    public void One_swap_solves_initial_play_order()
    {
        var order = PupilSchelpenrijPuzzle.InitialPlayOrder.ToList();
        Assert.False(PupilSchelpenrijPuzzle.IsSolved(order));
        (order[1], order[3]) = (order[3], order[1]);
        Assert.True(PupilSchelpenrijPuzzle.IsSolved(order));
    }
}
