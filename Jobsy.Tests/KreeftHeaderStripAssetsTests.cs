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
}
