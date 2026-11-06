using Jobsy.Web.Hosting;

namespace Jobsy.Tests;

public sealed class WwwrootAssetCatalogTests
{
    [Theory]
    [InlineData("css/features/kreeft-reiskaart.css")]
    [InlineData("css/features/candidate-header-strip.css")]
    public void Catalog_urls_use_single_manifest_version(string path)
    {
        var v = WwwrootAssetCatalog.Version(path);
        Assert.Equal(AssetVersions.Version(path), v);
        Assert.Contains("?v=", WwwrootAssetCatalog.Url(path), StringComparison.Ordinal);
    }
}
