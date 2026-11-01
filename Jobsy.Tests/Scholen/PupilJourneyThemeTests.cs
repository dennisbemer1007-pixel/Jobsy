using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Tests.Scholen;

public sealed class PupilJourneyThemeTests
{
    [Fact]
    public void Chip_label_maps_vies_worden()
    {
        Assert.Equal("Vies worden", PupilPortalChipLabels.Dutch("vies-worden"));
    }

    [Theory]
    [InlineData(PupilJourneyTheme.ZeeExpeditie, "koraalrif", "/leerling/werelden/img/rif.jpg")]
    [InlineData(PupilJourneyTheme.Eilandtuin, "koraalrif", "/leerling/werelden/img/tuin-koraal.jpg")]
    public void World_band_image_uses_theme(string theme, string world, string expected)
    {
        Assert.Equal(expected, PupilJourneyTheme.WorldBandImagePath(theme, world));
    }

    [Fact]
    public void Puzzle_sort_asset_uses_png_for_zee()
    {
        Assert.Contains("sort-1.png", PupilJourneyTheme.PuzzleSortAsset(PupilJourneyTheme.ZeeExpeditie, 1));
    }
}
