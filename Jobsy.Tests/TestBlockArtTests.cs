using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class TestBlockArtTests
{
    [Theory]
    [InlineData(AssessmentKind.Competence, "hoe-je-werkt")]
    [InlineData(AssessmentKind.Career, "leuk")]
    [InlineData(AssessmentKind.Culture, "sfeer")]
    [InlineData(AssessmentKind.Values, "waarden")]
    public void SlugForAssessment_maps_quick_scan_blocks(AssessmentKind kind, string expected)
        => Assert.Equal(expected, TestBlockArt.SlugForAssessment(kind));

    [Theory]
    [InlineData(AssessmentKind.Culture, "Autonomy", "sfeer")]
    [InlineData(AssessmentKind.Culture, "PeopleFirst", "sfeer")]
    [InlineData(AssessmentKind.Culture, "Openness", "hoe-je-werkt")]
    [InlineData(AssessmentKind.Culture, "EmotionalStability", "hoe-je-werkt")]
    [InlineData(AssessmentKind.Career, "Realistic", "leuk")]
    [InlineData(AssessmentKind.Competence, "Openness", "hoe-je-werkt")]
    public void SlugForDeepDomain_maps_culture_personality_to_hoe_je_werkt(AssessmentKind kind, string domain, string expected)
        => Assert.Equal(expected, TestBlockArt.SlugForDeepDomain(kind, domain));

    [Fact]
    public void ImagePathForSlug_uses_wwwroot_tests_folder()
        => Assert.Equal("/images/tests/bg-leuk.jpg", TestBlockArt.ImagePathForSlug("leuk"));

    [Theory]
    [InlineData("leuk")]
    [InlineData("sfeer")]
    public void HeroFigurePathForSlug_points_at_figure_png(string slug)
        => Assert.Equal($"/images/tests/figures/{slug}-hero.png", TestBlockArt.HeroFigurePathForSlug(slug));
}
