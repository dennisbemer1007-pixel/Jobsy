using System.Text;
using Jobsy.Web.Media;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Components.Brand;

/// <summary>Inline SVG Lobsy mascot — traced from Dennis reference (tools/brand/trace_mascot_reference.py).</summary>
public static class LobsyMascotArt
{
    public const string ViewBox = LobsyMascotTraced.ViewBox;
    public const string AvatarViewBox = LobsyMascotTraced.AvatarViewBox;

    public const string Coral = "#F54A1B";
    public const string CoralDark = "#D93A12";
    public const string Ink = "#0F2D5C";

    /// <summary>Legacy journey shell silhouette (256-space).</summary>
    public const string JourneyBodyPath =
        "M52 58 C48 78 50 98 64 108 C78 98 80 78 76 58 Z";

    public enum MascotVariant
    {
        Full,
        Avatar,
    }

    public static void AppendSvg(
        StringBuilder sb,
        MascotPose pose,
        string uid,
        MascotVariant variant = MascotVariant.Full,
        bool shellGhost = false)
    {
        _ = uid;
        var vb = variant == MascotVariant.Avatar ? AvatarViewBox : ViewBox;
        sb.Append(
            $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="{vb}" class="pub-mascot__svg" focusable="false" aria-hidden="true" overflow="visible" preserveAspectRatio="xMidYMid meet">""");
        if (shellGhost)
        {
            sb.Append("""<g class="pub-mascot__ghost">""");
        }

        sb.Append(LobsyMascotTraced.InnerMarkup(pose));

        if (shellGhost)
        {
            sb.Append("</g>");
        }

        sb.Append("</svg>");
    }

    public static MarkupString RenderMarkup(MascotPose pose, string uid, MascotVariant variant = MascotVariant.Full, bool shellGhost = false)
    {
        var sb = new StringBuilder(4096);
        AppendSvg(sb, pose, uid, variant, shellGhost);
        return (MarkupString)sb.ToString();
    }

    public static void AppendCharacter(StringBuilder sb, MascotPose pose, string uid)
    {
        _ = uid;
        sb.Append(LobsyMascotTraced.InnerMarkup(pose));
    }

    public static string PoseCssSlug(MascotPose pose) => MascotAssets.PoseSlug(pose);
}
