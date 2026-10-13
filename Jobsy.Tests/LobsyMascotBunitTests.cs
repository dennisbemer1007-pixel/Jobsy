using Bunit;
using Jobsy.Web.Components.Brand;
using Jobsy.Web.Media;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Tests;

public class LobsyMascotBunitTests : BunitContext
{
    [Fact]
    public void Fallback_waving_priority_renders_todays_mascot_with_fb_class()
    {
        var cut = Render<LobsyMascot>(p => p
            .Add(c => c.Pose, MascotPose.Waving)
            .Add(c => c.Size, MascotSize.Hero)
            .Add(c => c.Priority, true));

        var root = cut.Find("span.pub-mascot");
        Assert.Contains("pub-mascot--hero", root.ClassList);
        Assert.Contains("pub-mascot--fb-waving", root.ClassList);
        Assert.Equal("true", root.GetAttribute("aria-hidden"));

        var img = cut.Find("img.pub-mascot__img");
        Assert.Equal("256", img.GetAttribute("width"));
        Assert.Equal("256", img.GetAttribute("height"));
        Assert.Equal("eager", img.GetAttribute("loading"));
        Assert.Equal("high", img.GetAttribute("fetchpriority"));
        Assert.Contains("mascot-128.webp", img.GetAttribute("src")!, StringComparison.Ordinal);
        Assert.Contains("mascot-64.webp", img.GetAttribute("srcset")!, StringComparison.Ordinal);
        Assert.Contains("256w", img.GetAttribute("srcset")!, StringComparison.Ordinal);
        Assert.DoesNotContain("<picture", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Lazy_default_is_decorative_without_priority()
    {
        var cut = Render<LobsyMascot>(p => p
            .Add(c => c.Pose, MascotPose.Default)
            .Add(c => c.Size, MascotSize.Tiny));

        var img = cut.Find("img.pub-mascot__img");
        Assert.Equal("lazy", img.GetAttribute("loading"));
        Assert.Equal("async", img.GetAttribute("decoding"));
        Assert.Null(img.GetAttribute("fetchpriority"));
        Assert.Equal("40", img.GetAttribute("width"));
        Assert.Contains("pub-mascot--fb-default", cut.Find("span.pub-mascot").ClassList);
    }

    [Fact]
    public void Alt_makes_mascot_non_decorative()
    {
        var cut = Render<LobsyMascot>(p => p
            .Add(c => c.Alt, "Lobsy zwaait")
            .Add(c => c.Pose, MascotPose.Waving));

        var root = cut.Find("span.pub-mascot");
        Assert.Null(root.GetAttribute("aria-hidden"));
        Assert.Equal("img", root.GetAttribute("role"));
        Assert.Equal("Lobsy zwaait", root.GetAttribute("aria-label"));
    }

    [Fact]
    public void Mirror_adds_class()
    {
        var cut = Render<LobsyMascot>(p => p
            .Add(c => c.Mirror, true)
            .Add(c => c.Pose, MascotPose.Celebrating));

        Assert.Contains("pub-mascot--mirror", cut.Find("span.pub-mascot").ClassList);
        Assert.Contains("pub-mascot--fb-celebrating", cut.Find("span.pub-mascot").ClassList);
    }

    [Fact]
    public void Shell_fallback_uses_ghost_class()
    {
        var cut = Render<LobsyMascot>(p => p
            .Add(c => c.Pose, MascotPose.Shell));

        var root = cut.Find("span.pub-mascot");
        Assert.Contains("pub-mascot--ghost", root.ClassList);
        Assert.DoesNotContain("pub-mascot--fb-shell", root.ClassList);
    }

    [Fact]
    public void Svg_override_switches_markup_without_fallback_class()
    {
        using var _ = MascotAssets.OverrideArts(
            new MascotArt(MascotPose.Waving, MascotArtFormat.Svg, "20260930-test-waving"));

        var cut = Render<LobsyMascot>(p => p
            .Add(c => c.Pose, MascotPose.Waving)
            .Add(c => c.Size, MascotSize.Hero)
            .Add(c => c.Priority, true));

        var root = cut.Find("span.pub-mascot");
        Assert.DoesNotContain("pub-mascot--fb-waving", root.ClassList);

        var img = cut.Find("img.pub-mascot__img");
        Assert.Contains("mascot-waving.svg", img.GetAttribute("src")!, StringComparison.Ordinal);
        Assert.Contains("20260930-test-waving", img.GetAttribute("src")!, StringComparison.Ordinal);
        Assert.Null(img.GetAttribute("srcset"));
        Assert.DoesNotContain("<picture", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Raster_override_renders_picture_with_avif_source()
    {
        using var _ = MascotAssets.OverrideArts(
            new MascotArt(MascotPose.Celebrating, MascotArtFormat.Raster, "20260930-test-cele"));

        var cut = Render<LobsyMascot>(p => p
            .Add(c => c.Pose, MascotPose.Celebrating)
            .Add(c => c.Size, MascotSize.Medium));

        Assert.DoesNotContain("pub-mascot--fb-celebrating", cut.Find("span.pub-mascot").ClassList);
        var source = cut.Find("picture source");
        Assert.Equal("image/avif", source.GetAttribute("type"));
        Assert.Contains("mascot-celebrating-256.avif", source.GetAttribute("srcset")!, StringComparison.Ordinal);

        var img = cut.Find("picture img.pub-mascot__img");
        Assert.Contains("mascot-celebrating-256.webp", img.GetAttribute("src")!, StringComparison.Ordinal);
        Assert.Contains("512w", img.GetAttribute("srcset")!, StringComparison.Ordinal);
    }

    [Fact]
    public void PreloadLink_helper_emits_imagesrcset_for_fallback()
    {
        var markup = LobsyMascot.PreloadLink(MascotPose.Waving, MascotSize.Hero).Value;
        Assert.Contains("rel=\"preload\"", markup, StringComparison.Ordinal);
        Assert.Contains("as=\"image\"", markup, StringComparison.Ordinal);
        Assert.Contains("fetchpriority=\"high\"", markup, StringComparison.Ordinal);
        Assert.Contains("imagesrcset=", markup, StringComparison.Ordinal);
        Assert.Contains("imagesizes=\"256px\"", markup, StringComparison.Ordinal);
        Assert.Contains("mascot-256.webp", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PreloadLink_helper_emits_href_for_svg()
    {
        using var _ = MascotAssets.OverrideArts(
            new MascotArt(MascotPose.Waving, MascotArtFormat.Svg, "20260930-test-waving"));

        var markup = LobsyMascot.PreloadLink(MascotPose.Waving, MascotSize.Hero).Value;
        Assert.Contains("href=\"/images/brand/mascot/mascot-waving.svg?v=20260930-test-waving\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("imagesrcset=", markup, StringComparison.Ordinal);
    }
}
