using Jobsy.Core.Scholen;
using Jobsy.Web.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class G78PuzzleIntroMarkupTests
{
    [Fact]
    public void Schatkaart_preview_shows_all_three_match_symbols()
    {
        var html = PupilGroep78MockHtml.PuzzleMatchPreviewRow(PupilJourneyTheme.ZeeExpeditie);
        Assert.Contains("match-1.jpg", html, StringComparison.Ordinal);
        Assert.Contains("match-2.jpg", html, StringComparison.Ordinal);
        Assert.Contains("match-3.jpg", html, StringComparison.Ordinal);
        Assert.DoesNotContain("card-back.jpg", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Vuurtoren_preview_uses_beacon_slots()
    {
        var html = PupilGroep78MockHtml.PuzzleLampPreviewRow(PupilJourneyTheme.ZeeExpeditie);
        Assert.Contains("slot--beacon", html, StringComparison.Ordinal);
        Assert.Contains("data-ll-lamp-tone=\"3\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("lamp-1.jpg", html, StringComparison.Ordinal);
    }
}
