namespace Jobsy.Tests;

public sealed class ApplicationsAfterMatchCardSsotTests
{
    [Fact]
    public void After_match_rows_skip_timeline_card_hint()
    {
        var root = TestRepo.FindRoot();
        var data = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Applications", "ApplicationsSsrData.cs"));
        Assert.Contains("afterMatch is not null", data, StringComparison.Ordinal);
        Assert.Contains("? null", data, StringComparison.Ordinal);
        Assert.Contains("BuildCardHint", data, StringComparison.Ordinal);
    }

    [Fact]
    public void After_match_cards_use_detail_actions_primary_label()
    {
        var root = TestRepo.FindRoot();
        var shells = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Applications", "ApplicationsSsrShells.cs"));
        Assert.Contains("BuildDetailActions", shells, StringComparison.Ordinal);
        Assert.Contains("detail.PrimaryLabel", shells, StringComparison.Ordinal);
    }
}
