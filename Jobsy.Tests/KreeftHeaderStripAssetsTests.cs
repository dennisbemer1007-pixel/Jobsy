using Jobsy.Web.Media;

namespace Jobsy.Tests;

public sealed class KreeftHeaderStripAssetsTests
{
    [Fact]
    public void Desk_normalizes_full_url_keys_without_double_base()
    {
        var full = KreeftHeaderStripAssets.SollicitatiesDesk;
        var again = KreeftHeaderStripAssets.Desk(full);
        Assert.Equal(full, again);
        Assert.DoesNotContain("/strips//images/", again, StringComparison.Ordinal);
    }

    [Fact]
    public void NormalizeKey_extracts_slug_from_desk_url()
    {
        Assert.Equal("sollicitaties", KreeftHeaderStripAssets.NormalizeKey(KreeftHeaderStripAssets.SollicitatiesDesk));
    }

    [Fact]
    public void Desk_and_Mob_never_double_strip_base_on_partial_paths()
    {
        var partialDesk = "/images/kreeft/strips/sollicitaties-desk.webp?v=old";
        var partialMob = "/images/kreeft/strips/sollicitaties-mob.webp?v=old";
        var desk = KreeftHeaderStripAssets.Desk(partialDesk);
        var mob = KreeftHeaderStripAssets.Mob(partialMob);
        Assert.Equal(KreeftHeaderStripAssets.SollicitatiesDesk, desk);
        Assert.Equal(KreeftHeaderStripAssets.SollicitatiesMob, mob);
        Assert.DoesNotContain("/strips//images/", desk, StringComparison.Ordinal);
        Assert.DoesNotContain("/strips//images/", mob, StringComparison.Ordinal);
    }
}
