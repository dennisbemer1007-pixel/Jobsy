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
    [InlineData(PupilJourneyTheme.Sterrenkust, "vuurtoren", "/leerling/werelden/img/sterren-strand.jpg")]
    [InlineData(PupilJourneyTheme.Sterrenkust, "lagune", "/leerling/werelden/img/sterren-strand.jpg")]
    [InlineData(PupilJourneyTheme.Sterrenkust, "schatgrot", "/leerling/werelden/img/sterren-strand.jpg")]
    [InlineData(PupilJourneyTheme.Sterrenkust, "schelpenrij", "/leerling/werelden/img/sterren-strand.jpg")]
    public void World_band_image_uses_theme(string theme, string world, string expected)
    {
        Assert.Equal(expected, PupilJourneyTheme.WorldBandImagePath(theme, world));
    }

    [Fact]
    public void Puzzle_sort_asset_uses_png_for_zee()
    {
        Assert.Contains("sort-1.png", PupilJourneyTheme.PuzzleSortAsset(PupilJourneyTheme.ZeeExpeditie, 1));
    }

    [Fact]
    public void Schelpenrij_pattern_matches_mockup_row()
    {
        Assert.Equal([1, 2, 2, 3, 1], PupilJourneyTheme.SchelpenrijPatternSequence());
        Assert.Equal(2, PupilJourneyTheme.SchelpenrijPatternAnswer());
        Assert.Equal(5, PupilJourneyTheme.SchelpenrijPatternChoices().Count);
    }

    [Theory]
    [InlineData(PupilJourneyTheme.ZeeExpeditie, "Leerling.G78.Puzzle.IntroSub.Zee")]
    [InlineData(PupilJourneyTheme.Eilandtuin, "Leerling.Puzzle.Tuin.Schelpenrij.Lead")]
    [InlineData(PupilJourneyTheme.Sterrenkust, "Leerling.G78.Puzzle.IntroSub.Ster")]
    public void Schelpenrij_intro_sub_key_per_theme(string theme, string expectedKey)
    {
        Assert.Equal(expectedKey, PupilJourneyTheme.SchelpenrijIntroSubKey(theme));
    }
}
