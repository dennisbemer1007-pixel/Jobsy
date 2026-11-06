using Jobsy.Core.Rules;
using Jobsy.Web.Components.Shared;

namespace Jobsy.Tests;

public class ChoiceTileIconsTests
{
    [Fact]
    public void Every_dislike_code_has_an_icon()
    {
        foreach (var code in DiscoveryCatalogs.DislikeCodes)
        {
            Assert.False(string.IsNullOrWhiteSpace(ChoiceTileIcons.ForDislike(code)));
        }
    }

    [Fact]
    public void Shared_choice_tile_components_exist_in_web()
    {
        var root = TestRepo.FindRoot();
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Shared/ChoiceTile.razor")));
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Shared/ChoiceTileGrid.razor")));
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/choice-tiles.css")));
    }
}
