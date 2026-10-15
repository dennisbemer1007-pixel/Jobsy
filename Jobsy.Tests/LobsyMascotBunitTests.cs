using Bunit;
using Jobsy.Web.Components.Brand;
using Jobsy.Web.Media;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Tests;

public class LobsyMascotBunitTests : BunitContext
{
    [Fact]
    public void Inline_uitleggen_renders_svg_with_pose_class()
    {
        var cut = Render<LobsyMascot>(p => p
            .Add(c => c.Pose, MascotPose.Uitleggen)
            .Add(c => c.Size, MascotSize.Hero));

        var root = cut.Find("span.pub-mascot");
        Assert.Contains("pub-mascot--hero", root.ClassList);
        Assert.Contains("pub-mascot--pose-uitleggen", root.ClassList);
        Assert.Equal("true", root.GetAttribute("aria-hidden"));

        var svg = cut.Find("svg.pub-mascot__svg");
        Assert.Contains("lobsy-claw-", svg.InnerHtml, StringComparison.Ordinal);
        Assert.Contains("#F54A1B", svg.InnerHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("<img", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Avatar_variant_uses_tighter_viewBox()
    {
        var cut = Render<LobsyMascot>(p => p
            .Add(c => c.Variant, LobsyMascotArt.MascotVariant.Avatar)
            .Add(c => c.Pose, MascotPose.Rustig));

        var svg = cut.Find("svg.pub-mascot__svg");
        Assert.Contains(LobsyMascotArt.AvatarViewBox, svg.GetAttribute("viewBox")!, StringComparison.Ordinal);
    }

    [Fact]
    public void Alt_makes_mascot_non_decorative()
    {
        var cut = Render<LobsyMascot>(p => p
            .Add(c => c.Alt, "Lobsy legt uit")
            .Add(c => c.Pose, MascotPose.Uitleggen));

        var root = cut.Find("span.pub-mascot");
        Assert.Null(root.GetAttribute("aria-hidden"));
        Assert.Equal("img", root.GetAttribute("role"));
        Assert.Equal("Lobsy legt uit", root.GetAttribute("aria-label"));
    }

    [Fact]
    public void Mirror_adds_class()
    {
        var cut = Render<LobsyMascot>(p => p
            .Add(c => c.Mirror, true)
            .Add(c => c.Pose, MascotPose.Juichen));

        Assert.Contains("pub-mascot--mirror", cut.Find("span.pub-mascot").ClassList);
    }

    [Fact]
    public void Shell_uses_ghost_class()
    {
        var cut = Render<LobsyMascot>(p => p
            .Add(c => c.Pose, MascotPose.Shell));

        var root = cut.Find("span.pub-mascot");
        Assert.Contains("pub-mascot--ghost", root.ClassList);
        Assert.Contains("pub-mascot__ghost", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Svg_override_switches_markup_to_external_file()
    {
        using var _ = MascotAssets.OverrideArts(
            new MascotArt(MascotPose.Uitleggen, MascotArtFormat.Svg, "20260930-test-uitleggen"));

        var cut = Render<LobsyMascot>(p => p
            .Add(c => c.Pose, MascotPose.Uitleggen)
            .Add(c => c.Size, MascotSize.Hero)
            .Add(c => c.Priority, true));

        var img = cut.Find("img.pub-mascot__img");
        Assert.Contains("mascot-uitleggen.svg", img.GetAttribute("src")!, StringComparison.Ordinal);
        Assert.DoesNotContain("<svg", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PreloadLink_inline_returns_empty()
    {
        var markup = LobsyMascot.PreloadLink(MascotPose.Juichen, MascotSize.Hero).Value;
        Assert.True(string.IsNullOrEmpty(markup));
    }

    [Fact]
    public void PreloadLink_helper_emits_href_for_svg_override()
    {
        using var _ = MascotAssets.OverrideArts(
            new MascotArt(MascotPose.Uitleggen, MascotArtFormat.Svg, "20260930-test-uitleggen"));

        var markup = LobsyMascot.PreloadLink(MascotPose.Uitleggen, MascotSize.Hero).Value;
        Assert.Contains("href=\"/images/brand/mascot/mascot-uitleggen.svg?v=20260930-test-uitleggen\"", markup, StringComparison.Ordinal);
    }
}
