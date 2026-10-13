using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public class PupilInterestChipTests
{
    [Fact]
    public void Like_and_dislike_catalogs_have_expected_sizes_and_unique_keys()
    {
        Assert.True(PupilInterestChipCatalog.LikeChips.Count >= 20);
        Assert.True(PupilInterestChipCatalog.DislikeChips.Count > PupilInterestChipCatalog.LikeChips.Count);
        Assert.Equal(
            PupilInterestChipCatalog.LikeChips.Count,
            PupilInterestChipCatalog.LikeChips.Select(c => c.Key).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            PupilInterestChipCatalog.DislikeChips.Count,
            PupilInterestChipCatalog.DislikeChips.Select(c => c.Key).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Other_word_regex_accepts_letters_spaces_hyphens_only()
    {
        Assert.Matches(PupilInterestChipCatalog.OtherWordPattern, "paardrijden");
        Assert.Matches(PupilInterestChipCatalog.OtherWordPattern, "bouwen knutselen");
        Assert.Matches(PupilInterestChipCatalog.OtherWordPattern, "ski-springen");
        Assert.DoesNotMatch(PupilInterestChipCatalog.OtherWordPattern, "test@mail");
        Assert.DoesNotMatch(PupilInterestChipCatalog.OtherWordPattern, "code123");
        Assert.DoesNotMatch(PupilInterestChipCatalog.OtherWordPattern, "");
    }

    [Fact]
    public void Name_guard_flags_common_dutch_first_names()
    {
        Assert.True(PupilNameGuard.LooksLikeName("Emma"));
        Assert.True(PupilNameGuard.LooksLikeName("noah"));
        Assert.True(PupilNameGuard.LooksLikeName("Jan"));
        Assert.False(PupilNameGuard.LooksLikeName("paardrijden"));
        Assert.False(PupilNameGuard.LooksLikeName("skateboard"));
        Assert.True(PupilNameGuard.NamesLoadedCount() >= 400);
    }
}
