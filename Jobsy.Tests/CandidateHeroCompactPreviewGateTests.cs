using Jobsy.Web.Features;
using Xunit;

namespace Jobsy.Tests;

public sealed class CandidateHeroCompactPreviewGateTests
{
    [Fact]
    public void Default_is_variant_C_when_enabled()
    {
        var v = CandidateHeroCompactPreviewGate.ResolveVariant("https://lobsy.nl/carriere", enabled: true);
        Assert.Equal(CandidateHeroCompactPreviewVariant.C, v);
    }

    [Fact]
    public void Legacy_query_disables_compact()
    {
        var v = CandidateHeroCompactPreviewGate.ResolveVariant("https://lobsy.nl/carriere?heroPreview=legacy", enabled: true);
        Assert.Equal(CandidateHeroCompactPreviewVariant.None, v);
    }

    [Fact]
    public void Disabled_config_returns_none()
    {
        var v = CandidateHeroCompactPreviewGate.ResolveVariant("https://lobsy.nl/carriere", enabled: false);
        Assert.Equal(CandidateHeroCompactPreviewVariant.None, v);
    }
}
